namespace PiPEverywhere.Models;

public sealed record BrowserDefinition(
    string Id,
    string DisplayName,
    IReadOnlySet<string> ProcessNames,
    IReadOnlySet<string> WindowClasses);
