namespace PingMyNetwork.Implementation
{
    /// <summary>
    /// Checks the network for new devices while the app is closed.
    /// </summary>
    /// <remarks>
    /// Android only, through the system's own JobScheduler (no WorkManager dependency, which would
    /// raise the minimum Android version): about every 15 minutes, the OS minimum, and only on
    /// unmetered Wi-Fi. Doze can delay it. iOS has no dependable background time for a network
    /// scan, so it checks when the app opens instead.
    /// </remarks>
    public static class BackgroundCheck
    {
        #region Variables

        /// <summary>
        /// Identifies the job, so turning alerts on twice never schedules two.
        /// </summary>
        private const int JOB_ID = 4201;

        #endregion

        #region Methods

        /// <summary>
        /// Starts or cancels the periodic check.
        /// </summary>
        /// <param name="isOn">Whether alerts are on.</param>
        public static void Schedule(bool isOn)
        {
#if ANDROID
            Android.Content.Context context = Platform.AppContext;

            if (context.GetSystemService(Android.Content.Context.JobSchedulerService) is not Android.App.Job.JobScheduler scheduler)
            {
                return;
            }

            if (!isOn)
            {
                scheduler.Cancel(JOB_ID);
                return;
            }

            Android.App.Job.JobInfo job = new Android.App.Job.JobInfo.Builder(
                    JOB_ID,
                    new Android.Content.ComponentName(context, Java.Lang.Class.FromType(typeof(DeviceCheckJob))))
                .SetRequiredNetworkType(Android.App.Job.NetworkType.Unmetered)!
                .SetPeriodic((long)TimeSpan.FromMinutes(15).TotalMilliseconds)!
                .SetPersisted(false)!
                .Build()!;

            scheduler.Schedule(job);
#endif
        }

        #endregion
    }

#if ANDROID
    /// <summary>
    /// One background check: reads the connection, then runs a normal device scan, which
    /// announces anything new through DeviceAlerts.
    /// </summary>
    /// <remarks>
    /// ponytail: not persisted across reboots (that needs RECEIVE_BOOT_COMPLETED); the job is
    /// scheduled again whenever the app opens with alerts on.
    /// </remarks>
    [Android.App.Service(Name = "com.pingmynetwork.DeviceCheckJob", Permission = "android.permission.BIND_JOB_SERVICE", Exported = true)]
    public sealed class DeviceCheckJob : Android.App.Job.JobService
    {
        #region Variables

        /// <summary>
        /// Longest a background scan may take.
        /// </summary>
        private static readonly TimeSpan TIME_LIMIT = TimeSpan.FromSeconds(30);

        #endregion

        #region Methods

        /// <summary>
        /// Starts the check off the main thread.
        /// </summary>
        /// <param name="params">The job's parameters.</param>
        /// <returns>True: the work continues after this returns.</returns>
        public override bool OnStartJob(Android.App.Job.JobParameters? @params)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await CheckAsync();
                }
                catch (Exception ex)
                {
                    // Nothing awaits this task, so say why the check failed rather than lose it.
                    Android.Util.Log.Error("PingMyNetwork", $"Background check failed: {ex}");
                }
                finally
                {
                    JobFinished(@params, false);
                }
            });

            return true;
        }

        /// <summary>
        /// The system stopped the job early; the next run checks again.
        /// </summary>
        /// <param name="params">The job's parameters.</param>
        /// <returns>False: no need to reschedule, the job is periodic.</returns>
        public override bool OnStopJob(Android.App.Job.JobParameters? @params)
        {
            return false;
        }

        /// <summary>
        /// Reads the connection and scans it, within the time limit.
        /// </summary>
        /// <returns>A task that completes once done or out of time.</returns>
        private static async Task CheckAsync()
        {
            IServiceProvider? services = IPlatformApplication.Current?.Services;

            // UseMauiWifiManager hands the Wi-Fi library a context only when an activity starts;
            // a check that starts the app in the background has none.
            if (Platform.CurrentActivity is null)
            {
                MauiWifiManager.WifiNetworkService.Init(Platform.AppContext);
            }

            // Resolving the alerts subscribes them to the scan's new-device reports.
            if ((services?.GetService(typeof(NetworkMonitor)) is not NetworkMonitor monitor)
                || (services.GetService(typeof(DeviceManager)) is not DeviceManager manager)
                || (services.GetService(typeof(DeviceAlerts)) is null))
            {
                return;
            }

            await monitor.RefreshNowAsync();

            // Without the Wi-Fi name the network can't be told apart from a new one: a scan would
            // file it as one, and a first scan never alerts.
            if (!monitor.Current.IsConnected || (monitor.Current.Ssid is null))
            {
                return;
            }

            await manager.LoadCurrentNetworkAsync();

            Task scan = manager.ScanAsync();

            if (await Task.WhenAny(scan, Task.Delay(TIME_LIMIT)) != scan)
            {
                manager.CancelScan();
            }
        }

        #endregion
    }
#endif
}
