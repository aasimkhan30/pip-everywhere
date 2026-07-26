namespace PiPEverywhere.Models;

public sealed class AppSettings
{
    public HashSet<string> SelectedBrowserIds { get; set; } =
        new(StringComparer.OrdinalIgnoreCase) { "edge" };
}
