using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace MarkRead.Models;

public class TabDocument : INotifyPropertyChanged, IDisposable
{
    private string _filePath = string.Empty;
    private string _title = "Untitled";
    private bool _isRawView;
    private double _scrollRatio;
    private FileSystemWatcher? _watcher;
    private DateTime _lastReadTime = DateTime.MinValue;

    public event EventHandler? FileOnDiskChanged;

    public string FilePath
    {
        get => _filePath;
        set
        {
            if (_filePath != value)
            {
                _filePath = value;
                OnPropertyChanged();
                SetupWatcher();
            }
        }
    }

    public string Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsRawView
    {
        get => _isRawView;
        set
        {
            if (_isRawView != value)
            {
                _isRawView = value;
                OnPropertyChanged();
            }
        }
    }

    public double ScrollRatio
    {
        get => _scrollRatio;
        set
        {
            if (Math.Abs(_scrollRatio - value) > 0.001)
            {
                _scrollRatio = value;
                OnPropertyChanged();
            }
        }
    }

    public TabDocument(string filePath)
    {
        FilePath = filePath;
        Title = Path.GetFileName(filePath);
        _lastReadTime = DateTime.UtcNow;
    }

    private void SetupWatcher()
    {
        _watcher?.Dispose();
        _watcher = null;

        if (string.IsNullOrWhiteSpace(_filePath) || !File.Exists(_filePath))
            return;

        try
        {
            string? dir = Path.GetDirectoryName(_filePath);
            string? fileName = Path.GetFileName(_filePath);

            if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(fileName))
                return;

            _watcher = new FileSystemWatcher(dir, fileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            _watcher.Changed += OnWatcherChanged;
        }
        catch
        {
            // Ignore watcher failure if in restricted/network directory
        }
    }

    private void OnWatcherChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce external rapid writes
        if ((DateTime.UtcNow - _lastReadTime).TotalMilliseconds < 400)
            return;

        _lastReadTime = DateTime.UtcNow;
        FileOnDiskChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnWatcherChanged;
            _watcher.Dispose();
            _watcher = null;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
