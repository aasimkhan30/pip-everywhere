using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace PiPEverywhere.Services;

public static class NativeWindowService
{
    private delegate bool EnumWindowsProc(nint window, nint parameter);

    public static IReadOnlyList<WindowCandidate> EnumerateVisibleWindows()
    {
        var windows = new List<WindowCandidate>();

        EnumWindows((window, parameter) =>
        {
            if (!IsWindowVisible(window))
            {
                return true;
            }

            var titleLength = GetWindowTextLength(window);
            if (titleLength == 0)
            {
                return true;
            }

            var title = new StringBuilder(titleLength + 1);
            _ = GetWindowText(window, title, title.Capacity);

            var className = new StringBuilder(256);
            _ = GetClassName(window, className, className.Capacity);

            GetWindowThreadProcessId(window, out var processId);
            try
            {
                using var process = Process.GetProcessById((int)processId);
                windows.Add(new WindowCandidate(
                    window,
                    process.ProcessName,
                    className.ToString(),
                    title.ToString()));
            }
            catch (ArgumentException)
            {
                // The window's process exited during enumeration.
            }

            return true;
        }, 0);

        return windows;
    }

    public static void ShowExistingInstance()
    {
        var window = FindWindow(null, "PiP Everywhere");
        if (window == 0)
        {
            return;
        }

        _ = ShowWindow(window, 9);
        _ = SetForegroundWindow(window);
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc callback, nint parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint window);

    [DllImport("user32.dll", EntryPoint = "GetWindowTextLengthW", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(nint window);

    [DllImport("user32.dll", EntryPoint = "GetWindowTextW", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint window, StringBuilder text, int maximumCount);

    [DllImport("user32.dll", EntryPoint = "GetClassNameW", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(nint window, StringBuilder className, int maximumCount);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint window, out uint processId);

    [DllImport("user32.dll", EntryPoint = "FindWindowW", CharSet = CharSet.Unicode)]
    private static extern nint FindWindow(string? className, string windowName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(nint window, int command);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint window);
}

public sealed record WindowCandidate(
    nint Handle,
    string ProcessName,
    string ClassName,
    string Title);
