using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;

namespace MarkRead;

public partial class PdfExportWindow : Window
{
    // Remembers the last used export directory across the entire app session
    private static string? s_lastExportDirectory;

    private readonly CoreWebView2Environment _env;
    private readonly WebView2 _sourceWebView;
    private readonly string _documentTitle;
    private readonly string _documentSourcePath;

    private readonly string _tempFileA;
    private readonly string _tempFileB;
    private bool _useFileA;
    private string _currentTempFile = string.Empty;

    private bool _isInitialized;
    private bool _isRendering;
    private bool _pendingRenderRequest;

    public string ExportedFilePath { get; private set; } = string.Empty;

    public PdfExportWindow(CoreWebView2Environment env, WebView2 sourceWebView, string documentTitle, string documentSourcePath)
    {
        InitializeComponent();

        _env = env;
        _sourceWebView = sourceWebView;
        _documentTitle = string.IsNullOrWhiteSpace(documentTitle) ? "Document" : documentTitle;
        _documentSourcePath = documentSourcePath;

        string tempDir = Path.GetTempPath();
        string session = Guid.NewGuid().ToString("N");
        _tempFileA = Path.Combine(tempDir, $"MarkRead_preview_A_{session}.pdf");
        _tempFileB = Path.Combine(tempDir, $"MarkRead_preview_B_{session}.pdf");

        Title = $"Export PDF Preview - {_documentTitle}";

        InitializeDefaultRoute();
        InitializePreviewWebViewAsync();
    }

    private void InitializeDefaultRoute()
    {
        string defaultDir = string.Empty;

        // 1. Prefer the remembered export directory from this session if it still exists
        if (!string.IsNullOrEmpty(s_lastExportDirectory) && Directory.Exists(s_lastExportDirectory))
        {
            defaultDir = s_lastExportDirectory;
        }

        // 2. Otherwise use the current markdown file's directory
        if (string.IsNullOrEmpty(defaultDir) && !string.IsNullOrEmpty(_documentSourcePath))
        {
            try
            {
                defaultDir = Path.GetDirectoryName(_documentSourcePath) ?? string.Empty;
            }
            catch { }
        }

        // 3. Fallback to Documents folder
        if (string.IsNullOrEmpty(defaultDir) || !Directory.Exists(defaultDir))
        {
            defaultDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        string safeTitle = Path.GetFileNameWithoutExtension(_documentTitle);
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            safeTitle = safeTitle.Replace(c, '_');
        }

        if (string.IsNullOrWhiteSpace(safeTitle))
        {
            safeTitle = "Document";
        }

        string defaultFileName = safeTitle + ".pdf";
        TxtRoutePath.Text = Path.Combine(defaultDir, defaultFileName);
    }

    private async void InitializePreviewWebViewAsync()
    {
        try
        {
            await PreviewWebView.EnsureCoreWebView2Async(_env);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to initialize PDF preview engine: {ex.Message}", "MarkRead Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PreviewWebView_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        if (!e.IsSuccess)
        {
            MessageBox.Show($"Preview engine error: {e.InitializationException?.Message}", "MarkRead", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var settings = PreviewWebView.CoreWebView2.Settings;
            settings.HiddenPdfToolbarItems = CoreWebView2PdfToolbarItems.Print |
                                             CoreWebView2PdfToolbarItems.Save |
                                             CoreWebView2PdfToolbarItems.SaveAs;
            settings.IsStatusBarEnabled = false;
        }
        catch { }

        _isInitialized = true;
        _ = RefreshPreviewAsync();
    }

    private CoreWebView2PrintSettings CreatePrintSettings()
    {
        var ps = _env.CreatePrintSettings();

        // Orientation
        ps.Orientation = (RbLandscape.IsChecked == true)
            ? CoreWebView2PrintOrientation.Landscape
            : CoreWebView2PrintOrientation.Portrait;

        // Paper Size (inches)
        if (RbPaperLetter.IsChecked == true)
        {
            ps.PageWidth = 8.5;
            ps.PageHeight = 11.0;
        }
        else if (RbPaperLegal.IsChecked == true)
        {
            ps.PageWidth = 8.5;
            ps.PageHeight = 14.0;
        }
        else // A4 default
        {
            ps.PageWidth = 8.27;
            ps.PageHeight = 11.69;
        }

        // Margins (inches)
        if (RbMarginCompact.IsChecked == true)
        {
            ps.MarginTop = 0.25;
            ps.MarginBottom = 0.25;
            ps.MarginLeft = 0.25;
            ps.MarginRight = 0.25;
        }
        else if (RbMarginWide.IsChecked == true)
        {
            ps.MarginTop = 0.8;
            ps.MarginBottom = 0.8;
            ps.MarginLeft = 0.8;
            ps.MarginRight = 0.8;
        }
        else if (RbMarginNone.IsChecked == true)
        {
            ps.MarginTop = 0.0;
            ps.MarginBottom = 0.0;
            ps.MarginLeft = 0.0;
            ps.MarginRight = 0.0;
        }
        else // Standard default
        {
            ps.MarginTop = 0.4;
            ps.MarginBottom = 0.4;
            ps.MarginLeft = 0.5;
            ps.MarginRight = 0.5;
        }

        ps.ShouldPrintBackgrounds = ChkBackgrounds.IsChecked == true;
        ps.ShouldPrintHeaderAndFooter = ChkHeaderFooter.IsChecked == true;

        return ps;
    }

    private async Task RefreshPreviewAsync()
    {
        if (!_isInitialized || _sourceWebView.CoreWebView2 == null) return;

        if (_isRendering)
        {
            _pendingRenderRequest = true;
            return;
        }

        _isRendering = true;
        _pendingRenderRequest = false;

        try
        {
            TxtPreviewStatus.Text = "Updating preview...";
            LoadingOverlay.Visibility = Visibility.Visible;

            // Apply dark theme class if selected
            bool isDark = RbThemeDark?.IsChecked == true;
            if (isDark)
            {
                await _sourceWebView.ExecuteScriptAsync("document.body.classList.add('print-dark');");
            }
            else
            {
                await _sourceWebView.ExecuteScriptAsync("document.body.classList.remove('print-dark');");
            }

            // Alternate between file A and file B to bypass Chromium open-file locks
            _useFileA = !_useFileA;
            string targetTempFile = _useFileA ? _tempFileA : _tempFileB;

            var printSettings = CreatePrintSettings();

            bool success = await _sourceWebView.CoreWebView2.PrintToPdfAsync(targetTempFile, printSettings);

            // Revert print-dark class immediately so reader view is unchanged
            if (isDark)
            {
                await _sourceWebView.ExecuteScriptAsync("document.body.classList.remove('print-dark');");
            }

            if (success && File.Exists(targetTempFile))
            {
                _currentTempFile = targetTempFile;
                Uri fileUri = new(targetTempFile);
                PreviewWebView.CoreWebView2.Navigate(fileUri.AbsoluteUri);

                long sizeBytes = new FileInfo(targetTempFile).Length;
                TxtPreviewStatus.Text = $"Ready • {sizeBytes / 1024.0:F1} KB ({DateTime.Now:HH:mm:ss})";
            }
            else
            {
                TxtPreviewStatus.Text = "Failed to render preview";
            }
        }
        catch (Exception ex)
        {
            TxtPreviewStatus.Text = $"Render error: {ex.Message}";
        }
        finally
        {
            // Safeguard: ensure print-dark is always stripped from main reader view
            try
            {
                await _sourceWebView.ExecuteScriptAsync("document.body.classList.remove('print-dark');");
            }
            catch { }

            LoadingOverlay.Visibility = Visibility.Collapsed;
            _isRendering = false;

            if (_pendingRenderRequest)
            {
                _ = RefreshPreviewAsync();
            }
        }
    }

    private void SettingChanged(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized) return;
        _ = RefreshPreviewAsync();
    }

    private void BtnRefreshPreview_Click(object sender, RoutedEventArgs e)
    {
        _ = RefreshPreviewAsync();
    }

    // Export Route Selection
    private void BtnBrowseRoute_Click(object sender, RoutedEventArgs e)
    {
        string currentPath = TxtRoutePath.Text.Trim();
        string initialDir = string.Empty;
        string initialFile = $"{_documentTitle}.pdf";

        try
        {
            if (!string.IsNullOrEmpty(currentPath))
            {
                initialDir = Path.GetDirectoryName(currentPath) ?? string.Empty;
                initialFile = Path.GetFileName(currentPath);
            }
        }
        catch { }

        if (string.IsNullOrEmpty(initialDir) || !Directory.Exists(initialDir))
        {
            if (!string.IsNullOrEmpty(s_lastExportDirectory) && Directory.Exists(s_lastExportDirectory))
            {
                initialDir = s_lastExportDirectory;
            }
            else
            {
                initialDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
        }

        var dlg = new SaveFileDialog
        {
            Title = "Select Destination Route for PDF Export",
            Filter = "PDF Document (*.pdf)|*.pdf",
            FileName = initialFile,
            InitialDirectory = initialDir
        };

        if (dlg.ShowDialog(this) == true)
        {
            TxtRoutePath.Text = dlg.FileName;
            s_lastExportDirectory = Path.GetDirectoryName(dlg.FileName);
        }
    }

    private void TxtRoutePath_TextChanged(object sender, TextChangedEventArgs e)
    {
        ValidateRoute();
    }

    private bool ValidateRoute()
    {
        string route = TxtRoutePath.Text.Trim();

        if (string.IsNullOrWhiteSpace(route))
        {
            TxtRouteStatus.Text = "Route cannot be empty";
            TxtRouteStatus.Foreground = new SolidColorBrush(Color.FromRgb(248, 113, 113));
            BtnExport.IsEnabled = false;
            return false;
        }

        try
        {
            string? dir = Path.GetDirectoryName(route);
            string ext = Path.GetExtension(route);

            if (!string.Equals(ext, ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                TxtRouteStatus.Text = "Destination must have .pdf extension";
                TxtRouteStatus.Foreground = new SolidColorBrush(Color.FromRgb(251, 191, 36));
                BtnExport.IsEnabled = true;
                return true;
            }

            TxtRouteStatus.Text = "Export route is valid";
            TxtRouteStatus.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            BtnExport.IsEnabled = true;
            return true;
        }
        catch (Exception ex)
        {
            TxtRouteStatus.Text = $"Invalid path: {ex.Message}";
            TxtRouteStatus.Foreground = new SolidColorBrush(Color.FromRgb(248, 113, 113));
            BtnExport.IsEnabled = false;
            return false;
        }
    }

    // Export PDF Action
    private async void BtnExport_Click(object sender, RoutedEventArgs e)
    {
        string route = TxtRoutePath.Text.Trim();

        if (!ValidateRoute())
        {
            MessageBox.Show("Please enter a valid export route.", "Invalid Route", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!route.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            route += ".pdf";
            TxtRoutePath.Text = route;
        }

        // Overwrite Confirmation
        if (File.Exists(route))
        {
            var confirmResult = MessageBox.Show(
                $"A file named '{Path.GetFileName(route)}' already exists at this route:\n\n{route}\n\nDo you want to replace it?",
                "Confirm File Overwrite",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmResult != MessageBoxResult.Yes)
            {
                return;
            }
        }

        try
        {
            string? dir = Path.GetDirectoryName(route);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            BtnExport.IsEnabled = false;
            TxtPreviewStatus.Text = "Exporting PDF to selected route...";

            // If current temp preview exists, copy it directly for instantaneous export
            if (!string.IsNullOrEmpty(_currentTempFile) && File.Exists(_currentTempFile))
            {
                File.Copy(_currentTempFile, route, overwrite: true);
            }
            else
            {
                // Otherwise print freshly to destination route
                bool isDark = RbThemeDark?.IsChecked == true;
                if (isDark) await _sourceWebView.ExecuteScriptAsync("document.body.classList.add('print-dark');");

                var printSettings = CreatePrintSettings();
                bool success = await _sourceWebView.CoreWebView2.PrintToPdfAsync(route, printSettings);

                if (isDark) await _sourceWebView.ExecuteScriptAsync("document.body.classList.remove('print-dark');");

                if (!success)
                {
                    MessageBox.Show("Failed to export PDF file.", "Export Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    BtnExport.IsEnabled = true;
                    return;
                }
            }

            ExportedFilePath = route;
            s_lastExportDirectory = Path.GetDirectoryName(route);

            // Display completion overlay offering Open PDF, Show in Folder, Done
            TxtSuccessFilePath.Text = route;
            ExportSuccessOverlay.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error exporting PDF to '{route}': {ex.Message}", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            BtnExport.IsEnabled = true;
        }
    }

    private void BtnOpenPdf_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!string.IsNullOrEmpty(ExportedFilePath) && File.Exists(ExportedFilePath))
            {
                Process.Start(new ProcessStartInfo(ExportedFilePath) { UseShellExecute = true });
            }
        }
        catch { }

        DialogResult = true;
        Close();
    }

    private void BtnShowInFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!string.IsNullOrEmpty(ExportedFilePath) && File.Exists(ExportedFilePath))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{ExportedFilePath}\"") { UseShellExecute = true });
            }
        }
        catch { }

        DialogResult = true;
        Close();
    }

    private void BtnDone_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        // Ensure print-dark styling is removed from main reader view
        try
        {
            _sourceWebView.ExecuteScriptAsync("document.body.classList.remove('print-dark');");
        }
        catch { }

        // Clean up temporary preview files
        try
        {
            if (File.Exists(_tempFileA)) File.Delete(_tempFileA);
        }
        catch { }

        try
        {
            if (File.Exists(_tempFileB)) File.Delete(_tempFileB);
        }
        catch { }
    }
}
