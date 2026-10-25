using PingMyNetwork.Resources.Strings;

namespace PingMyNetwork.Implementation
{
    /// <summary>
    /// Posts local notifications with each platform's own API; no notification library needed.
    /// </summary>
    /// <remarks>
    /// ponytail: Android and iOS / Mac only. An unpackaged Windows app has no notification identity
    /// without the Windows App SDK's AppNotificationManager setup, so Settings hides alerts there.
    /// </remarks>
    public static class Notifier
    {
        #region Variables

        /// <summary>
        /// Android channel for new-device alerts; users can mute it on its own in system settings.
        /// </summary>
        private const string CHANNEL_ID = "new-devices";

        #endregion

        #region Properties

        /// <summary>
        /// Whether this platform can show the app's notifications.
        /// </summary>
        public static bool IsSupported =>
            OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst();

        #endregion

        #region Methods

        /// <summary>
        /// Whether the app may post notifications.
        /// </summary>
        /// <returns>True when allowed.</returns>
        public static async Task<bool> IsAllowedAsync()
        {
#if ANDROID
            return !OperatingSystem.IsAndroidVersionAtLeast(33)
                || (await Permissions.CheckStatusAsync<Permissions.PostNotifications>() == PermissionStatus.Granted);
#elif IOS || MACCATALYST
            UserNotifications.UNNotificationSettings settings = await UserNotifications.UNUserNotificationCenter.Current.GetNotificationSettingsAsync();

            return settings.AuthorizationStatus is UserNotifications.UNAuthorizationStatus.Authorized
                or UserNotifications.UNAuthorizationStatus.Provisional;
#else
            await Task.CompletedTask;
            return false;
#endif
        }

        /// <summary>
        /// Asks for permission to post notifications. When the system will not ask again, opens the
        /// app's settings page instead.
        /// </summary>
        /// <returns>True once allowed.</returns>
        public static async Task<bool> RequestPermissionAsync()
        {
#if ANDROID
            if (!OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                return true;
            }

            if (await Permissions.RequestAsync<Permissions.PostNotifications>() == PermissionStatus.Granted)
            {
                return true;
            }

            if (!Permissions.ShouldShowRationale<Permissions.PostNotifications>())
            {
                AppInfo.Current.ShowSettingsUI();
            }

            return false;
#elif IOS || MACCATALYST
            (bool granted, _) = await UserNotifications.UNUserNotificationCenter.Current.RequestAuthorizationAsync(
                UserNotifications.UNAuthorizationOptions.Alert | UserNotifications.UNAuthorizationOptions.Sound);

            return granted;
#else
            await Task.CompletedTask;
            return false;
#endif
        }

        /// <summary>
        /// Shows one notification. Tapping it opens the app.
        /// </summary>
        /// <param name="id">Identifies the notification; the same id replaces an earlier one.</param>
        /// <param name="title">The heading.</param>
        /// <param name="body">The text.</param>
        public static void Show(int id, string title, string body)
        {
#if ANDROID
            Android.Content.Context context = Platform.AppContext;

            if (context.GetSystemService(Android.Content.Context.NotificationService) is not Android.App.NotificationManager manager)
            {
                return;
            }

            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                manager.CreateNotificationChannel(new Android.App.NotificationChannel(
                    CHANNEL_ID, AppResources.AlertChannelName, Android.App.NotificationImportance.Default));
            }

            Android.Content.Intent? launch = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName!);
            Android.App.PendingIntent? open = launch is null ? null : Android.App.PendingIntent.GetActivity(
                context, 0, launch, Android.App.PendingIntentFlags.Immutable | Android.App.PendingIntentFlags.UpdateCurrent);

            AndroidX.Core.App.NotificationCompat.Builder builder = new(context, CHANNEL_ID);

            builder.SetSmallIcon(Resource.Drawable.ic_stat_devices);
            builder.SetContentTitle(title);
            builder.SetContentText(body);
            builder.SetStyle(new AndroidX.Core.App.NotificationCompat.BigTextStyle().BigText(body));
            builder.SetAutoCancel(true);
            builder.SetContentIntent(open);

            manager.Notify(id, builder.Build());
#elif IOS || MACCATALYST
            UserNotifications.UNUserNotificationCenter center = UserNotifications.UNUserNotificationCenter.Current;

            // Without a delegate, iOS drops notifications posted while the app is in front.
            center.Delegate ??= new ForegroundPresenter();

            UserNotifications.UNMutableNotificationContent content = new()
            {
                Title = title,
                Body = body,
                Sound = UserNotifications.UNNotificationSound.Default,
            };

            center.AddNotificationRequest(
                UserNotifications.UNNotificationRequest.FromIdentifier(id.ToString(), content, null),
                null);
#endif
        }

        #endregion

#if IOS || MACCATALYST
        #region Classes

        /// <summary>
        /// Shows notifications as banners even while the app is open.
        /// </summary>
        private sealed class ForegroundPresenter : UserNotifications.UNUserNotificationCenterDelegate
        {
            /// <summary>
            /// Presents the notification as a banner with sound.
            /// </summary>
            /// <param name="center">The notification centre.</param>
            /// <param name="notification">The notification.</param>
            /// <param name="completionHandler">Called with how to present it.</param>
            public override void WillPresentNotification(
                UserNotifications.UNUserNotificationCenter center,
                UserNotifications.UNNotification notification,
                Action<UserNotifications.UNNotificationPresentationOptions> completionHandler)
            {
                completionHandler(UserNotifications.UNNotificationPresentationOptions.Banner
                    | UserNotifications.UNNotificationPresentationOptions.Sound);
            }
        }

        #endregion
#endif
    }
}
