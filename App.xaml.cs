using PingMyNetwork.Helper.Utilities;
using PingMyNetwork.Implementation;
using PingMyNetwork.Resources.Strings;

namespace PingMyNetwork
{
    /// <summary>
    /// The application: creates the window and ties monitoring to whether the app is in front.
    /// </summary>
    public partial class App : Application
    {
        #region Variables

        /// <summary>
        /// Smallest usable window, from the PRD's responsive layout section.
        /// </summary>
        private const double MIN_WINDOW_WIDTH = 360;

        /// <summary>
        /// Smallest usable window height.
        /// </summary>
        private const double MIN_WINDOW_HEIGHT = 560;

        private const string WINDOW_X_KEY = "window.x";
        private const string WINDOW_Y_KEY = "window.y";
        private const string WINDOW_WIDTH_KEY = "window.width";
        private const string WINDOW_HEIGHT_KEY = "window.height";

        private readonly NetworkMonitor _Monitor;
        private readonly DeviceAlerts _Alerts;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the application.
        /// </summary>
        /// <param name="monitor">The connection monitor, paused while the app is in the background.</param>
        /// <param name="alerts">New-device alerts, which check the network while the app is open.</param>
        public App(NetworkMonitor monitor, DeviceAlerts alerts)
        {
            InitializeComponent();

            _Monitor = monitor;
            _Alerts = alerts;

            AppSettings.ApplyTheme(this);

#if DEBUG
            // Lets design QA capture both themes without changing the system setting:
            // set PMN_THEME=Light or PMN_THEME=Dark before launching.
            if (Enum.TryParse(Environment.GetEnvironmentVariable("PMN_THEME"), out AppTheme theme))
            {
                UserAppTheme = theme;
            }
#endif
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates the main window with the shell, restoring its last size and position on desktop.
        /// </summary>
        /// <param name="activationState">Platform activation details.</param>
        /// <returns>The window.</returns>
        protected override Window CreateWindow(IActivationState? activationState)
        {
            AppShell shell = new();
            Window window = new(shell)
            {
                MinimumWidth = MIN_WINDOW_WIDTH,
                MinimumHeight = MIN_WINDOW_HEIGHT,
            };

            if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
            {
                RestoreBounds(window);
                window.Destroying += (_, _) => SaveBounds(window);
                window.TitleBar = BuildTitleBar();
            }

            // The shell picks its chrome from the window's width. The window is what reports width
            // changes on every platform, including rotation on Android.
            window.SizeChanged += (_, _) => shell.ApplyWindowWidth(window.Width);

            // Nothing polls while the app is in the background.
            window.Stopped += (_, _) =>
            {
                _Monitor.Stop();
                _Alerts.Stop();
            };
            window.Resumed += (_, _) =>
            {
                _Monitor.Start();
                _Alerts.Start();
            };
            window.Created += (_, _) => _Alerts.Start();

            // First launch: explain and ask for permissions before the screens need them.
            if (!AppSettings.IsOnboarded)
            {
                async void ShowPermissions(object? sender, EventArgs e)
                {
                    shell.Loaded -= ShowPermissions;
                    await shell.GoToAsync(Routes.PERMISSIONS, animate: false);
                }

                shell.Loaded += ShowPermissions;
            }

#if DEBUG
            // Lets design QA open a screen without clicking through the app:
            // set PMN_ROUTE=//tools/ping before launching.
            if (Environment.GetEnvironmentVariable("PMN_ROUTE") is { Length: > 0 } route)
            {
                window.Created += (_, _) => shell.Dispatcher.DispatchDelayed(
                    TimeSpan.FromSeconds(1), async () => await shell.GoToAsync(route, animate: false));
            }
#endif

            return window;
        }

        /// <summary>
        /// A title bar in the app's own colours, instead of the system's dark strip that clashes with
        /// the light theme. It carries the app's name, so the rail needs no logo of its own.
        /// </summary>
        /// <returns>The title bar.</returns>
        private static TitleBar BuildTitleBar()
        {
            Label mark = new()
            {
                Text = MaterialIcon.LAN,
                FontFamily = MaterialIcon.FONT_FAMILY,
                FontSize = 18,
                Margin = new Thickness(16, 0, 0, 0),
                VerticalOptions = LayoutOptions.Center,
            };

            mark.SetAppThemeColor(
                Label.TextColorProperty,
                ResourceLookup.Get("AccentLight", Colors.Green),
                ResourceLookup.Get("AccentDark", Colors.Green));

            TitleBar titleBar = new()
            {
                Title = AppResources.AppName,
                LeadingContent = mark,
                HeightRequest = 40,
            };

            titleBar.SetAppThemeColor(
                VisualElement.BackgroundColorProperty,
                ResourceLookup.Get("SurfaceLight", Colors.White),
                ResourceLookup.Get("SurfaceDark", Colors.Black));
            titleBar.SetAppThemeColor(
                TitleBar.ForegroundColorProperty,
                ResourceLookup.Get("OnSurfaceLight", Colors.Black),
                ResourceLookup.Get("OnSurfaceDark", Colors.White));

            return titleBar;
        }

        /// <summary>
        /// Puts the window back where the user left it.
        /// </summary>
        /// <param name="window">The window to place.</param>
        private static void RestoreBounds(Window window)
        {
            double width = Preferences.Default.Get(WINDOW_WIDTH_KEY, 0d);
            double height = Preferences.Default.Get(WINDOW_HEIGHT_KEY, 0d);

            if ((width < MIN_WINDOW_WIDTH) || (height < MIN_WINDOW_HEIGHT))
            {
                return;
            }

            window.Width = width;
            window.Height = height;
            window.X = Preferences.Default.Get(WINDOW_X_KEY, window.X);
            window.Y = Preferences.Default.Get(WINDOW_Y_KEY, window.Y);
        }

        /// <summary>
        /// Remembers the window's size and position for the next launch.
        /// </summary>
        /// <param name="window">The window being closed.</param>
        private static void SaveBounds(Window window)
        {
            Preferences.Default.Set(WINDOW_X_KEY, window.X);
            Preferences.Default.Set(WINDOW_Y_KEY, window.Y);
            Preferences.Default.Set(WINDOW_WIDTH_KEY, window.Width);
            Preferences.Default.Set(WINDOW_HEIGHT_KEY, window.Height);
        }

        #endregion
    }
}
