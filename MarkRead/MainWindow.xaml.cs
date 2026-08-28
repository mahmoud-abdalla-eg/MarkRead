using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using MarkRead.Models;
using MarkRead.Services;

namespace MarkRead;

public partial class MainWindow : Window
{
    private readonly MarkdownRenderer _renderer = new();
    private readonly ObservableCollection<TabDocument> _tabs = [];
    private readonly AppSettings _settings = AppSettings.Load();
    private TabDocument? _activeTab;
    private bool _isWebViewReady;
    private string _currentTheme = "dark";
    private bool _isExplicitExit;

    public MainWindow()
    {
        InitializeComponent();
        _currentTheme = _settings.Theme;
        LoadWindowIcon();
        InitializeWebViewAsync();
        UpdateThemeUI();
    }

    private void LoadWindowIcon()
    {
        try
        {
            string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "app.ico");
            if (File.Exists(icoPath))
            {
                Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri(icoPath, UriKind.Absolute));
            }
        }
        catch { }
    }

    private async void InitializeWebViewAsync()
    {
        try
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string userDataFolder = Path.Combine(appData, "MarkRead", "WebView2");
            Directory.CreateDirectory(userDataFolder);

            var env = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
            await WebViewControl.EnsureCoreWebView2Async(env);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to initialize Microsoft WebView2: {ex.Message}", "MarkRead Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void WebViewControl_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        if (!e.IsSuccess)
        {
            MessageBox.Show($"WebView2 error: {e.InitializationException?.Message}", "MarkRead", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        _isWebViewReady = true;

        var core = WebViewControl.CoreWebView2;

        // Map Assets directory for reader.css, reader.js
        string assetsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
        if (Directory.Exists(assetsDir))
        {
            core.SetVirtualHostNameToFolderMapping("local.markread", assetsDir, CoreWebView2HostResourceAccessKind.Allow);
        }

        // Allow WPF to handle drag & drop
        WebViewControl.AllowDrop = true;

        core.WebMessageReceived += Core_WebMessageReceived;
        core.NavigationStarting += Core_NavigationStarting;

        // If a tab was opened before webview finished initializing, render it now
        if (_activeTab != null)
        {
            RenderActiveTab();
        }
    }

    private void Core_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        // Intercept external links and open in default browser
        if (e.Uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            e.Uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (!e.Uri.StartsWith("https://local.markread", StringComparison.OrdinalIgnoreCase) &&
                !e.Uri.StartsWith("https://doc.local", StringComparison.OrdinalIgnoreCase))
            {
                e.Cancel = true;
                OpenInBrowser(e.Uri);
            }
        }
    }

    private void Core_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            using var doc = JsonDocument.Parse(e.WebMessageAsJson);
            var root = doc.RootElement;
            string? type = root.GetProperty("type").GetString();

            if (type == "openExternal")
            {
                string? url = root.GetProperty("url").GetString();
                if (!string.IsNullOrEmpty(url)) OpenInBrowser(url);
            }
            else if (type == "openLocalFile" && _activeTab != null)
            {
                string? relPath = root.GetProperty("path").GetString();
                if (!string.IsNullOrEmpty(relPath))
                {
                    string docDir = Path.GetDirectoryName(_activeTab.FilePath) ?? "";
                    string target = Path.GetFullPath(Path.Combine(docDir, relPath));
                    if (File.Exists(target))
                    {
                        OpenFile(target);
                    }
                }
            }
        }
        catch
        {
            // Ignore malformed web messages
        }
    }

    public void OpenFile(string filePath)
    {
        if (!File.Exists(filePath)) return;

        // Check if already open
        foreach (var tab in _tabs)
        {
            if (string.Equals(tab.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
            {
                SelectTab(tab);
                return;
            }
        }

        var newTab = new TabDocument(filePath)
        {
            IsRawView = _settings.DefaultView.Equals("raw", StringComparison.OrdinalIgnoreCase)
        };
        newTab.FileOnDiskChanged += (s, ev) =>
        {
            Dispatcher.Invoke(() =>
            {
                if (_activeTab == newTab)
                {
                    ReloadActiveTab(preserveScroll: true);
                }
            });
        };

        _tabs.Add(newTab);
        SelectTab(newTab);

        ShellIntegration.AddToJumpList(filePath);
    }

    public void OpenTarget(string path)
    {
        if (File.Exists(path))
        {
            OpenFile(path);
        }
        else if (Directory.Exists(path))
        {
            OpenDirectory(path);
        }
    }

    public void OpenDirectory(string dirPath)
    {
        if (!Directory.Exists(dirPath)) return;

        try
        {
            // Find top-level markdown files
            var mdFiles = Directory.GetFiles(dirPath, "*.md", SearchOption.TopDirectoryOnly)
                .Concat(Directory.GetFiles(dirPath, "*.markdown", SearchOption.TopDirectoryOnly))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // If none in root, search subdirectories up to 15 files
            if (mdFiles.Count == 0)
            {
                var subFiles = Directory.GetFiles(dirPath, "*.md", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(dirPath, "*.markdown", SearchOption.AllDirectories))
                    .Take(15)
                    .ToList();
                mdFiles.AddRange(subFiles);
            }

            if (mdFiles.Count == 0)
            {
                MessageBox.Show($"No Markdown (.md) documents found in folder:\n{dirPath}", "MarkRead", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Prioritize README.md, readme.md, or index.md
            string? mainDoc = mdFiles.FirstOrDefault(f =>
                string.Equals(Path.GetFileName(f), "README.md", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetFileName(f), "readme.md", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetFileName(f), "index.md", StringComparison.OrdinalIgnoreCase));

            mainDoc ??= mdFiles[0];

            // Open other files as tabs first
            foreach (var file in mdFiles.Take(8))
            {
                if (!string.Equals(file, mainDoc, StringComparison.OrdinalIgnoreCase))
                {
                    OpenFile(file);
                }
            }

            // Open and select the primary document
            OpenFile(mainDoc);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening folder: {ex.Message}", "MarkRead", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SelectTab(TabDocument tab)
    {
        _activeTab = tab;
        RebuildTabsUI();

        EmptyStateOverlay.Visibility = Visibility.Collapsed;
        WebViewControl.Visibility = Visibility.Visible;

        StatusFilePath.Text = tab.FilePath;
        StatusMode.Text = tab.IsRawView ? "Raw Text" : "Rendered";

        if (_isWebViewReady)
        {
            RenderActiveTab();
        }
    }

    private void CloseTab(TabDocument tab)
    {
        int index = _tabs.IndexOf(tab);
        if (index < 0) return;

        tab.Dispose();
        _tabs.RemoveAt(index);

        if (_activeTab == tab)
        {
            if (_tabs.Count > 0)
            {
                int nextIndex = Math.Min(index, _tabs.Count - 1);
                SelectTab(_tabs[nextIndex]);
            }
            else
            {
                _activeTab = null;
                RebuildTabsUI();
                EmptyStateOverlay.Visibility = Visibility.Visible;
                WebViewControl.Visibility = Visibility.Collapsed;
                StatusFilePath.Text = "Ready";
                StatusDocInfo.Text = "";
                StatusMode.Text = "Ready";
            }
        }
        else
        {
            RebuildTabsUI();
        }
    }

    private void RebuildTabsUI()
    {
        TabsContainer.Children.Clear();

        foreach (var tab in _tabs)
        {
            bool isActive = tab == _activeTab;

            var tabBorder = new Border
            {
                Background = isActive 
                    ? new SolidColorBrush(Color.FromRgb(24, 24, 27))   // #18181B zinc-900 elevated
                    : new SolidColorBrush(Color.FromRgb(18, 18, 21)),  // #121215 zinc-950 surface
                BorderBrush = isActive 
                    ? new SolidColorBrush(Color.FromRgb(63, 63, 70))   // #3F3F46 zinc-700
                    : new SolidColorBrush(Color.FromRgb(39, 39, 42)),  // #27272A zinc-800
                BorderThickness = new Thickness(1, 1, 1, 0),
                CornerRadius = new CornerRadius(6, 6, 0, 0),
                Margin = new Thickness(0, 0, 4, 0),
                Padding = new Thickness(12, 6, 8, 6),
                Cursor = Cursors.Hand
            };

            var tabGrid = new Grid();
            tabGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            tabGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleBlock = new TextBlock
            {
                Text = tab.Title,
                Foreground = isActive 
                    ? Brushes.White 
                    : new SolidColorBrush(Color.FromRgb(161, 161, 170)), // #A1A1AA
                FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
                MaxWidth = 180,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(titleBlock, 0);
            tabGrid.Children.Add(titleBlock);

            var closeBtn = new Button
            {
                Content = "✕",
                Foreground = isActive 
                    ? new SolidColorBrush(Color.FromRgb(212, 212, 216)) 
                    : new SolidColorBrush(Color.FromRgb(113, 113, 122)),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 10,
                Padding = new Thickness(3, 1, 3, 1),
                Cursor = Cursors.Hand
            };
            closeBtn.MouseEnter += (s, e) => closeBtn.Foreground = Brushes.White;
            closeBtn.MouseLeave += (s, e) => closeBtn.Foreground = isActive 
                ? new SolidColorBrush(Color.FromRgb(212, 212, 216)) 
                : new SolidColorBrush(Color.FromRgb(113, 113, 122));
            closeBtn.Click += (s, e) =>
            {
                e.Handled = true;
                CloseTab(tab);
            };
            Grid.SetColumn(closeBtn, 1);
            tabGrid.Children.Add(closeBtn);

            tabBorder.Child = tabGrid;
            tabBorder.MouseLeftButtonDown += (s, e) => SelectTab(tab);

            if (!isActive)
            {
                tabBorder.MouseEnter += (s, e) =>
                {
                    tabBorder.Background = new SolidColorBrush(Color.FromRgb(24, 24, 27));
                    titleBlock.Foreground = new SolidColorBrush(Color.FromRgb(244, 244, 245));
                };
                tabBorder.MouseLeave += (s, e) =>
                {
                    tabBorder.Background = new SolidColorBrush(Color.FromRgb(18, 18, 21));
                    titleBlock.Foreground = new SolidColorBrush(Color.FromRgb(161, 161, 170));
                };
            }

            TabsContainer.Children.Add(tabBorder);
        }
    }

    private async void RenderActiveTab(bool preserveScroll = false)
    {
        if (_activeTab == null || !_isWebViewReady) return;

        double savedScrollRatio = 0;
        if (preserveScroll)
        {
            try
            {
                string ratioJson = await WebViewControl.ExecuteScriptAsync("window.markRead ? window.markRead.getScrollRatio() : 0");
                if (double.TryParse(ratioJson, out double r)) savedScrollRatio = r;
            }
            catch { }
        }

        try
        {
            string content = File.ReadAllText(_activeTab.FilePath);
            string? docDir = Path.GetDirectoryName(_activeTab.FilePath);

            // Map document directory to https://doc.local/ for local image resolution
            if (!string.IsNullOrEmpty(docDir) && Directory.Exists(docDir))
            {
                WebViewControl.CoreWebView2.SetVirtualHostNameToFolderMapping("doc.local", docDir, CoreWebView2HostResourceAccessKind.Allow);
            }

            string html = _renderer.RenderToHtml(content, _activeTab.Title, _activeTab.IsRawView, _currentTheme, _settings.FontSize, _settings.ReadingWidth);

            // Inject <base href="https://doc.local/"> to automatically resolve local images
            if (!string.IsNullOrEmpty(docDir))
            {
                int headIndex = html.IndexOf("<head>", StringComparison.OrdinalIgnoreCase);
                if (headIndex >= 0)
                {
                    html = html.Insert(headIndex + 6, "\n    <base href=\"https://doc.local/\">");
                }
            }

            WebViewControl.NavigateToString(html);

            // Update stats
            int wordCount = content.Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;
            long bytes = new FileInfo(_activeTab.FilePath).Length;
            StatusDocInfo.Text = $"{wordCount:N0} words  |  {bytes / 1024.0:F1} KB";

            if (preserveScroll && savedScrollRatio > 0)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(150);
                    await Dispatcher.InvokeAsync(async () =>
                    {
                        await WebViewControl.ExecuteScriptAsync($"window.markRead && window.markRead.setScrollRatio({savedScrollRatio})");
                    });
                });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to read file: {ex.Message}", "MarkRead", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ReloadActiveTab(bool preserveScroll = true)
    {
        if (_activeTab != null)
        {
            RenderActiveTab(preserveScroll);
        }
    }

    // Drag & Drop
    private void Window_PreviewDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null)
            {
                foreach (var file in files)
                {
                    OpenTarget(file);
                }
            }
        }
    }

    // Toolbar Event Handlers
    private void BtnOpen_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Open Markdown Document",
            Filter = "Markdown Files (*.md;*.markdown)|*.md;*.markdown|Text Files (*.txt;*.log;*.json)|*.txt;*.log;*.json|All Files (*.*)|*.*",
            Multiselect = true
        };

        if (dlg.ShowDialog() == true)
        {
            foreach (var filename in dlg.FileNames)
            {
                OpenFile(filename);
            }
        }
    }

    private void BtnNewTab_Click(object sender, RoutedEventArgs e)
    {
        BtnOpen_Click(sender, e);
    }

    private void BtnToggleRaw_Click(object sender, RoutedEventArgs e)
    {
        if (_activeTab == null) return;
        _activeTab.IsRawView = !_activeTab.IsRawView;

        TxtToggleRaw.Text = _activeTab.IsRawView ? "Rendered" : "Raw";
        TxtToggleRawIcon.Text = _activeTab.IsRawView ? "📖 " : "📄 ";
        StatusMode.Text = _activeTab.IsRawView ? "Raw Text" : "Rendered";

        RenderActiveTab(preserveScroll: true);
    }

    private void BtnThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        _currentTheme = _currentTheme switch
        {
            "dark" => "light",
            "light" => "system",
            _ => "dark"
        };

        _settings.Theme = _currentTheme;
        _settings.Save();

        UpdateThemeUI();

        if (_isWebViewReady)
        {
            WebViewControl.ExecuteScriptAsync($"window.markRead && window.markRead.setTheme('{_currentTheme}')");
        }
    }

    private void UpdateThemeUI()
    {
        TxtTheme.Text = _currentTheme switch
        {
            "dark" => "Dark",
            "light" => "Light",
            _ => "Auto"
        };
        TxtThemeIcon.Text = _currentTheme switch
        {
            "dark" => "🌙 ",
            "light" => "☀️ ",
            _ => "💻 "
        };
    }

    private void BtnExportPdf_Click(object sender, RoutedEventArgs e)
    {
        if (!_isWebViewReady || _activeTab == null)
        {
            MessageBox.Show("Please open a Markdown document first.", "MarkRead", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var env = WebViewControl.CoreWebView2.Environment;
            var exportWindow = new PdfExportWindow(env, WebViewControl, _activeTab.Title, _activeTab.FilePath)
            {
                Owner = this
            };

            if (exportWindow.ShowDialog() == true && !string.IsNullOrEmpty(exportWindow.ExportedFilePath))
            {
                StatusFilePath.Text = $"Exported: {exportWindow.ExportedFilePath}";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening PDF export preview: {ex.Message}", "MarkRead", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        if (SearchBar.Visibility == Visibility.Visible)
        {
            SearchBar.Visibility = Visibility.Collapsed;
        }
        else
        {
            SearchBar.Visibility = Visibility.Visible;
            SearchInput.Focus();
            SearchInput.SelectAll();
        }
    }

    private void BtnCloseSearch_Click(object sender, RoutedEventArgs e)
    {
        SearchBar.Visibility = Visibility.Collapsed;
    }

    private void SearchInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (Keyboard.Modifiers == ModifierKeys.Shift)
                BtnFindPrev_Click(sender, e);
            else
                BtnFindNext_Click(sender, e);
        }
        else if (e.Key == Key.Escape)
        {
            SearchBar.Visibility = Visibility.Collapsed;
        }
    }

    private void BtnFindNext_Click(object sender, RoutedEventArgs e)
    {
        PerformFind(SearchInput.Text, backward: false);
    }

    private void BtnFindPrev_Click(object sender, RoutedEventArgs e)
    {
        PerformFind(SearchInput.Text, backward: true);
    }

    private void PerformFind(string query, bool backward)
    {
        if (string.IsNullOrEmpty(query) || !_isWebViewReady) return;
        string escaped = JsonEncodedText.Encode(query).Value;
        string js = $"window.find(\"{escaped}\", false, {backward.ToString().ToLower()}, true, false, false, false)";
        WebViewControl.ExecuteScriptAsync(js);
    }

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        var settingsWin = new SettingsWindow(_settings, OnSettingsChanged)
        {
            Owner = this
        };
        settingsWin.ShowDialog();
    }

    private void OnSettingsChanged(AppSettings updated)
    {
        if (_currentTheme != updated.Theme)
        {
            _currentTheme = updated.Theme;
            UpdateThemeUI();
            if (_isWebViewReady)
            {
                WebViewControl.ExecuteScriptAsync($"window.markRead && window.markRead.setTheme('{_currentTheme}')");
            }
        }

        if (_isWebViewReady)
        {
            WebViewControl.ExecuteScriptAsync($"window.markRead && window.markRead.setFontSize({updated.FontSize})");
            WebViewControl.ExecuteScriptAsync($"window.markRead && window.markRead.setMaxWidth('{updated.ReadingWidth}')");
        }

        if (_activeTab != null)
        {
            ReloadActiveTab(preserveScroll: true);
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_isExplicitExit)
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;
            StatusFilePath.Text = "Minimized to taskbar (ready for drop). Press Ctrl+Q or Exit to quit.";
            return;
        }
        base.OnClosing(e);
    }

    private void BtnExit_Click(object sender, RoutedEventArgs e)
    {
        _isExplicitExit = true;
        Application.Current.Shutdown();
    }

    // Keyboard Shortcuts
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            switch (e.Key)
            {
                case Key.Q:
                    BtnExit_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                case Key.O:
                    BtnOpen_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                case Key.W:
                    if (_activeTab != null) CloseTab(_activeTab);
                    e.Handled = true;
                    break;
                case Key.T:
                    BtnNewTab_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                case Key.U:
                    BtnToggleRaw_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                case Key.P:
                    BtnExportPdf_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                case Key.F:
                    BtnSearch_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
            }
        }
        else if (e.Key == Key.F5)
        {
            ReloadActiveTab(preserveScroll: true);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && SearchBar.Visibility == Visibility.Visible)
        {
            SearchBar.Visibility = Visibility.Collapsed;
            e.Handled = true;
        }
    }

    private static void OpenInBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch { }
    }
}