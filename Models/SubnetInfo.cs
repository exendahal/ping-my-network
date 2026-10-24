namespace PingMyNetwork.Models
{
    /// <summary>
    /// An IPv4 subnet worked out from one address and its prefix length.
    /// </summary>
    /// <param name="Address">The address that was entered.</param>
    /// <param name="PrefixLength">Bits in the network part, 0 to 32.</param>
    /// <param name="Network">The network address.</param>
    /// <param name="Broadcast">The broadcast address.</param>
    /// <param name="Mask">The subnet mask.</param>
    /// <param name="Wildcard">The inverse of the mask, as used in router ACLs.</param>
    /// <param name="FirstHost">The first usable address.</param>
    /// <param name="LastHost">The last usable address.</param>
    /// <param name="UsableHosts">How many addresses can be given to devices.</param>
    /// <param name="IsPrivate">Whether the address is in a private (RFC 1918) range.</param>
    public sealed record SubnetInfo(
        string Address,
        int PrefixLength,
        string Network,
        string Broadcast,
        string Mask,
        string Wildcard,
        string FirstHost,
        string LastHost,
        long UsableHosts,
        bool IsPrivate);
}
