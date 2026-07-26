using System.Drawing;
using H.NotifyIcon;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using PiPEverywhere.Models;
using PiPEverywhere.Services;
using PiPEverywhere.Utilities;

namespace PiPEverywhere;

public partial class App : Application
{
    private readonly Mutex _singleInstanceMutex;
    private readonly bool _ownsSingleInstance;
    private MainWindow? _window;
    private TaskbarIcon? _trayIcon;
    private Icon? _traySystemIcon;

    public App()
    {
        InitializeComponent();

        _singleInstanceMutex = new Mutex(
            true,
            @"Local\PiPEverywhere-6D92293C-A535-41A5-A84A-1C42D8CA34CF",
            out _ownsSingleInstance);

        SettingsService = new SettingsService();
        Settings = SettingsService.Load();
        StartupService = new StartupService();
        Watcher = new PictureInPictureWatcher(
            new VirtualDesktopPinService(),
            Settings.SelectedBrowserIds);
    }

    public static App Instance => (App)Current;

    public AppSettings Settings { get; }

    public SettingsService SettingsService { get; }

    public StartupService StartupService { get; }

    public PictureInPictureWatcher Watcher { get; }

    public bool IsExiting { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        if (!_ownsSingleInstance)
        {
            NativeWindowService.ShowExistingInstance();
            Exit();
            return;
        }

        _window = new MainWindow();
        CreateTrayIcon();
        Watcher.Start();

        var activation = AppInstance.GetCurrent().GetActivatedEventArgs();
        var isBackgroundLaunch =
            activation.Kind == ExtendedActivationKind.StartupTask ||
            Environment.GetCommandLineArgs().Contains("--background", StringComparer.OrdinalIgnoreCase);

        if (!isBackgroundLaunch)
        {
            _window.Activate();
        }
    }

    public void ShowMainWindow()
    {
        if (_window is null)
        {
            return;
        }

        _window.Show();
        _window.Activate();
    }

    public void HideMainWindow()
    {
        _window?.Hide();
    }

    public void Quit()
    {
        if (IsExiting)
        {
            return;
        }

        IsExiting = true;
        Watcher.Dispose();
        _trayIcon?.Dispose();
        _traySystemIcon?.Dispose();
        _window?.Close();

        if (_ownsSingleInstance)
        {
            _singleInstanceMutex.ReleaseMutex();
        }

        _singleInstanceMutex.Dispose();
        Exit();
    }

    private void CreateTrayIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
        _traySystemIcon = new Icon(iconPath);
        _trayIcon = new TaskbarIcon
        {
            Icon = _traySystemIcon,
            ToolTipText = "PiP Everywhere — watching for picture-in-picture windows",
            LeftClickCommand = new ActionCommand(ShowMainWindow),
            NoLeftClickDelay = true,
        };
        _trayIcon.ForceCreate();
    }
}
