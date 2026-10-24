using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiWifiManager;
using MauiWifiManager.Abstractions;
using PingMyNetwork.Controls;
using PingMyNetwork.Helper.Utilities;
using PingMyNetwork.Implementation;
using PingMyNetwork.Models;
using PingMyNetwork.Resources.Strings;

namespace PingMyNetwork.ViewModels
{
    /// <summary>
    /// Pings a host once a second, a set number of times or until stopped, and keeps live statistics.
    /// </summary>
    public sealed partial class PingViewModel : ToolViewModel
    {
        #region Variables

        /// <summary>
        /// Time from one ping's start to the next.
        /// </summary>
        private static readonly TimeSpan INTERVAL = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Key of the "until stopped" choice.
        /// </summary>
        private const string CONTINUOUS = "0";

        /// <summary>
        /// Pinged when there is no router to suggest.
        /// </summary>
        private const string FALLBACK_HOST = "one.one.one.one";

        private readonly IWifiNetworkService _Wifi;

        private readonly List<long?> _Samples = [];

        private readonly List<long> _Replies = [];

        private int _Sent;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the view model, suggesting the router as the host.
        /// </summary>
        /// <param name="wifi">The library.</param>
        /// <param name="monitor">The connection monitor, for the router's address.</param>
        public PingViewModel(IWifiNetworkService wifi, NetworkMonitor monitor)
        {
            _Wifi = wifi;

            Host = monitor.Current.Gateway ?? FALLBACK_HOST;
            Counts =
            [
                new ChoiceOption("4", NetworkFormat.Text(AppResources.PingCountFormat, 4), true),
                new ChoiceOption("20", NetworkFormat.Text(AppResources.PingCountFormat, 20), false),
                new ChoiceOption(CONTINUOUS, AppResources.PingContinuous, false),
            ];
        }

        #endregion

        #region Properties

        /// <summary>
        /// The host name or address to ping.
        /// </summary>
        [ObservableProperty]
        public partial string Host { get; set; }

        /// <summary>
        /// How many pings to send.
        /// </summary>
        public ObservableCollection<ChoiceOption> Counts { get; }

        /// <summary>
        /// Round trips for the chart, oldest first; null for a lost ping.
        /// </summary>
        [ObservableProperty]
        public partial IReadOnlyList<long?> Samples { get; set; } = [];

        /// <summary>
        /// The latest reply, e.g. "12 ms" or "Lost".
        /// </summary>
        [ObservableProperty]
        public partial string LastText { get; set; } = AppResources.ValueMissing;

        /// <summary>
        /// Average round trip.
        /// </summary>
        [ObservableProperty]
        public partial string AverageText { get; set; } = AppResources.ValueMissing;

        /// <summary>
        /// Fastest and slowest, e.g. "3 / 41 ms".
        /// </summary>
        [ObservableProperty]
        public partial string RangeText { get; set; } = AppResources.ValueMissing;

        /// <summary>
        /// Average change between consecutive replies.
        /// </summary>
        [ObservableProperty]
        public partial string JitterText { get; set; } = AppResources.ValueMissing;

        /// <summary>
        /// Lost pings, e.g. "0% (0 of 20)".
        /// </summary>
        [ObservableProperty]
        public partial string LossText { get; set; } = AppResources.ValueMissing;

        /// <summary>
        /// Whether any ping has been sent, so the results show.
        /// </summary>
        [ObservableProperty]
        public partial bool HasResults { get; set; }

        /// <summary>
        /// Reads the chart aloud: what was pinged and the average.
        /// </summary>
        [ObservableProperty]
        public partial string Summary { get; set; } = string.Empty;

        #endregion

        #region Methods

        /// <summary>
        /// Picks how many pings to send.
        /// </summary>
        /// <param name="key">The choice's key.</param>
        [RelayCommand]
        private void SelectCount(string key)
        {
            for (var i = 0; i < Counts.Count; i++)
            {
                Counts[i] = Counts[i] with { IsSelected = Counts[i].Key == key };
            }
        }

        /// <summary>
        /// Pings until the count is reached or the user stops.
        /// </summary>
        /// <param name="token">Cancelled on stop.</param>
        /// <returns>A task that completes when pinging ends.</returns>
        protected override async Task RunCoreAsync(CancellationToken token)
        {
            string host = Host.Trim();
            int count = int.Parse(Counts.First(option => option.IsSelected).Key);
            bool isContinuous = count == 0;

            if (host.Length == 0)
            {
                ErrorText = AppResources.ToolHostRequired;
                return;
            }

            _Samples.Clear();
            _Replies.Clear();
            _Sent = 0;
            HasResults = true;
            Publish();

            while (isContinuous || (_Sent < count))
            {
                var started = Stopwatch.StartNew();
                WifiManagerResponse<PingStatistics> response = await _Wifi.PingAsync(host, 1, token);

                token.ThrowIfCancellationRequested();

                if (!Succeeded(response))
                {
                    // A name that does not resolve fails every time; carrying on would only repeat it.
                    return;
                }

                Add(response.Data is { Received: > 0, MinMs: long milliseconds } ? milliseconds : null);

                if (isContinuous || (_Sent < count))
                {
                    TimeSpan wait = INTERVAL - started.Elapsed;

                    if (wait > TimeSpan.Zero)
                    {
                        await Task.Delay(wait, token);
                    }
                }
            }
        }

        /// <summary>
        /// Records one ping and refreshes the statistics.
        /// </summary>
        /// <param name="milliseconds">The round trip, or null when lost.</param>
        private void Add(long? milliseconds)
        {
            _Sent++;
            _Samples.Add(milliseconds);

            if (_Samples.Count > LatencyChartDrawable.CAPACITY)
            {
                _Samples.RemoveAt(0);
            }

            if (milliseconds is long reply)
            {
                _Replies.Add(reply);
            }

            LastText = milliseconds is long last ? NetworkFormat.Latency(last) : AppResources.PingLost;
            Publish();
        }

        /// <summary>
        /// Recomputes the figures from every ping of this run.
        /// </summary>
        private void Publish()
        {
            // A new list each time: the chart redraws when the property changes, and must never see
            // the list being edited underneath it.
            Samples = [.. _Samples];

            double lossPercent = _Sent == 0 ? 0 : 100.0 * (_Sent - _Replies.Count) / _Sent;
            LossText = _Sent == 0
                ? AppResources.ValueMissing
                : NetworkFormat.Text(AppResources.PingLossFormat, Math.Round(lossPercent), _Sent - _Replies.Count, _Sent);

            if (_Replies.Count == 0)
            {
                AverageText = AppResources.ValueMissing;
                RangeText = AppResources.ValueMissing;
                JitterText = AppResources.ValueMissing;
                Summary = string.Empty;

                if (_Sent == 0)
                {
                    LastText = AppResources.ValueMissing;
                }

                return;
            }

            double average = _Replies.Average();
            double jitter = _Replies.Count < 2
                ? 0
                : _Replies.Zip(_Replies.Skip(1), (previous, next) => Math.Abs(next - previous)).Average();

            AverageText = NetworkFormat.Latency(average);
            RangeText = NetworkFormat.Text(AppResources.PingRangeFormat, _Replies.Min(), _Replies.Max());
            JitterText = NetworkFormat.Latency(jitter);
            Summary = NetworkFormat.Text(AppResources.PingSummaryFormat, Host.Trim(), AverageText, LossText);
        }

        #endregion
    }
}
