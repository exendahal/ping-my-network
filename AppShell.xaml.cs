using Microsoft.Maui.Platform;
using PingMyNetwork.Helper.Utilities;
using PingMyNetwork.Views;
using SimpleToolkit.SimpleShell;
using SimpleToolkit.SimpleShell.Extensions;
using SimpleToolkit.SimpleShell.Transitions;

namespace PingMyNetwork
{
    /// <summary>
    /// Routes for the four top-level destinations, plus the navigation chrome that sits over them.
    /// </summary>
    /// <remarks>
    /// Ported from sill_app. The tab bar and the rail are declared once here rather than on each
    /// page, so a route change never tears one down and builds another.
    /// </remarks>
    public partial class AppShell : SimpleShell
    {
        #region Variables

        /// <summary>
        /// Slide length for a tab switch.
        /// </summary>
        private const uint SWITCH_MS = 180;

        /// <summary>
        /// Slide length for pushing a screen onto the stack.
        /// </summary>
        private const uint PUSH_MS = 260;

        /// <summary>
        /// Name of the chrome show/hide animation.
        /// </summary>
        private const string CHROME_ANIMATION = "ChromeSlide";

        /// <summary>
        /// Extra travel so the chrome's shadow clears the edge too.
        /// </summary>
        private const double CHROME_CLEARANCE = 24;

        /// <summary>
        /// Used before the bar has been measured.
        /// </summary>
        private const double BAR_FALLBACK_HEIGHT = 86;

        /// <summary>
        /// Used before the rail has been measured.
        /// </summary>
        private const double RAIL_FALLBACK_WIDTH = 86;

        /// <summary>
        /// Whether the window is wide enough for the rail. Static because the page transitions are
        /// static and need to know which axis the destinations are laid out along.
        /// </summary>
        private static bool _IsWide =
            (DeviceInfo.Idiom != DeviceIdiom.Phone) && (DeviceInfo.Idiom != DeviceIdiom.Watch);

        private bool _ChromeShown = true;

        /// <summary>
        /// Longest a page waits for a switch to settle, should the transition never report back.
        /// </summary>
        private const int SETTLE_TIMEOUT_MS = 600;

        /// <summary>
        /// Idle time after launch before the other tabs are built, then between each of them.
        /// </summary>
        private const int WARM_UP_GAP_MS = 1200;

        /// <summary>
        /// Completes when the current tab switch has finished sliding in. Starts completed, since
        /// the first screen does not slide.
        /// </summary>
        private static TaskCompletionSource _Settled = Completed();

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the shell, picks the chrome that suits the window, and keeps it in step with the
        /// current route.
        /// </summary>
        public AppShell()
        {
            InitializeComponent();

            // Pushed on top of a destination rather than being destinations themselves, so they get
            // a back control and do not compete with the tabs.
            Routing.RegisterRoute(Routes.NETWORK_DETAILS, typeof(NetworkDetailsPage));
            Routing.RegisterRoute(Routes.DEVICE_DETAILS, typeof(DeviceDetailsPage));
            Routing.RegisterRoute(Routes.TOOL_PING, typeof(PingPage));
            Routing.RegisterRoute(Routes.TOOL_TRACEROUTE, typeof(TraceroutePage));
            Routing.RegisterRoute(Routes.TOOL_DNS, typeof(DnsLookupPage));
            Routing.RegisterRoute(Routes.TOOL_PORTS, typeof(PortCheckPage));
            Routing.RegisterRoute(Routes.TOOL_SPEED, typeof(SpeedTestPage));
            Routing.RegisterRoute(Routes.TOOL_WAKE, typeof(WakeOnLanPage));
            Routing.RegisterRoute(Routes.TOOL_SUBNET, typeof(SubnetPage));
            Routing.RegisterRoute(Routes.SETTINGS, typeof(SettingsPage));
            Routing.RegisterRoute(Routes.PERMISSIONS, typeof(PermissionsPage));

            TabBar.RouteRequested += OnRouteRequested;
            Rail.RouteRequested += OnRouteRequested;
            Navigated += OnNavigated;

            if (Application.Current is not null)
            {
                Application.Current.RequestedThemeChanged += (_, _) => ApplyAccent();
            }

            ApplyAccent();
            ApplyLayout();

            Loaded += (_, _) => WarmUpTabs();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Puts the app into the layout the window's width calls for. Called by the window, which
        /// is the one thing that reports width changes on every platform.
        /// </summary>
        /// <param name="width">The window's width in device-independent units.</param>
        internal void ApplyWindowWidth(double width)
        {
            bool wide = width >= LayoutWidthUtility.MEDIUM_MIN;

            if ((width <= 0) || (wide == _IsWide))
            {
                return;
            }

            _IsWide = wide;

            ApplyLayout();
        }

        /// <summary>
        /// Gives a page its transitions: siblings slide along the axis the navigation runs on, a
        /// push slides over from the right and a pop slides away to the right.
        /// </summary>
        /// <param name="page">The page to animate.</param>
        public static void ApplyTransition(Page page)
        {
            page.SetTransition(
                callback: static args =>
                {
                    switch (args.TransitionType)
                    {
                        // Moving along the tab bar slides sideways, along the rail up and down, so
                        // the motion says which way you went rather than just that something changed.
                        case SimpleShellTransitionType.Switching:
                        {
                            int direction = DirectionBetween(args.OriginPage, args.DestinationPage);

                            if (_IsWide)
                            {
                                double height = TravelDistance(args.DestinationPage.Height, args.OriginPage.Height, vertical: true);

                                args.DestinationPage.TranslationY = (1 - args.Progress) * direction * height;
                                args.OriginPage.TranslationY = -args.Progress * direction * height;
                            }
                            else
                            {
                                double width = TravelDistance(args.DestinationPage.Width, args.OriginPage.Width, vertical: false);

                                args.DestinationPage.TranslationX = (1 - args.Progress) * direction * width;
                                args.OriginPage.TranslationX = -args.Progress * direction * width;
                            }

                            break;
                        }

                        // Going deeper: the new screen slides over what you were on.
                        case SimpleShellTransitionType.Pushing:
                            args.DestinationPage.TranslationX = (1 - args.Progress) * PushDistance(args);
                            break;

                        // Coming back: the screen you are leaving slides away to reveal what was
                        // underneath. Moving the destination instead left the popped page on screen.
                        case SimpleShellTransitionType.Popping:
                            args.OriginPage.TranslationX = args.Progress * args.OriginPage.Width;
                            break;
                    }
                },
                starting: static args =>
                {
                    Rest(args.OriginPage);
                    Rest(args.DestinationPage);

                    // A pushed screen starts off the right edge. Resting it at zero drew it full
                    // screen for a frame before the slide began, which read as a flicker.
                    if (args.TransitionType == SimpleShellTransitionType.Pushing)
                    {
                        args.DestinationPage.TranslationX = PushDistance(args);
                    }
                },
                finished: static args =>
                {
                    _Settled.TrySetResult();

                    // Reset both axes, so a page reused later never inherits a stale offset.
                    Rest(args.OriginPage);
                    Rest(args.DestinationPage);
                },
                // ponytail: sill_app's durations even under reduce-motion; a zero length has not
                // been tried with SimpleShell. Switch to a short cross-fade if reviewers ask.
                duration: static args =>
                    args.TransitionType == SimpleShellTransitionType.Switching ? SWITCH_MS : PUSH_MS,

                // On a pop the destination is what was already underneath, so it stays behind while
                // the departing screen slides off it.
                destinationPageInFront: static args => args.TransitionType != SimpleShellTransitionType.Popping,
                easing: static args => Easing.CubicOut);
        }

        /// <summary>
        /// Completes once the current tab switch has finished, so a page can start loading data
        /// without competing with the slide for frames. Navigate first, load after.
        /// </summary>
        /// <returns>A task that completes when the switch has settled.</returns>
        public static async Task WhenSettledAsync()
        {
            Task settled = _Settled.Task;

            if (!settled.IsCompleted)
            {
                await Task.WhenAny(settled, Task.Delay(SETTLE_TIMEOUT_MS));
            }
        }

        /// <summary>
        /// Switches to a tab the chrome asked for. The tab lights up at once, before the shell has
        /// done any work, so a tap never looks ignored. Shell-level animation stays off: the custom
        /// transition does the motion, while the chrome itself stays put.
        /// </summary>
        /// <param name="sender">The tab bar or rail.</param>
        /// <param name="route">The destination's route.</param>
        private async void OnRouteRequested(object? sender, string route)
        {
            string[] segments = (CurrentState?.Location?.OriginalString ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries);

            // Already there: a second tap must not rebuild or re-slide the screen.
            if ((segments.Length == 1) && (segments[0] == route))
            {
                return;
            }

            TabBar.ActiveRoute = route;
            Rail.ActiveRoute = route;
            _Settled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            await GoToAsync($"//{route}", animate: false);
        }

        /// <summary>
        /// Builds the other tab pages, native views included, while the app is idle after launch,
        /// one at a time, so the first visit to each is as quick as every later one. Without this
        /// the first visit created a large page's platform views during its slide and starved it.
        /// </summary>
        private async void WarmUpTabs()
        {
            IMauiContext? context = Handler?.MauiContext;

            if (context is null)
            {
                return;
            }

            foreach (Type type in new[] { typeof(DevicesPage), typeof(WifiPage), typeof(ToolsPage) })
            {
                await Task.Delay(WARM_UP_GAP_MS);

                if (context.Services.GetService(type) is Page { Handler: null } page)
                {
                    _ = page.ToPlatform(context);
                }
            }
        }

        /// <summary>
        /// A source that has already completed.
        /// </summary>
        /// <returns>The source.</returns>
        private static TaskCompletionSource Completed()
        {
            TaskCompletionSource source = new();

            source.SetResult();

            return source;
        }

        /// <summary>
        /// Puts the chrome into whichever layout the current width calls for.
        /// </summary>
        private void ApplyLayout()
        {
            // A chrome animation may have left the outgoing one part way off its edge.
            Reset(Rail, _IsWide && _ChromeShown);
            Reset(TabBar, !_IsWide && _ChromeShown);

            SyncActiveRoute();
        }

        /// <summary>
        /// A usable travel distance, safe for a page that has not been measured yet.
        /// </summary>
        /// <param name="destination">The incoming page's size along the axis.</param>
        /// <param name="origin">The outgoing page's size along the axis.</param>
        /// <param name="vertical">Whether the travel is up and down rather than side to side.</param>
        /// <returns>A usable distance.</returns>
        private static double TravelDistance(double destination, double origin, bool vertical)
        {
            if (destination > 0)
            {
                return destination;
            }

            if (origin > 0)
            {
                return origin;
            }

            DisplayInfo display = DeviceDisplay.Current.MainDisplayInfo;
            double density = display.Density > 0 ? display.Density : 1;

            return (vertical ? display.Height : display.Width) / density;
        }

        /// <summary>
        /// How far a pushed screen travels.
        /// </summary>
        /// <param name="args">The transition in progress.</param>
        /// <returns>A usable distance.</returns>
        private static double PushDistance(SimpleShellTransitionArgs args)
        {
            return TravelDistance(args.DestinationPage.Width, args.OriginPage.Width, vertical: false);
        }

        /// <summary>
        /// Puts a page back to its resting position on both axes.
        /// </summary>
        /// <param name="page">The page to reset.</param>
        private static void Rest(VisualElement page)
        {
            page.Opacity = 1;
            page.TranslationX = 0;
            page.TranslationY = 0;
        }

        /// <summary>
        /// Which way a switch travels: 1 when moving forward along the tab order, -1 when back.
        /// </summary>
        /// <param name="origin">The screen being left.</param>
        /// <param name="destination">The screen being opened.</param>
        /// <returns>1 or -1.</returns>
        private static int DirectionBetween(VisualElement origin, VisualElement destination)
        {
            return OrderOf(destination) >= OrderOf(origin) ? 1 : -1;
        }

        /// <summary>
        /// Position of a page in the tab order. Each top-level page carries its route as ClassId.
        /// </summary>
        /// <param name="page">The page to place.</param>
        /// <returns>Its index along the tabs.</returns>
        private static int OrderOf(VisualElement page)
        {
            int index = -1;

            for (var i = 0; i < Routes.TABS.Count; i++)
            {
                if (Routes.TABS[i] == page.ClassId)
                {
                    index = i;
                    break;
                }
            }

            return Math.Max(index, 0);
        }

        /// <summary>
        /// Keeps the chrome in step with wherever the shell ended up, including after a back
        /// gesture rather than a tap.
        /// </summary>
        /// <param name="sender">The shell.</param>
        /// <param name="e">The navigation that happened.</param>
        private void OnNavigated(object? sender, ShellNavigatedEventArgs e)
        {
            SyncActiveRoute();
        }

        /// <summary>
        /// Marks the destination showing, and hides the chrome on screens pushed on top of one.
        /// </summary>
        private void SyncActiveRoute()
        {
            string location = CurrentState?.Location?.OriginalString ?? string.Empty;
            string[] segments = location.Split('/', StringSplitOptions.RemoveEmptyEntries);
            string route = segments.Length > 0 ? segments[0] : Routes.HOME;

            // A pushed screen has a back control; a tab bar over it would offer to switch away from
            // a screen the user is in the middle of.
            SetChromeShown(segments.Length <= 1);

            TabBar.ActiveRoute = route;
            Rail.ActiveRoute = route;
        }

        /// <summary>
        /// Slides the chrome out of the way, and back again, so it reads as one object moving
        /// rather than blinking between two states.
        /// </summary>
        /// <param name="shown">Whether the chrome should end up visible.</param>
        private void SetChromeShown(bool shown)
        {
            if (_ChromeShown == shown)
            {
                return;
            }

            _ChromeShown = shown;

            View chrome = _IsWide ? Rail : TabBar;
            double distance = _IsWide
                ? (chrome.Width > 0 ? chrome.Width : RAIL_FALLBACK_WIDTH)
                : (chrome.Height > 0 ? chrome.Height : BAR_FALLBACK_HEIGHT);
            double hidden = distance + CHROME_CLEARANCE;

            if (shown)
            {
                chrome.IsVisible = true;
            }

            if (MotionSettings.PrefersReducedMotion)
            {
                Reset(chrome, shown);
                return;
            }

            chrome.AbortAnimation(CHROME_ANIMATION);

            double from = _IsWide ? chrome.TranslationX : chrome.TranslationY;
            double to = shown ? 0 : (_IsWide ? -hidden : hidden);

            Animation slide = new(
                offset =>
                {
                    if (_IsWide)
                    {
                        chrome.TranslationX = offset;
                    }
                    else
                    {
                        chrome.TranslationY = offset;
                    }

                    // Fades slightly on the way out so it does not read as sliding behind content.
                    chrome.Opacity = 1 - (Math.Abs(offset) / Math.Max(1, hidden));
                },
                from,
                to,
                Easing.CubicOut);

            slide.Commit(
                chrome,
                CHROME_ANIMATION,
                rate: 16,
                length: PUSH_MS,
                finished: (_, _) => Reset(chrome, shown));
        }

        /// <summary>
        /// Puts the chrome in its final resting state.
        /// </summary>
        /// <param name="chrome">The bar or rail.</param>
        /// <param name="shown">Whether it should be visible.</param>
        private static void Reset(View chrome, bool shown)
        {
            if (shown)
            {
                chrome.TranslationX = 0;
                chrome.TranslationY = 0;
                chrome.Opacity = 1;
            }

            chrome.IsVisible = shown;
        }

        /// <summary>
        /// Applies the brand accent to the chrome.
        /// </summary>
        private void ApplyAccent()
        {
            TabBar.AccentColor = StatusPalette.Accent;
            TabBar.InactiveColor = StatusPalette.Neutral;
            Rail.AccentColor = StatusPalette.Accent;
        }

        #endregion
    }
}
