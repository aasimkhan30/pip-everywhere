using System.Diagnostics;
using Microsoft.Win32;
using PiPEverywhere.Models;

namespace PiPEverywhere.Services;

public static class BrowserDetectionService
{
    public static bool IsDetected(BrowserDefinition browser)
    {
        if (browser.ProcessNames.Any(IsProcessRunning))
        {
            return true;
        }

        return ExecutableNames(browser)
            .Any(executable => IsRegistered(executable) || KnownPaths(executable).Any(File.Exists));
    }

    private static bool IsProcessRunning(string processName)
    {
        var processes = Process.GetProcessesByName(processName);
        try
        {
            return processes.Length > 0;
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
    }

    private static IEnumerable<string> ExecutableNames(BrowserDefinition browser) =>
        browser.ProcessNames.Select(processName => $"{processName}.exe");

    private static bool IsRegistered(string executableName)
    {
        const string appPaths = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths";
        foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
        {
            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                    using var key = baseKey.OpenSubKey($@"{appPaths}\{executableName}");
                    if (key?.GetValue(null) is string path && File.Exists(path))
                    {
                        return true;
                    }
                }
                catch
                {
                    // Registry access is best-effort; known install paths are checked next.
                }
            }
        }

        return false;
    }

    private static IEnumerable<string> KnownPaths(string executableName)
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        return executableName.ToLowerInvariant() switch
        {
            "msedge.exe" =>
            [
                Path.Combine(programFiles, "Microsoft", "Edge", "Application", executableName),
                Path.Combine(programFilesX86, "Microsoft", "Edge", "Application", executableName),
            ],
            "chrome.exe" =>
            [
                Path.Combine(programFiles, "Google", "Chrome", "Application", executableName),
                Path.Combine(programFilesX86, "Google", "Chrome", "Application", executableName),
                Path.Combine(localAppData, "Google", "Chrome", "Application", executableName),
            ],
            "firefox.exe" =>
            [
                Path.Combine(programFiles, "Mozilla Firefox", executableName),
                Path.Combine(programFilesX86, "Mozilla Firefox", executableName),
            ],
            "brave.exe" =>
            [
                Path.Combine(programFiles, "BraveSoftware", "Brave-Browser", "Application", executableName),
                Path.Combine(programFilesX86, "BraveSoftware", "Brave-Browser", "Application", executableName),
                Path.Combine(localAppData, "BraveSoftware", "Brave-Browser", "Application", executableName),
            ],
            "opera.exe" =>
            [
                Path.Combine(localAppData, "Programs", "Opera", executableName),
                Path.Combine(localAppData, "Programs", "Opera GX", executableName),
            ],
            "vivaldi.exe" =>
            [
                Path.Combine(programFiles, "Vivaldi", "Application", executableName),
                Path.Combine(localAppData, "Vivaldi", "Application", executableName),
            ],
            _ => [],
        };
    }
}
