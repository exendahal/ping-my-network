using System.Diagnostics;
using PingMyNetwork.Models;

namespace PingMyNetwork.Helper.Utilities
{
    /// <summary>
    /// Decides whether two sightings are the same device, so a device keeps its name and Known
    /// status when its address changes.
    /// </summary>
    /// <remarks>
    /// The strongest identifier both sightings have decides, in the PRD's order: MAC, then NetBIOS
    /// name, then host name, then IP. A weaker identifier is only used when neither sighting has a
    /// stronger one; two devices that both report a MAC are never matched on their IP.
    ///
    /// ponytail: UPnP UDN and mDNS instance name belong between MAC and NetBIOS once the library
    /// reports them (PRD 7.1).
    /// </remarks>
    public static class DeviceIdentity
    {
        #region Methods

        /// <summary>
        /// Whether two sightings are the same device.
        /// </summary>
        /// <param name="first">One sighting.</param>
        /// <param name="second">The other.</param>
        /// <returns>True when the strongest shared identifier is equal.</returns>
        public static bool Matches(NetworkDevice first, NetworkDevice second)
        {
            if (Decide(first.MacAddress, second.MacAddress) is bool byMac)
            {
                return byMac;
            }

            if (Decide(first.NetBiosName, second.NetBiosName) is bool byNetBios)
            {
                return byNetBios;
            }

            if (Decide(first.HostName, second.HostName) is bool byHost)
            {
                return byHost;
            }

            return first.IpAddress == second.IpAddress;
        }

        /// <summary>
        /// Compares one identifier, when both sightings have it.
        /// </summary>
        /// <param name="first">The first sighting's value.</param>
        /// <param name="second">The second sighting's value.</param>
        /// <returns>Whether they are equal, or null when either is missing.</returns>
        private static bool? Decide(string? first, string? second)
        {
            if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second))
            {
                return null;
            }

            return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Runs the matching rules against known cases. Called once at startup in Debug builds.
        /// </summary>
        [Conditional("DEBUG")]
        public static void SelfCheck()
        {
            NetworkDevice phone = new() { IpAddress = "192.168.1.20", HostName = "galaxy-s24.lan" };

            // Same host name on a new address is the same phone.
            Debug.Assert(Matches(phone, new NetworkDevice { IpAddress = "192.168.1.45", HostName = "GALAXY-S24.lan" }));

            // A different host name on the same address is a different device.
            Debug.Assert(!Matches(phone, new NetworkDevice { IpAddress = "192.168.1.20", HostName = "printer.lan" }));

            // With no names at all, the address is all there is.
            Debug.Assert(Matches(
                new NetworkDevice { IpAddress = "192.168.1.9" },
                new NetworkDevice { IpAddress = "192.168.1.9" }));

            // MAC outranks everything below it.
            Debug.Assert(!Matches(
                new NetworkDevice { IpAddress = "192.168.1.9", HostName = "tv", MacAddress = "AA:BB:CC:00:00:01" },
                new NetworkDevice { IpAddress = "192.168.1.9", HostName = "tv", MacAddress = "AA:BB:CC:00:00:02" }));

            // A name on one side only falls through to the address.
            Debug.Assert(Matches(
                new NetworkDevice { IpAddress = "192.168.1.30", HostName = "nas.lan" },
                new NetworkDevice { IpAddress = "192.168.1.30" }));
        }

        #endregion
    }
}
