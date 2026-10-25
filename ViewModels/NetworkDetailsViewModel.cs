using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiWifiManager.Abstractions;
using PingMyNetwork.Helper.Utilities;
using PingMyNetwork.Implementation;
using PingMyNetwork.Models;
using PingMyNetwork.Resources.Strings;

namespace PingMyNetwork.ViewModels
{
    /// <summary>
    /// Every value the app knows about the current network, each one copyable.
    /// </summary>
    /// <remarks>
    /// Rows whose value is unknown are left out rather than shown as a dash: a long list of dashes
    /// reads as broken, and the Capabilities screen is where missing platform support is explained.
    /// </remarks>
    public sealed partial class NetworkDetailsViewModel : ObservableObject
    {
        #region Variables

        /// <summary>
        /// How long the "Copied" confirmation stays up.
        /// </summary>
        private const int COPIED_VISIBLE_MS = 2000;

        private readonly NetworkMonitor _Monitor;

        private CancellationTokenSource? _CopiedCancel;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the view model from the monitor's latest snapshot.
        /// </summary>
        /// <param name="monitor">The connection monitor.</param>
        public NetworkDetailsViewModel(NetworkMonitor monitor)
        {
            _Monitor = monitor;

            Sections = Build(_Monitor.Current);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The cards, in display order.
        /// </summary>
        [ObservableProperty]
        public partial IReadOnlyList<DetailSection> Sections { get; set; }

        /// <summary>
        /// Confirmation after a copy, or empty.
        /// </summary>
        [ObservableProperty]
        public partial string CopiedText { get; set; } = string.Empty;

        /// <summary>
        /// Whether the copy confirmation is showing.
        /// </summary>
        [ObservableProperty]
        public partial bool IsCopiedVisible { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Starts following live changes. Called when the screen appears.
        /// </summary>
        public void Attach()
        {
            _Monitor.SnapshotChanged += OnSnapshotChanged;

            Sections = Build(_Monitor.Current);
        }

        /// <summary>
        /// Stops following changes, so a closed screen is not kept alive by the monitor.
        /// </summary>
        public void Detach()
        {
            _Monitor.SnapshotChanged -= OnSnapshotChanged;
        }

        /// <summary>
        /// Copies one value and confirms it briefly.
        /// </summary>
        /// <param name="row">The row that was tapped.</param>
        /// <returns>A task that completes when the confirmation has gone.</returns>
        [RelayCommand]
        private async Task CopyAsync(DetailRow row)
        {
            await Clipboard.Default.SetTextAsync(row.Value);

            // Android 13 and later show their own preview of what was copied; a second notice
            // would only repeat it.
            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                return;
            }

            _CopiedCancel?.Cancel();
            _CopiedCancel = new CancellationTokenSource();

            CopiedText = NetworkFormat.Text(AppResources.DetailsCopied, row.Title.ToLowerInvariant());
            IsCopiedVisible = true;

            try
            {
                await Task.Delay(COPIED_VISIBLE_MS, _CopiedCancel.Token);
                IsCopiedVisible = false;
            }
            catch (TaskCanceledException)
            {
                // A newer copy is showing its own confirmation.
            }
        }

        /// <summary>
        /// Shares every value as plain text.
        /// </summary>
        /// <returns>A task that completes once the share sheet is shown.</returns>
        [RelayCommand]
        private async Task ShareAsync()
        {
            StringBuilder text = new();

            foreach (DetailSection section in Sections)
            {
                text.AppendLine(section.Title);

                foreach (DetailRow row in section.Rows)
                {
                    text.AppendLine($"  {row.Title}: {row.Value}");
                }

                text.AppendLine();
            }

            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Title = AppResources.ShareTitle,
                Text = text.ToString().TrimEnd(),
            });
        }

        /// <summary>
        /// Goes back to the screen underneath.
        /// </summary>
        /// <returns>A task that completes once navigation finishes.</returns>
        [RelayCommand]
        private async Task BackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        /// <summary>
        /// Rebuilds the cards on the UI thread when the connection changes.
        /// </summary>
        /// <param name="sender">The monitor.</param>
        /// <param name="snapshot">The new snapshot.</param>
        private void OnSnapshotChanged(object? sender, NetworkSnapshot snapshot)
        {
            IReadOnlyList<DetailSection> sections = Build(snapshot);

            MainThread.BeginInvokeOnMainThread(() => Sections = sections);
        }

        /// <summary>
        /// Builds the cards for a snapshot, leaving out unknown values and empty sections.
        /// </summary>
        /// <param name="snapshot">The snapshot to describe.</param>
        /// <returns>The non-empty sections.</returns>
        private static IReadOnlyList<DetailSection> Build(NetworkSnapshot snapshot)
        {
            bool isWifi = snapshot.ConnectionType == NetworkConnectionType.WiFi;
            string? signal = snapshot.SignalPercent is int percent
                ? snapshot.RssiDbm is int rssi
                    ? $"{NetworkFormat.Text(AppResources.PercentFormat, percent)} · {NetworkFormat.Text(AppResources.DbmFormat, rssi)}"
                    : NetworkFormat.Text(AppResources.PercentFormat, percent)
                : null;

            List<DetailSection> sections =
            [
                DetailSection.From(
                    AppResources.DetailsSectionConnection,
                    (AppResources.DetailsConnectionType, NetworkFormat.ConnectionType(snapshot.ConnectionType)),
                    (AppResources.DetailsSsid, isWifi ? snapshot.Ssid ?? AppResources.HiddenNetworkName : null),
                    (AppResources.DetailsBssid, snapshot.Bssid),
                    (AppResources.DetailsSecurity, WifiSecurity.Describe(snapshot.Security)),
                    (AppResources.DetailsSignal, isWifi ? signal : null),
                    (AppResources.DetailsBand, NetworkFormat.Band(snapshot.Band)),
                    (AppResources.DetailsChannel, snapshot.Channel?.ToString()),
                    (AppResources.DetailsChannelWidth, snapshot.ChannelWidthMhz is int width
                        ? NetworkFormat.Text(AppResources.ChannelWidthFormat, width)
                        : null),
                    (AppResources.DetailsStandard, NetworkFormat.Standard(snapshot.Standard)),
                    (AppResources.DetailsLinkSpeed, snapshot.LinkSpeedMbps is int speed
                        ? NetworkFormat.Text(AppResources.LinkSpeedFormat, speed)
                        : null)),
                DetailSection.From(
                    AppResources.DetailsSectionAddressing,
                    (AppResources.DetailsIpAddress, snapshot.IpAddress),
                    (AppResources.DetailsSubnetMask, snapshot.SubnetMask),
                    (AppResources.DetailsPrefix, snapshot.PrefixLength is int prefix
                        ? NetworkFormat.Text(AppResources.PrefixFormat, prefix)
                        : null),
                    (AppResources.DetailsBroadcast, snapshot.BroadcastAddress),
                    (AppResources.DetailsGateway, snapshot.Gateway),
                    (AppResources.DetailsDhcp, snapshot.DhcpServer)),
                DetailSection.From(
                    AppResources.DetailsSectionIpv6,
                    (AppResources.DetailsIpv6Address, snapshot.Ipv6Address)),
                DetailSection.From(
                    AppResources.DetailsSectionInternet,
                    (AppResources.DetailsPublicIp, snapshot.PublicIp),
                    (AppResources.TileProvider, snapshot.Provider)),
                new DetailSection(
                    AppResources.DetailsSectionDns,
                    snapshot.DnsServers.Select(server => new DetailRow(AppResources.DetailsSectionDns, server)).ToList()),
            ];

            return sections.Where(section => section.Rows.Count > 0).ToList();
        }

        #endregion
    }
}
