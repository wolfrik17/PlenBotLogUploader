# PlenBot Log Uploader v104 (Cross-Platform Avalonia UI)

Welcome to **PlenBot Log Uploader v104**! This major milestone release marks the complete cross-platform transition of PlenBot Log Uploader from Windows-only WinForms to **Avalonia UI (.NET 8)**, adding first-class support for **Linux (including Steam Deck)** and **Windows**.

---

### 🚀 Major Highlights

#### 1. Cross-Platform Avalonia UI (.NET 8)
- Fully ported away from legacy WinForms (`net8.0-windows`) to **Avalonia UI 11.1** running on **.NET 8**.
- Native cross-platform execution on both **Windows (x64)** and **Linux (x64)**.
- Clean separation of business logic into dedicated services (`LogUploaderService`, `DiscordWebhookService`, `StartupManager`, `ProcessHelper`).

#### 2. Redesigned Modern User Experience
- **Sidebar Navigation**: Fast switching between Dashboard, Integrations, Log Sessions, arcDPS Plugins, Teams & GW2, and Settings.
- **Fluent Theme Support**: Seamless support for **System**, **Dark**, and **Light** themes.
- **Interactive Dashboard**:
  - Live stat cards: **Total Logs Scanned**, **Session Uploads**, and **Concurrent Limit**.
  - **Drag & Drop Upload Zone**: Drag `.evtc` or `.zevtc` log files directly onto the window for instant uploading to dps.report.
  - Recent uploads table with boss icons, duration, status, and one-click copy/open links.
  - Live Activity Console stream displaying real-time background logs and upload progress.
- **Modern Dialogs**: Clean, validation-backed modal dialogs for Discord Webhooks, Teams, Remote Pings, Aleeva Integrations, GW2 API Keys, and dps.report User Tokens.

#### 3. Linux & Steam Deck Support
- **Standalone Linux AppImage**: Self-contained executable (`PlenBotLogUploader-x86_64.AppImage`) running out-of-the-box on Steam Deck, Ubuntu, Fedora, Arch, and other distributions without needing .NET installed.
- **Writable Configuration Path**: Automatically uses `~/.config/PlenBotLogUploader` on Linux to ensure settings, boss caches, and logs persist properly outside of read-only mount points.
- **Cross-Platform Startup**: Toggle "Launch at system startup" works across Windows (Registry Run key) and Linux (`~/.config/autostart` desktop entry).

#### 4. Smart Log Monitoring & Auto-Detection
- **Auto-Detection for arcDPS Logs**: Automatically discovers arcDPS combat log folders on application launch:
  - **Linux / Steam Deck**: Probes Steam Proton prefix (`1284210`), non-Steam shortcuts under `compatdata`, Flatpak Steam, Lutris prefixes (`~/Games/guild-wars-2`), and Bottles prefixes.
  - **Windows**: Checks standard Documents (`Documents\Guild Wars 2\addons\arcdps\arcdps.cbtlogs`) and OneDrive document paths.
- **Interactive Start Monitoring**: Clicking "Start Monitoring" when no folder is configured now automatically prompts you to choose your combat logs folder and begins monitoring immediately once selected.
- **Seamless Folder Setup**: Browsing and selecting a folder immediately starts log monitoring and triggers a background scan of existing logs.
- **Robust Observer**:
  - Case-insensitive extension matching (`.evtc`, `.zevtc`).
  - Listens to both `Created` and `Renamed` filesystem events to support atomic writes from Wine/Proton and native filesystems.
  - Automatic fallback to polling observer mode if `FileSystemWatcher` encounters inotify limitations.

---

### 🐛 Bugfixes Included in this Release

- **AppImage Startup Crash**: Resolved null reference and read-only filesystem errors inside the AppImage sandbox by lazily initializing `ApplicationSettings.Current` and redirecting `LocalDir` to user configuration storage.
- **Teams & GW2 View Crash**: Resolved unhandled XAML reflection type casting crash in DataGrid templates when viewing or editing Teams.
- **Button Labels Showing "False"**: Replaced invalid boolean string format bindings with dynamic ViewModel properties (`MonitoringButtonText`, `SessionButtonText`, `SessionPauseButtonText`, `TwitchConnectButtonText`).
- **Monitoring Feedback**: Fixed "Start Monitoring" doing nothing when unconfigured; added real-time visual status badge colors (**Active: Green**, **Stopped: Gray**) and an alert banner on the Dashboard.
- **UI Thread Safety**: All background service notifications, log scans, and monitoring state updates are safely dispatched to Avalonia's UI thread (`Dispatcher.UIThread`).

---

### 📦 Available Release Assets

| Asset | Platform | Description |
|---|---|---|
| **`PlenBotLogUploader-x86_64.AppImage`** | Linux (x64) | **Recommended for Linux / Steam Deck**. Standalone executable AppImage. |
| **`PlenBotLogUploader-linux-x64.tar.gz`** | Linux (x64) | Portable tarball containing the self-contained Linux binary. |
| **`PlenBotLogUploader-win-x64.exe`** | Windows (x64) | **Recommended for Windows**. Standalone single-file executable (no .NET required). |
| **`PlenBotLogUploader-win-x64-framework.exe`** | Windows (x64) | Lightweight executable (requires .NET 8 Desktop Runtime). |
| **`PlenBotLogUploader-win-x64-dlls.zip`** | Windows (x64) | Portable zip package with all application DLLs. |

---

### 🛠️ Quick Start (Linux / Steam Deck)

```bash
chmod +x PlenBotLogUploader-x86_64.AppImage
./PlenBotLogUploader-x86_64.AppImage
```
