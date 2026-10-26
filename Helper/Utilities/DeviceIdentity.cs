using System.Diagnostics;
using PingMyNetwork.Models;

namespace PingMyNetwork.Helper.Utilities
{
    /// <summary>
    /// Decides whether two sightings are the same device, so a device keeps its name and Known
    /// status when its address changes.
    /// </summary>
    /// <remarks>
    /// The strongest identifier both sightings have decides, in this order: MAC, then NetBIOS
    /// name, then host name, then IP. A weaker identifier is only used when neither sighting has a
    /// stronger one; two devices that both report a MAC are never matched on their IP.
    ///
    /// ponytail: UPnP UDN and mDNS instance name belong between MAC and NetBIOS once the library
    /// reports them.
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
        /// Whether two sightings share an identifier that names one device, leaving out the IP
        /// address, which every home network hands out the same way (192.168.1.x).
        /// </summary>
        /// <param name="first">One sighting.</param>
        /// <param name="second">The other.</param>
        /// <returns>True when a MAC, mDNS, NetBIOS or host name is equal.</returns>
        public static bool MatchesStrongly(NetworkDevice first, NetworkDevice second)
        {
            return (Decide(first.MacAddress, second.MacAddress) == true)
                || (Decide(first.MdnsName, second.MdnsName) == true)
                || (Decide(first.NetBiosName, second.NetBiosName) == true)
                || (Decide(first.HostName, second.HostName) == true);
        }

        /// <summary>
        /// Whether two access points are radios of the same router. A router's 2.4 and 5 GHz
        /// radios, and its extra networks, take neighbouring addresses: the same first five
        /// bytes, apart from the "locally administered" bit some routers set on extra networks.
        /// </summary>
        /// <param name="first">One BSSID.</param>
        /// <param name="second">The other.</param>
        /// <returns>True when they come from one router.</returns>
        public static bool IsSameRouter(string first, string second)
        {
            if (!TryParseMac(first, out byte[] a) || !TryParseMac(second, out byte[] b))
            {
                return false;
            }

            return ((a[0] | 0x02) == (b[0] | 0x02)) && a.AsSpan(1, 4).SequenceEqual(b.AsSpan(1, 4));
        }

        /// <summary>
        /// Reads a MAC address written with colons or dashes.
        /// </summary>
        /// <param name="text">The address.</param>
        /// <param name="bytes">Its six bytes.</param>
        /// <returns>True when it is a MAC address.</returns>
        private static bool TryParseMac(string text, out byte[] bytes)
        {
            string[] parts = text.Split(':', '-');

            bytes = new byte[6];

            if (parts.Length != 6)
            {
                return false;
            }

            for (var i = 0; i < 6; i++)
            {
                if (!byte.TryParse(parts[i], System.Globalization.NumberStyles.HexNumber, null, out bytes[i]))
                {
                    return false;
                }
            }

            return true;
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
            // A router's two bands, and an extra network with the locally administered bit set.
            Debug.Assert(IsSameRouter("A4:2B:B0:11:22:30", "a4:2b:b0:11:22:34"));
            Debug.Assert(IsSameRouter("A4:2B:B0:11:22:30", "A6:2B:B0:11:22:31"));

            // Two routers of the same make in two homes.
            Debug.Assert(!IsSameRouter("A4:2B:B0:11:22:30", "A4:2B:B0:11:98:30"));
            Debug.Assert(!IsSameRouter("A4:2B:B0:11:22:30", "not a mac"));

            // A shared address is not a shared device; a shared name is.
            Debug.Assert(!MatchesStrongly(new NetworkDevice { IpAddress = "192.168.1.5" }, new NetworkDevice { IpAddress = "192.168.1.5" }));
            Debug.Assert(MatchesStrongly(new NetworkDevice { MdnsName = "Anna's iPhone" }, new NetworkDevice { MdnsName = "anna's iphone" }));

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
