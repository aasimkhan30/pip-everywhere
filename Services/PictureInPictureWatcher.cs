using PiPEverywhere.Models;

namespace PiPEverywhere.Services;

public sealed class PictureInPictureWatcher : IDisposable
{
    private readonly VirtualDesktopPinService _pinService;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Dictionary<nint, TrackedWindow> _trackedWindows = [];
    private readonly object _selectionLock = new();
    private HashSet<string> _selectedBrowserIds;
    private Task? _watchTask;

    public PictureInPictureWatcher(
        VirtualDesktopPinService pinService,
        IEnumerable<string> selectedBrowserIds)
    {
        _pinService = pinService;
        _selectedBrowserIds = selectedBrowserIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public event EventHandler<PictureInPicturePinnedEventArgs>? PictureInPicturePinned;

    public void Start()
    {
        _watchTask ??= Task.Run(WatchAsync);
    }

    public void SetSelectedBrowsers(IEnumerable<string> selectedBrowserIds)
    {
        lock (_selectionLock)
        {
            _selectedBrowserIds = selectedBrowserIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
    }

    private async Task WatchAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(750));

        try
        {
            do
            {
                await ScanAsync(_cancellation.Token);
            }
            while (await timer.WaitForNextTickAsync(_cancellation.Token));
        }
        catch (OperationCanceledException)
        {
            // Expected during application shutdown.
        }
    }

    private async Task ScanAsync(CancellationToken cancellationToken)
    {
        HashSet<string> selected;
        lock (_selectionLock)
        {
            selected = new HashSet<string>(_selectedBrowserIds, StringComparer.OrdinalIgnoreCase);
        }

        foreach (var tracked in _trackedWindows.ToArray())
        {
            if (selected.Contains(tracked.Value.Browser.Id))
            {
                continue;
            }

            if (tracked.Value.PinnedByThisApp)
            {
                _ = await _pinService.UnpinAsync(tracked.Key, cancellationToken);
            }

            _trackedWindows.Remove(tracked.Key);
        }

        var candidates = NativeWindowService.EnumerateVisibleWindows();
        var currentHandles = candidates.Select(candidate => candidate.Handle).ToHashSet();

        foreach (var handle in _trackedWindows.Keys.Where(handle => !currentHandles.Contains(handle)).ToArray())
        {
            _trackedWindows.Remove(handle);
        }

        foreach (var candidate in candidates)
        {
            var browser = BrowserCatalog.Match(
                candidate.ProcessName,
                candidate.ClassName,
                candidate.Title);

            if (browser is null ||
                !selected.Contains(browser.Id) ||
                _trackedWindows.ContainsKey(candidate.Handle))
            {
                continue;
            }

            var wasAlreadyPinned = await _pinService.IsPinnedAsync(candidate.Handle, cancellationToken);
            var pinned = wasAlreadyPinned ||
                await _pinService.PinAsync(candidate.Handle, cancellationToken);

            if (!pinned)
            {
                continue;
            }

            _trackedWindows[candidate.Handle] = new TrackedWindow(browser, !wasAlreadyPinned);
            PictureInPicturePinned?.Invoke(
                this,
                new PictureInPicturePinnedEventArgs(browser.Id, browser.DisplayName, candidate.Handle));
        }
    }

    public void Dispose()
    {
        _cancellation.Cancel();
        _cancellation.Dispose();
    }

    private sealed record TrackedWindow(BrowserDefinition Browser, bool PinnedByThisApp);
}

public sealed record PictureInPicturePinnedEventArgs(
    string BrowserId,
    string BrowserName,
    nint WindowHandle);
