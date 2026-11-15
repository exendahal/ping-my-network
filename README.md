# Ping My Network

A free network utility for Android, iOS, Windows and Mac. It answers the questions people ask about their network: *Who is on my Wi-Fi? Is my internet actually working? Why is it slow? Which channel should my router use?*

## Download

Get the Android APK, the iOS IPA and the Windows app from the [latest release](https://github.com/exendahal/ping-my-network/releases/latest).
- **iOS:** the IPA is unsigned; see [Installing the iOS app](docs/install-ios.md).
- **Windows:** unzip and run `ping-my-network.exe`. It isn't code-signed, so SmartScreen asks first: choose **More info → Run anyway**.

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

## Platform support

| | Android | iOS | Windows | Mac |
|---|:-:|:-:|:-:|:-:|
| Connection health | ✅ | ✅ (no signal strength) | ✅ | ⚠️ needs testing |
| Device scan and names | ✅ | ✅ | ✅ | ⚠️ |
| Nearby networks, channels, signal walk | ✅ | ❌ (iOS does not allow scanning) | ✅ | ❌ |
| Tools | ✅ | ✅ | ✅ | ✅ |
| New-device alerts | ✅ including background | ✅ while open | ❌ | ✅ while open |

## Privacy

No account, no ads, no analytics, and no server of its own. The networks you mark as yours are backed up only to your own Google account or iCloud. Only the speed test and the provider lookup contact a third party (Cloudflare). See the [privacy policy](docs/privacy-policy.md).

## Reporting bugs and ideas

Found a problem or have an idea? [Open an issue](https://github.com/exendahal/ping-my-network/issues). The app links there too, from **Settings → Help and feedback**, with your app version and device filled in.

## License

[MIT](LICENSE) © Santosh Dahal
