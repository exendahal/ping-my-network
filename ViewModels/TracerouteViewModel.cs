using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MauiWifiManager;
using MauiWifiManager.Abstractions;
using PingMyNetwork.Helper.Utilities;
using PingMyNetwork.Models;
using PingMyNetwork.Resources.Strings;

namespace PingMyNetwork.ViewModels
{
    /// <summary>
    /// Shows the route to a host one hop at a time, as each router answers.
    /// </summary>
    public sealed partial class TracerouteViewModel : ToolViewModel
    {
        #region Variables

        /// <summary>
        /// Most routes on the internet are well under this.
        /// </summary>
        private const int MAX_HOPS = 30;

        private readonly IWifiNetworkService _Wifi;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the view model.
        /// </summary>
        /// <param name="wifi">The library.</param>
        public TracerouteViewModel(IWifiNetworkService wifi)
        {
            _Wifi = wifi;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Where to trace to.
        /// </summary>
        [ObservableProperty]
        public partial string Host { get; set; } = "one.one.one.one";

        /// <summary>
        /// The hops so far, in order.
        /// </summary>
        public ObservableCollection<ResultRow> Hops { get; } = [];

        /// <summary>
        /// How the trace ended, or progress while it runs.
        /// </summary>
        [ObservableProperty]
        public partial string Summary { get; set; } = string.Empty;

        /// <summary>
        /// Whether a trace has started, so the results card shows.
        /// </summary>
        [ObservableProperty]
        public partial bool HasResults { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Traces the route, adding each hop as it answers.
        /// </summary>
        /// <param name="token">Cancelled on stop.</param>
        /// <returns>A task that completes when the trace ends.</returns>
        protected override async Task RunCoreAsync(CancellationToken token)
        {
            string host = Host.Trim();

            if (host.Length == 0)
            {
                ErrorText = AppResources.ToolHostRequired;
                return;
            }

            Hops.Clear();
            HasResults = true;
            Summary = NetworkFormat.Text(AppResources.TracerouteRunningFormat, host);

            // Created here, on the UI thread, so each hop is reported back on it.
            Progress<TracerouteHop> progress = new(hop =>
            {
                if (!token.IsCancellationRequested)
                {
                    Hops.Add(ToRow(hop));
                }
            });
            WifiManagerResponse<List<TracerouteHop>> response = await _Wifi.TracerouteAsync(host, MAX_HOPS, progress, token);

            if (token.IsCancellationRequested || !Succeeded(response))
            {
                Summary = Hops.Count == 0 ? string.Empty : AppResources.TracerouteStopped;
                HasResults = Hops.Count > 0;
                return;
            }

            List<TracerouteHop> hops = response.Data ?? [];

            Summary = (hops.Count > 0) && hops[^1].IsDestination
                ? NetworkFormat.Text(AppResources.TracerouteReachedFormat, host, hops.Count)
                : NetworkFormat.Text(AppResources.TracerouteNotReachedFormat, host, MAX_HOPS);
        }

        /// <summary>
        /// One hop as a result line: number, who answered, and how long it took.
        /// </summary>
        /// <param name="hop">The hop.</param>
        /// <returns>The row.</returns>
        private static ResultRow ToRow(TracerouteHop hop)
        {
            if (hop.Address is null)
            {
                return new ResultRow(hop.Hop.ToString(), AppResources.TracerouteNoReply, AppResources.TracerouteNoReplyHint);
            }

            return new ResultRow(
                hop.Hop.ToString(),
                hop.HostName ?? hop.Address,
                hop.HostName is null ? null : hop.Address,
                hop.RoundTripMs is long milliseconds ? NetworkFormat.Latency(milliseconds) : null,
                hop.RoundTripMs is long level ? NetworkFormat.LatencyLevel(level) : null);
        }

        #endregion
    }
}
