namespace PingMyNetwork.Models
{
    /// <summary>
    /// One remembered network offered when a known Wi-Fi name shows up on a new access point.
    /// </summary>
    /// <param name="Id">The network's id.</param>
    /// <param name="Description">How to recognise it, e.g. "12 devices, last scanned 3 Oct".</param>
    public sealed record NetworkChoice(string Id, string Description);
}
