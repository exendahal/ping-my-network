using MauiWifiManager.Abstractions;
using PingMyNetwork.Helper.Enums;

namespace PingMyNetwork.Models
{
    /// <summary>
    /// Everything the app knows about the current connection at one moment.
    /// </summary>
    /// <remarks>
    /// Immutable, so a screen can hold on to one while the monitor builds the next. Addresses are
    /// kept as display strings because nothing in the app does arithmetic on them.
    /// </remarks>
    public sealed record NetworkSnapshot
    {
        #region Properties

        /// <summary>
        /// Whether the internet is reachable, or why not.
        /// </summary>
        public ConnectionStatus Status { get; init; } = ConnectionStatus.Checking;

        /// <summary>
        /// The transport in use, or Unknown when there is none.
        /// </summary>
        public NetworkConnectionType ConnectionType { get; init; } = NetworkConnectionType.Unknown;

        /// <summary>
        /// The Wi-Fi network name, or null when the platform withheld it or the link is not Wi-Fi.
        /// </summary>
        public string? Ssid { get; init; }

        /// <summary>
        /// True when on Wi-Fi but the platform would not say which network, which on Windows means
        /// the app has no location access.
        /// </summary>
        public bool IsSsidHidden { get; init; }

        /// <summary>
        /// The access point's MAC address, when the platform reports a real one.
        /// </summary>
        public string? Bssid { get; init; }

        /// <summary>
        /// The security type as the platform names it.
        /// </summary>
        public string? Security { get; init; }

        /// <summary>
        /// Signal quality from 0 to 100, or null when unknown.
        /// </summary>
        public int? SignalPercent { get; init; }

        /// <summary>
        /// Raw signal strength, where the platform reports it.
        /// </summary>
        public int? RssiDbm { get; init; }

        /// <summary>
        /// Radio band of the connection.
        /// </summary>
        public WifiFrequencyBand Band { get; init; } = WifiFrequencyBand.Unknown;

        /// <summary>
        /// Channel number.
        /// </summary>
        public int? Channel { get; init; }

        /// <summary>
        /// Channel width in MHz.
        /// </summary>
        public int? ChannelWidthMhz { get; init; }

        /// <summary>
        /// Wi-Fi generation of the link.
        /// </summary>
        public WifiStandard Standard { get; init; } = WifiStandard.Unknown;

        /// <summary>
        /// Negotiated link speed in Mbps.
        /// </summary>
        public int? LinkSpeedMbps { get; init; }

        /// <summary>
        /// This device's IPv4 address.
        /// </summary>
        public string? IpAddress { get; init; }

        /// <summary>
        /// IPv4 subnet mask.
        /// </summary>
        public string? SubnetMask { get; init; }

        /// <summary>
        /// Prefix length, e.g. 24 for 255.255.255.0.
        /// </summary>
        public int? PrefixLength { get; init; }

        /// <summary>
        /// IPv4 broadcast address.
        /// </summary>
        public string? BroadcastAddress { get; init; }

        /// <summary>
        /// The router's address.
        /// </summary>
        public string? Gateway { get; init; }

        /// <summary>
        /// The DHCP server's address.
        /// </summary>
        public string? DhcpServer { get; init; }

        /// <summary>
        /// This device's global IPv6 address.
        /// </summary>
        public string? Ipv6Address { get; init; }

        /// <summary>
        /// DNS servers in use.
        /// </summary>
        public IReadOnlyList<string> DnsServers { get; init; } = [];

        /// <summary>
        /// The internet provider, e.g. "WorldLink Communications", once looked up.
        /// </summary>
        public string? Provider { get; init; }

        /// <summary>
        /// The address the internet sees this network as, once looked up.
        /// </summary>
        public string? PublicIp { get; init; }

        /// <summary>
        /// Whether the connection is metered or data-limited.
        /// </summary>
        public bool IsMetered { get; init; }

        /// <summary>
        /// Average round trip to the router, once measured.
        /// </summary>
        public double? GatewayLatencyMs { get; init; }

        /// <summary>
        /// True once the router has been pinged, whether or not it answered.
        /// </summary>
        public bool IsGatewayMeasured { get; init; }

        /// <summary>
        /// When this snapshot was taken.
        /// </summary>
        public DateTimeOffset CheckedAt { get; init; } = DateTimeOffset.Now;

        /// <summary>
        /// True once there is a network to describe.
        /// </summary>
        public bool IsConnected => ConnectionType != NetworkConnectionType.Unknown;

        /// <summary>
        /// The empty snapshot shown before the first check finishes.
        /// </summary>
        public static NetworkSnapshot Initial { get; } = new();

        #endregion
    }
}
