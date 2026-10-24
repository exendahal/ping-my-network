using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using PingMyNetwork.Models;

namespace PingMyNetwork.Helper.Utilities
{
    /// <summary>
    /// IPv4 subnet arithmetic for the subnet calculator.
    /// </summary>
    public static class SubnetCalculator
    {
        #region Methods

        /// <summary>
        /// Works out the subnet from text such as "192.168.1.10/24" or "192.168.1.10 255.255.255.0".
        /// An address on its own is read as /24, the size almost every home network uses.
        /// </summary>
        /// <param name="text">The address, with an optional prefix length or mask.</param>
        /// <returns>The subnet, or null when the text is not a valid IPv4 address and mask.</returns>
        public static SubnetInfo? Calculate(string? text)
        {
            string[] parts = (text ?? string.Empty).Split(['/', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if ((parts.Length is < 1 or > 2) || !TryParseIpv4(parts[0], out uint address))
            {
                return null;
            }

            int prefix = 24;

            if ((parts.Length == 2) && !int.TryParse(parts[1], out prefix))
            {
                if (!TryParseIpv4(parts[1], out uint mask) || !TryPrefixOf(mask, out prefix))
                {
                    return null;
                }
            }

            if (prefix is < 0 or > 32)
            {
                return null;
            }

            uint maskBits = prefix == 0 ? 0 : uint.MaxValue << (32 - prefix);
            uint network = address & maskBits;
            uint broadcast = network | ~maskBits;

            // A /31 is a point-to-point link where both addresses are usable (RFC 3021); a /32 is one host.
            bool hasReservedEnds = prefix < 31;
            uint first = hasReservedEnds ? network + 1 : network;
            uint last = hasReservedEnds ? broadcast - 1 : broadcast;
            long usable = hasReservedEnds ? (1L << (32 - prefix)) - 2 : (1L << (32 - prefix));

            return new SubnetInfo(
                Format(address),
                prefix,
                Format(network),
                Format(broadcast),
                Format(maskBits),
                Format(~maskBits),
                Format(first),
                Format(last),
                usable,
                IsPrivate(address));
        }

        /// <summary>
        /// Reads a dotted IPv4 address as a number.
        /// </summary>
        /// <param name="text">The address.</param>
        /// <param name="value">The address as a big-endian number.</param>
        /// <returns>True when the text is exactly four dotted numbers.</returns>
        private static bool TryParseIpv4(string text, out uint value)
        {
            value = 0;

            // IPAddress.TryParse also accepts "10" or "10.1" as shorthand, which reads as a typo here.
            if ((text.Count(c => c == '.') != 3)
                || !IPAddress.TryParse(text, out IPAddress? address)
                || (address.AddressFamily != AddressFamily.InterNetwork))
            {
                return false;
            }

            byte[] bytes = address.GetAddressBytes();
            value = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];

            return true;
        }

        /// <summary>
        /// The prefix length of a subnet mask.
        /// </summary>
        /// <param name="mask">The mask as a number.</param>
        /// <param name="prefix">Its prefix length.</param>
        /// <returns>False when the mask's ones are not contiguous.</returns>
        private static bool TryPrefixOf(uint mask, out int prefix)
        {
            prefix = System.Numerics.BitOperations.PopCount(mask);

            uint expected = prefix == 0 ? 0 : uint.MaxValue << (32 - prefix);

            return mask == expected;
        }

        /// <summary>
        /// Whether an address is in 10/8, 172.16/12 or 192.168/16.
        /// </summary>
        /// <param name="address">The address as a number.</param>
        /// <returns>True for private addresses.</returns>
        private static bool IsPrivate(uint address)
        {
            return ((address >> 24) == 10)
                || ((address >> 20) == ((172u << 4) | 1))
                || ((address >> 16) == ((192u << 8) | 168));
        }

        /// <summary>
        /// Writes a number as a dotted IPv4 address.
        /// </summary>
        /// <param name="value">The address as a number.</param>
        /// <returns>For example "192.168.1.0".</returns>
        private static string Format(uint value)
        {
            return $"{value >> 24}.{(value >> 16) & 0xFF}.{(value >> 8) & 0xFF}.{value & 0xFF}";
        }

        /// <summary>
        /// Checks the arithmetic on the cases that usually go wrong. Called at startup in Debug.
        /// </summary>
        [Conditional("DEBUG")]
        public static void SelfCheck()
        {
            SubnetInfo? home = Calculate("192.168.1.10/24");
            Debug.Assert(home is { Network: "192.168.1.0", Broadcast: "192.168.1.255", FirstHost: "192.168.1.1", LastHost: "192.168.1.254", UsableHosts: 254, IsPrivate: true });

            SubnetInfo? masked = Calculate("10.20.30.40 255.255.240.0");
            Debug.Assert(masked is { PrefixLength: 20, Network: "10.20.16.0", Broadcast: "10.20.31.255", Wildcard: "0.0.15.255", UsableHosts: 4094 });

            Debug.Assert(Calculate("172.31.0.1")?.IsPrivate == true);
            Debug.Assert(Calculate("172.32.0.1")?.IsPrivate == false);
            Debug.Assert(Calculate("1.2.3.4/31") is { UsableHosts: 2, FirstHost: "1.2.3.4", LastHost: "1.2.3.5" });
            Debug.Assert(Calculate("1.2.3.4/32") is { UsableHosts: 1 });
            Debug.Assert(Calculate("0.0.0.0/0") is { UsableHosts: 4294967294, Broadcast: "255.255.255.255" });
            Debug.Assert(Calculate("1.2.3.4 255.0.255.0") is null);
            Debug.Assert(Calculate("1.2.3/24") is null);
            Debug.Assert(Calculate("1.2.3.4/33") is null);
            Debug.Assert(Calculate(string.Empty) is null);
        }

        #endregion
    }
}
