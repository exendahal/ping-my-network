namespace PingMyNetwork.Helper.Utilities
{
    /// <summary>
    /// Whether the operating system has asked for reduced motion.
    /// </summary>
    /// <remarks>
    /// .NET MAUI 10 has no cross-platform API for this. Despite what some guidance says, there is
    /// no VisualElement.IsAnimationEnabled in Microsoft.Maui.Controls or Microsoft.Maui, verified
    /// against 10.0.90, so this reads each platform's own setting directly.
    ///
    /// ponytail: conditional compilation in one file rather than partial classes across four
    /// Platforms folders. One property, three one-liners, nothing to gain by spreading it out.
    /// </remarks>
    public static class MotionSettings
    {
        #region Properties

        /// <summary>
        /// Reads the live operating system setting. Callers cache the result for the lifetime of a
        /// screen rather than hitting the platform on every frame.
        /// </summary>
        public static bool PrefersReducedMotion
        {
            get
            {
#if ANDROID
                // Android exposes reduce-motion as the animator duration scale. Zero means off, and
                // it is the same value the platform's own animators honour.
                Android.Content.Context? context = Android.App.Application.Context;

                if (context?.ContentResolver is not { } resolver)
                {
                    return false;
                }

                float scale = Android.Provider.Settings.Global.GetFloat(
                    resolver,
                    Android.Provider.Settings.Global.AnimatorDurationScale,
                    1f);

                return scale == 0f;
#elif IOS || MACCATALYST
                return UIKit.UIAccessibility.IsReduceMotionEnabled;
#elif WINDOWS
                return !new Windows.UI.ViewManagement.UISettings().AnimationsEnabled;
#else
                return false;
#endif
            }
        }

        #endregion
    }
}
