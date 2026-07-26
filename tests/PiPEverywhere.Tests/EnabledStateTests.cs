using PiPEverywhere.Models;
using PiPEverywhere.Services;

namespace PiPEverywhere.Tests;

public sealed class EnabledStateTests
{
    [Fact]
    public void Settings_DefaultToEnabled()
    {
        var settings = new AppSettings();

        Assert.True(settings.IsEnabled);
    }

    [Fact]
    public void Watcher_CanBeDisabledAndReenabledWithoutChangingBrowserSelection()
    {
        var selectedBrowsers = new[] { "edge", "firefox" };
        using var watcher = new PictureInPictureWatcher(
            new VirtualDesktopPinService(),
            selectedBrowsers);

        watcher.SetEnabled(false);
        Assert.False(watcher.IsEnabled);

        watcher.SetEnabled(true);
        Assert.True(watcher.IsEnabled);
    }
}
