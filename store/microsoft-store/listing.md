# Microsoft Store listing

Paste into **Partner Center → your app → Store listings → English**. The Windows app needs an MSIX package before it can be submitted; see "Before you submit" below.

## Product name

```
Ping My Network
```

## Description (10,000)

```
Who is on my network? Is my internet actually working? Why is the Wi-Fi slow? Ping My Network answers the questions people ask about their network, in plain words.

SEE EVERY DEVICE ON YOUR NETWORK
Scan your network and watch devices appear as they answer. Each one is named from what it reveals, such as its name, maker, model and MAC address, and given a type: phone, computer, TV, printer, camera, router, smart home and more. Mark the devices you recognise and give them your own names; they are recognised again when their address changes, and anything new stands out.

IS YOUR CONNECTION OK?
See whether the internet really works (online, no internet, or a sign-in page waiting), with signal, band, channel, Wi-Fi standard, link speed, router latency and your internet provider at a glance. Every detail of the connection is in one place, each value copyable: addresses, subnet, router, DHCP, DNS, IPv6 and public address.

MAKE YOUR WI-FI FASTER
Insights point out what to fix: a crowded channel, a faster band you could use, a stronger access point nearby, weak security. The channel map draws every nearby network across the channels it uses and suggests the quietest one. Signal walk follows your signal as you move a laptop around and saves a reading per room.

NETWORK TOOLS
Ping with a live latency chart, traceroute, DNS lookup, port check, speed test, Wake-on-LAN and a subnet calculator. Each tool explains what it does and how to read the result.

PRIVATE BY DESIGN
No account, no ads, no analytics. Your devices and networks stay on your PC. Only the speed test and the provider lookup contact a third party (Cloudflare).

Free and open source: github.com/exendahal/ping-my-network
```

## What's new in this version

```
First release on the Microsoft Store.
```

## Product features (up to 20, 200 characters each)

```
See every device on your network, named and given a type
Mark devices you recognise so new ones stand out
Check whether the internet really works, at a glance
Signal, band, channel, Wi-Fi standard and link speed
Wi-Fi insights that point out what to fix
Channel map with the quietest channel suggested
Signal walk: save a reading per room
Ping, traceroute, DNS lookup and port check
Speed test, Wake-on-LAN and subnet calculator
Light and dark themes
No account, no ads, no analytics
```

## Short description (Windows 10/11 shows it at the top of the listing)

```
See who's on your network, check your connection and fix slow Wi-Fi.
```

## Search terms (up to 7, 30 characters each)

```
network scanner
wifi analyzer
ip scanner
ping
speed test
wake on lan
who is on my wifi
```

## Graphics

| Partner Center field | File | Size |
|---|---|---|
| Screenshots (Desktop) | `screenshots/01…05-*.png` | 1920×1080; 1–10 allowed, at least 1366×768 |
| 1:1 app tile icon | `app-tile-icon-300.png` | 300×300 |
| 1:1 box art | `box-art-2160.png` | 2160×2160 |
| 2:3 poster art | `poster-art-1440x2160.png` | 1440×2160 |
| 16:9 super hero art | `super-hero-art-1920x1080.png` | 1920×1080, no text, as the Store lays the name over it |

The in-app tiles and taskbar icons are not listed here. MAUI generates them into the MSIX from `Resources/AppIcon` when the app is packaged.

## Properties

| Field | Value |
|---|---|
| Category | Utilities & tools |
| Privacy policy URL | `https://exendahal.github.io/ping-my-network/privacy-policy/` |
| Website | `https://github.com/exendahal/ping-my-network` |
| Support contact | `https://github.com/exendahal/ping-my-network/issues` |
| Product declarations | Not designed for kids. No in-app purchases. |
| Age rating (IARC questionnaire) | Utility; answer No throughout. Result: 3+ / Everyone |
| Pricing | Free |

## Before you submit

1. **Reserve the name** "Ping My Network" in Partner Center. It shows the **Package/Identity/Name**, **Publisher** (`CN=…`) and **Publisher display name**.
2. **Package the app as MSIX.** Today `ping-my-network.csproj` builds Windows unpackaged (`WindowsPackageType=None`), and `Platforms/Windows/Package.appxmanifest` still has template placeholders. Packaging moves the app's data folder, so existing Windows users need a one-time move of their saved devices.
3. Partner Center signs the package for Store installs. You don't need a certificate of your own.
