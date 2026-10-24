namespace PingMyNetwork.Helper.Utilities
{
    /// <summary>
    /// Shell routes, in one place so the chrome, the shell and the view models agree.
    /// </summary>
    public static class Routes
    {
        #region Variables

        public const string HOME = "home";
        public const string DEVICES = "devices";
        public const string WIFI = "wifi";
        public const string TOOLS = "tools";

        /// <summary>
        /// Pushed on top of Home.
        /// </summary>
        public const string NETWORK_DETAILS = "details";

        /// <summary>
        /// Pushed on top of Devices on narrow windows; takes the device's id as "id".
        /// </summary>
        public const string DEVICE_DETAILS = "device";

        /// <summary>
        /// The tools, each pushed on top of Tools.
        /// </summary>
        public const string TOOL_PING = "ping";
        public const string TOOL_TRACEROUTE = "traceroute";
        public const string TOOL_DNS = "dns";
        public const string TOOL_PORTS = "ports";
        public const string TOOL_SPEED = "speedtest";
        public const string TOOL_WAKE = "wake";
        public const string TOOL_SUBNET = "subnet";

        /// <summary>
        /// The top-level destinations in tab order, which is also the order transitions follow.
        /// </summary>
        public static readonly IReadOnlyList<string> TABS = [HOME, DEVICES, WIFI, TOOLS];

        #endregion
    }
}
