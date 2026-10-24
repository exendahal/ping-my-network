# Ping My Network — Product Requirements Document

| | |
|---|---|
| **Status** | Draft v0.2 |
| **Author** | Santosh Dahal |
| **Date** | 2026-10-24 |
| **Stack** | .NET 10 MAUI (Android, iOS, Windows, Mac Catalyst) |
| **Core dependency** | [`WifiManager.Maui`](https://www.nuget.org/packages/WifiManager.Maui/) (maui_wifi_manager) |
| **Reference app** | `MyIndoorHealth/sill_app` — navigation shell, motion system and shared controls |
| **License** | MIT |

---

## 1. Summary

Ping My Network is a free, open-source network utility in the spirit of Fing. It answers the everyday questions people ask about their network: *Who is on my Wi-Fi? Is my internet actually working? Why is it slow? Which channel should my router use?*

It has a second job: **it is the flagship app for `maui_wifi_manager`**. Every screen is built on the library's public API, so the app shows .NET MAUI developers what the library can do on each platform, with production-quality code they can copy.

**This is a full product, not an MVP.** Everything in §6 is in scope for v1.0. The phases in §12 set the build order; they do not cut scope.

## 2. Product principles (in priority order)

1. **Performance comes first.** When performance conflicts with a feature, animation or abstraction, performance wins. The budgets in §8 are release gates, not aspirations.
2. **Crafted, not vibe-coded.** The app must look and behave like a designed product made by one careful team (§9). Template defaults, inconsistent spacing, generic AI-styled UI and copy-pasted boilerplate are defects.
3. **Honest about the platform.** When a platform can't do something, the app says why in one sentence. It never hides a feature without explanation and never fails silently.
4. **Private by default.** No ads, no account, no telemetry. Data stays on the device.
5. **Showcase through real use.** The library is shown by a genuinely useful app, not a demo page of buttons.

## 3. Goals

### G1 — The best free network utility on mobile and desktop
- Matches what people actually use Fing for: device list, device details, speed test, ping, Wi-Fi info, and alerts for new devices.
- Adds tools Fing keeps behind a paywall or doesn't offer: channel analyzer, mDNS browser, port check, Wake-on-LAN, traceroute, DNS lookup, IoT setup mode.
- Free forever, open source, works offline on the LAN.

### G2 — The definitive showcase for maui_wifi_manager
- Uses **every** library feature (coverage map in §7).
- The Capabilities screen shows on the device itself what the library supports there.
- The codebase is a reference for how to build a fast, well-designed MAUI app with the library.
- Gaps found while building the app are fixed in the library first, then used in the app.

### Non-goals
- Cloud accounts, an app backend, and remote monitoring of networks you aren't on. (Backup and sync use the user's own cloud storage instead, §6.16.)
- A crowd-sourced device fingerprint database (we show what each device reveals about itself).
- Blocking devices or changing router settings (that needs router APIs we can't rely on).
- A Linux build (MAUI doesn't support it).

## 4. Target users

| Persona | Need | Key screens |
|---|---|---|
| **Home user** | "Is someone on my Wi-Fi?" "Why is streaming buffering?" | Home, Devices, Speed test |
| **Power user / IT helper** | Find a printer's IP, check ports, pick the best channel, fix a friend's network | Device details, Channels, Tools |
| **IoT / maker** | Find an ESP32 on the LAN, join a device's setup AP, provision it | Devices, IoT setup, Hotspot |
| **.NET MAUI developer** | "Can the library do X on platform Y, and how?" | Capabilities, source code |

## 5. Platform support

| Area | Android | iOS | Windows | Mac Catalyst |
|---|---|---|---|---|
| Current network info | ✅ full (RSSI, link speed) | ✅ partial (no RSSI/band) | ✅ | ⚠️ needs testing |
| Nearby Wi-Fi scan / channel analyzer | ✅ (OS limit: 4 scans per 2 min) | ❌ | ✅ | ❌ |
| LAN devices + details | ✅ (MAC via NetBIOS only) | ✅ (MAC via NetBIOS only) | ✅ (MAC via ARP) | ⚠️ |
| mDNS / Bonjour | ✅ | ⚠️ multicast entitlement | ✅ | ⚠️ |
| Ping / speed test / internet check | ✅ | ✅ | ✅ | ✅ |
| Hotspot | ✅ local-only | ❌ | ✅ shares internet | ❌ |
| New-device alerts in the background | ✅ WorkManager (≥ 15 min) | ⚠️ best-effort + on app open | ✅ background task (≥ 15 min) | ⚠️ on app open |
| Home-screen widget | ✅ | ✅ (WidgetKit) | ❌ | ❌ |

All four platforms ship at v1.0. Android is the reference platform for performance budgets.

## 6. Features (all in v1.0)

### 6.1 Home
One-screen health check of the current connection.
- Connection type (Wi-Fi / cellular / Ethernet) with live updates.
- SSID, signal (bars, %, dBm), band, channel, Wi-Fi standard (4–7), link speed.
- Status: **Online** / **Captive portal — sign in** (opens the portal page) / **No internet** / **Wi-Fi off**.
- Metered badge.
- Tiles: device count (with a "new" count), last speed test, gateway latency.
- Quick actions: Scan devices, Speed test, Ping gateway.

*Library:* `GetConnectionTypesAsync`, `ConnectionTypeChanged`, `GetNetworkInfoAsync`, `WifiNetworkChanged`, `IsInternetAvailableAsync`, `IsCaptivePortalDetectedAsync`, `IsMeteredConnectionAsync`, `SignalStrengthChanged`, `IsWifiEnabledAsync`, `WifiStateChanged`, `OpenWifiSetting`.

### 6.2 Network details
Every value about the current network, each one copyable: IPv4, IPv6, subnet mask, prefix length, broadcast, gateway, DHCP server, DNS servers, BSSID, security type, public IP. "Share" exports it all as text.

*Library:* `GetNetworkInfoAsync` (`NetworkData`).

### 6.3 Devices
- Scan the subnet. Devices appear as they answer and are tagged "This device", "Gateway" or "New".
- Each row shows the device's name (custom → host → NetBIOS → UPnP model → IP), vendor/model, IP, type icon and online state.
- mDNS results are merged into the same list, so devices that ignore ping still appear when they advertise services.
- Sort by IP, name or last seen; filter by online, new or type; search.
- Devices are remembered per network (keyed by gateway BSSID), each with a custom name, a type/icon, notes, first-seen and last-seen times, and a **Known/Unknown** status (§6.11). Unknown devices are highlighted, can be filtered, and can be marked as known with a swipe.
- Pull to refresh, cancel at any time, scan progress shown.
- A one-line limits note: devices that don't answer ping or mDNS can't be found, and MAC addresses are hidden on Android 10+ and iOS.

*Library:* `DiscoverDevicesAsync`, `BrowseServicesAsync`.

### 6.4 Device details
- Host name, NetBIOS name, MAC (with a randomized-MAC flag), vendor, manufacturer/model (UPnP), open ports, web page title, advertised mDNS services, latency, first/last seen.
- Actions: Ping, Open web page, Port check, Wake-on-LAN (when the MAC is known), Traceroute, Copy, Rename, Change type, Forget.

*Library:* `GetDeviceDetailsAsync`, `PingAsync`.

### 6.5 Speed test
- Download, upload and latency, with a live gauge while it runs.
- Warns before running on a metered connection (the test uses about 30 MB).
- History per network, shown as a chart and a list.
- Custom endpoints in Settings.

*Library:* `MeasureSpeedAsync`, `SpeedTestOptions`.

### 6.6 Wi-Fi analyzer
The Wi-Fi tab answers "is my Wi-Fi set up well?" instead of starting with a raw list of networks. It has four sections, in this order: **Insights** · **Signal walk** · **Channels** · **All networks**.

What a nearby-network scan can and can't tell you sets the scope:
- It **can** show how many networks share each channel, how strong they are, which bands and access points of your network are visible, and the security of nearby networks.
- It **can't** measure how busy a channel actually is (airtime). Ten idle networks can matter less than one busy one, so every recommendation is labeled as an estimate.
- Android allows only 4 fresh scans per 2 minutes, and iOS doesn't allow scanning at all. Features that need to update quickly (Signal walk) use the **connected network's signal** instead, which doesn't use up the scan allowance.

#### 6.6.1 Insights (Android, Windows; partial on iOS)
A short list of findings about *your* network, each with a one-line explanation and one action. Only findings that apply are shown; if none apply, the screen says "No problems found" and shows the last scan time.

| Insight | Rule (summary) | Action |
|---|---|---|
| Crowded channel | Your channel has more overlapping networks (weighted by signal) than the best alternative by a clear margin | Open Channels with the suggestion |
| Wide channel in a crowded 2.4 GHz area | Your 2.4 GHz network uses 40 MHz and neighbors overlap (Android only, where channel width is known) | Explain how to switch to 20 MHz in router settings |
| On 2.4 GHz while 5/6 GHz is available | A 5/6 GHz access point of the same network (same SSID or a common variant like `-5G`) is visible with a usable signal | Explain the band difference; open Wi-Fi settings |
| Not on the strongest access point | Another BSSID of your network is clearly stronger (mesh / multiple APs) | Explain roaming; suggest reconnecting |
| Possible look-alike network | An open or differently secured network uses your network's name | Warn, with details of both networks |
| Weak security | Your network uses open, WEP or WPA (TKIP) | Recommend WPA2/WPA3 |
| Old Wi-Fi standard | Your router is Wi-Fi 4 or older | Informational |
| Weak signal where you are | Your signal is below −70 dBm (or 1 bar) | Start Signal walk |

On iOS, only the rules that don't need a scan run (security of the connected network), and the screen explains why the others aren't available.

#### 6.6.2 Signal walk (Android, Windows)
Finds weak spots by walking around while the app follows the connected network's signal.
- Big live readout (dBm and %, or bars on Windows) with a rolling chart, updated as fast as the platform reports it: Android on each RSSI change, Windows every 5 s (bars only, since Windows doesn't report dBm for the connected network).
- **Tag places**: tap "Living room", "Bedroom" or a custom name to save the current reading as that place. The summary ranks places from strong to weak: "Bedroom: weak (−79 dBm). Consider moving the router or adding a mesh node."
- Shows when the device **switches access point** (BSSID change) during the walk, so users can see mesh handoffs.
- Optional sound/haptic feedback that changes with signal strength, so the screen doesn't need watching while walking. The screen stays awake during a walk.
- Walks are saved per network in History (§6.12) and can be compared later ("Bedroom improved from −79 to −61 dBm").
- iOS has no signal API, so this section explains that instead.

#### 6.6.3 Channels (Android, Windows)
- Per-band chart (2.4 / 5 / 6 GHz) of every network drawn as a curve across the channels it occupies, with height by signal; your network is highlighted.
- Suggestion in plain words: "Your router is on channel 6 with 9 overlapping networks. Channel 11 has 2." On 2.4 GHz only channels 1, 6 and 11 are suggested.
- Labeled as an **estimate based on nearby networks, not measured traffic**, with a reminder that many routers choose their channel automatically.

#### 6.6.4 All networks (Android, Windows)
- The full list for power users: SSID, BSSID, signal, band, channel, width, standard, security. Sort by signal, channel or name; filter by band; group access points by SSID.
- Networks appear and disappear as the library reports them.

#### 6.6.5 Scanning behavior (all sections)
- Always shows whether a fresh scan can run now ("Throttled — next fresh scan in 24 s", "Turn on location", "Wi-Fi is off").
- Uses `ScanResultsAvailable` to pick up scans the OS makes on its own, which don't use up the app's scan allowance.
- Mentions the Android developer option for turning off scan throttling, as a hint for power users.
- Scanning runs only while a Wi-Fi section is visible and stops when the user leaves it or the app goes to the background.

*Library:* `StartScanningForDevicesAsync`, `DeviceDiscovered`, `DeviceLost`, `StopScanningAsync`, `CanStartScanAsync`, `ScanResultsAvailable`, `ScanWifiNetworksAsync`, `WifiChannelUsage.FromScanResults`, `GetNetworkInfoAsync`, `SignalStrengthChanged`, `WifiSignal`, `WifiNetworkChanged`.

### 6.7 (merged into 6.6)
The channel analyzer is now §6.6.3.

### 6.8 Services (mDNS / Bonjour)
Service types on the LAN (printers, Chromecast, AirPlay, HomeKit, SMB…), the devices behind each one, and a link from each to its device details.

*Library:* `BrowseServicesAsync`.

### 6.9 Tools
| Tool | Notes | Library / source |
|---|---|---|
| Ping | Single or continuous, with a live latency chart, min/avg/max/jitter and loss | `PingAsync` |
| Traceroute | Hop list with latency per hop | Library addition (§7.1) |
| DNS lookup | A/AAAA/MX/TXT/CNAME, choice of resolver | Library addition |
| Port check | Common ports or a custom list/range for one host | Library addition (extends known-port check) |
| Wake-on-LAN | Send a magic packet to a saved device | Library addition |
| Subnet calculator | Offline calculator built from the current network | `BroadcastAddress`, `NetworkPrefixLength` |

### 6.10 Wi-Fi management
- **Connect**: SSID + password, BSSID, hidden networks, WPA3, connect by SSID prefix with a timeout. Also joins from a Wi-Fi QR code (`WIFI:` format).
- **Enterprise (802.1X)**: PEAP/TTLS/PWD form.
- **IoT setup mode**: join a device's setup AP as a local-only network, send an HTTP request to it (a small request builder), then release it.
- **Saved networks**: list, forget, add as a suggestion (Android).
- **Hotspot**: start/stop, share as a QR code, list clients (Windows).
- **Wi-Fi radio**: on/off where the platform allows it, otherwise opens system settings.
- **Share current Wi-Fi as QR code** (when the user enters the password).

*Library:* `ConnectWifiAsync`, `WifiConnectionOptions`, `ConnectEnterpriseWifiAsync`, `ConnectLocalOnlyAsync`, `ReleaseLocalOnlyAsync`, `GetSavedNetworksAsync`, `ForgetNetworkAsync`, `AddNetworkSuggestionAsync`, `StartHotspotAsync`, `GetHotspotClientsAsync`, `StopHotspotAsync`, `SetWifiEnabledAsync`, `DisconnectWifi`, `OpenWirelessSetting`.

### 6.11 New-device alerts (network watch)
Get notified when an unknown device joins a network you trust, and stop being notified once you mark it as known.

**Trusted networks**
- Alerts are only for networks the user marks as **My network** (for example home or office), never for every network the phone joins. The setting is offered after the first scan of a network.
- **Baseline:** when a network is first marked as trusted, every device found on it is treated as known, so turning alerts on doesn't produce a burst of notifications.

**Device status**
- Every device on a trusted network is either **Known** or **Unknown**. Devices found after the baseline start as Unknown.
- **Unknown device joins** → local notification: "New device on Home Wi-Fi: *Galaxy-S24* (192.168.1.37)". The notification has two actions, **Mark as known** (handled without opening the app) and **Details** (opens device details).
- **Mark as known** (from the notification, device details or a swipe action in the list) → that device never triggers a notification again. It can be set back to Unknown at any time.
- An Unknown device that is seen again isn't announced again. It stays highlighted in the list until the user acts on it. One alert per device, not one alert per scan.
- **Bulk actions**: "Mark all as known" and "Forget offline unknown devices".
- Optional, off by default: "Notify when a known device goes offline / comes back" per device (for example a camera or NAS).

**Device identity (matching a device across scans)**
A device is matched on the strongest identifier available, in this order: MAC → UPnP UDN → mDNS instance name → NetBIOS name → host name → IP (only when nothing else is known).
- A device that matches only by IP is marked **Unidentified**. The setting "Alert for unidentified devices" (on by default) lets users turn off alerts for these, since IP-only matching can produce false alerts when the router reassigns addresses.
- When a device's IP changes but a stronger identifier still matches, it stays the same device with the same name and status.

**When checks run**
| Platform | While the app is open | In the background |
|---|---|---|
| Android | Every 2 min | WorkManager periodic job, OS minimum 15 min (Doze can delay it), only on unmetered Wi-Fi to a trusted network |
| Windows | Every 2 min | Background task, 15 min |
| iOS / Mac | Every 2 min | Opportunistic background refresh, best-effort; plus a check every time the app opens |

- The settings screen states the interval, that alerts are not instant, and that they only work while this device is on that network.
- A background check is a single device scan with a strict time limit (≤ 30 s), with no mDNS browsing or device detail lookups. Each new device's details are fetched once, when its notification is created.
- The same check also reports **Internet down / back up** on trusted networks (optional).

**Data**: per device, `Status` (Known/Unknown), `IdentityConfidence` (Strong/Weak/IpOnly), `NotifyOnline/Offline` flags, `FirstSeen`, `LastSeen`, `LastNotified`. Every alert also creates an entry in History (§6.12).

### 6.12 History and export
- A timeline per network: devices that joined or left, speed tests, outages, signal walks.
- Export devices and history as CSV or JSON. Import is covered by §6.16.

### 6.13 Widget (Android, iOS)
Connection status, SSID, last speed result and device count, with a tap that opens the app. The widget reads a snapshot the app writes; it never runs scans itself.

### 6.14 Capabilities (showcase)
A live table of every library feature on this device: ✅ works / ❌ not on this platform / ⚠️ needs permission. Each row has a one-line reason and a link to the library README.

### 6.15 Settings
Theme (system/light/dark), speed test endpoints, internet check URL, network watch, notifications, backup and sync folder (§6.16), data export/import/clear, permissions status, About (version, library version, licenses, GitHub).

### 6.16 Backup and restore (no account, no server)
After a reinstall or on a new device, the user must not have to set up their trusted networks and known devices again. This uses the platforms' own backup and the user's own cloud storage. The app has no server and no sign-in.

**What is restored ("setup data")**: trusted networks, devices with their identity keys, custom names, types, notes and Known/Unknown status, per-device alert flags, and app settings. It's a few KB to a few hundred KB of JSON. History and speed results stay on the device (they're larger and lose value with age); they're included only in a manual export.

| Platform | Mechanism | Restores on |
|---|---|---|
| Android | **Auto Backup** to the user's Google Drive (up to 25 MB, doesn't use their Drive storage). `dataExtractionRules` / `fullBackupContent` include the database and settings and exclude caches. The database is checkpointed (WAL flushed) when the app goes to the background, so the backup is consistent. | Reinstall, new phone set up from a Google backup |
| iOS / Mac | **iCloud key-value store** (`NSUbiquitousKeyValueStore`, 1 MB limit) holds the setup data; the app's data folder is also part of the normal iCloud/device backup | Reinstall, new iPhone/iPad with the same Apple ID; also syncs live between the user's Apple devices |
| Windows | **Export/import** of a setup file, plus the optional sync folder below | New PC, reinstall |
| All (optional) | **Sync folder**: the user picks a folder (OneDrive, Google Drive, iCloud Drive, Syncthing…). The app writes `ping-my-network.setup.json` there after each change and merges it on start and when it changes | Moving between platforms (e.g. phone ↔ Windows PC) |

**Rules**
- **Restore is automatic.** On first launch, if restored setup data exists, the app uses it and says so once ("Restored 3 networks and 42 devices"). No wizard.
- **Merge, don't overwrite.** Devices are matched on their identity keys (§6.11). Each field keeps the most recently changed value, and a device marked Known on any device stays Known. Deleting a device is recorded as a tombstone so it doesn't come back.
- **Only user-made changes are synced** (rename, Known status, trusted network, settings). Scan results such as last seen, IP and latency are not synced, which keeps the data small and keeps writes rare.
- **Privacy**: data goes only to storage the user already owns (their Google Drive backup, iCloud, or a folder they chose). The setup file contains no Wi-Fi passwords. Settings has a switch to turn backup off and a "Delete backup data" action.
- **No third-party backend.** Firebase and similar services are not used: they would require accounts, conflict with §11, and are rejected by F-Droid.

## 7. Library coverage map

| Library feature | Screen |
|---|---|
| Connect (SSID, BSSID, hidden, WPA3, prefix, timeout) | Wi-Fi management |
| Network info, broadcast/prefix | Home, Network details, Subnet calculator |
| Internet availability, custom check URL | Home, Settings |
| Captive portal | Home |
| Wi-Fi network changed | Home, History |
| Scan / continuous discovery / scan readiness / scan results event | Wi-Fi analyzer |
| Richer scan data / channel usage | Wi-Fi analyzer (Insights, Channels, All networks) |
| Disconnect, open Wi-Fi/wireless settings | Wi-Fi management, empty states |
| Wi-Fi on/off state + toggle | Home, Wi-Fi management |
| Permission helper | First run, permission states |
| Signal helpers + signal event | Home, Signal walk |
| Metered detection | Home, Speed test |
| Saved networks / suggestions | Wi-Fi management |
| Enterprise 802.1X | Wi-Fi management |
| Local-only (IoT) | IoT setup mode |
| Connection type + event | Home |
| Hotspot | Wi-Fi management |
| Ping / speed test | Tools, Speed test, Device details |
| LAN discovery / device details | Devices, Device details |
| mDNS browsing | Services, Devices |

**Rule:** a library feature counts as covered only when it appears in this table and on the Capabilities screen.

### 7.1 Library additions required
Built in `maui_wifi_manager` first, with the library's own tests, then used by the app: **traceroute, DNS lookup, custom port scan, Wake-on-LAN, MAC vendor (OUI) lookup, stable identity hints in device details (UPnP UDN and mDNS instance name, used to match devices in §6.11), and a streaming version of `DiscoverDevicesAsync`** (results reported as each device answers rather than all at the end; this matters for perceived performance, §8).

## 8. Performance (top priority)

Performance is a requirement, not polish. Each budget is measured on a **mid-range Android device** (e.g. Pixel 6a class) in a Release build, and a build that misses any budget does not ship.

### 8.1 Budgets

| Metric | Budget | How it's measured |
|---|---|---|
| Cold start to first frame | ≤ 800 ms | `adb shell am start -W` (TotalTime) |
| Cold start to Home with live data | ≤ 1.5 s | Stopwatch log from app start to first `NetworkData` bound |
| Tab switch | ≤ 1 frame of delay before the transition starts, 0 dropped frames | `dumpsys gfxinfo`, Perfetto |
| Push/pop transition | 60 fps (120 fps on capable displays), 0 frames ≥ 32 ms | `dumpsys gfxinfo framestats` |
| Janky frames overall (scripted session) | < 1 % | `dumpsys gfxinfo` |
| Device list scroll, 254 items | 0 dropped frames | gfxinfo while flinging |
| First device row visible after scan starts | ≤ 500 ms | Stopwatch log |
| UI-thread work per event | < 8 ms (half a 60 fps frame) | Perfetto main-thread slices |
| Memory after a full scan + all tabs visited | ≤ 150 MB PSS, no growth across 10 tab cycles | `dumpsys meminfo` |
| Android APK (arm64) | ≤ 35 MB | Build output |
| Idle battery | No timers, sensors or polling while the app is in the background, except network watch | Perfetto / Battery Historian |

### 8.2 Engineering rules that follow
- **Navigation** reuses sill_app's architecture (§10.2): one persistent navigation shell, pages built once and kept, custom transitions that animate only `Translation`/`Opacity`.
- **XAML**: `MauiXamlInflator=SourceGen` (already set). All bindings compiled (`x:DataType` everywhere); a build warning for a reflection binding fails CI.
- **Layout**: flat trees. `Grid` over nested `StackLayout`s, no `StackLayout` inside `CollectionView` templates, at most 3 layout levels in a list item. Fixed item heights in long lists. Prefer `Border` to `Frame`.
- **Lists**: `CollectionView` only; never `ListView` or `BindableLayout` for unbounded data. Item templates are bound once, with no converters in hot templates (values are precomputed on the model).
- **Streaming updates**: scan, ping and Wi-Fi events are coalesced and dispatched to the UI at most every 250 ms, in batches, never one dispatch per event.
- **Threading**: no I/O, JSON, SQLite or library call on the UI thread. Every long operation takes a `CancellationToken` and is cancelled when its page goes away.
- **Startup**: nothing on the startup path except DI, the shell and the Home view model. Database, mDNS, history and widgets load lazily. No `Task.Delay` and no synchronous waits (`.Result`/`.Wait()`).
- **Drawing**: charts, gauges and skeletons are `GraphicsView` drawables (as in sill_app) that redraw only when their data changes. No chart libraries, no Lottie, no SkiaSharp unless profiling proves it's needed.
- **Images and icons**: one icon font (Material Icons, via sill_app's `MaterialIcon` helper) instead of image files; SVGs only for the app icon and splash.
- **Build**: full trimming, Android profiled AOT with a custom startup profile, R8. Trim/AOT warnings are fixed, not suppressed.
- **Dependencies**: each new package needs a measured reason. The allowed list for v1.0 is MAUI, `WifiManager.Maui`, `SimpleToolkit.SimpleShell`, `CommunityToolkit.Mvvm`, `sqlite-net-pcl`, and a QR library (to be chosen, smallest that works).

### 8.3 Verification
- Each release candidate gets a scripted profiling run (cold start, gfxinfo across a fixed tab and push tour, meminfo, a Perfetto trace) on the reference device, with a written report kept in `docs/perf/`.
- A regression of more than 10 % on any budget blocks the merge until it's fixed or explained.
- Windows: startup ≤ 1 s, and resizing between narrow and wide layouts stays smooth (no layout pass ≥ 16 ms).

## 9. Design quality ("not vibe-coded")

The app must look like a designed product. A reviewer who opens it should not be able to tell it came from a template or a code generator.

### 9.1 Design system
- **Tokens only.** Every color, spacing, radius, font size and duration comes from `Resources/Styles` tokens. A hard-coded color or magic number in a view is a defect.
- **Color**: the sill_app brand palette, copied from `sill_app/Resources/Styles/Colors.xaml` and `Helper/Utilities/BandPalette.cs`. It's built on Apple's semantic system colors, so it looks native on iOS and consistent elsewhere. No other accent, no gradients used as decoration, no glassmorphism, no neon "AI" palette.

  | Token | Light | Dark | Use |
  |---|---|---|---|
  | `Accent` (brand) | `#248A3D` | `#30D158` | Selection, tab bar/rail, links, primary actions. One stable accent that never changes with the data |
  | `Page` | `#F2F2F7` | `#000000` | Page background |
  | `Surface` | `#FFFFFF` | `#1C1C1E` | Cards |
  | `Outline` | `#C6C6C8` | `#38383A` | Separators |
  | `OnSurface` | `#000000` | `#FFFFFF` | Primary text |
  | `Muted` | `#6C6C70` | `#98989E` | Secondary text |
  | `Caption` / `Neutral` | `#8E8E93` | `#8E8E93` | Captions, inactive icons, "no data" state |
  | `Track` | `#E5E5EA` | `#2C2C2E` | Unfilled gauge and meter track |
  | `Rail` | `#F2F2F7` | `#2C2C2E` | Skeleton background |
  | Popup tokens | sill_app `Popup*` set | sill_app `Popup*` set | Dialogs and sheets (`DialogHost`) |

  **Status colors** reuse sill_app's band colors (light colors are the accessible variants, tuned for at least 3:1 on light surfaces):

  | Status | Light | Dark | Network meaning |
  |---|---|---|---|
  | Good | `#248A3D` | `#30D158` | Online, strong signal (≥ −60 dBm), Known device, low latency |
  | Fair | `#8A6D00` | `#FFD60A` | Fair signal (−60 to −70 dBm), crowded channel |
  | Poor | `#B25000` | `#FF9F0A` | Weak signal (−70 to −80 dBm), captive portal, high latency/loss |
  | Bad | `#D70015` | `#FF453A` | No internet, very weak signal, weak security, look-alike network |
  | Notice | `#8944AB` | `#BF5AF2` | Unknown / new device |

  Status tints (soft card backgrounds and outlines) are blended from the page color, as in `BandPalette.SurfaceTint` / `OutlineTint`. Status is always paired with an icon or text, never shown by color alone. Every pair is checked for WCAG AA.
- **Spacing**: 4-point grid (4/8/12/16/24/32/48). Screen gutter 16 on phones, 24 on wide layouts.
- **Type**: Open Sans (Regular and Semibold, as in sill_app) with a fixed 6-step scale (Display, Title, Headline, Body, Label, Caption). Numbers use tabular figures, so changing values don't jitter.
- **Icons**: a single icon set (Material Icons font, as in sill_app). No emoji used as icons.
- **Shape and elevation**: one corner-radius scale and at most two elevation levels.
- **Motion**: sill_app's `PopupMotion` curves and durations (enter `cubic-bezier(0.22,1,0.36,1)`, exit faster than enter, spring only for badges and check marks). Everything respects reduced motion via `MotionSettings`.

### 9.2 Every state designed
Each screen has a specified design for: **loading** (skeletons, §9.6; never a lone spinner), **empty**, **error** (what happened + one action), **permission needed** (why + Grant), **not supported on this platform** (why), **offline**, and **partial data** (e.g. no MAC). A screen isn't done until all of its states are.

### 9.3 Interaction
- In-app dialogs and sheets through sill_app's `DialogHost`; never stock `DisplayAlert`/`DisplayActionSheet`.
- Touch targets ≥ 48 dp; primary actions within thumb reach on phones.
- Haptic feedback on scan complete, copy and destructive confirms (where the platform supports it).
- Copy is short, plain and specific: "3 new devices since yesterday", not "Operation completed successfully".
- Screen reader labels, heading levels and live announcements for scan progress and results; status is never shown by color alone; layouts survive 200 % text size.
- Phone, tablet and desktop each get a proper layout, not a stretched phone UI (§9.5).

### 9.4 Design process
- A Figma file (or equivalent) with the token sheet and every screen in every state, approved before the screen is built.
- A design QA pass per screen: side-by-side comparison with the design in light and dark mode, at every width class in §9.5.

### 9.5 Responsive layout (phone, tablet, Windows)
Layouts depend on **window width**, not device type. A Windows window resized to 400 units wide gets the phone layout, and a phone in landscape gets the medium layout. The app is designed for every width class, not just phone and desktop.

**Width classes** (device-independent units; 600 matches sill_app's `WIDE_BREAKPOINT`)

| Class | Width | Typical | Navigation | Content |
|---|---|---|---|---|
| Compact | < 600 | Phone portrait, narrow window | Bottom tab bar | One pane, pushed detail pages |
| Medium | 600–1023 | Tablet portrait, phone landscape, foldable open, half-screen window | Side rail (icons + captions) | Two panes for list → detail screens |
| Expanded | 1024–1439 | Tablet landscape, laptop window | Side rail | Two panes; Home as a multi-column dashboard |
| Large | ≥ 1440 | Full-screen desktop | Side rail | Three columns where useful; content capped at a maximum width and centered, never stretched edge to edge |

**Screen behavior per class**

| Screen | Compact | Medium | Expanded / Large |
|---|---|---|---|
| Home | Single column of cards | Two-column grid | Three-column dashboard: status, devices summary, speed/latency |
| Devices → Device details | List, details pushed | List + details side by side | List + details + services/history column (Large) |
| Wi-Fi analyzer | Sections as segmented tabs | Insights + channel chart side by side | Insights, chart and All networks in three columns (Large) |
| Tools | List of tools, each pushed | Tool list + selected tool | Same |
| Settings | Pushed list | List + selected section | Same, max width |
| Charts (speed, ping, history) | Full width, fixed height | Grow with the pane, keeping aspect ratio | Same; axis labels and legends are never clipped |

**Rules**
- **Panes instead of pushes.** On Medium and wider, selecting an item fills the detail pane instead of pushing a page. If the window then shrinks to Compact, the selected item becomes a pushed page with the back stack intact, and the reverse also works.
- **No state is lost on resize or rotation.** Selection, scroll position, text input, a running scan and a running speed test all survive a width-class change. View models are shared between the narrow and wide variants of a page (sill_app's `DashboardPage` / `DashboardWidePage` pattern) and are never recreated by a layout switch.
- **Readable line length.** Text blocks are at most about 680 units wide; forms are at most 560.
- **Adaptive tiles and lists** reflow by column count (`GridItemsLayout.Span` computed from width), not by scaling elements up.
- **Multi-window**: Android split-screen and freeform windows, iPad Split View / Stage Manager and Windows Snap layouts all work at every size above the minimum. Foldables get the correct class when folded and unfolded.
- **Safe areas**: notches, the Android navigation bar and iPad rounded corners are respected in both orientations.

**Windows (and keyboard/mouse on tablets)**
- Minimum window size 360 × 560. Window size, position and maximized state are restored on the next launch.
- Resizing is smooth: the layout updates live while dragging, crossing a breakpoint takes ≤ 1 frame, and no layout pass takes ≥ 16 ms (§8.3).
- **Mouse**: hover states on every interactive element, the hand cursor on links, right-click context menus on device rows (Rename, Mark as known, Ping, Copy IP, Forget), scroll-wheel and visible scrollbars, no pull-to-refresh-only actions (a Refresh button is always there).
- **Keyboard**: full Tab/arrow navigation with visible focus rings; `Ctrl+F` search, `F5`/`Ctrl+R` rescan, `Esc` back or close pane, `Ctrl+C` copy the selected value, `Ctrl+,` settings, `Ctrl+1–4` switch sections. Shortcuts also work with a hardware keyboard on iPad and Android tablets.
- Title bar matches the app theme; follows Windows light/dark mode and text scaling.
- Touch on Windows tablets and 2-in-1s keeps the same 48-unit touch targets.

**Verification**
- Screenshot matrix for every screen and state at widths **360, 412, 600, 840, 1024, 1280, 1440, 1920**, light and dark, compared against the design.
- Manual checks on a real Android tablet, an iPad (Split View), a foldable or emulator, and Windows at 100 %, 150 % and 200 % scaling.
- Resizing on Windows and rotating on tablets is included in the §8 profiling run.

### 9.6 Skeleton loading (from sill_app)
Every loading state uses sill_app's `SkeletonView` / `SkeletonDrawable`, copied as-is and themed with this app's tokens.

- **How it works:** one `GraphicsView` draws rounded blocks at positions given as fractions of the view (`RectF` in 0–1 units), so one skeleton scales to any width. A soft highlight sweeps across (linear, 1200 ms, repeating). Base color is `Track`, highlight is `Rail` (light `#E5E5EA` → `#F2F2F7`, dark `#2C2C2E` → `#3A3A3C`).
- **Starts and stops itself:** the sweep runs only while the skeleton is visible and is cancelled when it's hidden, so a hidden skeleton costs nothing. With reduced motion, the blocks are drawn flat without the sweep.
- **Block layouts:** reuse sill_app's `METRIC_TILE`, `STAT_TILE` and `BLOCK`, and add this app's own in the same format: `DEVICE_ROW` (icon circle, two text lines, status pill), `NETWORK_ROW`, `DETAIL_SECTION` (label/value pairs), `CHART` (axis plus area) and `GAUGE` (ring).
- **The skeleton matches the real layout.** Each skeleton has the same size and position as the content it stands for, so nothing shifts when the data arrives.
- **Where:** Home tiles on launch, Network details, Device details while details load, device and network lists before the first result (3–6 placeholder rows, then real rows replace them as they arrive), and charts before their data.
- **No flashing:** a skeleton appears only if loading takes longer than 150 ms, and real content cross-fades in over it (`REDUCED_MS` = 150 ms).
- Skeletons are only for the first load. Refreshing keeps the existing content on screen with a small progress indicator.

### 9.7 Chart and gauge animation (from sill_app)
Charts and gauges reuse sill_app's drawables and animation code. Every animation runs through MAUI's `Animation` API at 16 ms per frame, only redraws its own `GraphicsView` (`Invalidate`), and stops (`AbortAnimation`) before a new one starts or when its page goes away.

| Pattern (sill_app source) | Behavior | Used for |
|---|---|---|
| **Ring sweep** (`AirQualityRingDrawable` + `AnimateRing`) | A 270° arc sweeps from the old value to the new one in 600 ms (`CubicOut`). Its color fades across status colors in 500 ms during the same motion, so crossing from Fair to Good never jumps. | Speed test gauge (live Mbps while the test runs, then the final result), Home signal gauge, Signal walk readout |
| **Time-series chart** (`HistoryChartDrawable`) | Axis, light status-band zones in the background (e.g. latency Good/Fair/Poor), line with a soft area fill, headroom above the maximum | Speed history, ping latency, Signal walk, History |
| **Sparkline + meter** (`TileChartDrawable`) | A small trend line and a thin meter bar, with a flat line when the range is tiny | Home tiles: gateway latency, last speed, device count |
| **Live pulse** (`StartPulse`/`StopPulse`) | A dot that grows to 1.35× and fades to 50 % and back, 1500 ms (`SinInOut`), repeating | "Live" indicator while scanning, during a Signal walk, during continuous ping, while network watch is active |

**Rules**
- **Values animate; streams don't.** A gauge animates to each new value. A live chart (ping, Signal walk) adds points and redraws without animating each point, to stay inside the frame budget (§8).
- **First appearance:** a time-series chart draws in from left to right over 400 ms using the `PopupMotion.ENTER` curve. This is new code following the ring-sweep pattern; sill_app's line charts don't animate. Charts redrawn later because of new data or a resize don't replay it.
- **Reduced motion** (`MotionSettings.PrefersReducedMotion`): the ring cross-fades in 150 ms (linear) instead of sweeping; the pulse and the chart draw-in are turned off.
- **Colors** come from the status colors in §9.1 and are re-read when the theme changes.
- **Accessibility:** every gauge and chart has a text equivalent ("Download 212 Mbps, Good") for screen readers, updated when the value settles rather than on every frame.

## 10. Architecture

### 10.1 Patterns
- **MVVM** with `CommunityToolkit.Mvvm` source generators (`[ObservableProperty]`, `[RelayCommand]`, async commands with cancellation). Views contain no logic beyond view concerns (animation, focus, layout).
- **Dependency injection** via the MAUI host builder. View models for tab destinations are singletons so their state survives navigation (sill_app pattern); pushed pages and their view models are transient.
- **Services** wrap the library where the app adds behavior (device memory, coalescing, history). An interface exists only when there's a second implementation, e.g. a fake network service for demo mode, UI tests and screenshots (sill_app's `IAirSensorDevice` + `FakeAirSensorDevice` pattern).
- **Messaging**: `WeakReferenceMessenger` for app-wide events (network changed, device found), so view models aren't coupled to each other.
- **Persistence**: SQLite through one small repository per aggregate (devices, history, speed results). `Preferences` for settings. Schema versioned from day one.
- **Error model**: library responses (`ErrorCode`/`ErrorMessage`) are mapped in one place into the app's state set (§9.2). No `try/catch` swallowing.
- **Code standard**: same conventions as sill_app (Bajra .NET coding standard — XML docs on public members, regions, naming). Nullable enabled, warnings as errors, analyzers on.

### 10.2 Navigation (reused from sill_app)
Copy these pieces from `MyIndoorHealth/sill_app` into this project's source tree and adapt them here. There is no shared package: each app owns its own copy and the two are free to diverge.

| From sill_app | Used for |
|---|---|
| `AppShell` on `SimpleToolkit.SimpleShell` with `UseSimpleShell(usePlatformTransitions: false)` | One persistent navigation chrome that survives every route change, so tab switches don't rebuild or flicker |
| `Controls/BottomTabBar` | Phone navigation: Home · Devices · Wi-Fi · Tools |
| `Controls/SideRail` | Wide navigation (≥ 600 units), with Settings at the bottom |
| `AppShell.ApplyTransition` | Direction-aware switch transitions (horizontal on phone, vertical on rail), slide-over push, slide-away pop, resets of stale offsets |
| `ApplyWindowWidth` + `WIDE_BREAKPOINT = 600` | Switching between phone and wide layouts on resize or rotation; separate `ShellContent` per layout where it differs. Extended with the 1024 and 1440 breakpoints (§9.5) |
| Chrome show/hide animation | Hiding the bar on immersive screens (speed test running, full-screen chart) |
| `Controls/DialogHost`, `Helper/Utilities/PopupMotion`, `MotionSettings` | Dialogs, sheets, motion curves, reduced motion |
| `Controls/SkeletonView` / `SkeletonDrawable` | Loading states (§9.6) |
| `Controls/AirQualityRingDrawable` + `AnimateRing` (in `DashboardPage`) | Gauges: speed test, signal (§9.7) |
| `Controls/HistoryChartDrawable` | Time-series charts with status-band zones (§9.7) |
| `Controls/TileChartDrawable` | Sparklines and meters in Home tiles (§9.7) |
| `StartPulse` / `StopPulse` (in `DashboardPage`) | "Live" indicator dot (§9.7) |
| `Controls/SettingsRow`, `StatTileView`, `MetricTileView`, `FirstRunView` | Settings list, Home tiles, first-run flow |
| `Helper/Utilities/MaterialIcon` | Icon font glyphs |

Routes: tab destinations `home`, `devices`, `wifi`, `tools` (plus `*wide` variants where the layout differs); pushed routes registered with `Routing.RegisterRoute` for device details, network details, speed test, channels, services, each tool, settings and capabilities. Tab switches use `animate: false` at the Shell level and the custom transition handles the motion, exactly as in sill_app.

### 10.3 Project structure
Matches sill_app's layout: `Controls/`, `Helper/{Converters,Enums,Utilities}/`, `Implementation/`, `Interface/`, `Models/`, `ViewModels/`, `Views/`, `Resources/Styles/`, plus `Data/` for repositories and `docs/perf/` for profiling reports. The library is consumed from NuGet in Release builds and as a project reference to `../maui_wifi_manager` in Debug builds.

### 10.4 Quality gates (CI)
- GitHub Actions on each PR: build Android, Windows, iOS; run unit tests (view models, coalescing, parsers, repositories) against the fake network service.
- Fails on: compiler warnings, trim/AOT warnings, reflection bindings, hard-coded colors in XAML (simple lint), any new dependency not on the allowed list.
- UI smoke test on an Android emulator: launch, visit every tab, open every pushed page in demo mode.

## 11. Privacy and security
- No analytics or crash SDKs. Crash data comes only from store consoles.
- All data stays on the device; export is user-initiated.
- Network watch and notifications are opt-in.
- The speed test and internet check are the only features that contact third parties, and the UI names them.
- Android cleartext HTTP is allowed only for private address ranges (network security config), so device web titles and UPnP work without opening cleartext to the internet.
- The Wi-Fi password entered for a QR share is never stored.

## 12. Build plan (order, not scope cuts)

| Phase | Delivers |
|---|---|
| **1 — Foundation** | Rename/app ID, design system tokens and Figma, navigation shell ported from sill_app, DI, MVVM base, fake network service, CI with all gates, perf baseline on the reference device |
| **2 — Core network** | Home, Network details, Devices (streaming), Device details, Speed test, first run and permissions; library: streaming discovery |
| **3 — Wi-Fi** | Wi-Fi analyzer (Insights, Signal walk, Channels, All networks), Wi-Fi management (connect, enterprise, IoT setup mode, saved networks, hotspot, QR) |
| **4 — Tools** | Ping, Traceroute, DNS, Port check, WoL, Subnet calculator, Services; library: those additions |
| **5 — Persistence and background** | Device memory, History, Export/import, Network watch, Widgets |
| **6 — Polish and release** | Capabilities, accessibility audit, localization setup, full perf report against §8, store listings and screenshots |

v1.0 ships to Google Play, F-Droid (or IzzyOnDroid if MAUI builds aren't reproducible), the Microsoft Store, the App Store and GitHub Releases at the same time.

## 13. Risks

| Risk | Mitigation |
|---|---|
| Android 10+ and iOS hide device MACs, which weakens device naming | Combine host name, NetBIOS, UPnP, mDNS and open ports; let users rename devices; explain the limit once |
| False "new device" alerts when a known device gets a new IP or a phone rotates its private MAC | Match devices on several identifiers (§6.11), alert once per device, offer an "Alert for unidentified devices" switch, and make Mark as known one tap from the notification |
| Background checks are delayed by Doze / iOS scheduling | State on the settings screen that alerts aren't instant; check every 2 min while the app is open and on every app open |
| Devices that answer neither ping nor mDNS are missed | Add ARP and TCP-connect probing to the library; merge every source |
| Android scan throttling makes the scanner feel slow | Lead with Insights (a few scans are enough); Signal walk uses the connected network's signal, which isn't throttled; use `ScanResultsAvailable` (costs no scan quota) and show a countdown |
| Channel suggestions are wrong because a scan can't see traffic | Label them as estimates, weight by signal, suggest only 1/6/11 on 2.4 GHz |
| iOS multicast entitlement is refused | Ship without mDNS on iOS; the Capabilities screen explains why |
| Store review of location / local-network permissions | Ask in context, use `neverForLocation`, publish a clear privacy policy |
| Perf budgets slip as features land | Budgets are CI/release gates from Phase 1; profile each phase, not only at the end |
| Background work is limited by the OS | Network watch is opt-in and honest about its interval; iOS checks on open |

## 14. Success metrics

**Product:** 10,000 installs across stores within 6 months of v1.0; store rating ≥ 4.5; crash-free sessions ≥ 99.5 %.
**Performance:** every §8 budget met at release and in each later minor release.
**Showcase:** 100 % library coverage (§7); all §7.1 additions merged into the library; the library README links the app; NuGet downloads tracked before and after launch.

## 15. Open questions

1. **Name and ID**: keep "Ping My Network"? The `ApplicationId` is still the template's `com.companyname.pingmynetwork` (e.g. change to `com.exendahal.pingmynetwork`).
2. **Repo**: keep the app in its own repo, or move it into `maui_wifi_manager/samples` as the flagship sample?
3. **Reference devices**: confirm the Android device used for §8 budgets, plus a low-end device for a sanity check.
4. **App icon**: the colors follow sill_app's brand (§9.1); who designs the icon and owns the Figma file?
