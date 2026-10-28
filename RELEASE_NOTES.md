# PlenBot Log Uploader v106 (Performance & Optimization Overhaul)

Welcome to **PlenBot Log Uploader v106**! This release delivers a comprehensive pass across the entire codebase targeting **performance optimizations, algorithmic bottlenecks, concurrency deadlocks, memory & unmanaged handle leaks, and critical integration reliability**.

---

### ⚡ Performance & Algorithmic Optimizations

#### 1. $O(1)$ Boss & Encounter Lookup Index
- Replaced the linear list scan (`All.Find(...)`) with a cached, high-speed dictionary lookup index (`Dictionary<int, BossData>`) synchronized on data load/save.
- Every encounter query throughout log uploads, webhook payloads, Twitch bot commands, and raid session construction is now instantaneous $O(1)$.

#### 2. Log Session Builder Optimization
- Eliminated redundant multi-pass scans over the entire boss database during session embed construction.
- Replaced expensive heap allocations from repeatedly converting multi-kilobyte `StringBuilder` buffers to strings with a zero-allocation char inspection.
- Cached session embeds across Discord webhooks that share identical squad filtering criteria, eliminating duplicate table construction and Markdown formatting.

#### 3. Single-Pass WvW Squad & Enemy Statistics
- Replaced 5 consecutive LINQ iterations over squad players with a single-pass loop calculating squad players, damage, DPS, downs, and deaths simultaneously.
- Fixed integer division truncation in the WvW Kill/Death Ratio calculation.
- Added bounds and null safety guards across combat player defense and stats target collections.

#### 4. Team Condition Short-Circuiting
- Team evaluations (`AND` and `NOT`) now short-circuit immediately via `.All(...)` and `.Any(...)`, eliminating wasted condition checks once the result is determined.
- Member counts are evaluated lazily only when required by counting limiters (`Exact`, `Except`, `AtLeast`), skipping unused computations for logical limiters.

---

### 🛡️ Concurrency, Thread Safety & Memory Leaks

#### 1. Retry Queue Deadlock & Thread Safety Resolution
- **Critical Deadlock Fix**: In `LogReuploader.ProcessLogs`, an outer semaphore acquisition blocked `UploadLogFileAsync` from acquiring its internal semaphore permit, resulting in a permanent deadlock when `MaxConcurrentUploads` was set to 1. Concurrency throttling is now managed uniformly by `UploadLogFileAsync`.
- Added thread-safe locking and atomic operations to `FailedLogs` to prevent concurrent modification exceptions and race conditions during simultaneous background uploads.
- Resolved collection modification exceptions during retry iterations.

#### 2. Responsive Log Observer Polling
- Replaced non-cancellable `Thread.Sleep(30000)` loops with responsive `WaitHandle.WaitOne(30000)` cancellations so stopping monitoring or switching observer modes responds instantly.
- Replaced double-lookup HashSet operations with a single atomic check and case-insensitive path comparisons.

#### 3. Operating System Process Handle Leaks
- `Process.GetProcesses()` returns unmanaged OS process handles that must be deterministically released. Added `try-finally` disposal blocks in `IsStreamingSoftwareRunning()` and `Program.cs` to eliminate process handle leaks during active streaming detection.
- Cached process filter definitions and eliminated repetitive string lowercase allocations.

#### 4. HTTP Stream & Socket Connection Disposal
- Added deterministic `using var response = ...` disposal across Discord webhooks and Aleeva HTTP requests to ensure TCP sockets and streams are immediately returned to the connection pool.

---

### 🐛 Critical Bug Fixes & Improvements

- **Aleeva Integration Without Teams**: Fixed an issue where integrations without team filters evaluated to `false` and were discarded. Aleeva integrations now correctly execute for all squad logs when no specific team limiter is selected.
- **Remote Ping Test Crash**: Fixed a `NullReferenceException` thrown when testing a remote ping with no active log selected. Added null-conditional safety checks across ping authenticators and permalink formatters.
- **Twitch Chat Regex Deserialization**: Fixed a JSON deserialization ordering issue where `Command` was assigned before `IsRegEx`, causing compiled regex patterns to remain null. Regex properties are now compiled lazily on-demand.
- **Twitch Command Matching**: Replaced multi-array allocations with a single allocation-free loop and case-insensitive prefix comparisons.
- **ArcDPS Component Updater**: Resolved an issue where component checks without a cached release retrieved release metadata but skipped downloading the new DLL.
- **Safe Permalinks**: Protected `UrlId` and `Emboldened` in `DpsReportJson` from throwing `ArgumentOutOfRangeException` or `NullReferenceException` on non-standard links.
- **Granular UI Updates**: Replaced full-table redraws across all 8 collections with granular collection refresh methods (`RefreshWebhooks`, `RefreshTeams`, `RefreshRemotePings`, etc.), preventing UI flickering and maintaining scroll positions.
- **UI Thread Safety**: Ensured background tasks dispatch collection mutations through Avalonia's `Dispatcher.UIThread`.

---

### 📦 Available Release Assets

| Asset | Platform | Description |
|---|---|---|
| `PlenBotLogUploader-x86_64.AppImage` | Linux (x64) | **Recommended for Linux / Steam Deck**. Self-contained executable with embedded runtime. |
| `PlenBotLogUploader-linux-x64.tar.gz` | Linux (x64) | Portable tarball containing the standalone Linux binary. |
| `PlenBotLogUploader-win-x64.exe` | Windows (x64) | **Recommended for Windows**. Standalone self-contained single-file executable. |
| `PlenBotLogUploader-win-x64-framework.exe` | Windows (x64) | Lightweight executable (requires .NET 8 Desktop Runtime installed). |
| `PlenBotLogUploader-win-x64-dlls.zip` | Windows (x64) | Portable zip package with all libraries for custom setups. |
