using PingMyNetwork.Helper.Enums;

namespace PingMyNetwork.Models
{
    /// <summary>
    /// One line of a tool's results: a traceroute hop, a DNS record, an open port or a device.
    /// </summary>
    /// <param name="Leading">A short key on the left, such as the hop number or record type.</param>
    /// <param name="Title">The main value.</param>
    /// <param name="Subtitle">A second line, or null.</param>
    /// <param name="Trailing">A figure on the right, such as a round trip, or null.</param>
    /// <param name="Level">Colours the trailing figure; null leaves it neutral.</param>
    /// <param name="Key">Identifies what the row stands for, e.g. a device's MAC address.</param>
    public sealed record ResultRow(
        string Leading,
        string Title,
        string? Subtitle = null,
        string? Trailing = null,
        StatusLevel? Level = null,
        string? Key = null)
    {
        /// <summary>
        /// Whether there is a leading key.
        /// </summary>
        public bool HasLeading => Leading.Length > 0;

        /// <summary>
        /// Whether there is a second line.
        /// </summary>
        public bool HasSubtitle => !string.IsNullOrEmpty(Subtitle);
    }
}
