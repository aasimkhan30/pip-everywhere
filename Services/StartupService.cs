using Microsoft.Win32;
using Windows.ApplicationModel;

namespace PiPEverywhere.Services;

public sealed class StartupService
{
    private const string StartupTaskId = "PiPEverywhereStartup";
    private const string RegistryValueName = "PiPEverywhere";

    public async Task<StartupStateInfo> GetStateAsync()
    {
        try
        {
            var task = await StartupTask.GetAsync(StartupTaskId);
            return FromPackagedState(task.State);
        }
        catch
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run");
            var enabled = key?.GetValue(RegistryValueName) is string;
            return new StartupStateInfo(
                enabled,
                true,
                enabled
                    ? "Starts quietly in the notification area."
                    : "Start the watcher automatically after you sign in.");
        }
    }

    public async Task<StartupStateInfo> SetEnabledAsync(bool enabled)
    {
        try
        {
            var task = await StartupTask.GetAsync(StartupTaskId);
            if (enabled)
            {
                if (task.State is StartupTaskState.Disabled)
                {
                    _ = await task.RequestEnableAsync();
                }
            }
            else if (task.State is StartupTaskState.Enabled)
            {
                task.Disable();
            }

            return FromPackagedState(task.State);
        }
        catch
        {
            using var key = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run");

            if (enabled)
            {
                var executable = Environment.ProcessPath
                    ?? throw new InvalidOperationException("Unable to locate the application executable.");
                key.SetValue(RegistryValueName, $"\"{executable}\" --background");
            }
            else
            {
                key.DeleteValue(RegistryValueName, false);
            }

            return await GetStateAsync();
        }
    }

    private static StartupStateInfo FromPackagedState(StartupTaskState state) =>
        state switch
        {
            StartupTaskState.Enabled => new(
                true,
                true,
                "Starts quietly in the notification area."),
            StartupTaskState.Disabled => new(
                false,
                true,
                "Start the watcher automatically after you sign in."),
            StartupTaskState.DisabledByUser => new(
                false,
                false,
                "Disabled in Windows Startup Apps. Re-enable it there to use this option."),
            StartupTaskState.DisabledByPolicy => new(
                false,
                false,
                "Startup is disabled by an administrator policy."),
            _ => new(false, true, "Start the watcher automatically after you sign in."),
        };
}

public sealed record StartupStateInfo(bool IsEnabled, bool CanChange, string Message);
