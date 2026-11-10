---
title: Privacy policy · Ping My Network
description: What Ping My Network reads, what it keeps, and what leaves your device.
permalink: /privacy-policy/
---

# Privacy policy

**Ping My Network** · Effective 10 November 2026

Ping My Network is a free, open-source network utility for Android, iOS, Windows and Mac. It has no account, no server of its own, no ads and no analytics. This policy explains what the app reads, what it keeps, and what leaves your device.

## In short

- Everything the app learns about your network stays on your device.
- The developer receives nothing. There is no server to send it to.
- Your own networks are backed up with your phone's own Google or iCloud backup, if you have backup turned on.
- Only the speed test and the internet-provider lookup contact a third party (Cloudflare).

## What the app reads

| What | Why |
|---|---|
| Your Wi-Fi connection: network name (SSID), access point (BSSID), signal, band, channel, addresses | To show your connection's health and recognise which of your networks you are on. |
| Devices on your local network: addresses, names they announce, maker and model, open ports, and on Windows their MAC address | To list the devices on your network and tell them apart. The app talks to these devices directly on your local network. |
| Nearby Wi-Fi networks (Android, Windows) | For the Wi-Fi analyzer: channels, signal and security of the networks around you. |
| Host names and addresses you type into a tool | To ping, trace, look up or check them. |

## Location permission

Android and iOS only give apps the name of your Wi-Fi network when location access is allowed. Ping My Network uses this permission **only** to read the Wi-Fi name and access point, so it can recognise which of your networks you are on.

The app never reads your GPS position, and never stores or sends your location.

On Android, allowing location "all the time" lets new-device alerts run while the app is closed. Without it, alerts work only while the app is open.

## What the app keeps, and where

The app stores these on your device:
- the networks you have used it on, and the devices seen on them
- the names, types and "known" marks you give
- your settings
- Signal walk readings

Nothing is sent to the developer.

**Backup.** Networks you mark as *My network*, and on Android the app's settings, are included in your device's own backup:
- **Android:** to your Google account.
- **iOS and Mac:** to iCloud.

That backup is handled by Google or Apple under their privacy policies, and only if you have backup turned on. Guest networks are never backed up. Wi-Fi passwords are never read or stored.

## What leaves your device

| Service | When | What it receives |
|---|---|---|
| Cloudflare (`speed.cloudflare.com`) | When you run a speed test | Test data and your public IP address, as with any website. |
| Cloudflare (`speed.cloudflare.com/meta`) | Once per network, to show your internet provider | Your public IP address, as with any website. The app shows the provider name and does not keep or send it anywhere else. |
| Connectivity checks: Cloudflare (`1.1.1.1`), and on Apple devices `captive.apple.com` and `www.apple.com` | When the app checks whether your internet works | A request with no personal data, apart from your public IP address. |
| Hosts you enter in a tool | When you run ping, traceroute, DNS lookup or port check | The requests the tool sends to that host. |
| GitHub (`github.com`) | Only if you tap **Help and feedback** | Opens a new issue in your browser with the app version, device model and OS version filled in. You choose whether to submit it. |

The app does not sell, rent or share data with anyone, and contains no advertising or tracking code.

## Notifications

New-device alerts are local notifications created on your device. No push service is involved.

## Children

The app is not directed at children and collects no personal information from anyone.

## Deleting your data

- Forget any device in the app, or clear the app's data, or uninstall the app. Each removes the data from your device.
- To remove the backup copy, delete the app's backup in your Google account (Android) or iCloud settings (iOS, Mac).

## Changes

Changes to this policy are published at this address, with a new effective date. The full history is in the project's repository.

## Contact

Questions about privacy: [open an issue](https://github.com/exendahal/ping-my-network/issues) on GitHub.
