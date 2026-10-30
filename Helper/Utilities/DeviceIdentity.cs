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
    /// stronger one; two devices that both report a MAC are never matched on their IP. Names that
    /// many devices share out of the box (Android.local, iPhone, localhost) count as no name.
    ///
    /// ponytail: UPnP UDN and mDNS instance name belong between MAC and NetBIOS once the library
    /// reports them.
    /// </remarks>
    public static class DeviceIdentity
    {
        #region Variables

        /// <summary>
        /// Default names, without the local domain, that whole product lines answer with.
        /// </summary>
        private static readonly HashSet<string> GENERIC_NAMES = new(StringComparer.OrdinalIgnoreCase)
        {
            "android", "iphone", "ipad", "localhost", "espressif",
        };

        #endregion

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

            if (DecideName(first.NetBiosName, second.NetBiosName) is bool byNetBios)
            {
                return byNetBios;
            }

            if (DecideName(first.HostName, second.HostName) is bool byHost)
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
                || (DecideName(first.MdnsName, second.MdnsName) == true)
                || (DecideName(first.NetBiosName, second.NetBiosName) == true)
                || (DecideName(first.HostName, second.HostName) == true);
        }

        /// <summary>
        /// Whether a device has an identifier that names it alone: a MAC, or a name that is not
        /// a product line's default.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <returns>True when it can be told apart from others by more than its address.</returns>
        public static bool IsIdentifiable(NetworkDevice device)
        {
            return !string.IsNullOrWhiteSpace(device.MacAddress)
                || (OwnName(device.MdnsName) ?? OwnName(device.NetBiosName) ?? OwnName(device.HostName)) is not null;
        }

        /// <summary>
        /// Whether an older record is the device that just turned up on another address. A MAC on
        /// both decides; otherwise a shared name does, as long as the older record was not also
        /// online this scan (two devices at once are two devices).
        /// </summary>
        /// <param name="older">The remembered record.</param>
        /// <param name="newer">The device just found.</param>
        /// <returns>True when they are one device.</returns>
        public static bool IsSameDeviceMoved(NetworkDevice older, NetworkDevice newer)
        {
            return Decide(older.MacAddress, newer.MacAddress) ?? (!older.IsOnline && MatchesStrongly(older, newer));
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
        /// Compares one name, when both sightings have one of their own.
        /// </summary>
        /// <param name="first">The first sighting's name.</param>
        /// <param name="second">The second sighting's name.</param>
        /// <returns>Whether they are equal, or null when either is missing or generic.</returns>
        private static bool? DecideName(string? first, string? second)
        {
            return Decide(OwnName(first), OwnName(second));
        }

        /// <summary>
        /// A name, or null when it is missing or a default many devices share.
        /// </summary>
        /// <param name="name">The name as reported.</param>
        /// <returns>The name, or null.</returns>
        private static string? OwnName(string? name)
        {
            return GENERIC_NAMES.Contains(DeviceDescriber.ShortHostName(name) ?? string.Empty) ? null : name;
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

            // A phone back on a new DHCP address, known only by its mDNS name, is the same phone...
            Debug.Assert(IsSameDeviceMoved(
                new NetworkDevice { IpAddress = "192.168.1.20", MdnsName = "Anna's iPhone" },
                new NetworkDevice { IpAddress = "192.168.1.47", MdnsName = "Anna's iPhone" }));

            // ...unless the old record answered this scan too, or the MACs disagree.
            Debug.Assert(!IsSameDeviceMoved(
                new NetworkDevice { IpAddress = "192.168.1.20", MdnsName = "Anna's iPhone", IsOnline = true },
                new NetworkDevice { IpAddress = "192.168.1.47", MdnsName = "Anna's iPhone" }));
            Debug.Assert(!IsSameDeviceMoved(
                new NetworkDevice { HostName = "tv", MacAddress = "AA:BB:CC:00:00:01" },
                new NetworkDevice { HostName = "tv", MacAddress = "AA:BB:CC:00:00:02" }));

            // Two Android devices both calling themselves Android.local are not the same device...
            Debug.Assert(!Matches(
                new NetworkDevice { IpAddress = "192.168.1.9", HostName = "Android.local" },
                new NetworkDevice { IpAddress = "192.168.1.14", HostName = "android.local" }));
            Debug.Assert(!IsSameDeviceMoved(
                new NetworkDevice { IpAddress = "192.168.1.9", HostName = "Android.local" },
                new NetworkDevice { IpAddress = "192.168.1.14", HostName = "Android.local", MdnsName = "Pixel 8" }));
            Debug.Assert(!IsIdentifiable(new NetworkDevice { HostName = "iPhone.lan", NetBiosName = "localhost" }));

            // ...but the generic name does not hide the address, nor a real name elsewhere.
            Debug.Assert(Matches(
                new NetworkDevice { IpAddress = "192.168.1.9", HostName = "Android.local" },
                new NetworkDevice { IpAddress = "192.168.1.9", HostName = "Android.local" }));
            Debug.Assert(IsSameDeviceMoved(
                new NetworkDevice { IpAddress = "192.168.1.9", HostName = "Android.local", MdnsName = "MiTV-AXSO0" },
                new NetworkDevice { IpAddress = "192.168.1.14", HostName = "Android.local", MdnsName = "MiTV-AXSO0" }));
            Debug.Assert(IsIdentifiable(new NetworkDevice { HostName = "android-5f2c9e.lan" }));

            // A name on one side only falls through to the address.
            Debug.Assert(Matches(
                new NetworkDevice { IpAddress = "192.168.1.30", HostName = "nas.lan" },
                new NetworkDevice { IpAddress = "192.168.1.30" }));
        }

        #endregion
    }
}
