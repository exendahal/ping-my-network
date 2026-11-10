# Renders every store asset from art.html with headless Edge, at each store's exact pixel size.
# Run from anywhere: powershell -File store\source\render.ps1
# Raw screenshots live in screenshots\; capture new ones in demo mode (see store\README.md).

# Edge reports "bytes written" on stderr, which Windows PowerShell would treat as a failure.
$ErrorActionPreference = 'Continue'
$source = $PSScriptRoot
$store = Split-Path $source -Parent
$edge = 'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe'
$profile = Join-Path $env:TEMP 'pmn-store-render'

function Render([string]$Query, [int]$Width, [int]$Height, [string]$Out) {
    $url = 'file:///' + ((Join-Path $source 'art.html') -replace '\\', '/') + '?' + $Query
    $target = Join-Path $store $Out
    New-Item -ItemType Directory -Force (Split-Path $target) | Out-Null

    # A separate profile, so the render never touches the user's browser session.
    & $edge --headless=new --disable-gpu --hide-scrollbars --force-device-scale-factor=1 `
        --allow-file-access-from-files --user-data-dir="$profile" `
        --virtual-time-budget=4000 --window-size="$Width,$Height" --screenshot="$target" $url 2>$null | Out-Null
    "$Out  ${Width}x$Height"
}

function Shrink([string]$File, [int]$Size) {
    Add-Type -AssemblyName System.Drawing
    $path = Join-Path $store $File
    $big = [Drawing.Image]::FromFile($path)
    $small = New-Object Drawing.Bitmap $Size, $Size
    $g = [Drawing.Graphics]::FromImage($small)
    $g.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.DrawImage($big, 0, 0, $Size, $Size); $g.Dispose(); $big.Dispose()
    $small.Save($path, [Drawing.Imaging.ImageFormat]::Png); $small.Dispose()
    "  scaled to ${Size}x$Size"
}

function Q([hashtable]$Values) {
    ($Values.GetEnumerator() | ForEach-Object { "$($_.Key)=$([uri]::EscapeDataString($_.Value))" }) -join '&'
}

# Google Play
Render 'kind=icon' 512 512 'google-play\icon-512.png'
Render 'kind=feature' 1024 500 'google-play\feature-graphic-1024x500.png'

$phone = @(
    @('home', 'Is your network OK?', 'Signal, speed and router health at a glance'),
    @('devices', 'Every device on your Wi-Fi', 'Named, sorted, and new ones flagged'),
    @('device', 'Know what each device is', 'Maker, model, type and what it has open'),
    @('wifi-channels', 'Find the quietest channel', 'See which networks crowd yours'),
    @('wifi-all', 'Every network around you', 'Band, channel, security and signal'),
    @('tools', 'Handy network tools', 'Ping, traceroute, DNS, ports, speed test')
)
$i = 1
foreach ($p in $phone) {
    Render (Q @{ kind = 'phone'; shot = "screenshots/android/$($p[0]).png"; caption = $p[1]; sub = $p[2] }) 1080 1920 ("google-play\screenshots\{0:D2}-{1}.png" -f $i, $p[0])
    $i++
}

# Microsoft Store
# Headless Edge will not open a window under 500 px, so small assets render large and scale down.
Render 'kind=tile' 1200 1200 'microsoft-store\app-tile-icon-300.png'
Shrink 'microsoft-store\app-tile-icon-300.png' 300
Render 'kind=boxart' 2160 2160 'microsoft-store\box-art-2160.png'
Render 'kind=poster' 1440 2160 'microsoft-store\poster-art-1440x2160.png'
Render 'kind=hero' 1920 1080 'microsoft-store\super-hero-art-1920x1080.png'

$desktop = @(
    @('home', 'Is your network OK?', 'Signal, speed and router health at a glance'),
    @('devices', 'Know every device on your network', 'Named, sorted, and new ones flagged'),
    @('wifi', 'Make your Wi-Fi faster', 'Insights, a signal walk and a channel map'),
    @('tools', 'Network tools in one place', 'Ping, traceroute, DNS, ports, Wake-on-LAN, speed test'),
    @('devices-light', 'Light or dark, your choice', 'Follows your Windows theme')
)
$i = 1
foreach ($d in $desktop) {
    Render (Q @{ kind = 'desktop'; shot = "screenshots/windows/$($d[0]).png"; caption = $d[1]; sub = $d[2] }) 1920 1080 ("microsoft-store\screenshots\{0:D2}-{1}.png" -f $i, $d[0])
    $i++
}
