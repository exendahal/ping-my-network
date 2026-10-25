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

        /// <summary>
        /// Where bugs and ideas are reported: the project's GitHub issues.
        /// </summary>
        public const string NEW_ISSUE_URL = "https://github.com/exendahal/ping-my-network/issues/new";

        /// <summary>
        /// A new GitHub issue, already filled in.
        /// </summary>
        /// <param name="label">The issue's label, e.g. "bug"; GitHub drops it for people who cannot label.</param>
        /// <param name="body">The text to start the issue with.</param>
        /// <returns>The page that opens the issue.</returns>
        public static Uri NewIssue(string label, string body)
        {
            return new Uri($"{NEW_ISSUE_URL}?labels={Uri.EscapeDataString(label)}&body={Uri.EscapeDataString(body)}");
        }

        #endregion
    }
}
