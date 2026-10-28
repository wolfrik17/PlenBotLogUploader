# PlenBot Log Uploader (Cross-Platform)

[![Build and test](https://github.com/wolfrik17/PlenBotLogUploader/actions/workflows/build-and-test.yml/badge.svg?branch=main)](https://github.com/wolfrik17/PlenBotLogUploader/actions/workflows/build-and-test.yml)

The all-in-one cross-platform solution for all your Guild Wars 2 arcDPS logs, now ported from Windows Forms to **Avalonia UI (.NET 8)** to run natively on both **Windows** and **Linux** (including Steam Deck / desktop Linux gaming setups).

---

## Features

- **Cross-Platform Native UI**: Modern Fluent interface built on Avalonia UI with dark/light/system theme variants and system tray support.
- **Automated arcDPS Log Uploads**: Automatically uploads `.evtc` and `.zevtc` logs to `dps.report` (supports user tokens and server selection) and `Gw2Wingman`.
- **Drag & Drop**: Drag `.evtc` / `.zevtc` log files or folders directly into the app dashboard for instant upload.
- **Discord Webhooks**: Rich Discord embed summaries for PvE encounters and WvW fights (squad breakdown, enemy targets, top damage, cleanses, strips).
- **Log Sessions**: Group raid, strike, or fractal clears into a unified session summary with custom names and announcements.
- **Twitch Chatbot Integration**: Automatically announces combat results in Twitch chat (via default `gw2loguploader` or custom bot account) with customizable chat commands (`!uploader`, `!lastlog`, `!song`, `!build`, `!ign`).
- **Aleeva & GW2Bot**: Integrated webhook and notification forwarding directly to Aleeva Discord server channels and GW2Bot.
- **Custom Remote Pings**: Send HTTP webhooks (POST/PUT/PATCH/GET) with JSON payloads to your own REST endpoints or web services.
- **Teams & Filter Rules**: Filter notifications and webhooks based on specific account names, character names, or squad presence.
- **arcDPS Plugin Manager**: Automatic updates and installation for arcDPS and popular plugins (Mechanics, Boon Table, Killproof.me, Healing Stats, Clears, Scrolling Combat Text, Food Reminder, Commander's Toolkit, Blish HUD bridge).
- **Cross-Platform Autostart**: Seamless autostart on system boot for both Windows (Registry `Run` key) and Linux (`~/.config/autostart/*.desktop`).

---

## Downloads & Installation

Pre-built releases for Windows and Linux are available under the **[Releases](https://github.com/wolfrik17/PlenBotLogUploader/releases)** page.

### Linux

1. Download `PlenBotLogUploader-x86_64.AppImage` (or `PlenBotLogUploader-linux-x64.tar.gz`).
2. Make the file executable:
   ```bash
   chmod +x PlenBotLogUploader-x86_64.AppImage
   ```
3. Run it:
   ```bash
   ./PlenBotLogUploader-x86_64.AppImage
   ```

#### Linux & Steam Deck (Proton / Wine) Log Monitoring:
If playing Guild Wars 2 through Steam Proton, Lutris, or Bottles, your arcDPS combat logs are located inside the Proton prefix. For example:
- **Steam**:
  `~/.local/share/Steam/steamapps/compatdata/1284210/pfx/drive_c/users/steamuser/Documents/Guild Wars 2/addons/arcdps/arcdps.txt` / combat logs folder
- **Lutris / Bottles**:
  `<prefix_path>/drive_c/users/<user>/Documents/Guild Wars 2/addons/arcdps/arcdps.txt`

Select this directory in PlenBot Log Uploader via the **Dashboard** folder picker. If running on networked or Wine virtual drives, enable the **"Use Polling"** option under **Settings** for reliable detection.

### Windows

1. Download `PlenBotLogUploader-win-x64.exe` (self-contained, no prerequisites required) or `PlenBotLogUploader-win-x64-framework.exe` (requires .NET 8 Desktop Runtime).
2. Run the executable.

---

## Configuration & Data Files

All configurations are stored as clean JSON files inside the application directory (or local data directory):

| File | Purpose |
|---|---|
| `app_settings.json` | General application, upload, Twitch, Aleeva, and theme settings |
| `boss_data.json` | Encounter definitions, custom success/failure templates, and Discord icons |
| `discord_webhooks.json` | Configured Discord webhook endpoints and notification options |
| `teams.json` | Player teams and limiter rules |
| `remote_pings.json` | Remote REST ping configurations and authentication |
| `aleeva_integrations.json` | Configured Aleeva Discord servers and channels |
| `twitch_commands.json` | Custom and default Twitch bot commands |
| `uploaded_logs.csv` | Record of all uploaded logs with permalinks and metadata |

To reset any setting or remove an integration, either manage it through the application UI or remove the corresponding `.json` file.

---

## Building from Source

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (or newer)
- Linux build dependencies (for AppImage packaging): `icoutils`, `file`, `libfuse2`

### Build

```bash
# Clone the repository
git clone https://github.com/wolfrik17/PlenBotLogUploader.git
cd PlenBotLogUploader

# Build Debug
dotnet build PlenBotLogUploader.csproj

# Publish Linux Self-Contained Binary
dotnet publish PlenBotLogUploader.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true

# Publish Windows Self-Contained Binary
dotnet publish PlenBotLogUploader.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### Packaging Linux AppImage

Run the build script in `packaging/linux`:
```bash
dotnet publish PlenBotLogUploader.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o artifacts/publish/linux-x64
bash packaging/linux/build-appimage.sh artifacts/publish/linux-x64 artifacts/appimage
```

---

## Continuous Integration & Releases

Automated builds and tests are run via GitHub Actions on every push and pull request:
- **`build-and-test.yml`**: Compiles and tests both Windows (x64 single file & portable zip) and Linux (self-contained binary, tar.gz, and AppImage).
- **`release-uploader.yml`**: Automatically generates GitHub Releases with Windows and Linux assets whenever a tag matching `Release-*` or `v*` is pushed.

---

## Acknowledgments & Credits

- Original project created by **Plenyx** and contributed to by **sobrinth**.
- Cross-platform port powered by **[Avalonia UI](https://avaloniaui.net/)**.
