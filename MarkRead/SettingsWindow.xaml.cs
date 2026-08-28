using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MarkRead.Models;
using MarkRead.Services;

namespace MarkRead;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly Action<AppSettings>? _onSettingsChanged;
    private bool _isInitializing = true;

    public SettingsWindow(AppSettings settings, Action<AppSettings>? onSettingsChanged = null)
    {
        _settings = settings;
        _onSettingsChanged = onSettingsChanged;

        InitializeComponent();
        ApplyCurrentSettingsToUI();
        _isInitializing = false;
    }

    private void ApplyCurrentSettingsToUI()
    {
        // Theme
        switch (_settings.Theme.ToLowerInvariant())
        {
            case "light":
                RadioThemeLight.IsChecked = true;
                break;
            case "system":
                RadioThemeSystem.IsChecked = true;
                break;
            default:
                RadioThemeDark.IsChecked = true;
                break;
        }

        // Font size
        switch (_settings.FontSize)
        {
            case 14:
                RadioFont14.IsChecked = true;
                break;
            case 18:
                RadioFont18.IsChecked = true;
                break;
            case 20:
                RadioFont20.IsChecked = true;
                break;
            default:
                RadioFont16.IsChecked = true;
                break;
        }

        // Reading width
        switch (_settings.ReadingWidth.ToLowerInvariant())
        {
            case "720px":
                RadioWidth720.IsChecked = true;
                break;
            case "1080px":
                RadioWidth1080.IsChecked = true;
                break;
            case "100%":
                RadioWidthFull.IsChecked = true;
                break;
            default:
                RadioWidth860.IsChecked = true;
                break;
        }

        // Default view
        if (_settings.DefaultView.Equals("raw", StringComparison.OrdinalIgnoreCase))
        {
            RadioViewRaw.IsChecked = true;
        }
        else
        {
            RadioViewRendered.IsChecked = true;
        }

        // PDF Orientation
        if (_settings.PdfOrientation.Equals("landscape", StringComparison.OrdinalIgnoreCase))
        {
            RadioPdfLandscape.IsChecked = true;
        }
        else
        {
            RadioPdfPortrait.IsChecked = true;
        }

        // PDF Margins
        switch (_settings.PdfMargins.ToLowerInvariant())
        {
            case "compact":
                RadioMarginCompact.IsChecked = true;
                break;
            case "spacious":
                RadioMarginSpacious.IsChecked = true;
                break;
            default:
                RadioMarginStandard.IsChecked = true;
                break;
        }

        UpdateIntegrationStatusUI();
    }

    // Sidebar navigation
    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;

        string? tag = btn.Tag as string;
        PanelAppearance.Visibility = tag == "Appearance" ? Visibility.Visible : Visibility.Collapsed;
        PanelIntegration.Visibility = tag == "Integration" ? Visibility.Visible : Visibility.Collapsed;
        PanelPdf.Visibility = tag == "Pdf" ? Visibility.Visible : Visibility.Collapsed;
        PanelAbout.Visibility = tag == "About" ? Visibility.Visible : Visibility.Collapsed;

        if (tag == "Integration")
        {
            UpdateIntegrationStatusUI();
        }

        // Reset nav button styles
        var defaultStyle = (Style)FindResource("NavButtonStyle");
        var activeStyle = (Style)FindResource("ActiveNavButtonStyle");

        NavAppearance.Style = tag == "Appearance" ? activeStyle : defaultStyle;
        NavIntegration.Style = tag == "Integration" ? activeStyle : defaultStyle;
        NavPdf.Style = tag == "Pdf" ? activeStyle : defaultStyle;
        NavAbout.Style = tag == "About" ? activeStyle : defaultStyle;
    }

    // Appearance handlers
    private void ThemeOption_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        if (RadioThemeDark.IsChecked == true) _settings.Theme = "dark";
        else if (RadioThemeLight.IsChecked == true) _settings.Theme = "light";
        else if (RadioThemeSystem.IsChecked == true) _settings.Theme = "system";

        SaveAndNotify();
    }

    private void FontOption_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        if (RadioFont14.IsChecked == true) _settings.FontSize = 14;
        else if (RadioFont16.IsChecked == true) _settings.FontSize = 16;
        else if (RadioFont18.IsChecked == true) _settings.FontSize = 18;
        else if (RadioFont20.IsChecked == true) _settings.FontSize = 20;

        SaveAndNotify();
    }

    private void WidthOption_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        if (RadioWidth720.IsChecked == true) _settings.ReadingWidth = "720px";
        else if (RadioWidth860.IsChecked == true) _settings.ReadingWidth = "860px";
        else if (RadioWidth1080.IsChecked == true) _settings.ReadingWidth = "1080px";
        else if (RadioWidthFull.IsChecked == true) _settings.ReadingWidth = "100%";

        SaveAndNotify();
    }

    private void DefaultViewOption_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        _settings.DefaultView = RadioViewRaw.IsChecked == true ? "raw" : "rendered";
        SaveAndNotify();
    }

    // PDF options
    private void PdfOrientation_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        _settings.PdfOrientation = RadioPdfLandscape.IsChecked == true ? "landscape" : "portrait";
        SaveAndNotify();
    }

    private void PdfMargin_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        if (RadioMarginCompact.IsChecked == true) _settings.PdfMargins = "compact";
        else if (RadioMarginSpacious.IsChecked == true) _settings.PdfMargins = "spacious";
        else _settings.PdfMargins = "standard";

        SaveAndNotify();
    }

    private void UpdateIntegrationStatusUI()
    {
        bool fileRegistered = ShellIntegration.IsFileContextMenuRegistered();
        TxtFileMenuStatus.Text = fileRegistered ? "✓ Enabled" : "Not added";
        TxtFileMenuStatus.Foreground = fileRegistered 
            ? new SolidColorBrush(Color.FromRgb(52, 211, 153)) 
            : new SolidColorBrush(Color.FromRgb(161, 161, 170));
        BtnFileMenu.Content = fileRegistered ? "Remove" : "Add to Menu";

        bool dirRegistered = ShellIntegration.IsDirectoryContextMenuRegistered();
        TxtDirMenuStatus.Text = dirRegistered ? "✓ Enabled" : "Not added";
        TxtDirMenuStatus.Foreground = dirRegistered 
            ? new SolidColorBrush(Color.FromRgb(52, 211, 153)) 
            : new SolidColorBrush(Color.FromRgb(161, 161, 170));
        BtnDirMenu.Content = dirRegistered ? "Remove" : "Add to Menu";

        bool win11Direct = ShellIntegration.IsClassicContextMenuEnabled();
        TxtWin11MenuStatus.Text = win11Direct ? "✓ Direct Menu Active" : "Behind 'Show more options'";
        TxtWin11MenuStatus.Foreground = win11Direct 
            ? new SolidColorBrush(Color.FromRgb(52, 211, 153)) 
            : new SolidColorBrush(Color.FromRgb(161, 161, 170));
        BtnToggleWin11Menu.Content = win11Direct ? "Restore Win 11 Menu" : "Show Directly";
    }

    // Windows Shell Integration
    private void BtnToggleFileContextMenu_Click(object sender, RoutedEventArgs e)
    {
        bool isCurrentlyRegistered = ShellIntegration.IsFileContextMenuRegistered();
        ShellIntegration.SetFileContextMenu(!isCurrentlyRegistered);
        UpdateIntegrationStatusUI();

        ShowStatus(!isCurrentlyRegistered 
            ? "✓ Successfully added 'Open with MarkRead' to file context menu." 
            : "✓ Removed 'Open with MarkRead' from file context menu.", true);
    }

    private void BtnToggleDirectoryContextMenu_Click(object sender, RoutedEventArgs e)
    {
        bool isCurrentlyRegistered = ShellIntegration.IsDirectoryContextMenuRegistered();
        ShellIntegration.SetDirectoryContextMenu(!isCurrentlyRegistered);
        UpdateIntegrationStatusUI();

        ShowStatus(!isCurrentlyRegistered 
            ? "✓ Successfully added 'Open with MarkRead' to directory context menu." 
            : "✓ Removed 'Open with MarkRead' from directory context menu.", true);
    }

    private void BtnToggleWin11Menu_Click(object sender, RoutedEventArgs e)
    {
        bool isEnabled = ShellIntegration.IsClassicContextMenuEnabled();
        ShellIntegration.SetClassicContextMenu(!isEnabled);
        UpdateIntegrationStatusUI();

        ShowStatus(!isEnabled 
            ? "✓ Direct Right-Click Menu enabled! 'Open with MarkRead' will now appear directly on first right-click." 
            : "✓ Modern Windows 11 context menu restored.", true);
    }

    private void BtnSetDefault_Click(object sender, RoutedEventArgs e)
    {
        ShellIntegration.PromptSetAsDefault();
        UpdateIntegrationStatusUI();
        ShowStatus("✓ Opened Windows Default Apps dialog. Select MarkRead as default for .md files.", true);
    }

    private void BtnCreateShortcut_Click(object sender, RoutedEventArgs e)
    {
        ShellIntegration.CreateDesktopShortcut();
        ShowStatus("✓ MarkRead shortcut successfully created on your Desktop.", true);
    }

    private void BtnUnregister_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Are you sure you want to unregister MarkRead and remove all Explorer context menu entries?",
            "Confirm Clean Up",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            bool success = ShellIntegration.UnregisterFileAssociations();
            UpdateIntegrationStatusUI();
            ShowStatus(success 
                ? "✓ All MarkRead associations and context menus cleanly removed." 
                : "⚠️ Could not completely remove all entries.", success);
        }
    }

    private void ShowStatus(string message, bool success)
    {
        StatusBanner.Visibility = Visibility.Visible;
        TxtStatusBanner.Text = message;
        TxtStatusBanner.Foreground = success 
            ? new SolidColorBrush(Color.FromRgb(52, 211, 153)) 
            : new SolidColorBrush(Color.FromRgb(248, 113, 113));
    }

    private void SaveAndNotify()
    {
        _settings.Save();
        _onSettingsChanged?.Invoke(_settings);
    }

    private void BtnDone_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
