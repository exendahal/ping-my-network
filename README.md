# Ping My Network

A free network utility for Android, iOS, Windows and Mac. It answers the questions people ask about their network: *Who is on my Wi-Fi? Is my internet actually working? Why is it slow? Which channel should my router use?*

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
- Your own networks are backed up to your Google account or iCloud, and guest networks never leave the device. See [Backup and restore](#backup-and-restore).

## Platform support

| | Android | iOS | Windows | Mac |
|---|:-:|:-:|:-:|:-:|
| Connection health | ✅ | ✅ (no signal strength) | ✅ | ⚠️ needs testing |
| Device scan and names | ✅ | ✅ | ✅ | ⚠️ |
| Nearby networks, channels, signal walk | ✅ | ❌ (iOS does not allow scanning) | ✅ | ❌ |
| Tools | ✅ | ✅ | ✅ | ✅ |
| New-device alerts | ✅ including background | ✅ while open | ❌ | ✅ while open |
| Backup | Google | iCloud | — | iCloud |

## Backup and restore

The app has no account and no server of its own. It uses the backup your phone already has, so the networks you set up come back after a reinstall or on a new device without signing in to anything.

**What is backed up**
- Networks you mark as **My network**, with their devices, the names you gave them, their types and Known status.
- On Android, the app's settings and Signal walk places too.

**What is not**
- Guest networks and networks you have not answered for. They stay on the device.
- Live scan results: which devices are online right now and their latency. The next scan finds those again.
- Anything from the speed test or the tools.

| | How | Comes back when |
|---|---|---|
| **Android** | Android's automatic backup to your Google account. Only your networks file and the app's settings are included, a few KB. | You reinstall the app, or set up a new phone from your Google backup. Android uploads the backup about once a day, while the phone is idle, charging and on Wi-Fi. |
| **iOS, Mac** | A copy of your networks in iCloud key-value storage, updated whenever you change them. The app's files are also part of the normal iCloud device backup. | You reinstall the app or sign in to a new iPhone, iPad or Mac with the same Apple ID. The networks may appear a few seconds after the app opens, once iCloud has synced. Settings come back with a full device restore. |
| **Windows** | Not backed up yet. | — |

The backup goes only to storage you already own, and it never includes Wi-Fi passwords.

To build the iOS or Mac app with backup working, the app's App ID needs the **iCloud** capability with **Key-value storage** turned on in your Apple developer account.

## Building

Requirements: the .NET 10 SDK (see `global.json`) with the MAUI workload, and the Android, iOS or Windows tooling for the platform you build.

```sh
dotnet workload install maui
dotnet build -f net10.0-android
```

## Releases

Pushing a version tag builds a signed Android APK, an unsigned iOS IPA and an unsigned Windows app, and attaches them to a GitHub release (`.github/workflows/release.yml`):

```sh
git tag v1.0.0
git push origin v1.0.0
```

The tag becomes the version shown in the app. The build number is the workflow's run number. A tag with a suffix, such as `v1.0.0_beta`, is published as a pre-release, and the app shows only the number (`1.0.0`).

To install the IPA on an iPhone or iPad, see [Installing the iOS app](docs/install-ios.md). On Windows, unzip the app and run `ping-my-network.exe`. It is not code-signed, so SmartScreen asks first: choose **More info → Run anyway**.

Signing the APK needs these repository secrets (**Settings → Secrets and variables → Actions**). Encode the keystore with `base64 -i <file>` on a Mac, or `[Convert]::ToBase64String([IO.File]::ReadAllBytes("<file>"))` in PowerShell.

| Secret | What it is |
|---|---|
| `ANDROID_KEYSTORE_BASE64` | The upload keystore (`.keystore` / `.jks`), base64 |
| `ANDROID_KEYSTORE_PASSWORD` | Keystore password |
| `ANDROID_KEY_ALIAS` | Alias of the signing key in the keystore |
| `ANDROID_KEY_PASSWORD` | Password of that key |

Make a keystore once with `keytool -genkeypair -v -keystore release.keystore -alias pingmynetwork -keyalg RSA -keysize 2048 -validity 10000`, and keep it safe: Android updates must be signed with the same key.

## Reporting bugs and ideas

Found a problem or have an idea? [Open an issue](https://github.com/exendahal/ping-my-network/issues). The app links there too, from **Settings → Help and feedback**, with your app version and device filled in.

## License

[MIT](LICENSE) © Santosh Dahal
