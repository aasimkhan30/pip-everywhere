namespace PiPEverywhere.Models;

public sealed class AppSettings
{
    public bool IsEnabled { get; set; } = true;

    public HashSet<string> SelectedBrowserIds { get; set; } =
        new(StringComparer.OrdinalIgnoreCase) { "edge" };
}
