using System.Diagnostics;
using MauiWifiManager.Abstractions;

namespace PingMyNetwork.Helper.Utilities
{
    /// <summary>
    /// How crowded a channel is, from the networks a scan can see.
    /// </summary>
    /// <param name="Band">The radio band.</param>
    /// <param name="Channel">The channel number.</param>
    /// <param name="Score">Overlap weighted by signal; higher is more crowded.</param>
    /// <param name="Overlapping">Networks that overlap the channel noticeably.</param>
    public sealed record ChannelScore(WifiFrequencyBand Band, int Channel, double Score, int Overlapping);

    /// <summary>
    /// Estimates channel congestion and suggests a quieter channel.
    /// </summary>
    /// <remarks>
    /// A scan shows who is on a channel, not how busy they are, so this is an estimate (PRD 6.6.3):
    /// each other network counts by how much its channel overlaps, weighted by how strong it is
    /// here. On 2.4 GHz a 20 MHz channel spans five channel numbers, so neighbours overlap
    /// partially; on 5 and 6 GHz only the same channel is counted. Only 1, 6 and 11 are suggested on
    /// 2.4 GHz, the only three that do not overlap each other.
    /// </remarks>
    public static class ChannelPlanner
    {
        #region Variables

        /// <summary>
        /// The non-overlapping 2.4 GHz channels.
        /// </summary>
        public static readonly int[] PREFERRED_2_4 = [1, 6, 11];

        /// <summary>
        /// Common 5 GHz channels that avoid radar-detection (DFS) delays.
        /// </summary>
        public static readonly int[] PREFERRED_5 = [36, 40, 44, 48, 149, 153, 157, 161];

        /// <summary>
        /// A network overlapping by at least this much counts as "overlapping" in the text.
        /// </summary>
        private const double OVERLAP_COUNTED = 0.5;

        /// <summary>
        /// Name endings routers use to tell the bands of one network apart.
        /// </summary>
        private static readonly string[] BAND_SUFFIXES =
            ["-5ghz", "_5ghz", " 5ghz", "-5g", "_5g", " 5g", "5g", "-2.4ghz", "_2.4ghz", "-2.4g", "_2.4g", "-2g", "_2g", "-6g", "_6g"];

        #endregion

        #region Methods

        /// <summary>
        /// The name of a network with any band suffix removed, so "Home-5G" and "Home" are one family.
        /// </summary>
        /// <param name="ssid">The network name.</param>
        /// <returns>The lower-case family name, or empty for a hidden network.</returns>
        public static string FamilyOf(string? ssid)
        {
            string name = (ssid ?? string.Empty).Trim().ToLowerInvariant();

            foreach (string suffix in BAND_SUFFIXES)
            {
                if ((name.Length > suffix.Length) && name.EndsWith(suffix, StringComparison.Ordinal))
                {
                    return name[..^suffix.Length].TrimEnd('-', '_', ' ');
                }
            }

            return name;
        }

        /// <summary>
        /// How much a network's strength counts: strong neighbours interfere more than faint ones.
        /// </summary>
        /// <param name="rssiDbm">Signal strength, or null when unknown.</param>
        /// <returns>From 0.1 (about -95 dBm) to 1 (-50 dBm or stronger).</returns>
        public static double Weight(int? rssiDbm)
        {
            return rssiDbm is int rssi ? Math.Clamp((rssi + 95) / 45.0, 0.1, 1.0) : 0.5;
        }

        /// <summary>
        /// How much two channels overlap.
        /// </summary>
        /// <param name="band">The band both are on.</param>
        /// <param name="first">One channel.</param>
        /// <param name="second">The other.</param>
        /// <returns>From 0 (no overlap) to 1 (same channel).</returns>
        public static double Overlap(WifiFrequencyBand band, int first, int second)
        {
            if (band != WifiFrequencyBand.Band2_4GHz)
            {
                return first == second ? 1 : 0;
            }

            int distance = Math.Abs(first - second);

            return distance >= 5 ? 0 : (5 - distance) / 5.0;
        }

        /// <summary>
        /// How crowded a channel is, leaving out the user's own network family.
        /// </summary>
        /// <param name="networks">Scan results.</param>
        /// <param name="band">The band.</param>
        /// <param name="channel">The channel to score.</param>
        /// <param name="ownFamilies">The user's networks, from <see cref="OwnFamilies"/>.</param>
        /// <returns>The score.</returns>
        public static ChannelScore Score(IEnumerable<NetworkData> networks, WifiFrequencyBand band, int channel, IReadOnlyCollection<string> ownFamilies)
        {
            double score = 0;
            int overlapping = 0;

            foreach (NetworkData network in networks)
            {
                if ((network.FrequencyBand != band) || (network.ChannelNumber is not int other) || IsOwn(network, ownFamilies))
                {
                    continue;
                }

                double overlap = Overlap(band, channel, other);

                score += overlap * Weight(network.RssiDbm);

                if (overlap >= OVERLAP_COUNTED)
                {
                    overlapping++;
                }
            }

            return new ChannelScore(band, channel, score, overlapping);
        }

        /// <summary>
        /// The least crowded of the usual channels on a band.
        /// </summary>
        /// <param name="networks">Scan results.</param>
        /// <param name="band">The band; only 2.4 and 5 GHz get suggestions.</param>
        /// <param name="ownFamilies">The user's networks.</param>
        /// <returns>The quietest channel, or null for other bands.</returns>
        public static ChannelScore? Quietest(IReadOnlyList<NetworkData> networks, WifiFrequencyBand band, IReadOnlyCollection<string> ownFamilies)
        {
            int[] candidates = band switch
            {
                WifiFrequencyBand.Band2_4GHz => PREFERRED_2_4,
                WifiFrequencyBand.Band5GHz => PREFERRED_5,
                _ => [],
            };

            return candidates
                .Select(channel => Score(networks, band, channel, ownFamilies))
                .OrderBy(score => score.Score)
                .ThenBy(score => score.Channel)
                .FirstOrDefault();
        }

        /// <summary>
        /// Whether a scan result belongs to the user's own networks.
        /// </summary>
        /// <param name="network">The scan result.</param>
        /// <param name="ownFamilies">The user's networks.</param>
        /// <returns>True for the user's own access points.</returns>
        public static bool IsOwn(NetworkData network, IReadOnlyCollection<string> ownFamilies)
        {
            return (FamilyOf(network.Ssid) is { Length: > 0 } family) && ownFamilies.Contains(family);
        }

        /// <summary>
        /// The user's networks, by family name: the one they are connected to, and any other name
        /// the same router broadcasts, such as "flyoderr_2" beside "flyoderr_5". Those share
        /// nothing in their names a rule could rely on, but the router gives its networks
        /// neighbouring addresses.
        /// </summary>
        /// <param name="ssid">The connected network's name.</param>
        /// <param name="bssid">The connected access point, when the platform reports it.</param>
        /// <param name="networks">The latest scan.</param>
        /// <returns>The family names; empty when not on a named Wi-Fi network.</returns>
        public static IReadOnlyCollection<string> OwnFamilies(string? ssid, string? bssid, IEnumerable<NetworkData> networks)
        {
            HashSet<string> families = [];

            if (FamilyOf(ssid) is { Length: > 0 } family)
            {
                families.Add(family);
            }

            if (bssid is null)
            {
                return families;
            }

            foreach (NetworkData network in networks)
            {
                if ((NetworkFormat.Bssid(network.Bssid) is string other)
                    && DeviceIdentity.IsSameRouter(other, bssid)
                    && (FamilyOf(network.Ssid) is { Length: > 0 } sibling))
                {
                    families.Add(sibling);
                }
            }

            return families;
        }

        /// <summary>
        /// Runs the rules against known cases. Called at startup in Debug builds.
        /// </summary>
        [Conditional("DEBUG")]
        public static void SelfCheck()
        {
            Debug.Assert(FamilyOf("Home-5G") == "home");
            Debug.Assert(FamilyOf("Home_5GHz") == "home");
            Debug.Assert(FamilyOf("Home") == "home");
            Debug.Assert(FamilyOf("5G") == "5g");

            Debug.Assert(Overlap(WifiFrequencyBand.Band2_4GHz, 6, 6) == 1);
            Debug.Assert(Overlap(WifiFrequencyBand.Band2_4GHz, 1, 6) == 0);
            Debug.Assert(Overlap(WifiFrequencyBand.Band2_4GHz, 6, 8) > 0);
            Debug.Assert(Overlap(WifiFrequencyBand.Band5GHz, 36, 40) == 0);

            List<NetworkData> scan =
            [
                new() { Ssid = "Home", FrequencyBand = WifiFrequencyBand.Band2_4GHz, ChannelNumber = 6, RssiDbm = -40 },
                new() { Ssid = "A", FrequencyBand = WifiFrequencyBand.Band2_4GHz, ChannelNumber = 6, RssiDbm = -50 },
                new() { Ssid = "B", FrequencyBand = WifiFrequencyBand.Band2_4GHz, ChannelNumber = 7, RssiDbm = -60 },
                new() { Ssid = "C", FrequencyBand = WifiFrequencyBand.Band2_4GHz, ChannelNumber = 1, RssiDbm = -55 },
            ];

            // Our own network does not count against our channel.
            ChannelScore own = Score(scan, WifiFrequencyBand.Band2_4GHz, 6, ["home"]);
            Debug.Assert(own.Overlapping == 2);

            // Channel 1 has a strong neighbour and 11 only a partial overlap from 7, so 11 wins.
            Debug.Assert(Quietest(scan, WifiFrequencyBand.Band2_4GHz, ["home"])?.Channel == 11);

            // The router's other network counts as the user's, whatever its name.
            List<NetworkData> router =
            [
                new() { Ssid = "flyoderr_2", Bssid = "D8:B0:20:B7:0C:6C", FrequencyBand = WifiFrequencyBand.Band2_4GHz },
                new() { Ssid = "Neighbour", Bssid = "10:20:30:40:50:60", FrequencyBand = WifiFrequencyBand.Band2_4GHz },
            ];
            IReadOnlyCollection<string> mine = OwnFamilies("flyoderr_5", "D8:B0:20:B7:0C:6D", router);
            Debug.Assert(mine.Contains("flyoderr_5") && mine.Contains("flyoderr_2") && !mine.Contains("neighbour"));
        }

        #endregion
    }
}
