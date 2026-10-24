using System.Diagnostics;
using PingMyNetwork.Helper.Enums;

namespace PingMyNetwork.Helper.Utilities
{
    /// <summary>
    /// Reads how a network is protected from the text each platform reports: Windows says
    /// "WPA2-PSK", Android lists capabilities such as "[WPA2-PSK-CCMP][RSN-PSK-CCMP][ESS]".
    /// </summary>
    /// <remarks>
    /// ponytail: string matching on platform text. A typed security field in the library's scan
    /// results would remove this; worth adding there when another app needs it too.
    /// </remarks>
    public static class WifiSecurity
    {
        #region Methods

        /// <summary>
        /// The protection a security string describes.
        /// </summary>
        /// <param name="security">The platform's security text.</param>
        /// <returns>The level, or Unknown when the text is missing or not recognised.</returns>
        public static SecurityLevel Classify(string? security)
        {
            if (string.IsNullOrWhiteSpace(security))
            {
                return SecurityLevel.Unknown;
            }

            string text = security.ToUpperInvariant();

            if (text.Contains("SAE", StringComparison.Ordinal) || text.Contains("WPA3", StringComparison.Ordinal))
            {
                return SecurityLevel.Wpa3;
            }

            if (text.Contains("WPA2", StringComparison.Ordinal) || text.Contains("RSN", StringComparison.Ordinal))
            {
                return SecurityLevel.Wpa2;
            }

            if (text.Contains("WPA", StringComparison.Ordinal))
            {
                return SecurityLevel.Wpa;
            }

            if (text.Contains("WEP", StringComparison.Ordinal))
            {
                return SecurityLevel.Wep;
            }

            // Enhanced Open (OWE) encrypts traffic without a password, so it is not "open" in the
            // sense that matters here.
            if (text.Contains("OWE", StringComparison.Ordinal))
            {
                return SecurityLevel.Wpa3;
            }

            // Android lists only "[ESS]" for an open network; Windows says "Open".
            if (text.Contains("OPEN", StringComparison.Ordinal) || text.Trim() == "[ESS]")
            {
                return SecurityLevel.Open;
            }

            return SecurityLevel.Unknown;
        }

        /// <summary>
        /// Whether a level is weak enough to warn about: open, WEP or the original WPA.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <returns>True for weak protection.</returns>
        public static bool IsWeak(SecurityLevel level)
        {
            return level is SecurityLevel.Open or SecurityLevel.Wep or SecurityLevel.Wpa;
        }

        /// <summary>
        /// Runs the rules against the strings each platform produces. Called at startup in Debug.
        /// </summary>
        [Conditional("DEBUG")]
        public static void SelfCheck()
        {
            Debug.Assert(Classify("WPA2-PSK") == SecurityLevel.Wpa2);
            Debug.Assert(Classify("WPA3-SAE") == SecurityLevel.Wpa3);
            Debug.Assert(Classify("Open (No Security)") == SecurityLevel.Open);
            Debug.Assert(Classify("WEP") == SecurityLevel.Wep);
            Debug.Assert(Classify("[WPA2-PSK-CCMP][RSN-PSK-CCMP][ESS]") == SecurityLevel.Wpa2);
            Debug.Assert(Classify("[RSN-SAE-CCMP][ESS]") == SecurityLevel.Wpa3);
            Debug.Assert(Classify("[WPA-PSK-TKIP][ESS]") == SecurityLevel.Wpa);
            Debug.Assert(Classify("[ESS]") == SecurityLevel.Open);
            Debug.Assert(Classify(null) == SecurityLevel.Unknown);
        }

        #endregion
    }
}
