#if ANDROID
using MauiLocalNotification.Services;
#endif
using Microsoft.Extensions.Logging;

namespace MauiLocalNotification;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
		builder.Logging.AddDebug();
#endif

#if ANDROID
        builder.Services.AddSingleton<INotificationManagerService, Platforms.Android.Services.NotificationManagerService>();
#endif
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
    }
}
