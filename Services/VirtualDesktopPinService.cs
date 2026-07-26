using System.Diagnostics;

namespace PiPEverywhere.Services;

public sealed class VirtualDesktopPinService
{
    private readonly string _toolPath = Path.Combine(
        AppContext.BaseDirectory,
        "ThirdParty",
        "VirtualDesktop",
        "VirtualDesktop11-24H2.exe");

    public Task<bool> IsPinnedAsync(nint window, CancellationToken cancellationToken) =>
        RunAsync("IsWindowHandlePinned", window, cancellationToken);

    public Task<bool> PinAsync(nint window, CancellationToken cancellationToken) =>
        RunAsync("PinWindowHandle", window, cancellationToken);

    public Task<bool> UnpinAsync(nint window, CancellationToken cancellationToken) =>
        RunAsync("UnPinWindowHandle", window, cancellationToken);

    private async Task<bool> RunAsync(string command, nint window, CancellationToken cancellationToken)
    {
        if (!File.Exists(_toolPath))
        {
            return false;
        }

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = _toolPath,
            Arguments = $"/Quiet /{command}:{window}",
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden,
        });

        if (process is null)
        {
            return false;
        }

        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode == 0;
    }
}
