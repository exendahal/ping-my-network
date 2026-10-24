using MauiWifiManager.Abstractions;
using PingMyNetwork.Helper.Enums;

namespace PingMyNetwork.ViewModels
{
    /// <summary>
    /// One network name in the All networks list, formatted for display. Immutable, so an unchanged
    /// row compares equal and is left alone when the list refreshes.
    /// </summary>
    /// <param name="Key">The network name, which identifies the row.</param>
    /// <param name="Name">What to show as the name.</param>
    /// <param name="Subtitle">Band, channel, standard, security and access point count.</param>
    /// <param name="SignalText">Strength, e.g. "-52 dBm".</param>
    /// <param name="SignalIcon">Bars glyph for the strength.</param>
    /// <param name="Level">Status level of the strength, which colours the bars.</param>
    /// <param name="IsOwn">Whether this is the network the device is on.</param>
    /// <param name="IsWeakSecurity">Whether it is open or poorly protected.</param>
    /// <param name="SemanticDescription">The row read as one phrase by a screen reader.</param>
    public sealed record WifiNetworkItem(
        string Key,
        string Name,
        string Subtitle,
        string SignalText,
        string SignalIcon,
        StatusLevel Level,
        bool IsOwn,
        bool IsWeakSecurity,
        string SemanticDescription);

    /// <summary>
    /// A band the channel chart can show.
    /// </summary>
    /// <param name="Band">The band.</param>
    /// <param name="Name">Its name, e.g. "5 GHz".</param>
    /// <param name="IsSelected">Whether the chart shows it now.</param>
    public sealed record BandOption(WifiFrequencyBand Band, string Name, bool IsSelected);
}
