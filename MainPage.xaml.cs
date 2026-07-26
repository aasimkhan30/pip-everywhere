using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PiPEverywhere.Services;

namespace PiPEverywhere;

public sealed partial class MainPage : Page
{
    private bool _isLoading = true;
    private int _pinnedCount;

    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var selected = App.Instance.Settings.SelectedBrowserIds;
        foreach (var checkBox in BrowserCheckBoxes())
        {
            checkBox.IsChecked = selected.Contains((string)checkBox.Tag);
        }

        var startupState = await App.Instance.StartupService.GetStateAsync();
        StartupToggle.IsOn = startupState.IsEnabled;
        StartupToggle.IsEnabled = startupState.CanChange;
        StartupDescription.Text = startupState.Message;

        App.Instance.Watcher.PictureInPicturePinned += OnPictureInPicturePinned;
        Unloaded += OnUnloaded;
        _isLoading = false;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        App.Instance.Watcher.PictureInPicturePinned -= OnPictureInPicturePinned;
    }

    private async void OnBrowserSelectionChanged(object sender, RoutedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        var selected = BrowserCheckBoxes()
            .Where(checkBox => checkBox.IsChecked is true)
            .Select(checkBox => (string)checkBox.Tag)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        App.Instance.Settings.SelectedBrowserIds = selected;
        await App.Instance.SettingsService.SaveAsync(App.Instance.Settings);
        App.Instance.Watcher.SetSelectedBrowsers(selected);
    }

    private async void OnStartupToggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        StartupToggle.IsEnabled = false;
        var result = await App.Instance.StartupService.SetEnabledAsync(StartupToggle.IsOn);
        StartupToggle.IsOn = result.IsEnabled;
        StartupToggle.IsEnabled = result.CanChange;
        StartupDescription.Text = result.Message;
    }

    private void OnPictureInPicturePinned(object? sender, PictureInPicturePinnedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            _pinnedCount++;
            StatusDetail.Text = _pinnedCount == 1
                ? $"Pinned {e.BrowserName} PiP to every desktop"
                : $"Pinned {_pinnedCount} PiP windows in this session";
        });
    }

    private void OnHideToTrayClicked(object sender, RoutedEventArgs e)
    {
        App.Instance.HideMainWindow();
    }

    private void OnQuitClicked(object sender, RoutedEventArgs e)
    {
        App.Instance.Quit();
    }

    private IEnumerable<CheckBox> BrowserCheckBoxes()
    {
        yield return EdgeCheckBox;
        yield return ChromeCheckBox;
        yield return FirefoxCheckBox;
        yield return BraveCheckBox;
        yield return OperaCheckBox;
        yield return VivaldiCheckBox;
    }
}
