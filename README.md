# MarkRead 📖

<div align="center">

[![.NET 8](https://img.shields.io/badge/.NET-8.0_WPF-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![WebView2](https://img.shields.io/badge/Microsoft-WebView2-0078D4?logo=microsoftedge&logoColor=white)](https://developer.microsoft.com/en-us/microsoft-edge/webview2/)
[![React 19](https://img.shields.io/badge/React-19-61DAFB?logo=react&logoColor=black)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.x-3178C6?logo=typescript&logoColor=white)](https://www.typescriptlang.org/)
[![Vite](https://img.shields.io/badge/Vite-8.x-646CFF?logo=vite&logoColor=white)](https://vitejs.dev/)
[![License: Non-Commercial](https://img.shields.io/badge/License-Non--Commercial-orange.svg)](./LICENSE)

**A high-performance, document-grade Markdown viewer and companion web app designed for readability, speed, and seamless Windows integration.**

[Desktop Features](#-desktop-features) • [Web Application](#-web-application) • [Quick Start](#-quick-start) • [Shortcuts](#-keyboard-shortcuts) • [Project Structure](#-project-structure) • [Build & Development](#-build--development) • [License](#-license)

</div>

---

## 📖 Overview

**MarkRead** is a modern Markdown reading and rendering suite created by [Mahmoud Abdalla](https://github.com/mahmoud-abdalla-eg). 

It provides both:
1. **MarkRead Desktop**: A native Windows application written in **C# .NET 8 WPF** with **Microsoft WebView2** and **Markdig**, delivering book-grade reading typography, tabbed document navigation, publication PDF export, and native Windows Explorer right-click integration.
2. **MarkRead Web**: A lightweight, lightning-fast browser companion built with **React 19**, **TypeScript**, and **Vite**.

---

## ✨ Desktop Features

- **Document-Grade Reading Typography**: Clean, comfortable formatting with the *mdtopdf.pro Neutral Zinc Dark Theme* (charcoal/zinc aesthetic without blue glare) and responsive light/dark themes.
- **GitHub Flavored Markdown (GFM)**: Full support for syntax highlighting, 1-click code block copying, pipe tables, task checklists, strikethrough, blockquotes, and math.
- **Tabs & Single Instance IPC**: Opening multiple files from Windows Explorer organizes them neatly in tabs within the same active window.
- **Raw Mode (`Ctrl + U`)**: Instantly toggle between beautifully rendered Markdown and raw Markdown source view.
- **Publication PDF Export & Print (`Ctrl + P`)**: High-fidelity print dialog with customizable page sizes, margins, page breaks, and preview.
- **In-Page Search (`Ctrl + F`)**: Real-time keyword search with match highlighting and rapid jumping between results.
- **Live File Auto-Reload**: Modifying open documents externally in editors like VS Code or Notepad immediately reloads the view without resetting your scroll position.
- **Local Relative Image Rendering**: Automatically loads relative images (e.g., `./diagram.png` or `assets/image.png`).
- **Comprehensive Settings**: Full configuration for default font size, document reading width, color theme, PDF layout defaults, and file associations.
- **Native Windows Explorer Integration**:
  - Direct right-click context menu: **"Open with MarkRead"**.
  - Default file association helper for `.md` and `.markdown`.
  - Desktop shortcut and Windows taskbar pinning support.

---

## 🌐 Web Application

The repository includes **MarkRead Web** (`markread-web/`), a modern web-based document reader:
- **Stack**: React 19, TypeScript, Vite, Modern CSS.
- **Speed**: Instant local dev server with hot module replacement.
- **Cross-Platform**: Run MarkRead on any browser or operating system.

---

## 🚀 Quick Start

### Running MarkRead Desktop
1. Clone the repository:
   ```bash
   git clone https://github.com/mahmoud-abdalla-eg/MarkRead.git
   cd MarkRead
   ```
2. Double-click **`Launch MarkRead.bat`** (or run `.\build.bat` once).  
   *If the application hasn't been built yet, the launcher will automatically compile it for you using the .NET 8 SDK.*

### Running MarkRead Web
1. Double-click **`Launch MarkRead Web.bat`** (or navigate to `markread-web` and run `npm run dev`).
2. Your browser will automatically open to `http://localhost:5173`.

### Windows Explorer Setup (Optional)
Inside the [`scripts/`](./scripts/) folder:
- **`Register Context Menu.bat`**: Adds **"Open with MarkRead"** to Windows File Explorer right-click menu and creates a desktop shortcut.
- **`Make Default Viewer.bat`**: Opens the Windows system app picker to set MarkRead as the default viewer for `.md` files.
- **`Enable Full Right-Click Menu.bat`**: (Windows 11) Restores classic direct context menu so "Open with MarkRead" appears on the very first click without clicking "Show more options".
- **`Restore Windows 11 Menu.bat`**: Reverts Windows 11 context menu back to default modern styling.
- **`Unregister Context Menu.bat`**: Cleanly removes all context menus and registry entries.

---

## ⌨️ Keyboard Shortcuts

| Shortcut | Action |
| :--- | :--- |
| <kbd>Ctrl</kbd> + <kbd>O</kbd> | Open Markdown document |
| <kbd>Ctrl</kbd> + <kbd>T</kbd> | Open new tab / document |
| <kbd>Ctrl</kbd> + <kbd>W</kbd> | Close current tab |
| <kbd>Ctrl</kbd> + <kbd>U</kbd> | Toggle Raw Markdown / Rendered Document mode |
| <kbd>Ctrl</kbd> + <kbd>P</kbd> | Export to PDF / Print dialog |
| <kbd>Ctrl</kbd> + <kbd>F</kbd> | Open in-page search bar |
| <kbd>F5</kbd> | Reload current document |
| <kbd>Esc</kbd> | Close search bar or active dialog |

---

## 📁 Project Structure

```
MarkRead/
├── Launch MarkRead.bat        # 1-click desktop launcher (with auto-build)
├── Launch MarkRead Web.bat    # 1-click web companion launcher
├── build.bat                  # 1-click build pipeline for Desktop & Web
├── LICENSE                    # Custom Non-Commercial License
├── README.md                  # Project documentation
├── .gitignore                 # Clean repository ignore configuration
│
├── MarkRead/                  # 🖥️ Desktop WPF Project (.NET 8)
│   ├── Assets/                # App icon, reader stylesheet, JS bridge
│   ├── Models/                # Settings and tab document models
│   ├── Services/              # Markdig renderer & Windows Shell integration
│   ├── MainWindow.xaml(.cs)   # Primary window with tab management & WebView2
│   ├── PdfExportWindow.xaml   # Publication PDF export preview & print engine
│   ├── SettingsWindow.xaml    # User settings and theme configuration
│   └── MarkRead.csproj        # Project configuration
│
├── markread-web/              # 🌐 Web Companion Project (React + Vite)
│   ├── src/                   # React components and Markdown renderers
│   ├── public/                # Web assets and icons
│   ├── package.json           # Node project configuration
│   └── vite.config.ts         # Vite build configuration
│
├── scripts/                   # 🛠️ Windows Integration & Shell Scripts
│   ├── Register Context Menu.bat
│   ├── Unregister Context Menu.bat
│   ├── Make Default Viewer.bat
│   ├── Enable Full Right-Click Menu.bat
│   └── Restore Windows 11 Menu.bat
│
└── examples/                  # 📄 Sample Markdown Documents
    ├── sample_document.md     # Rich sample document showing all formatting
    └── diagram.png            # Relative image test asset
```

---

## 🔨 Build & Development

### Prerequisites
- **Desktop**: [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and Windows 10/11
- **Web**: [Node.js](https://nodejs.org/) (v18+ recommended)

### Build Desktop from Terminal
```bash
dotnet publish MarkRead/MarkRead.csproj -c Release -r win-x64 --self-contained false -o MarkRead/bin/Release/net8.0-windows/win-x64/publish
```

### Build Web from Terminal
```bash
cd markread-web
npm install
npm run build
```

---

## 📜 License

This project is released under the **MarkRead Non-Commercial License**.

- ✅ **Free for Personal Use**: Anyone is free to download, use, learn from, fork, and modify MarkRead for personal, educational, research, and non-commercial community purposes.
- ❌ **No Commercial Resale**: You may **not** sell, resell, monetize, license for a fee, or distribute MarkRead or derivative works for commercial gain.

See full terms in the [LICENSE](./LICENSE) file.

---

<div align="center">
Developed by <b>Mahmoud Abdalla</b> &bull; Built with passion for clean reading.
</div>
