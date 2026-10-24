using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiWifiManager;
using MauiWifiManager.Abstractions;
using PingMyNetwork.Controls;
using PingMyNetwork.Helper.Enums;
using PingMyNetwork.Helper.Utilities;
using PingMyNetwork.Implementation;
using PingMyNetwork.Models;
using PingMyNetwork.Resources.Strings;

namespace PingMyNetwork.ViewModels
{
    /// <summary>
    /// The Wi-Fi analyzer: findings about the user's own Wi-Fi, a signal walk, the channel chart and
    /// every network around.
    /// </summary>
    /// <remarks>
    /// A singleton, so switching tabs keeps the section, the walk and its saved spots. Updates from
    /// the scanner and the connection monitor are coalesced into at most one refresh every 500 ms.
    /// </remarks>
    public sealed partial class WifiViewModel : ObservableObject
    {
        #region Variables

        /// <summary>
        /// Shortest gap between two refreshes.
        /// </summary>
        private const int REFRESH_INTERVAL_MS = 500;

        /// <summary>
        /// How often the signal walk takes a reading.
        /// </summary>
        private const int WALK_SAMPLE_MS = 1000;

        /// <summary>
        /// A scan reading of the user's own network newer than this beats the monitor's, which on
        /// Windows only knows signal bars.
        /// </summary>
        private static readonly TimeSpan SCAN_READING_FRESH = TimeSpan.FromSeconds(10);

        private readonly WifiScanner _Scanner;
        private readonly NetworkMonitor _Monitor;
        private readonly IWifiNetworkService _Wifi;
        private readonly List<int> _Samples = [];

        private bool _IsRefreshQueued;
        private IDispatcherTimer? _WalkTimer;
        private int? _ScanRssi;
        private DateTimeOffset _ScanRssiAt;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the view model.
        /// </summary>
        /// <param name="scanner">The Wi-Fi scanner.</param>
        /// <param name="monitor">The connection monitor.</param>
        /// <param name="wifi">The Wi-Fi manager, for handing over to system settings.</param>
        public WifiViewModel(WifiScanner scanner, NetworkMonitor monitor, IWifiNetworkService wifi)
        {
            _Scanner = scanner;
            _Monitor = monitor;
            _Wifi = wifi;

            _Scanner.Changed += (_, _) => QueueRefresh();
            _Monitor.SnapshotChanged += (_, _) => QueueRefresh();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Whether this platform can scan at all; iOS cannot.
        /// </summary>
        public bool IsSupported => WifiScanner.IsSupported;

        /// <summary>
        /// Whether to show the "not available here" message instead of the analyzer.
        /// </summary>
        public bool IsUnsupported => !WifiScanner.IsSupported;

        /// <summary>
        /// The section showing on narrow windows.
        /// </summary>
        [ObservableProperty]
        public partial WifiSection Section { get; set; }

        /// <summary>
        /// Network, band and channel, and how many networks are around.
        /// </summary>
        [ObservableProperty]
        public partial string Summary { get; set; } = AppResources.WifiScanning;

        /// <summary>
        /// Why scanning is held back, or empty.
        /// </summary>
        [ObservableProperty]
        public partial string StatusMessage { get; set; } = string.Empty;

        /// <summary>
        /// Whether there is a scanning problem to show.
        /// </summary>
        [ObservableProperty]
        public partial bool HasStatusMessage { get; set; }

        /// <summary>
        /// The fix the status message offers, or empty.
        /// </summary>
        [ObservableProperty]
        public partial string StatusActionText { get; set; } = string.Empty;

        /// <summary>
        /// Whether the status message has a fix to offer.
        /// </summary>
        [ObservableProperty]
        public partial bool HasStatusAction { get; set; }

        /// <summary>
        /// Findings about the user's Wi-Fi, most serious first.
        /// </summary>
        [ObservableProperty]
        public partial IReadOnlyList<WifiInsight> Insights { get; set; } = [];

        /// <summary>
        /// Whether a scan has arrived and nothing needs attention.
        /// </summary>
        [ObservableProperty]
        public partial bool ShowAllGood { get; set; }

        /// <summary>
        /// Whether there are findings to list.
        /// </summary>
        [ObservableProperty]
        public partial bool HasInsights { get; set; }

        /// <summary>
        /// Whether the first scan is still on its way.
        /// </summary>
        [ObservableProperty]
        public partial bool IsWaitingForScan { get; set; } = true;

        /// <summary>
        /// Every network name around, own first, then strongest first.
        /// </summary>
        public ObservableCollection<WifiNetworkItem> Networks { get; } = [];

        /// <summary>
        /// The bands that have networks, for the chart's band picker.
        /// </summary>
        [ObservableProperty]
        public partial IReadOnlyList<BandOption> BandOptions { get; set; } = [];

        /// <summary>
        /// The band the chart shows.
        /// </summary>
        [ObservableProperty]
        public partial WifiFrequencyBand ChartBand { get; set; } = WifiFrequencyBand.Band2_4GHz;

        /// <summary>
        /// The networks on the chart's band.
        /// </summary>
        [ObservableProperty]
        public partial IReadOnlyList<ChannelCurve> Curves { get; set; } = [];

        /// <summary>
        /// The quietest usual channel on the chart's band.
        /// </summary>
        [ObservableProperty]
        public partial int? SuggestedChannel { get; set; }

        /// <summary>
        /// The chart in a sentence: where the user is and the quietest channel.
        /// </summary>
        [ObservableProperty]
        public partial string ChannelSummary { get; set; } = string.Empty;

        /// <summary>
        /// Whether a signal walk is running.
        /// </summary>
        [ObservableProperty]
        public partial bool IsWalking { get; set; }

        /// <summary>
        /// The walk's start or stop label.
        /// </summary>
        [ObservableProperty]
        public partial string WalkButtonText { get; set; } = AppResources.SignalWalkStart;

        /// <summary>
        /// Current signal as a percentage.
        /// </summary>
        [ObservableProperty]
        public partial string WalkPercentText { get; set; } = AppResources.ValueMissing;

        /// <summary>
        /// Current signal in a word, with dBm where known.
        /// </summary>
        [ObservableProperty]
        public partial string WalkCaption { get; set; } = string.Empty;

        /// <summary>
        /// Current signal as 0 to 1, for the gauge.
        /// </summary>
        [ObservableProperty]
        public partial float WalkFraction { get; set; }

        /// <summary>
        /// Status level of the current signal.
        /// </summary>
        [ObservableProperty]
        public partial StatusLevel WalkLevel { get; set; }

        /// <summary>
        /// Recent readings for the chart, oldest first.
        /// </summary>
        [ObservableProperty]
        public partial IReadOnlyList<int> WalkSamples { get; set; } = [];

        /// <summary>
        /// The place name being typed.
        /// </summary>
        [ObservableProperty]
        public partial string PlaceName { get; set; } = string.Empty;

        /// <summary>
        /// Saved spots, strongest first.
        /// </summary>
        public ObservableCollection<SignalSpot> Spots { get; } = [];

        /// <summary>
        /// Whether any spot has been saved.
        /// </summary>
        [ObservableProperty]
        public partial bool HasSpots { get; set; }

        /// <summary>
        /// One line comparing the strongest and weakest saved spots.
        /// </summary>
        [ObservableProperty]
        public partial string SpotsSummary { get; set; } = string.Empty;

        #endregion

        #region Methods

        /// <summary>
        /// Starts scanning. Called when the analyzer appears.
        /// </summary>
        /// <returns>A task that completes once the scan session has started.</returns>
        public async Task AppearAsync()
        {
            _Monitor.Start();
            Refresh();
            await _Scanner.StartAsync();
        }

        /// <summary>
        /// Stops scanning and the walk. Called when the analyzer is left.
        /// </summary>
        /// <returns>A task that completes once stopped.</returns>
        public async Task DisappearAsync()
        {
            StopWalk();
            await _Scanner.StopAsync();
        }

        /// <summary>
        /// Shows a section.
        /// </summary>
        /// <param name="section">The section's name.</param>
        [RelayCommand]
        private void ShowSection(string section)
        {
            Section = Enum.Parse<WifiSection>(section);
        }

        /// <summary>
        /// Shows a band on the chart.
        /// </summary>
        /// <param name="option">The band.</param>
        [RelayCommand]
        private void ShowBand(BandOption option)
        {
            ChartBand = option.Band;
            Refresh();
        }

        /// <summary>
        /// Does what an insight suggests.
        /// </summary>
        /// <param name="insight">The insight.</param>
        /// <returns>A task that completes once handed over.</returns>
        [RelayCommand]
        private async Task RunInsightAsync(WifiInsight insight)
        {
            switch (insight.Action)
            {
                case InsightAction.OpenChannels:
                    Section = WifiSection.Channels;
                    break;

                case InsightAction.OpenSignalWalk:
                    Section = WifiSection.SignalWalk;

                    if (!IsWalking)
                    {
                        ToggleWalk();
                    }

                    break;

                case InsightAction.OpenWifiSettings:
                    await _Wifi.OpenWifiSetting();
                    break;
            }
        }

        /// <summary>
        /// Fixes what holds scanning back: asks for permission or opens settings.
        /// </summary>
        /// <returns>A task that completes once handed over.</returns>
        [RelayCommand]
        private async Task FixScanningAsync()
        {
            if (_Scanner.Availability == WifiScanAvailability.PermissionDenied)
            {
                await _Scanner.RequestPermissionsAsync();
                return;
            }

            await _Wifi.OpenWifiSetting();
        }

        /// <summary>
        /// Starts or stops the signal walk.
        /// </summary>
        [RelayCommand]
        private void ToggleWalk()
        {
            if (IsWalking)
            {
                StopWalk();
                return;
            }

            IsWalking = true;
            WalkButtonText = AppResources.SignalWalkStop;

            // A walk is watched while moving; the screen must not dim halfway through.
            DeviceDisplay.Current.KeepScreenOn = true;

            _WalkTimer ??= CreateWalkTimer();
            _WalkTimer.Start();
            TakeSample();
        }

        /// <summary>
        /// Saves the current reading under the typed place name.
        /// </summary>
        [RelayCommand]
        private void SaveSpot()
        {
            NetworkSnapshot connection = _Monitor.Current;
            (int? percent, int? rssi) = CurrentReading(connection);

            if (percent is not int value)
            {
                return;
            }

            string name = string.IsNullOrWhiteSpace(PlaceName)
                ? NetworkFormat.Text(AppResources.SignalWalkSpotFormat, Spots.Count + 1)
                : PlaceName.Trim();

            // ponytail: spots live for the session. Saving them per network belongs with History
            // (PRD 6.12), which does not exist yet.
            string valueText = rssi is int dbm
                ? $"{NetworkFormat.Text(AppResources.PercentFormat, value)} · {NetworkFormat.Text(AppResources.DbmFormat, dbm)}"
                : NetworkFormat.Text(AppResources.PercentFormat, value);
            List<SignalSpot> spots = [.. Spots, new SignalSpot(name, value, rssi, NetworkFormat.SignalLevel(value), valueText)];

            Spots.Clear();

            foreach (SignalSpot spot in spots.OrderByDescending(spot => spot.Strength))
            {
                Spots.Add(spot);
            }

            PlaceName = string.Empty;
            HasSpots = true;

            SignalSpot best = Spots[0];
            SignalSpot worst = Spots[^1];

            SpotsSummary = Spots.Count < 2
                ? string.Empty
                : NetworkFormat.Text(AppResources.SignalWalkSpotsSummary, best.Name, best.ValueText, worst.Name, worst.ValueText);
        }

        /// <summary>
        /// Clears the saved spots.
        /// </summary>
        [RelayCommand]
        private void ClearSpots()
        {
            Spots.Clear();
            HasSpots = false;
            SpotsSummary = string.Empty;
        }

        /// <summary>
        /// Stops the walk timer and lets the screen dim again.
        /// </summary>
        private void StopWalk()
        {
            if (!IsWalking)
            {
                return;
            }

            IsWalking = false;
            WalkButtonText = AppResources.SignalWalkStart;
            _WalkTimer?.Stop();
            DeviceDisplay.Current.KeepScreenOn = false;
        }

        /// <summary>
        /// The timer that takes a reading every second while walking.
        /// </summary>
        /// <returns>The timer.</returns>
        private IDispatcherTimer CreateWalkTimer()
        {
            IDispatcherTimer timer = Application.Current!.Dispatcher.CreateTimer();

            timer.Interval = TimeSpan.FromMilliseconds(WALK_SAMPLE_MS);
            timer.Tick += (_, _) => TakeSample();

            return timer;
        }

        /// <summary>
        /// Adds the current reading to the chart.
        /// </summary>
        private void TakeSample()
        {
            (int? percent, _) = CurrentReading(_Monitor.Current);

            if (percent is not int value)
            {
                return;
            }

            _Samples.Add(value);

            if (_Samples.Count > SignalHistoryDrawable.CAPACITY)
            {
                _Samples.RemoveAt(0);
            }

            WalkSamples = [.. _Samples];
            ApplyWalkReading(_Monitor.Current);
        }

        /// <summary>
        /// The best current reading of the connected network: a fresh scan reading where there is
        /// one (it has dBm on Windows), otherwise the monitor's.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <returns>Signal percentage and dBm, either of which may be unknown.</returns>
        private (int? Percent, int? Rssi) CurrentReading(NetworkSnapshot connection)
        {
            if ((_ScanRssi is int rssi) && (DateTimeOffset.Now - _ScanRssiAt < SCAN_READING_FRESH))
            {
                return (WifiSignal.GetSignalPercentage(rssi), rssi);
            }

            return (connection.SignalPercent, connection.RssiDbm);
        }

        /// <summary>
        /// Schedules one refresh for every burst of changes, on the UI thread.
        /// </summary>
        private void QueueRefresh()
        {
            if (_IsRefreshQueued)
            {
                return;
            }

            _IsRefreshQueued = true;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(REFRESH_INTERVAL_MS);
                _IsRefreshQueued = false;
                Refresh();
            });
        }

        /// <summary>
        /// Rebuilds every section from the latest scan and connection.
        /// </summary>
        private void Refresh()
        {
            NetworkSnapshot connection = _Monitor.Current;
            List<NetworkData> networks = _Scanner.GetNetworks();
            string family = ChannelPlanner.FamilyOf(connection.Ssid);

            NoteScanReading(connection, networks);
            ApplyStatus();
            ApplySummary(connection, networks);
            ApplyInsights(connection, networks);
            ApplyChart(connection, networks, family);
            ApplyNetworks(connection, networks);
            ApplyWalkReading(connection);
        }

        /// <summary>
        /// Keeps the scan's reading of the connected network: the strongest access point with its
        /// name, which on Windows is the only source of dBm.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="networks">The latest scan.</param>
        private void NoteScanReading(NetworkSnapshot connection, List<NetworkData> networks)
        {
            int? rssi = networks
                .Where(network => (connection.Ssid is not null) && string.Equals(network.Ssid, connection.Ssid, StringComparison.Ordinal))
                .Max(network => network.RssiDbm);

            if ((rssi is not null) && (_Scanner.LastUpdate is DateTimeOffset at))
            {
                _ScanRssi = rssi;
                _ScanRssiAt = at;
            }
        }

        /// <summary>
        /// Says why scanning is held back, and what fixes it.
        /// </summary>
        private void ApplyStatus()
        {
            (StatusMessage, StatusActionText) = _Scanner.Availability switch
            {
                WifiScanAvailability.PermissionDenied => (AppResources.WifiPermissionNeeded, AppResources.ActionAllow),
                WifiScanAvailability.LocationServicesDisabled => (AppResources.WifiLocationOff, AppResources.ActionOpenWifiSettings),
                WifiScanAvailability.WifiDisabled => (AppResources.StatusWifiOffDetail, AppResources.ActionOpenWifiSettings),
                WifiScanAvailability.Throttled => (AppResources.WifiThrottled, string.Empty),
                _ => (string.Empty, string.Empty),
            };

            HasStatusMessage = StatusMessage.Length > 0;
            HasStatusAction = StatusActionText.Length > 0;
        }

        /// <summary>
        /// One line under the title: the connection and the count of networks around.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="networks">The latest scan.</param>
        private void ApplySummary(NetworkSnapshot connection, List<NetworkData> networks)
        {
            IsWaitingForScan = _Scanner.LastUpdate is null;

            int names = networks.Select(network => network.Ssid ?? string.Empty).Distinct().Count();
            string? band = NetworkFormat.Band(connection.Band);
            string where = (connection.Ssid, band, connection.Channel) switch
            {
                (string ssid, string b, int channel) => NetworkFormat.Text(AppResources.WifiSummaryConnectedFormat, ssid, b, channel),
                (string ssid, _, _) => ssid,
                _ => AppResources.WifiNotConnected,
            };

            Summary = IsWaitingForScan
                ? AppResources.WifiScanning
                : NetworkFormat.Text(AppResources.WifiSummaryFormat, where, names);
        }

        /// <summary>
        /// Rebuilds the findings.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="networks">The latest scan.</param>
        private void ApplyInsights(NetworkSnapshot connection, List<NetworkData> networks)
        {
            List<WifiInsight> insights = WifiInsightBuilder.Build(connection, networks);

            if (!insights.SequenceEqual(Insights))
            {
                Insights = insights;
            }

            HasInsights = insights.Count > 0;
            ShowAllGood = !IsWaitingForScan && (insights.Count == 0);
        }

        /// <summary>
        /// Rebuilds the channel chart for the chosen band.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="networks">The latest scan.</param>
        /// <param name="family">The user's network family.</param>
        private void ApplyChart(NetworkSnapshot connection, List<NetworkData> networks, string family)
        {
            List<WifiFrequencyBand> bands = networks
                .Select(network => network.FrequencyBand)
                .Where(band => band != WifiFrequencyBand.Unknown)
                .Distinct()
                .Order()
                .ToList();

            // Start on the user's own band; keep the user's choice after that.
            if (!bands.Contains(ChartBand) && (bands.Count > 0))
            {
                ChartBand = bands.Contains(connection.Band) ? connection.Band : bands[0];
            }

            List<BandOption> options = bands.Select(band => new BandOption(band, NetworkFormat.Band(band) ?? string.Empty, band == ChartBand)).ToList();

            if (!options.SequenceEqual(BandOptions))
            {
                BandOptions = options;
            }

            Curves = networks
                .Where(network => (network.FrequencyBand == ChartBand) && (network.ChannelNumber is not null) && (network.RssiDbm is not null))
                .Select(network =>
                {
                    bool isOwn = ChannelPlanner.IsOwn(network, family);

                    return new ChannelCurve(network.ChannelNumber!.Value, network.RssiDbm!.Value, isOwn, isOwn ? network.Ssid : null);
                })
                .ToList();

            ChannelScore? quietest = ChannelPlanner.Quietest(networks, ChartBand, family);

            SuggestedChannel = quietest?.Channel;

            if ((connection.Band == ChartBand) && (connection.Channel is int channel))
            {
                ChannelScore own = ChannelPlanner.Score(networks, ChartBand, channel, family);

                ChannelSummary = (quietest is null) || (quietest.Channel == channel)
                    ? NetworkFormat.Text(AppResources.ChannelsOnBestFormat, channel, own.Overlapping)
                    : NetworkFormat.Text(AppResources.ChannelsSummaryFormat, channel, own.Overlapping, quietest.Channel, quietest.Overlapping);
            }
            else
            {
                ChannelSummary = quietest is null
                    ? string.Empty
                    : NetworkFormat.Text(AppResources.ChannelsQuietestFormat, quietest.Channel, quietest.Overlapping);
            }
        }

        /// <summary>
        /// Rebuilds the list of networks: one row per name, the user's own first, then strongest
        /// first.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="networks">The latest scan.</param>
        private void ApplyNetworks(NetworkSnapshot connection, List<NetworkData> networks)
        {
            List<WifiNetworkItem> rows = networks
                .GroupBy(network => network.Ssid ?? string.Empty)
                .Select(group => (Item: ToItem(group.ToList(), connection), Strongest: group.Max(network => network.RssiDbm) ?? int.MinValue))
                .OrderByDescending(row => row.Item.IsOwn)
                .ThenByDescending(row => row.Strongest)
                .Select(row => row.Item)
                .ToList();

            Sync(rows);
        }

        /// <summary>
        /// Formats one network name and its access points as a row.
        /// </summary>
        /// <param name="accessPoints">Every access point with the name.</param>
        /// <param name="connection">The connection, to mark the user's own network.</param>
        /// <returns>The row.</returns>
        private static WifiNetworkItem ToItem(List<NetworkData> accessPoints, NetworkSnapshot connection)
        {
            NetworkData strongest = accessPoints.OrderByDescending(network => network.RssiDbm ?? int.MinValue).First();
            string key = strongest.Ssid ?? string.Empty;
            string name = string.IsNullOrEmpty(key) ? AppResources.HiddenNetworkTitle : key;
            bool isOwn = (connection.Ssid is not null) && (key == connection.Ssid);
            SecurityLevel security = WifiSecurity.Classify(strongest.SecurityType?.ToString());
            int percent = NetworkFormat.SignalPercent(strongest.RssiDbm, strongest.SignalStrength) ?? 0;
            StatusLevel level = NetworkFormat.SignalLevel(percent);

            List<string> parts = [];

            if (NetworkFormat.Band(strongest.FrequencyBand) is string band)
            {
                parts.Add(strongest.ChannelNumber is int channel ? NetworkFormat.Text(AppResources.BandChannelFormat, band, channel) : band);
            }

            if (NetworkFormat.StandardShort(strongest.WifiStandard) is string standard)
            {
                parts.Add(standard);
            }

            parts.Add(SecurityName(security));

            if (accessPoints.Count > 1)
            {
                parts.Add(NetworkFormat.Text(AppResources.AccessPointsFormat, accessPoints.Count));
            }

            string signal = strongest.RssiDbm is int rssi ? NetworkFormat.Text(AppResources.DbmFormat, rssi) : NetworkFormat.Text(AppResources.PercentFormat, percent);
            string subtitle = string.Join(" · ", parts);

            return new WifiNetworkItem(
                key,
                name,
                subtitle,
                signal,
                BarsIcon(percent),
                level,
                isOwn,
                WifiSecurity.IsWeak(security),
                $"{name}, {subtitle}, {signal}{(isOwn ? $", {AppResources.TagConnected}" : string.Empty)}");
        }

        /// <summary>
        /// The bars glyph for a signal percentage.
        /// </summary>
        /// <param name="percent">Signal from 0 to 100.</param>
        /// <returns>A Material Icons glyph.</returns>
        private static string BarsIcon(int percent)
        {
            return percent switch
            {
                >= 80 => MaterialIcon.SIGNAL_4,
                >= 60 => MaterialIcon.SIGNAL_3,
                >= 40 => MaterialIcon.SIGNAL_2,
                >= 20 => MaterialIcon.SIGNAL_1,
                _ => MaterialIcon.SIGNAL_0,
            };
        }

        /// <summary>
        /// The name of a security level.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <returns>For example "WPA2".</returns>
        private static string SecurityName(SecurityLevel level)
        {
            return level switch
            {
                SecurityLevel.Open => AppResources.SecurityOpen,
                SecurityLevel.Wep => "WEP",
                SecurityLevel.Wpa => "WPA",
                SecurityLevel.Wpa2 => "WPA2",
                SecurityLevel.Wpa3 => "WPA3",
                _ => AppResources.SecurityUnknown,
            };
        }

        /// <summary>
        /// Makes the visible list match the new rows, replacing only rows that changed.
        /// </summary>
        /// <param name="rows">The rows in display order.</param>
        private void Sync(List<WifiNetworkItem> rows)
        {
            while (Networks.Count > rows.Count)
            {
                Networks.RemoveAt(Networks.Count - 1);
            }

            for (var i = 0; i < rows.Count; i++)
            {
                if (i >= Networks.Count)
                {
                    Networks.Add(rows[i]);
                }
                else if (Networks[i] != rows[i])
                {
                    Networks[i] = rows[i];
                }
            }
        }

        /// <summary>
        /// Updates the walk's gauge and caption from the current reading.
        /// </summary>
        /// <param name="connection">The connection.</param>
        private void ApplyWalkReading(NetworkSnapshot connection)
        {
            (int? percent, int? rssi) = CurrentReading(connection);

            if (percent is not int value)
            {
                WalkPercentText = AppResources.ValueMissing;
                WalkCaption = AppResources.SignalUnavailable;
                WalkFraction = 0;
                return;
            }

            string word = NetworkFormat.SignalWord(value);

            WalkPercentText = NetworkFormat.Text(AppResources.PercentFormat, value);
            WalkCaption = rssi is int dbm ? $"{word} · {NetworkFormat.Text(AppResources.DbmFormat, dbm)}" : word;
            WalkFraction = value / 100f;
            WalkLevel = NetworkFormat.SignalLevel(value);
        }

        #endregion
    }
}
