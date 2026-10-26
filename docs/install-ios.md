# Installing the iOS app from an IPA

Each [release](https://github.com/exendahal/ping-my-network/releases) includes `PingMyNetwork-<version>.ipa`. It is not from the App Store, so it installs only on iPhones and iPads that were added to the app's provisioning profile.

## 1. Get your device added

The IPA is signed with an Ad Hoc or Development profile, which lists every device allowed to run it. If your device is not on the list, iOS refuses to install the app with "Unable to install".

Send the maintainer your device's **UDID**:

- **Mac:** connect the device, open **Finder**, select the device in the sidebar, and click the grey text under its name until the UDID shows. Right-click it to copy.
- **Windows:** connect the device, open the **Apple Devices** app (or iTunes), select the device, and click the serial number until the UDID shows.

The device is added in the Apple developer account, a new profile is made, and the app is signed again. Use the IPA from the next release after that.

## 2. Install the IPA

Use any one of these.

### Apple Configurator (Mac)

1. Install [Apple Configurator](https://apps.apple.com/app/apple-configurator/id1037126344) from the Mac App Store.
2. Connect and unlock the device, and tap **Trust** if it asks.
3. Drag the `.ipa` file onto the device in Apple Configurator.

### Finder (Mac)

1. Connect and unlock the device, and select it in the Finder sidebar.
2. Drag the `.ipa` file onto the device's **General** tab.

### Xcode (Mac)

1. Connect the device and open **Window → Devices and Simulators**.
2. Select the device, click **+** under **Installed Apps**, and choose the `.ipa` file.

### Windows

Apple has no tool for installing an IPA from Windows. Third-party tools such as [Sideloadly](https://sideloadly.io) can install it. They sign the app again with your own Apple ID, so the step 1 device list does not apply to them, but a free Apple ID's signature expires after 7 days.

## 3. Open the app

- **Development profile:** on iOS 16 and later, turn on **Settings → Privacy & Security → Developer Mode** and restart when asked. Ad Hoc builds do not need this.
- If iOS says **Untrusted Developer**, go to **Settings → General → VPN & Device Management**, tap the developer's name and tap **Trust**.

## Troubleshooting

| Message | Cause |
|---|---|
| Unable to install "Ping My Network" | The device is not in the provisioning profile. See step 1. |
| The app opens and closes straight away | The profile has expired or was revoked. Install the IPA from a newer release. |
| Integrity could not be verified | The IPA was changed or signed for other devices. Download it again from the release. |
