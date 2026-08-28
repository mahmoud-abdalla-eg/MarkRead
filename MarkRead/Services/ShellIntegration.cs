using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Shell;
using Microsoft.Win32;

namespace MarkRead.Services;

public static class ShellIntegration
{
    private const string ProgId = "MarkRead.Document";
    private const string AppName = "MarkRead.exe";

    public static bool RegisterFileAssociations()
    {
        try
        {
            string exePath = Process.GetCurrentProcess().MainModule?.FileName 
                             ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppName);

            // 1. HKCU\Software\Classes\Applications\MarkRead.exe
            using (var appKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\Applications\{AppName}"))
            {
                appKey.SetValue("FriendlyAppName", "MarkRead");
                using var iconKey = appKey.CreateSubKey("DefaultIcon");
                iconKey.SetValue("", $"\"{exePath}\",0");

                using var cmdKey = appKey.CreateSubKey(@"shell\open\command");
                cmdKey.SetValue("", $"\"{exePath}\" \"%1\"");

                using var supportedKey = appKey.CreateSubKey("SupportedTypes");
                supportedKey.SetValue(".md", "");
                supportedKey.SetValue(".markdown", "");
                supportedKey.SetValue(".txt", "");
            }

            // 2. HKCU\Software\Classes\MarkRead.Document
            using (var docKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}"))
            {
                docKey.SetValue("", "Markdown Document");
                using var iconKey = docKey.CreateSubKey("DefaultIcon");
                iconKey.SetValue("", $"\"{exePath}\",0");

                using var cmdKey = docKey.CreateSubKey(@"shell\open\command");
                cmdKey.SetValue("", $"\"{exePath}\" \"%1\"");
            }

            // 3. Associate with .md & .markdown and set as default ProgId
            using (var mdKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.md"))
            {
                mdKey.SetValue("", ProgId);
                using var openWith = mdKey.CreateSubKey("OpenWithProgids");
                openWith.SetValue(ProgId, string.Empty);
            }
            using (var markdownKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.markdown"))
            {
                markdownKey.SetValue("", ProgId);
                using var openWith = markdownKey.CreateSubKey("OpenWithProgids");
                openWith.SetValue(ProgId, string.Empty);
            }

            // 4. Direct right-click context menu: "Open with MarkRead"
            using (var ctxKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\SystemFileAssociations\.md\shell\Open with MarkRead"))
            {
                ctxKey.SetValue("", "Open with MarkRead");
                ctxKey.SetValue("Icon", $"\"{exePath}\",0");
                using var cmdKey = ctxKey.CreateSubKey("command");
                cmdKey.SetValue("", $"\"{exePath}\" \"%1\"");
            }

            using (var ctxKey2 = Registry.CurrentUser.CreateSubKey(@"Software\Classes\SystemFileAssociations\.markdown\shell\Open with MarkRead"))
            {
                ctxKey2.SetValue("", "Open with MarkRead");
                ctxKey2.SetValue("Icon", $"\"{exePath}\",0");
                using var cmdKey = ctxKey2.CreateSubKey("command");
                cmdKey.SetValue("", $"\"{exePath}\" \"%1\"");
            }

            // Create desktop shortcut for taskbar pinning
            CreateDesktopShortcut(exePath);

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to register associations: {ex.Message}");
            return false;
        }
    }

    public static bool UnregisterFileAssociations()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\Applications\{AppName}", false);
            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{ProgId}", false);
            Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\SystemFileAssociations\.md\shell\Open with MarkRead", false);
            Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\SystemFileAssociations\.markdown\shell\Open with MarkRead", false);

            using (var mdKey = Registry.CurrentUser.OpenSubKey(@"Software\Classes\.md\OpenWithProgids", true))
            {
                mdKey?.DeleteValue(ProgId, false);
            }
            using (var markdownKey = Registry.CurrentUser.OpenSubKey(@"Software\Classes\.markdown\OpenWithProgids", true))
            {
                markdownKey?.DeleteValue(ProgId, false);
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to unregister: {ex.Message}");
            return false;
        }
    }

    public static void CreateDesktopShortcut(string? exePath = null)
    {
        try
        {
            exePath ??= Process.GetCurrentProcess().MainModule?.FileName 
                        ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppName);

            string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string shortcutPath = Path.Combine(desktopDir, "MarkRead.lnk");

            Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return;

            dynamic? shell = Activator.CreateInstance(shellType);
            if (shell == null) return;

            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = exePath;
            shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
            shortcut.Description = "MarkRead - Modern Markdown Document Viewer";
            shortcut.IconLocation = $"{exePath},0";
            shortcut.Save();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to create shortcut: {ex.Message}");
        }
    }

    public static void AddToJumpList(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return;

            var jumpList = JumpList.GetJumpList(Application.Current) ?? new JumpList();
            jumpList.ShowRecentCategory = true;

            var jumpPath = new JumpPath
            {
                Path = filePath,
                CustomCategory = "Recent Documents"
            };

            jumpList.JumpItems.Add(jumpPath);
            jumpList.Apply();
        }
        catch
        {
            // Ignore jump list errors on unsupported configurations
        }
    }

    public static void PromptSetAsDefault()
    {
        try
        {
            RegisterFileAssociations();
            string sampleFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sample_document.md");
            if (!File.Exists(sampleFile))
            {
                sampleFile = Path.Combine(Path.GetTempPath(), "test.md");
                File.WriteAllText(sampleFile, "# MarkRead");
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = "rundll32.exe",
                Arguments = $"shell32.dll,OpenAs_RunDLL \"{sampleFile}\"",
                UseShellExecute = true
            });
        }
        catch { }
    }
}
