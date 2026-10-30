using System.Text.Json.Serialization;
using PingMyNetwork.Helper.Enums;

namespace PingMyNetwork.Models
{
    /// <summary>
    /// A device on a network, as remembered between scans plus what the latest scan saw.
    /// </summary>
    /// <remarks>
    /// Mutable because a device is filled in over several steps of one scan (answer, then details)
    /// and edited by the user. Only DeviceManager writes to it, on its own lock.
    /// </remarks>
    public sealed class NetworkDevice
    {
        #region Properties

        /// <summary>
        /// Stable id for this app, independent of the address the device has today.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// The IPv4 address it last answered on.
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// Host name from reverse DNS.
        /// </summary>
        public string? HostName { get; set; }

        /// <summary>
        /// NetBIOS computer name.
        /// </summary>
        public string? NetBiosName { get; set; }

        /// <summary>
        /// MAC address, where the platform reveals it.
        /// </summary>
        public string? MacAddress { get; set; }

        /// <summary>
        /// Whether the MAC address is a private, randomised one.
        /// </summary>
        public bool? IsRandomizedMac { get; set; }

        /// <summary>
        /// Maker of its network adapter, from the MAC address.
        /// </summary>
        public string? Vendor { get; set; }

        /// <summary>
        /// Name the device gives itself over UPnP.
        /// </summary>
        public string? FriendlyName { get; set; }

        /// <summary>
        /// Name the device advertises over mDNS / Bonjour, e.g. "Anna's iPhone".
        /// </summary>
        public string? MdnsName { get; set; }

        /// <summary>
        /// Model the device advertises over mDNS, e.g. "iPhone15,2" or "MacBookPro18,1".
        /// </summary>
        public string? MdnsModel { get; set; }

        /// <summary>
        /// Manufacturer from UPnP.
        /// </summary>
        public string? Manufacturer { get; set; }

        /// <summary>
        /// Model from UPnP.
        /// </summary>
        public string? ModelName { get; set; }

        /// <summary>
        /// Title of the device's web page.
        /// </summary>
        public string? HttpTitle { get; set; }

        /// <summary>
        /// Well-known TCP ports that accepted a connection.
        /// </summary>
        public List<int> OpenPorts { get; set; } = [];

        /// <summary>
        /// Name the user gave it, which wins over every discovered name.
        /// </summary>
        public string? CustomName { get; set; }

        /// <summary>
        /// What kind of device it is.
        /// </summary>
        public DeviceKind Type { get; set; }

        /// <summary>
        /// True once the user picked the type, so a later guess never overrides it.
        /// </summary>
        public bool IsTypeSetByUser { get; set; }

        /// <summary>
        /// Whether the user has said they recognise this device.
        /// </summary>
        public bool IsKnown { get; set; }

        /// <summary>
        /// When it was first seen on this network.
        /// </summary>
        public DateTimeOffset FirstSeen { get; set; }

        /// <summary>
        /// When it last answered.
        /// </summary>
        public DateTimeOffset LastSeen { get; set; }

        /// <summary>
        /// When its details were last fetched, so a scan fetches them once per device, not every time.
        /// </summary>
        public DateTimeOffset? DetailsFetchedAt { get; set; }

        /// <summary>
        /// Whether it answered in the scan that is running or just finished.
        /// </summary>
        [JsonIgnore]
        public bool IsOnline { get; set; }

        /// <summary>
        /// Ping round trip from the latest scan.
        /// </summary>
        [JsonIgnore]
        public long? RoundTripMs { get; set; }

        /// <summary>
        /// True when it first appeared in the latest scan of a network scanned before.
        /// </summary>
        [JsonIgnore]
        public bool IsNew { get; set; }

        /// <summary>
        /// True when a device not marked as known answered the latest scan after missing the one
        /// before and being away for at least an hour.
        /// </summary>
        [JsonIgnore]
        public bool IsBack { get; set; }

        /// <summary>
        /// True for the device running this app.
        /// </summary>
        [JsonIgnore]
        public bool IsThisDevice { get; set; }

        /// <summary>
        /// True for the router.
        /// </summary>
        [JsonIgnore]
        public bool IsGateway { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// A copy a screen can read while the original keeps changing.
        /// </summary>
        /// <returns>The copy.</returns>
        public NetworkDevice Clone()
        {
            NetworkDevice copy = (NetworkDevice)MemberwiseClone();

            copy.OpenPorts = [.. OpenPorts];

            return copy;
        }

        #endregion
    }
}
