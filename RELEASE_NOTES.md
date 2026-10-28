# PlenBot Log Uploader v105 (Modern UI & UX Overhaul)

Welcome to **PlenBot Log Uploader v105**! This release introduces a comprehensive **UI and UX overhaul**, delivering a sleek, modern visual aesthetic with consistent rounded corners, real-time status feedback, refined interactions, and polished modal dialogs across Linux (Steam Deck) and Windows.

---

### 🎨 Major UI & UX Improvements

#### 1. Unified Modern Design System & Rounded Corners
- **Consistent Rounded Corners**: Applied a cohesive border radius hierarchy throughout the application:
  - **Cards & Containers**: Smooth `10px` rounded corners with subtle borders and adaptive theme backgrounds.
  - **Buttons**: Consistent `6-8px` rounded corners with styled button classes (`Button.accent`, `Button.primary`, `Button.success`, `Button.danger`, `Button.compact`).
  - **Inputs & Dropdowns**: Rounded `6px` styling on all `TextBox`, `ComboBox`, and `NumericUpDown` controls.
  - **Status Badges & Pills**: Smooth `12-16px` pill designs with bold typography.
  - **Data Grids**: Rounded table containers with padded rows and styled headers.
- **Adaptive Theme Palette**: Harmonious design tokens (`BrandPrimary`, `BrandSuccess`, `BrandDanger`, `BrandWarning`, `BrandPurple`) optimized for both **Dark** and **Light** modes.

#### 2. Enhanced Top App Bar & Navigation
- **Live Status Badges**: Real-time status indicators in the header bar:
  - **● Monitoring Active** (Emerald green) / **○ Monitoring Stopped** (Slate neutral).
  - **⏱ Session Active** / **⏸ Session Paused** (Amber) with automatic visibility toggling.
  - **🟣 Twitch Connected** (Purple) when Twitch chatbot is active.
- **Header Actions**: Quick-action buttons with rounded corners and distinct active states for monitoring and raid sessions.
- **Modern Sidebar**:
  - Padded navigation items with rounded pill selection states.
  - Clear icon-label pairings for Dashboard, Integrations, Log Sessions, arcDPS Plugins, Teams & GW2, and Settings.
  - Sidebar footer featuring the application version and a one-click **GitHub Repository ↗** link.

#### 3. Interactive Dashboard & Activity Console
- **Unified Metric Cards**: 4 side-by-side cards highlighting **Total Scanned**, **Session Uploads**, **Concurrent Limit**, and an interactive **Drag & Drop Upload Zone** with a dashed accent border.
- **Logs Folder Management**: Streamlined folder banner with browse and folder-open shortcuts, accompanied by an actionable alert banner when unconfigured.
- **Recent Uploads Table**:
  - Friendly illustrated empty state when no fights have been uploaded yet in the session.
  - **Dynamic Result Badges**: Emerald green pill for `SUCCESS`, rose red pill for `FAIL`.
  - **Encounter Mode Badges**: Purple for `LCM`, amber for `CM`, slate for `Normal`.
  - Compact rounded `Open` and `Copy` action buttons.
- **Terminal-Style Activity Log**:
  - Carbon/dark terminal theme with macOS-inspired window controls (🔴 🟡 🟢).
  - Color-coded log level icons (`✔` Success, `⚠` Warning, `✖` Error, `ℹ` Info) with muted timestamps.
  - **Auto-Scroll to Bottom**: Automatically follows new incoming logs so you never miss background upload activity.

#### 4. Polished Integrations & Settings
- **Segmented Subtabs**: Modern rounded tabs for Discord Webhooks, Twitch Chat, Aleeva, GW2Bot, Remote Pings, and dps.report & Wingman.
- **Responsive Empty States**: Helpful prompts and "+ Add" shortcuts when no webhooks, teams, or tokens are configured.
- **Danger Actions**: Red-tinted delete buttons to prevent accidental removals.
- **Grouped Settings**: Neatly categorized cards for System & Startup, Performance, Appearance, and Data Troubleshooting.

#### 5. Redesigned Dialog Windows
All 6 modal dialog windows have been redesigned with a standardized card structure, informative header banners, rounded inputs, and primary accent `Save` buttons:
- **Discord Webhook Dialog**: Grouped sections for basic info, embed formats, and encounter mode filters.
- **Aleeva Integration Dialog**: Server, channel, and notification settings card.
- **Team Filter Dialog**: Team name, limiter filter mode, and descriptive hints.
- **Remote Ping Dialog**: Target REST URL, HTTP method, and JSON format toggles.
- **Guild Wars 2 API Dialog**: Account label and API key configuration.
- **dps.report User Token Dialog**: Account label, user token, and active status toggle.

---

### 📦 Available Release Assets

| Asset | Platform | Description |
|---|---|---|
| **`PlenBotLogUploader-x86_64.AppImage`** | Linux (x64) | **Recommended for Linux / Steam Deck**. Standalone executable AppImage. |
| **`PlenBotLogUploader-linux-x64.tar.gz`** | Linux (x64) | Portable tarball containing the self-contained Linux binary. |
| **`PlenBotLogUploader-win-x64.exe`** | Windows (x64) | **Recommended for Windows**. Standalone single-file executable (no .NET required). |
| **`PlenBotLogUploader-win-x64-framework.exe`** | Windows (x64) | Lightweight executable (requires .NET 8 Desktop Runtime). |
| **`PlenBotLogUploader-win-x64-dlls.zip`** | Windows (x64) | Portable zip package with all application DLLs. |
