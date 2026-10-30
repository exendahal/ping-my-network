using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using MauiWifiManager.Abstractions;
using PingMyNetwork.Helper.Enums;
using PingMyNetwork.Resources.Strings;

namespace PingMyNetwork.Helper.Utilities
{
    /// <summary>
    /// Turns raw network values into the words and numbers the screens show.
    /// </summary>
    public static partial class NetworkFormat
    {
        #region Variables

        /// <summary>
        /// Signal at or above this is Excellent.
        /// </summary>
        private const int EXCELLENT_PERCENT = 80;

        /// <summary>
        /// Signal at or above this is Good. Roughly -70 dBm.
        /// </summary>
        private const int GOOD_PERCENT = 60;

        /// <summary>
        /// Signal at or above this is Fair. Roughly -80 dBm.
        /// </summary>
        private const int FAIR_PERCENT = 40;

        /// <summary>
        /// Router latency at or under this is Good. A home router normally answers in a few ms.
        /// </summary>
        private const double GOOD_LATENCY_MS = 20;

        /// <summary>
        /// Router latency at or under this is Fair.
        /// </summary>
        private const double FAIR_LATENCY_MS = 60;

        #endregion

        #region Methods

        /// <summary>
        /// Formats an IPv4 address stored the way the library stores it: first octet in the lowest
        /// byte.
        /// </summary>
        /// <param name="address">The packed address.</param>
        /// <returns>Dotted notation, or null for 0.</returns>
        public static string? Ipv4(int address)
        {
            if (address == 0)
            {
                return null;
            }

            return new IPAddress(BitConverter.GetBytes(address)).ToString();
        }

        /// <summary>
        /// Keeps a BSSID only when it really is a MAC address. Windows reports the adapter's id in
        /// that field, which would mislead anyone reading it as the access point.
        /// </summary>
        /// <param name="bssid">Whatever the platform reported.</param>
        /// <returns>The MAC address in upper case, or null.</returns>
        public static string? Bssid(object? bssid)
        {
            string? text = bssid?.ToString();

            // Android hands apps without location access a placeholder instead of the real one.
            if (string.IsNullOrWhiteSpace(text) || !MacPattern().IsMatch(text) || (text == "02:00:00:00:00:00"))
            {
                return null;
            }

            return text.ToUpperInvariant();
        }

        /// <summary>
        /// Signal quality as a percentage, from whichever reading the platform provides.
        /// </summary>
        /// <param name="rssiDbm">Raw strength, when known.</param>
        /// <param name="signalBars">Signal bars from 0 to 5, which is all Windows reports.</param>
        /// <returns>0 to 100, or null when neither is known.</returns>
        public static int? SignalPercent(int? rssiDbm, object? signalBars)
        {
            if (rssiDbm is int rssi && rssi < 0)
            {
                return WifiSignal.GetSignalPercentage(rssi);
            }

            if (signalBars is not null
                && int.TryParse(signalBars.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int bars))
            {
                return Math.Clamp(bars * 20, 0, 100);
            }

            return null;
        }

        /// <summary>
        /// The status level for a signal percentage.
        /// </summary>
        /// <param name="percent">Signal from 0 to 100.</param>
        /// <returns>Good, Fair, Poor or Bad.</returns>
        public static StatusLevel SignalLevel(int percent)
        {
            if (percent >= GOOD_PERCENT)
            {
                return StatusLevel.Good;
            }

            if (percent >= FAIR_PERCENT)
            {
                return StatusLevel.Fair;
            }

            return percent >= FAIR_PERCENT / 2 ? StatusLevel.Poor : StatusLevel.Bad;
        }

        /// <summary>
        /// One word for a signal percentage.
        /// </summary>
        /// <param name="percent">Signal from 0 to 100.</param>
        /// <returns>Excellent, Good, Fair or Weak.</returns>
        public static string SignalWord(int percent)
        {
            if (percent >= EXCELLENT_PERCENT)
            {
                return AppResources.SignalExcellent;
            }

            if (percent >= GOOD_PERCENT)
            {
                return AppResources.SignalGood;
            }

            return percent >= FAIR_PERCENT ? AppResources.SignalFair : AppResources.SignalWeak;
        }

        /// <summary>
        /// The status level for a round trip to the router.
        /// </summary>
        /// <param name="milliseconds">Average round trip.</param>
        /// <returns>Good, Fair or Poor.</returns>
        public static StatusLevel LatencyLevel(double milliseconds)
        {
            if (milliseconds <= GOOD_LATENCY_MS)
            {
                return StatusLevel.Good;
            }

            return milliseconds <= FAIR_LATENCY_MS ? StatusLevel.Fair : StatusLevel.Poor;
        }

        /// <summary>
        /// A round trip for display. Ping reports whole milliseconds, so a LAN reply often reads 0.
        /// </summary>
        /// <param name="milliseconds">The round trip.</param>
        /// <returns>For example "12 ms", or "&lt; 1 ms".</returns>
        public static string Latency(double milliseconds)
        {
            return milliseconds < 1 ? AppResources.LatencyUnderOne : Text(AppResources.LatencyFormat, milliseconds);
        }

        /// <summary>
        /// The name of a radio band.
        /// </summary>
        /// <param name="band">The band.</param>
        /// <returns>For example "5 GHz", or null when unknown.</returns>
        public static string? Band(WifiFrequencyBand band)
        {
            return band switch
            {
                WifiFrequencyBand.Band2_4GHz => AppResources.Band24,
                WifiFrequencyBand.Band5GHz => AppResources.Band5,
                WifiFrequencyBand.Band6GHz => AppResources.Band6,
                _ => null,
            };
        }

        /// <summary>
        /// The marketing name of a Wi-Fi standard, which is what people recognise.
        /// </summary>
        /// <param name="standard">The standard.</param>
        /// <returns>For example "Wi-Fi 6 (802.11ax)", or null when unknown.</returns>
        public static string? Standard(WifiStandard standard)
        {
            return standard switch
            {
                WifiStandard.Legacy => AppResources.StandardLegacy,
                WifiStandard.Ieee80211n => AppResources.Standard4,
                WifiStandard.Ieee80211ac => AppResources.Standard5,
                WifiStandard.Ieee80211ax => AppResources.Standard6,
                WifiStandard.Ieee80211ad => AppResources.StandardAd,
                WifiStandard.Ieee80211be => AppResources.Standard7,
                _ => null,
            };
        }

        /// <summary>
        /// A short name for a Wi-Fi standard, for tiles where the full name does not fit.
        /// </summary>
        /// <param name="standard">The standard.</param>
        /// <returns>For example "Wi-Fi 6", or null when unknown.</returns>
        public static string? StandardShort(WifiStandard standard)
        {
            return standard switch
            {
                WifiStandard.Legacy => AppResources.StandardShortLegacy,
                WifiStandard.Ieee80211n => Text(AppResources.StandardShortFormat, 4),
                WifiStandard.Ieee80211ac => Text(AppResources.StandardShortFormat, 5),
                WifiStandard.Ieee80211ax => Text(AppResources.StandardShortFormat, 6),
                WifiStandard.Ieee80211be => Text(AppResources.StandardShortFormat, 7),
                WifiStandard.Ieee80211ad => AppResources.StandardAd,
                _ => null,
            };
        }

        /// <summary>
        /// The name of a connection type.
        /// </summary>
        /// <param name="type">The transport.</param>
        /// <returns>Wi-Fi, Ethernet, Mobile data or Unknown.</returns>
        public static string ConnectionType(NetworkConnectionType type)
        {
            return type switch
            {
                NetworkConnectionType.WiFi => AppResources.ConnectionWifi,
                NetworkConnectionType.Ethernet => AppResources.ConnectionEthernet,
                NetworkConnectionType.Cellular => AppResources.ConnectionCellular,
                _ => AppResources.ConnectionUnknown,
            };
        }

        /// <summary>
        /// Formats a resource pattern with the current culture.
        /// </summary>
        /// <param name="format">The pattern from AppResources.</param>
        /// <param name="args">Values for its placeholders.</param>
        /// <returns>The formatted text.</returns>
        public static string Text(string format, params object?[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, format, args);
        }

        /// <summary>
        /// A count of overlapping networks in words: "no overlapping networks", "1 overlapping
        /// network", "3 overlapping networks".
        /// </summary>
        /// <param name="count">How many.</param>
        /// <returns>The phrase.</returns>
        public static string Overlapping(int count)
        {
            return count switch
            {
                0 => AppResources.OverlapNone,
                1 => AppResources.OverlapOne,
                _ => Text(AppResources.OverlapManyFormat, count),
            };
        }

        /// <summary>
        /// Shortens a registered company name to what people call their provider:
        /// "WorldLink Communications Pvt. Ltd." becomes "WorldLink Communications".
        /// </summary>
        /// <param name="organization">The name from the address registry.</param>
        /// <returns>The short name, or null.</returns>
        public static string? Provider(string? organization)
        {
            if (string.IsNullOrWhiteSpace(organization))
            {
                return null;
            }

            string name = CompanySuffix().Replace(organization.Trim(), string.Empty).Trim().TrimEnd(',', '.').Trim();

            return name.Length > 0 ? name : organization.Trim();
        }

        /// <summary>
        /// Runs the formatting rules against known cases. Called at startup in Debug builds.
        /// </summary>
        [Conditional("DEBUG")]
        public static void SelfCheck()
        {
            Debug.Assert(Provider("WorldLink Communications Pvt. Ltd.") == "WorldLink Communications");
            Debug.Assert(Provider("Comcast Cable Communications, LLC") == "Comcast Cable Communications");
            Debug.Assert(Provider("Deutsche Telekom AG") == "Deutsche Telekom");
            Debug.Assert(Provider("Google LLC") == "Google");
            Debug.Assert(Provider("Ltd") == "Ltd");
            Debug.Assert(Provider(" ") is null);
        }

        /// <summary>
        /// Legal endings of company names, with the punctuation before them.
        /// </summary>
        /// <returns>The compiled pattern.</returns>
        [GeneratedRegex(@"[,\s]+(pvt\.?\s*ltd\.?|private limited|pty\.?\s*ltd\.?|ltd\.?|limited|llc|inc\.?|corp\.?|corporation|gmbh|ag|s\.?a\.?|b\.?v\.?|plc)$", RegexOptions.IgnoreCase)]
        private static partial Regex CompanySuffix();

        /// <summary>
        /// Matches a MAC address written with colons or dashes.
        /// </summary>
        /// <returns>The compiled pattern.</returns>
        [GeneratedRegex("^([0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}$")]
        private static partial Regex MacPattern();

        #endregion
    }
}
