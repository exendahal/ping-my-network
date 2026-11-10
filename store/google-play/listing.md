# Google Play listing

Paste into **Play Console → Grow → Store presence → Main store listing**. Character limits are Play's.

## App name (30)

```
Ping My Network
```

## Short description (80)

```
See who's on your Wi-Fi, get alerts for new devices and fix slow Wi-Fi.
```

## Full description (4000)

```
Who is on my Wi-Fi? Is my internet actually working? Why is it slow? Ping My Network answers the questions people ask about their network, in plain words.

SEE EVERY DEVICE ON YOUR NETWORK
• Scan your network and watch devices appear as they answer.
• Devices are named from what they reveal: their names, the names they announce ("Living Room TV"), maker and model.
• Each device gets a type, such as phone, computer, TV, printer, camera, router or smart home, so the list is easy to read.
• Mark the devices you recognise, give them your own names, and they are recognised again when their address changes.

KNOW WHEN SOMETHING NEW JOINS
• Get a notification when a device you have not marked as known joins one of your networks.
• Checks every 2 minutes while the app is open, and about every 15 minutes in the background on Wi-Fi.
• Only for networks you mark as yours; guest networks are left alone.

IS YOUR CONNECTION OK?
• Whether the internet really works: online, no internet, a sign-in page waiting, or Wi-Fi off.
• Signal strength, band, channel, Wi-Fi standard, link speed, router latency and your internet provider at a glance.
• Every detail of your connection in one place, each value copyable.

MAKE YOUR WI-FI FASTER
• Insights point out what to fix: a crowded channel, a faster band you could use, a stronger access point nearby, weak security.
• Signal walk: follow your signal around the house and save a reading per room.
• Channels: every nearby network drawn across the channels it uses, with the quietest channel suggested.
• All networks: every Wi-Fi around you with its signal, band, channel and security.

NETWORK TOOLS
• Ping with a live latency chart
• Traceroute
• DNS lookup
• Port check
• Speed test
• Wake-on-LAN
• Subnet calculator
Each tool explains what it does and how to read the result.

PRIVATE BY DESIGN
• No account, no ads, no analytics. Your devices and networks stay on your phone.
• Your own networks are backed up with your phone's Google backup, so they come back on a new phone.
• Only the speed test and the provider lookup contact a third party (Cloudflare).

WHY LOCATION?
Android only tells apps the name of your Wi-Fi network when location access is allowed. Ping My Network uses it only to recognise which of your networks you are on. Your location is never stored or sent anywhere. Allowing it "all the time" lets new-device alerts work while the app is closed; without it, alerts still work while the app is open.

Free and open source: github.com/exendahal/ping-my-network
```

## Release notes for 1.0 (500)

```
First release: device scan with names and types, new-device alerts, connection health, Wi-Fi analyzer with channels and signal walk, and network tools (ping, traceroute, DNS, port check, speed test, Wake-on-LAN, subnet calculator).
```

## Graphics

| Asset | File | Play requirement |
|---|---|---|
| App icon | `icon-512.png` | 512×512 PNG |
| Feature graphic | `feature-graphic-1024x500.png` | 1024×500 |
| Phone screenshots | `screenshots/01…06-*.png` | 2–8 images, 9:16, at least 1080 px for promotion |

## Store settings

| Field | Value |
|---|---|
| App category | Tools |
| Tags | Network tools, Wi-Fi analyzer, Utilities |
| Contains ads | No |
| In-app purchases | None |
| Privacy policy URL | `https://exendahal.github.io/ping-my-network/privacy-policy/` (once GitHub Pages is on; see `store/README.md`) |
| Website | `https://github.com/exendahal/ping-my-network` |

## App content (Policy → App content)

**Target audience:** 18 and over is the simplest choice. It keeps the app out of the Families policy, which a network scanner does not need.

**Content rating questionnaire:** category *Utility, Productivity, Communication, or Other*. Answer No to everything: no violence, no user-generated content, no sharing of location with other users, no gambling, no purchases. The result should be *Everyone* / *PEGI 3*.

**Data safety:**

| Question | Answer | Why |
|---|---|---|
| Does your app collect or share user data? | **No** | Device lists, network names and settings are stored only on the phone. Backup uses Android's own Google backup, which Play does not count as collection. |
| Is data encrypted in transit? | Yes | The only internet calls (Cloudflare speed test and provider lookup) are HTTPS. |
| Can users request deletion? | Yes | Uninstalling or clearing app data removes everything, and any device can be forgotten in the app. |

> Check this against Play's current definitions before submitting. The provider lookup sends an HTTPS request to Cloudflare, which sees the phone's public IP like any website does. The app does not send or keep it anywhere. If you would rather declare it, mark **Approximate location → Shared → App functionality**, ephemeral.

**Permissions declaration for background location** (`ACCESS_BACKGROUND_LOCATION`). Play reviews this by hand and asks for a short video. Paste this as the justification:

```
Ping My Network alerts the user when an unknown device joins one of their own Wi-Fi networks. The check runs in the background about every 15 minutes. To know which network the phone is on, the app reads the Wi-Fi network name (SSID) and access point (BSSID), which Android only provides to apps with location access; in the background that requires ACCESS_BACKGROUND_LOCATION. The app never reads, stores or sends the device's location. The feature is off until the user turns on new-device alerts, and the app explains the request before asking. Without the permission, alerts still work while the app is open.
```

Video, about 30 seconds, screen recording:
1. Settings → turn on **New device alerts**.
2. The app's explanation screen ("Check while the app is closed?"), then Android's **Allow all the time** prompt.
3. Close the app, connect a new device to the Wi-Fi, and show the notification arriving.

If Play still refuses, drop the permission. Alerts then only work while the app is open, and the in-app wording already covers that case.
