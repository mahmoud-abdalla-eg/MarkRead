using System;
using System.IO;
using System.Text.Json;

namespace MarkRead.Models;

public class AppSettings
{
    public string Theme { get; set; } = "dark"; // "dark", "light", "system"
    public int FontSize { get; set; } = 16;     // 14, 16, 18, 20
    public string ReadingWidth { get; set; } = "860px"; // "720px", "860px", "1080px", "100%"
    public string DefaultView { get; set; } = "rendered"; // "rendered", "raw"
    public string PdfOrientation { get; set; } = "portrait"; // "portrait", "landscape"
    public string PdfMargins { get; set; } = "standard"; // "standard", "compact", "spacious"

    private static readonly string SettingsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
        "MarkRead");
    private static readonly string SettingsFile = Path.Combine(SettingsFolder, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                string json = File.ReadAllText(SettingsFile);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null) return settings;
            }
        }
        catch { }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsFolder);
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
        }
        catch { }
    }
}
