# Store assets

Everything needed to list Ping My Network on Google Play and the Microsoft Store.

| Folder | Contents |
|---|---|
| [`google-play/`](google-play) | Icon, feature graphic, 6 phone screenshots, and [`listing.md`](google-play/listing.md): texts, store settings, Data safety answers, and the background-location declaration |
| [`microsoft-store/`](microsoft-store) | Tile icon, box art, poster art, super hero art, 5 desktop screenshots, and [`listing.md`](microsoft-store/listing.md): texts, search terms, properties, and the steps before submitting |
| [`source/`](source) | Raw screenshots, the art template and the render script |

The privacy policy both stores ask for is [`docs/privacy-policy.md`](../docs/privacy-policy.md). Every version tag publishes `docs/` to `https://exendahal.github.io/ping-my-network/privacy-policy/`, the address both listings use. It's the `pages` job in `.github/workflows/release.yml`.

One-time setup in the repository settings:
1. **Settings → Pages → Build and deployment → Source: GitHub Actions.**
2. **Settings → Environments → github-pages → Deployment branches and tags:** add a tag rule `v*`. By default the environment only accepts deploys from `main`, and a tag deploy fails with "not allowed to deploy to github-pages due to environment protection rules".

## Screenshots show demo data

The screenshots come from **demo mode**, a Debug-only stand-in for the Wi-Fi manager. It shows a made-up home network ("Home Wi-Fi", 192.168.1.x, "Front Door Camera"), so no real Wi-Fi name, device or address ends up in a public listing.
- Its devices are saved in separate files (`demo.devices*.json`), so real data is never touched.
- Release builds don't include it.

The code is in `Implementation/DemoWifiService.cs`.

### Retaking them after the UI changes

1. **Load the starting state.** Copy `source/demo.devices.json` into the app's data folder. It's the demo network marked as yours with its devices known, so two newcomers show as new.
   - Windows: `%LOCALAPPDATA%\User Name\com.exendahal.pingmynetwork\Data\`.
   - Android: push it to `/data/local/tmp`, then `adb shell run-as com.exendahal.pingmynetwork cp /data/local/tmp/demo.devices.json files/`.
2. **Turn demo mode on** in a Debug build.
   - Windows: set `PMN_DEMO=1`.
   - Android: `adb shell run-as com.exendahal.pingmynetwork touch files/demo`. Delete the file afterwards.
3. **Windows: open screens directly.** These Debug-only variables do it:
   - `PMN_ROUTE=//devices` opens a screen.
   - `PMN_THEME=Light` picks the theme.
   - `PMN_SELECT=Front Door Camera` opens a device in the details pane.

   Capture the window at 1600×1000.
4. **Android: clean the status bar** with its demo mode: `adb shell settings put global sysui_demo_allowed 1`, then the `com.android.systemui.demo` broadcasts for the clock, battery and notifications. Exit it afterwards.
5. **Save the raw captures** over the files in `source/screenshots/android` and `source/screenshots/windows`, keeping the names.

## Rendering

```powershell
powershell -ExecutionPolicy Bypass -File store\source\render.ps1
```

It renders `source/art.html` with headless Edge at each asset's exact size, using the app's own icon geometry, colours and Open Sans. Captions are set in `render.ps1`.
