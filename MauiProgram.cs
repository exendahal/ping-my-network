using MauiWifiManager;
using Microsoft.Extensions.Logging;
using PingMyNetwork.Helper.Utilities;
using PingMyNetwork.Implementation;
using PingMyNetwork.ViewModels;
using PingMyNetwork.Views;
using SimpleToolkit.SimpleShell;

namespace PingMyNetwork
{
    /// <summary>
    /// Builds the app: fonts, navigation and the services each screen is given.
    /// </summary>
    public static class MauiProgram
    {
        #region Methods

        /// <summary>
        /// Builds the application.
        /// </summary>
        /// <returns>The configured app.</returns>
        public static MauiApp CreateMauiApp()
        {
            MauiAppBuilder builder = MauiApp.CreateBuilder();

            FlattenFields();

            builder
                .UseMauiApp<App>()
                .UseMauiWifiManager()

                // Platform transitions off: the shell drives its own, so a tab switch and a push
                // look the same on every platform.
                .UseSimpleShell(usePlatformTransitions: false)
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("MaterialIcons-Regular.ttf", MaterialIcon.FONT_FAMILY);
                });

            // The library's own interface, so a fake can stand in for demo mode and tests later.
            builder.Services.AddSingleton<IWifiNetworkService>(_ => CrossWifiManager.Current);
            builder.Services.AddSingleton<NetworkMonitor>();
            builder.Services.AddSingleton<DeviceStore>();
            builder.Services.AddSingleton<DeviceManager>();
            builder.Services.AddSingleton<WifiScanner>();

            // Singletons so a tab comes back as it was left: Home with what it last knew, Devices
            // with its list, filter and any scan still running.
            builder.Services.AddSingleton<HomeViewModel>();
            builder.Services.AddSingleton<DevicesViewModel>();
            builder.Services.AddSingleton<WifiViewModel>();
            builder.Services.AddTransient<NetworkDetailsViewModel>();
            builder.Services.AddTransient<DeviceDetailsViewModel>();

            builder.Services.AddTransient<HomePage>();
            builder.Services.AddTransient<NetworkDetailsPage>();
            builder.Services.AddTransient<DevicesPage>();
            builder.Services.AddTransient<DeviceDetailsPage>();
            builder.Services.AddTransient<WifiPage>();

#if DEBUG
            builder.Logging.AddDebug();
            DeviceIdentity.SelfCheck();
            DeviceDescriber.SelfCheck();
            WifiSecurity.SelfCheck();
            ChannelPlanner.SelfCheck();
#endif

            return builder.Build();
        }

        /// <summary>
        /// Every text field and picker in the app sits inside a framed card, so the platform's own
        /// box around it would be a second frame. Windows draws a filled box that changes on hover
        /// and focus; Android draws an underline. Both are removed; the card is the frame.
        /// </summary>
        private static void FlattenFields()
        {
#if WINDOWS
            Microsoft.UI.Xaml.Media.SolidColorBrush clear = new(Microsoft.UI.Colors.Transparent);
            string[] textBoxKeys =
            [
                "TextControlBackground",
                "TextControlBackgroundPointerOver",
                "TextControlBackgroundFocused",
                "TextControlBorderBrush",
                "TextControlBorderBrushPointerOver",
                "TextControlBorderBrushFocused",
            ];
            string[] comboBoxKeys =
            [
                "ComboBoxBackground",
                "ComboBoxBackgroundPointerOver",
                "ComboBoxBackgroundPressed",
                "ComboBoxBackgroundFocused",
                "ComboBoxBorderBrush",
                "ComboBoxBorderBrushPointerOver",
                "ComboBoxBorderBrushPressed",
                "ComboBoxBorderBrushFocused",
            ];

            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("Flat", (handler, _) =>
            {
                foreach (string key in textBoxKeys)
                {
                    handler.PlatformView.Resources[key] = clear;
                }

                handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
            });

            Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("Flat", (handler, _) =>
            {
                foreach (string key in comboBoxKeys)
                {
                    handler.PlatformView.Resources[key] = clear;
                }

                handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
            });
#elif ANDROID
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("Flat", static (handler, _) =>
            {
                handler.PlatformView.BackgroundTintList =
                    Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
            });
#endif
        }

        #endregion
    }
}
