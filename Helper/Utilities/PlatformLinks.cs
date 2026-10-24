namespace PingMyNetwork.Helper.Utilities
{
    /// <summary>
    /// Addresses the app opens to hand the user over to the system or a portal.
    /// </summary>
    public static class PlatformLinks
    {
        #region Properties

        /// <summary>
        /// A plain-HTTP page the platform itself uses to detect captive portals. Opening it in a
        /// browser makes the portal show its sign-in page.
        /// </summary>
        public static Uri SignInProbe =>
            OperatingSystem.IsWindows() ? new Uri("http://www.msftconnecttest.com/redirect")
            : OperatingSystem.IsAndroid() ? new Uri("http://connectivitycheck.gstatic.com/generate_204")
            : new Uri("http://captive.apple.com");

        /// <summary>
        /// The Windows location privacy page, where desktop apps are allowed to read the Wi-Fi name.
        /// </summary>
        public static Uri WindowsLocationSettings { get; } = new("ms-settings:privacy-location");

        #endregion
    }
}
