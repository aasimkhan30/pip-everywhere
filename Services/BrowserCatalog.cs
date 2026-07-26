using PiPEverywhere.Models;

namespace PiPEverywhere.Services;

public static class BrowserCatalog
{
    private static readonly IReadOnlySet<string> ChromiumClasses =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Chrome_WidgetWin_1" };

    public static IReadOnlyList<BrowserDefinition> All { get; } =
    [
        new("edge", "Microsoft Edge", Set("msedge"), ChromiumClasses),
        new("chrome", "Google Chrome", Set("chrome"), ChromiumClasses),
        new("firefox", "Mozilla Firefox", Set("firefox"), Set("MozillaDialogClass", "MozillaWindowClass")),
        new("brave", "Brave", Set("brave"), ChromiumClasses),
        new("opera", "Opera", Set("opera"), ChromiumClasses),
        new("vivaldi", "Vivaldi", Set("vivaldi"), ChromiumClasses),
    ];

    public static BrowserDefinition? Match(string processName, string windowClass, string title)
    {
        if (!IsPictureInPictureTitle(title))
        {
            return null;
        }

        return All.FirstOrDefault(browser =>
            browser.ProcessNames.Contains(processName) &&
            browser.WindowClasses.Contains(windowClass));
    }

    private static bool IsPictureInPictureTitle(string title)
    {
        var normalized = title
            .Replace('-', ' ')
            .Trim();

        return normalized.Equals("Picture in picture", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlySet<string> Set(params string[] values) =>
        new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
}
