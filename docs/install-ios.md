# Installing the iOS app from an IPA

Each [release](https://github.com/exendahal/ping-my-network/releases) includes `PingMyNetwork-<version>.ipa`. It is not from the App Store and it is **unsigned**, so iOS will not install it as it is. A sideloading tool signs it with your own Apple ID and installs it. A free Apple ID is enough.

## What you need

- An iPhone or iPad on iOS 15 or later, and a USB cable.
- A Windows PC or a Mac.
- An Apple ID. Using a spare one rather than your main account is a good idea, since the tool signs in with it.

## Install with Sideloadly (Windows or Mac)

1. **Windows only:** install iTunes and iCloud from [apple.com](https://www.apple.com/itunes/download/) (the website versions, not the Microsoft Store ones). Sideloadly needs their drivers.
2. Download and install [Sideloadly](https://sideloadly.io).
3. Connect and unlock the device, and tap **Trust** if it asks.
4. Open Sideloadly, drag the `.ipa` onto it, check that your device is selected, enter your Apple ID and click **Start**.
5. Enter the verification code if Apple asks for one.

[AltStore](https://altstore.io) works the same way, and can refresh the app over Wi-Fi.

## Open the app

1. On iOS 16 and later, turn on **Settings → Privacy & Security → Developer Mode** and restart when asked.
2. Open **Settings → General → VPN & Device Management**, tap your Apple ID under **Developer App** and tap **Trust**.

## Keep it working

- **Free Apple ID:** the app stops opening after **7 days**. Install it again with Sideloadly, or let AltStore refresh it. Your data is kept.
- **Paid Apple developer account:** the signature lasts a year.
- A free Apple ID can have at most 3 sideloaded apps installed at once.

## Not available in sideloaded builds

- **iCloud backup** of your networks. It needs the app's own iCloud setup, which a re-signed app does not have. Your networks stay on the device.

## Troubleshooting

| Message | What to do |
|---|---|
| The app opens and closes straight away | The 7-day signature has expired. Install it again. |
| Untrusted Developer | Trust your Apple ID in **VPN & Device Management** (see above). |
| Sideloadly: "Your maximum App ID limit has been reached" | A free Apple ID can create 10 App IDs a week. Wait, or use another Apple ID. |
| Sideloadly does not see the device | On Windows, reinstall iTunes from apple.com, then unplug and reconnect the device. |
