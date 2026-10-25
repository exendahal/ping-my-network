namespace PingMyNetwork.Models
{
    /// <summary>
    /// One network the app has scanned, and the devices it remembers on it.
    /// </summary>
    public sealed class NetworkRecord
    {
        #region Properties

        /// <summary>
        /// Identifies the network across visits; see DeviceManager.NetworkKey.
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// What to call the network, usually its Wi-Fi name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// How many scans have finished on it. A device found on the first scan is not "new".
        /// </summary>
        public int ScanCount { get; set; }

        /// <summary>
        /// When the last scan finished.
        /// </summary>
        public DateTimeOffset? LastScan { get; set; }

        /// <summary>
        /// Whether the user says it is their own network (home, office) rather than a guest one:
        /// null until they say. Only their own networks are backed up and send alerts.
        /// </summary>
        public bool? IsMine { get; set; }

        /// <summary>
        /// Every device seen on this network.
        /// </summary>
        public List<NetworkDevice> Devices { get; set; } = [];

        #endregion
    }
}
