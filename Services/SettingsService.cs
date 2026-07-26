using System.Text.Json;
using System.Text.Json.Serialization;
using PiPEverywhere.Models;

namespace PiPEverywhere.Services;

public sealed class SettingsService
{
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly string _settingsPath;

    public SettingsService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PiPEverywhere");
        Directory.CreateDirectory(folder);
        _settingsPath = Path.Combine(folder, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                return JsonSerializer.Deserialize(
                    File.ReadAllText(_settingsPath),
                    AppSettingsJsonContext.Default.AppSettings)
                    ?? new AppSettings();
            }
        }
        catch
        {
            // A malformed settings file should not prevent the utility from starting.
        }

        return new AppSettings();
    }

    public async Task SaveAsync(AppSettings settings)
    {
        await _writeLock.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(
                settings,
                AppSettingsJsonContext.Default.AppSettings);
            await File.WriteAllTextAsync(_settingsPath, json);
        }
        finally
        {
            _writeLock.Release();
        }
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppSettings))]
internal sealed partial class AppSettingsJsonContext : JsonSerializerContext;
