# MarkRead - Sample Markdown Document

Welcome to **MarkRead**, your modern Windows Markdown viewer! This document demonstrates the rich formatting, clean typography, code highlighting, tables, and local image rendering.

---

## 🚀 Key Features

- **Document-Grade Reading Mode**: Formatted typography designed for clarity and eye comfort.
- **Dark & Light Themes**: Easily toggle themes via the top bar or use your Windows system mode.
- **Drag & Drop**: Drop `.md` files onto the window, taskbar icon, or desktop shortcut.
- **Windows Explorer Integration**: Right-click any `.md` file in File Explorer and choose **"Open with MarkRead"**.
- **Print / PDF Export**: Press `Ctrl+P` or click Print to generate clean PDF documents.
- **Live File Sync**: Modifying this file externally in Notepad or VS Code auto-refreshes the view instantly!

---

## 📊 Sample Table

| Feature | Support | Description |
| :--- | :---: | :--- |
| **GitHub Flavored Markdown** | ✅ | Full support for pipe tables, task lists, and strikethrough |
| **Local Images** | ✅ | Automatically renders `./images` or relative assets |
| **Single-Instance IPC** | ✅ | Opening another file adds a tab to the active window |
| **Fast Startup** | ✅ | Native C# .NET 8 WPF single-file executable |

---

## ✅ Task Checklist

- [x] Create native .NET 8 WPF application
- [x] Design multi-resolution application icon (`app.ico`)
- [x] Configure Microsoft WebView2 with isolated user data directory
- [x] Build dark/light responsive reader stylesheet
- [x] Enable 1-click code block copying
- [x] Add Windows Explorer context menu registration (`Register-MarkRead.bat`)
- [ ] Try opening another file using drag-and-drop

---

## 💻 Code Block with 1-Click Copy

```csharp
// Example C# code snippet
public class MarkdownViewer
{
    public string Name { get; set; } = "MarkRead";

    public void OpenDocument(string filePath)
    {
        Console.WriteLine($"Opening: {filePath}");
    }
}
```

---

## 🖼️ Local Relative Image Test

Below is a local PNG file (`diagram.png`) located right next to this markdown file:

![Sample Diagram](diagram.png)

---

## 📝 Blockquote Example

> "Simplicity is prerequisite for reliability."  
> — *Edsger W. Dijkstra*

---

💡 *Tip: Press `Ctrl+F` to search inside this document, or `Ctrl+U` to switch to raw Markdown text mode!*
