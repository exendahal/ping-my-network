using System.Diagnostics;
using PingMyNetwork.Helper.Enums;
using PingMyNetwork.Resources.Strings;

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
        /// Whether a WPA2 or WPA3 network still lets old devices in with the original WPA or the
        /// TKIP cipher ("mixed mode"), which weakens it for everyone. Android reports this as a
        /// separate [WPA-...] entry or a TKIP cipher; Windows does not report it.
        /// </summary>
        /// <param name="security">The platform's security text.</param>
        /// <returns>True for mixed mode.</returns>
        public static bool AllowsLegacyWpa(string? security)
        {
            if ((security is null) || (Classify(security) < SecurityLevel.Wpa2))
            {
                return false;
            }

            string text = security.ToUpperInvariant();

            return text.Contains("[WPA-", StringComparison.Ordinal) || text.Contains("TKIP", StringComparison.Ordinal);
        }

        /// <summary>
        /// Whether Wi-Fi Protected Setup is switched on. Its PIN method can be brute-forced, which
        /// gets around even a strong password. Android reports it; Windows does not.
        /// </summary>
        /// <param name="security">The platform's security text.</param>
        /// <returns>True when WPS is advertised.</returns>
        public static bool HasWps(string? security)
        {
            return security?.Contains("[WPS]", StringComparison.OrdinalIgnoreCase) == true;
        }

        /// <summary>
        /// The protection in words people recognise, e.g. "WPA2" or "WPA/WPA2 (mixed) · WPS on",
        /// instead of the platform's raw text.
        /// </summary>
        /// <param name="security">The platform's security text.</param>
        /// <returns>The description, or null when there is no text.</returns>
        public static string? Describe(string? security)
        {
            if (string.IsNullOrWhiteSpace(security))
            {
                return null;
            }

            SecurityLevel level = Classify(security);
            string text = security.ToUpperInvariant();
            string name = level switch
            {
                SecurityLevel.Open => AppResources.SecurityOpen,
                SecurityLevel.Wep => "WEP",
                SecurityLevel.Wpa => "WPA",
                SecurityLevel.Wpa2 when AllowsLegacyWpa(security) => AppResources.SecurityWpaMixed,
                SecurityLevel.Wpa2 => "WPA2",
                SecurityLevel.Wpa3 when text.Contains("PSK", StringComparison.Ordinal) => "WPA2/WPA3",
                SecurityLevel.Wpa3 => "WPA3",
                _ => security,
            };

            if (text.Contains("EAP", StringComparison.Ordinal) || text.Contains("ENTERPRISE", StringComparison.Ordinal))
            {
                name = NetworkFormat.Text(AppResources.SecurityEnterpriseFormat, name);
            }

            return HasWps(security) ? NetworkFormat.Text(AppResources.SecurityWithWpsFormat, name) : name;
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

            const string MIXED = "[WPA-PSK-TKIP+CCMP-128][WPA2-PSK-TKIP+CCMP-128][RSN-PSK-TKIP+CCMP-128][ESS][WPS]";
            Debug.Assert(AllowsLegacyWpa(MIXED));
            Debug.Assert(HasWps(MIXED));
            Debug.Assert(!AllowsLegacyWpa("[WPA2-PSK-CCMP][RSN-PSK-CCMP][ESS]"));
            Debug.Assert(!AllowsLegacyWpa("WPA2-PSK"));
            Debug.Assert(Describe("WPA2-PSK") == "WPA2");
            Debug.Assert(Describe("[RSN-PSK+SAE-CCMP][ESS]") == "WPA2/WPA3");
        }

        #endregion
    }
}
