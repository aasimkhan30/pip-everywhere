using System.Drawing;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
        UnhandledException += OnUnhandledException;
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
            Settings.SelectedBrowserIds,
            Settings.IsEnabled);
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

        var activation = AppInstance.GetCurrent().GetActivatedEventArgs();
        var isBackgroundLaunch =
            activation.Kind == ExtendedActivationKind.StartupTask ||
            Environment.GetCommandLineArgs().Contains("--background", StringComparer.OrdinalIgnoreCase);

        try
        {
            _window = new MainWindow();
            _window.Activate();

            if (isBackgroundLaunch)
            {
                _window.Hide();
            }

            CreateTrayIcon();
            Watcher.Start();
        }
        catch (Exception exception)
        {
            LogStartupException("OnLaunched", exception);
            throw;
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

    public void UpdateTrayStatus(bool isEnabled)
    {
        if (_trayIcon is not null)
        {
            _trayIcon.ToolTipText = isEnabled
                ? "PiP Everywhere — running"
                : "PiP Everywhere — off";
        }
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
        var contextMenu = new MenuFlyout
        {
            AreOpenCloseAnimationsEnabled = false,
        };
        var quitMenuItem = new MenuFlyoutItem
        {
            Width = 100,
            Text = "Quit",
        };
        quitMenuItem.Click += (_, _) => Quit();
        contextMenu.Items.Add(quitMenuItem);

        _traySystemIcon = new Icon(iconPath);
        _trayIcon = new TaskbarIcon
        {
            ContextMenuMode = ContextMenuMode.SecondWindow,
            ContextFlyout = contextMenu,
            Icon = _traySystemIcon,
            ToolTipText = Settings.IsEnabled
                ? "PiP Everywhere — running"
                : "PiP Everywhere — off",
            LeftClickCommand = new ActionCommand(ShowMainWindow),
            NoLeftClickDelay = true,
            Visibility = Visibility.Visible,
        };
        _trayIcon.ForceCreate();
    }

    private static void OnUnhandledException(
        object sender,
        Microsoft.UI.Xaml.UnhandledExceptionEventArgs args)
    {
        LogStartupException("UnhandledException", args.Exception);
    }

    private static void LogStartupException(string stage, Exception exception)
    {
        try
        {
            var logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PiPEverywhere");
            Directory.CreateDirectory(logDirectory);
            File.AppendAllText(
                Path.Combine(logDirectory, "startup-error.log"),
                $"[{DateTimeOffset.Now:O}] {stage}{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // Logging must never replace the original startup exception.
        }
    }
}
