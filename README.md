# Ping My Network

A free network utility for Android, iOS, Windows and Mac. It answers the questions people ask about their network: *Who is on my Wi-Fi? Is my internet actually working? Why is it slow? Which channel should my router use?*

It is also the flagship app for [maui_wifi_manager](https://github.com/exendahal/maui_wifi_manager) ([`WifiManager.Maui`](https://www.nuget.org/packages/WifiManager.Maui/) on NuGet): every screen is built on the library's public API, so .NET MAUI developers can see what it does on each platform.

> **Status:** in active development. Not yet on any app store.

## Features

### Connection health
- Whether the internet actually works: online, no internet, a captive portal that wants you to sign in, or Wi-Fi off.
- Signal strength, band, channel, Wi-Fi standard, link speed, router latency and your internet provider at a glance.
- Explains why the Wi-Fi name is hidden (location access, or location turned off) and offers the fix.
- Every detail of the connection in one place, each value copyable: addresses, subnet, router, DHCP, DNS, IPv6 and public address.

### Devices on your network
- Scans the network and lists devices as they answer.
- Names devices from what they reveal: host names, NetBIOS, UPnP, and the names they announce over mDNS / Bonjour ("Anna's iPhone", "Living Room TV"). Shows maker, model, MAC address, open ports and latency.
- Guesses each device's type (phone, computer, TV, printer, camera, router, smart home…) from its names, ports and maker.
- Mark devices as known, give them your own names and types. Devices are recognised again when their address changes.
- **My network vs guest network:** mark the networks that are yours. Only those are backed up and send alerts.
- Tells apart two networks that share a Wi-Fi name, from their access points and the devices on them, and asks only when it cannot tell.

### New-device alerts
- A notification when a device you have not marked as known joins one of your networks.
- Checks every 2 minutes while the app is open; on Android also in the background, about every 15 minutes on unmetered Wi-Fi.

### Wi-Fi analyzer
- **Insights:** findings about your own Wi-Fi with one action each: crowded channel, a faster band available, a stronger access point nearby, look-alike networks, weak security.
- **Signal walk:** follow your signal while you walk around, save readings per room, and compare them later.
- **Channels:** every nearby network drawn across the channels it uses, with the quietest channel suggested. Your router's other band is recognised as yours.
- **All networks:** every nearby network with its signal, band, channel, width, standard and security.

### Tools
Each tool has a short built-in explanation of what it does and how to read the result.
- **Ping** with live latency chart, jitter and loss
- **Traceroute** with every hop and its latency
- **DNS lookup** for A, AAAA, CNAME, MX, TXT and NS records, with a choice of server
- **Port check** for common ports or your own list
- **Speed test** for download, upload and latency
- **Wake-on-LAN**
- **Subnet calculator**

### Privacy and backup
- No account, no server, no analytics. Everything stays on your device.
- Only the speed test and the provider lookup contact a third party (Cloudflare), and the app says so.
- Your own networks, device names and known devices are backed up to your Google account (Android) or iCloud (iOS, Mac) and come back after a reinstall or on a new device. Guest networks stay on the device.

## Platform support

| | Android | iOS | Windows | Mac |
|---|:-:|:-:|:-:|:-:|
| Connection health | ✅ | ✅ (no signal strength) | ✅ | ⚠️ needs testing |
| Device scan and names | ✅ | ✅ | ✅ | ⚠️ |
| Nearby networks, channels, signal walk | ✅ | ❌ (iOS does not allow scanning) | ✅ | ❌ |
| Tools | ✅ | ✅ | ✅ | ✅ |
| New-device alerts | ✅ including background | ✅ while open | ❌ | ✅ while open |
| Backup | Google | iCloud | — | iCloud |

## Building

Requirements: the .NET 10 SDK (see `global.json`) with the MAUI workload, and the Android, iOS or Windows tooling for the platform you build.

```sh
dotnet workload install maui
dotnet build -f net10.0-android
```

Debug builds use the library's source from a sibling checkout, so library changes can be tried in the app straight away. Clone both side by side:

```
Workspace/
├── maui_wifi_manager/   https://github.com/exendahal/maui_wifi_manager
└── ping-my-network/
```

Release builds use the published `WifiManager.Maui` package and need no sibling checkout.

## Reporting bugs and ideas

Found a problem or have an idea? [Open an issue](https://github.com/exendahal/ping-my-network/issues). The app links there too, from **Settings → Help and feedback**, with your app version and device filled in.

## License

[MIT](LICENSE) © Santosh Dahal
