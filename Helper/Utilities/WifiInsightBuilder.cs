using MauiWifiManager.Abstractions;
using PingMyNetwork.Helper.Enums;
using PingMyNetwork.Models;
using PingMyNetwork.Resources.Strings;

namespace PingMyNetwork.Helper.Utilities
{
    /// <summary>
    /// Turns a scan and the current connection into findings about the user's own Wi-Fi.
    /// </summary>
    /// <remarks>
    /// Each rule only speaks when it is confident; a finding the scan cannot support (no BSSID, no
    /// channel) is left out rather than guessed. Findings come back most serious first.
    /// </remarks>
    public static class WifiInsightBuilder
    {
        #region Variables

        /// <summary>
        /// Below this signal percentage (about -80 dBm) a connection struggles.
        /// </summary>
        private const int WEAK_SIGNAL_PERCENT = 40;

        /// <summary>
        /// A 5/6 GHz access point at least this strong is worth moving to.
        /// </summary>
        private const int USABLE_RSSI = -70;

        /// <summary>
        /// Another access point must be this much stronger before suggesting it.
        /// </summary>
        private const int STRONGER_AP_MARGIN_DB = 10;

        /// <summary>
        /// The user's channel must be at least this crowded before calling it crowded.
        /// </summary>
        private const double CROWDED_SCORE = 2.0;

        /// <summary>
        /// And at least this much more crowded than the suggested channel.
        /// </summary>
        private const double CROWDED_MARGIN = 1.5;

        #endregion

        #region Methods

        /// <summary>
        /// Every finding that applies, most serious first.
        /// </summary>
        /// <param name="connection">The current connection.</param>
        /// <param name="networks">The latest scan.</param>
        /// <returns>The findings; empty when nothing needs attention.</returns>
        public static List<WifiInsight> Build(NetworkSnapshot connection, IReadOnlyList<NetworkData> networks)
        {
            List<WifiInsight> insights = [];

            if ((connection.ConnectionType != NetworkConnectionType.WiFi) || (connection.Ssid is not string ssid))
            {
                return insights;
            }

            string family = ChannelPlanner.FamilyOf(ssid);
            SecurityLevel security = WifiSecurity.Classify(connection.Security);

            AddWeakSecurity(insights, ssid, security, connection.Security);
            AddLookAlike(insights, ssid, security, networks);
            AddCrowded(insights, connection, networks, family);
            AddOtherBand(insights, connection, networks, family);
            AddStrongerAccessPoint(insights, connection, networks, ssid);
            AddWeakSignal(insights, connection);
            AddOldStandard(insights, connection);

            return insights.OrderByDescending(insight => Severity(insight.Level)).ToList();
        }

        /// <summary>
        /// Open, WEP or WPA (TKIP) on the user's own network.
        /// </summary>
        /// <param name="insights">The findings so far.</param>
        /// <param name="ssid">The user's network.</param>
        /// <param name="security">Its protection.</param>
        /// <param name="securityText">How the platform names it.</param>
        private static void AddWeakSecurity(List<WifiInsight> insights, string ssid, SecurityLevel security, string? securityText)
        {
            if (!WifiSecurity.IsWeak(security))
            {
                return;
            }

            insights.Add(new WifiInsight(
                StatusLevel.Bad,
                MaterialIcon.LOCK,
                AppResources.InsightWeakSecurityTitle,
                NetworkFormat.Text(AppResources.InsightWeakSecurityBody, ssid, securityText)));
        }

        /// <summary>
        /// Another network using the user's exact name with weaker protection, the shape of an
        /// "evil twin" access point.
        /// </summary>
        /// <param name="insights">The findings so far.</param>
        /// <param name="ssid">The user's network.</param>
        /// <param name="security">Its protection.</param>
        /// <param name="networks">The latest scan.</param>
        private static void AddLookAlike(List<WifiInsight> insights, string ssid, SecurityLevel security, IReadOnlyList<NetworkData> networks)
        {
            if (security == SecurityLevel.Unknown)
            {
                return;
            }

            bool hasLookAlike = networks.Any(network =>
                string.Equals(network.Ssid, ssid, StringComparison.Ordinal)
                && (WifiSecurity.Classify(network.SecurityType?.ToString()) is SecurityLevel other)
                && (other != SecurityLevel.Unknown)
                && (other < security));

            if (hasLookAlike)
            {
                insights.Add(new WifiInsight(
                    StatusLevel.Bad,
                    MaterialIcon.WARNING,
                    AppResources.InsightLookAlikeTitle,
                    NetworkFormat.Text(AppResources.InsightLookAlikeBody, ssid)));
            }
        }

        /// <summary>
        /// The user's channel is clearly more crowded than the best alternative.
        /// </summary>
        /// <param name="insights">The findings so far.</param>
        /// <param name="connection">The current connection.</param>
        /// <param name="networks">The latest scan.</param>
        /// <param name="family">The user's network family.</param>
        private static void AddCrowded(List<WifiInsight> insights, NetworkSnapshot connection, IReadOnlyList<NetworkData> networks, string family)
        {
            if (connection.Channel is not int channel)
            {
                return;
            }

            ChannelScore own = ChannelPlanner.Score(networks, connection.Band, channel, family);
            ChannelScore? quietest = ChannelPlanner.Quietest(networks, connection.Band, family);

            if ((quietest is null)
                || (quietest.Channel == channel)
                || (own.Score < CROWDED_SCORE)
                || (own.Score < quietest.Score + CROWDED_MARGIN))
            {
                return;
            }

            insights.Add(new WifiInsight(
                StatusLevel.Poor,
                MaterialIcon.ROUTER,
                NetworkFormat.Text(AppResources.InsightCrowdedTitle, channel),
                NetworkFormat.Text(AppResources.InsightCrowdedBody, own.Overlapping, quietest.Channel, quietest.Overlapping),
                InsightAction.OpenChannels,
                AppResources.ActionSeeChannels));
        }

        /// <summary>
        /// On 2.4 GHz while the same network is usable on 5 or 6 GHz here.
        /// </summary>
        /// <param name="insights">The findings so far.</param>
        /// <param name="connection">The current connection.</param>
        /// <param name="networks">The latest scan.</param>
        /// <param name="family">The user's network family.</param>
        private static void AddOtherBand(List<WifiInsight> insights, NetworkSnapshot connection, IReadOnlyList<NetworkData> networks, string family)
        {
            if (connection.Band != WifiFrequencyBand.Band2_4GHz)
            {
                return;
            }

            NetworkData? faster = networks
                .Where(network => (network.FrequencyBand is WifiFrequencyBand.Band5GHz or WifiFrequencyBand.Band6GHz)
                    && ChannelPlanner.IsOwn(network, family)
                    && (network.RssiDbm >= USABLE_RSSI))
                .OrderByDescending(network => network.RssiDbm)
                .FirstOrDefault();

            if (faster is null)
            {
                return;
            }

            insights.Add(new WifiInsight(
                StatusLevel.Fair,
                MaterialIcon.WIFI,
                NetworkFormat.Text(AppResources.InsightOtherBandTitle, NetworkFormat.Band(faster.FrequencyBand)),
                NetworkFormat.Text(AppResources.InsightOtherBandBody, faster.Ssid, NetworkFormat.Band(faster.FrequencyBand)),
                InsightAction.OpenWifiSettings,
                AppResources.ActionOpenWifiSettings));
        }

        /// <summary>
        /// Another access point of the same network is clearly stronger. Needs the connected BSSID,
        /// which Windows does not report, so it only runs where the platform gives one.
        /// </summary>
        /// <param name="insights">The findings so far.</param>
        /// <param name="connection">The current connection.</param>
        /// <param name="networks">The latest scan.</param>
        /// <param name="ssid">The user's network.</param>
        private static void AddStrongerAccessPoint(List<WifiInsight> insights, NetworkSnapshot connection, IReadOnlyList<NetworkData> networks, string ssid)
        {
            if (connection.Bssid is not string bssid)
            {
                return;
            }

            List<NetworkData> mine = networks.Where(network => string.Equals(network.Ssid, ssid, StringComparison.Ordinal)).ToList();
            int? current = mine
                .FirstOrDefault(network => string.Equals(NetworkFormat.Bssid(network.Bssid), bssid, StringComparison.OrdinalIgnoreCase))?
                .RssiDbm;
            int? strongest = mine
                .Where(network => !string.Equals(NetworkFormat.Bssid(network.Bssid), bssid, StringComparison.OrdinalIgnoreCase))
                .Max(network => network.RssiDbm);

            if ((current is int now) && (strongest is int best) && (best - now >= STRONGER_AP_MARGIN_DB))
            {
                insights.Add(new WifiInsight(
                    StatusLevel.Fair,
                    MaterialIcon.ROUTER,
                    AppResources.InsightStrongerApTitle,
                    NetworkFormat.Text(AppResources.InsightStrongerApBody, best - now),
                    InsightAction.OpenWifiSettings,
                    AppResources.ActionOpenWifiSettings));
            }
        }

        /// <summary>
        /// The signal where the user is now is weak.
        /// </summary>
        /// <param name="insights">The findings so far.</param>
        /// <param name="connection">The current connection.</param>
        private static void AddWeakSignal(List<WifiInsight> insights, NetworkSnapshot connection)
        {
            if (connection.SignalPercent is not int percent || percent >= WEAK_SIGNAL_PERCENT)
            {
                return;
            }

            insights.Add(new WifiInsight(
                StatusLevel.Poor,
                MaterialIcon.WIFI,
                AppResources.InsightWeakSignalTitle,
                NetworkFormat.Text(AppResources.InsightWeakSignalBody, percent),
                InsightAction.OpenSignalWalk,
                AppResources.ActionStartSignalWalk));
        }

        /// <summary>
        /// The router is Wi-Fi 4 or older.
        /// </summary>
        /// <param name="insights">The findings so far.</param>
        /// <param name="connection">The current connection.</param>
        private static void AddOldStandard(List<WifiInsight> insights, NetworkSnapshot connection)
        {
            if (connection.Standard is not (WifiStandard.Legacy or WifiStandard.Ieee80211n))
            {
                return;
            }

            insights.Add(new WifiInsight(
                StatusLevel.Fair,
                MaterialIcon.SPEED,
                AppResources.InsightOldStandardTitle,
                NetworkFormat.Text(AppResources.InsightOldStandardBody, NetworkFormat.Standard(connection.Standard))));
        }

        /// <summary>
        /// Orders levels from most to least serious.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <returns>A number; higher is more serious.</returns>
        private static int Severity(StatusLevel level)
        {
            return level switch
            {
                StatusLevel.Bad => 3,
                StatusLevel.Poor => 2,
                StatusLevel.Fair => 1,
                _ => 0,
            };
        }

        #endregion
    }
}
