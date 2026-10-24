using System.Diagnostics;

namespace PingMyNetwork.Helper.Utilities
{
    /// <summary>
    /// Reads the port list typed into the port check, such as "22, 80, 8000-8100".
    /// </summary>
    public static class PortList
    {
        #region Variables

        /// <summary>
        /// The most ports one check will try; a whole 65,535 range would take minutes and look
        /// like an attack to the device being checked.
        /// </summary>
        public const int MAX_PORTS = 1024;

        #endregion

        #region Methods

        /// <summary>
        /// The ports in a comma- or space-separated list of ports and ranges.
        /// </summary>
        /// <param name="text">The list.</param>
        /// <returns>The distinct ports in order, or null when an entry is not a port or range, or the list is empty or too long.</returns>
        public static List<int>? Parse(string? text)
        {
            SortedSet<int> ports = [];

            foreach (string entry in (text ?? string.Empty).Split([',', ' ', ';'], StringSplitOptions.RemoveEmptyEntries))
            {
                string[] ends = entry.Split('-', StringSplitOptions.TrimEntries);

                if ((ends.Length is < 1 or > 2)
                    || !TryPort(ends[0], out int from)
                    || !TryPort(ends[^1], out int to)
                    || (to < from)
                    || (to - from >= MAX_PORTS))
                {
                    return null;
                }

                for (int port = from; port <= to; port++)
                {
                    ports.Add(port);
                }

                if (ports.Count > MAX_PORTS)
                {
                    return null;
                }
            }

            return ports.Count == 0 ? null : [.. ports];
        }

        /// <summary>
        /// Reads one port number.
        /// </summary>
        /// <param name="text">The number.</param>
        /// <param name="port">The port.</param>
        /// <returns>True for 1 to 65535.</returns>
        private static bool TryPort(string text, out int port)
        {
            return int.TryParse(text, out port) && (port is >= 1 and <= 65535);
        }

        /// <summary>
        /// Checks the parser. Called at startup in Debug.
        /// </summary>
        [Conditional("DEBUG")]
        public static void SelfCheck()
        {
            Debug.Assert(Parse("22, 80,443")?.SequenceEqual([22, 80, 443]) == true);
            Debug.Assert(Parse("8000-8002 80 80")?.SequenceEqual([80, 8000, 8001, 8002]) == true);
            Debug.Assert(Parse("1-1024")?.Count == 1024);
            Debug.Assert(Parse("1-1025") is null);
            Debug.Assert(Parse("0") is null);
            Debug.Assert(Parse("65536") is null);
            Debug.Assert(Parse("90-80") is null);
            Debug.Assert(Parse("http") is null);
            Debug.Assert(Parse("   ") is null);
        }

        #endregion
    }
}
