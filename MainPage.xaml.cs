using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using PiPEverywhere.Services;

namespace PiPEverywhere;

public sealed partial class MainPage : Page
{
    private bool _isLoading = true;
    private bool _startupCanChange;

    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var selected = App.Instance.Settings.SelectedBrowserIds;
        foreach (var checkBox in BrowserCheckBoxes())
        {
            checkBox.IsChecked = selected.Contains((string)checkBox.Tag);
        }

        var startupState = await App.Instance.StartupService.GetStateAsync();
        _startupCanChange = startupState.CanChange;
        StartupToggle.IsOn = startupState.IsEnabled;
        StartupDescription.Text = startupState.Message;

        EnabledToggle.IsOn = App.Instance.Settings.IsEnabled;
        RefreshBrowserSummary();
        RefreshBrowserDetection();
        ApplyBrowserFilter();
        UpdateEnabledBanner(App.Instance.Settings.IsEnabled);

        _isLoading = false;
    }

    private async void OnBrowserSelectionChanged(object sender, RoutedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        var selected = BrowserCheckBoxes()
            .Where(checkBox => checkBox.IsChecked is true)
            .Select(checkBox => (string)checkBox.Tag)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        App.Instance.Settings.SelectedBrowserIds = selected;
        await App.Instance.SettingsService.SaveAsync(App.Instance.Settings);
        App.Instance.Watcher.SetSelectedBrowsers(selected);
        RefreshBrowserSummary();
        UpdateEnabledBanner(App.Instance.Settings.IsEnabled);
    }

    private async void OnEnabledToggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        var isEnabled = EnabledToggle.IsOn;
        App.Instance.Settings.IsEnabled = isEnabled;
        App.Instance.Watcher.SetEnabled(isEnabled);
        App.Instance.UpdateTrayStatus(isEnabled);
        UpdateEnabledBanner(isEnabled);
        await App.Instance.SettingsService.SaveAsync(App.Instance.Settings);
    }

    private async void OnStartupToggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        StartupToggle.IsEnabled = false;
        var result = await App.Instance.StartupService.SetEnabledAsync(StartupToggle.IsOn);
        _startupCanChange = result.CanChange;
        StartupToggle.IsOn = result.IsEnabled;
        StartupToggle.IsEnabled = App.Instance.Settings.IsEnabled && result.CanChange;
        StartupDescription.Text = result.Message;
    }

    private void OnBrowserFilterChanged(object sender, TextChangedEventArgs e)
    {
        ApplyBrowserFilter();
    }

    private void UpdateEnabledBanner(bool isEnabled)
    {
        if (isEnabled)
        {
            StatusIcon.Glyph = "\uE73E";
            StatusIcon.Foreground = new SolidColorBrush(
                Windows.UI.Color.FromArgb(255, 18, 41, 26));
            StatusIconCircle.Background = ResourceBrush("SuccessBrush");
            StatusCard.Background = ResourceBrush("StatusBackgroundBrush");
            StatusCard.BorderBrush = ResourceBrush("StatusBorderBrush");
            StatusTitle.Text = "PiP Everywhere is running";
            StatusSubtitle.Foreground = ResourceBrush("StatusEnabledTextBrush");
            StatusSubtitle.Text = WatchingSummary();
            StartupCard.Opacity = 1;
            StartupToggle.IsEnabled = _startupCanChange;
            return;
        }

        StatusIcon.Glyph = "\uE73E";
        StatusIcon.Foreground = ResourceBrush("MutedTextBrush");
        StatusIconCircle.Background = ResourceBrush("DimTextBrush");
        StatusCard.Background = ResourceBrush("PanelBrush");
        StatusCard.BorderBrush = ResourceBrush("PanelBorderBrush");
        StatusTitle.Text = "PiP Everywhere is off";
        StatusSubtitle.Foreground = ResourceBrush("MutedTextBrush");
        StatusSubtitle.Text = "Turn on to keep video floating across desktops";
        StartupCard.Opacity = 0.55;
        StartupToggle.IsEnabled = false;
    }

    private void RefreshBrowserSummary()
    {
        var count = BrowserCheckBoxes().Count(checkBox => checkBox.IsChecked is true);
        SelectedCountText.Text = $"{count} selected";
    }

    private string WatchingSummary()
    {
        var count = BrowserCheckBoxes().Count(checkBox => checkBox.IsChecked is true);
        return $"Watching {count} browser{(count == 1 ? string.Empty : "s")} for video";
    }

    private void RefreshBrowserDetection()
    {
        foreach (var entry in BrowserEntries())
        {
            entry.Detection.Visibility = BrowserDetectionService.IsDetected(entry.Browser)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }

    private void ApplyBrowserFilter()
    {
        if (BrowserFilter is null)
        {
            return;
        }

        var filter = BrowserFilter.Text.Trim();
        var entries = BrowserEntries().ToArray();
        foreach (var entry in entries)
        {
            entry.Row.Visibility =
                entry.Browser.DisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        var visibleEntries = entries
            .Where(entry => entry.Row.Visibility == Visibility.Visible)
            .ToArray();

        foreach (var entry in entries)
        {
            entry.Separator.Visibility = Visibility.Collapsed;
        }

        foreach (var entry in visibleEntries.SkipLast(1))
        {
            entry.Separator.Visibility = Visibility.Visible;
        }

        NoBrowsersMessage.Text = $"No browsers match \"{filter}\"";
        NoBrowsersMessage.Visibility =
            visibleEntries.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private SolidColorBrush ResourceBrush(string key) =>
        (SolidColorBrush)Resources[key];

    private IEnumerable<BrowserEntry> BrowserEntries()
    {
        yield return new BrowserEntry(
            BrowserCatalog.All[0], EdgeRow, EdgeCheckBox, EdgeDetection, EdgeSeparator);
        yield return new BrowserEntry(
            BrowserCatalog.All[1], ChromeRow, ChromeCheckBox, ChromeDetection, ChromeSeparator);
        yield return new BrowserEntry(
            BrowserCatalog.All[2], FirefoxRow, FirefoxCheckBox, FirefoxDetection, FirefoxSeparator);
        yield return new BrowserEntry(
            BrowserCatalog.All[3], BraveRow, BraveCheckBox, BraveDetection, BraveSeparator);
        yield return new BrowserEntry(
            BrowserCatalog.All[4], OperaRow, OperaCheckBox, OperaDetection, OperaSeparator);
        yield return new BrowserEntry(
            BrowserCatalog.All[5], VivaldiRow, VivaldiCheckBox, VivaldiDetection, OperaSeparator);
    }

    private void OnHideToTrayClicked(object sender, RoutedEventArgs e)
    {
        App.Instance.HideMainWindow();
    }

    private void OnQuitClicked(object sender, RoutedEventArgs e)
    {
        App.Instance.Quit();
    }

    private IEnumerable<CheckBox> BrowserCheckBoxes()
    {
        yield return EdgeCheckBox;
        yield return ChromeCheckBox;
        yield return FirefoxCheckBox;
        yield return BraveCheckBox;
        yield return OperaCheckBox;
        yield return VivaldiCheckBox;
    }

    private sealed record BrowserEntry(
        Models.BrowserDefinition Browser,
        Grid Row,
        CheckBox CheckBox,
        TextBlock Detection,
        Rectangle Separator);
}
