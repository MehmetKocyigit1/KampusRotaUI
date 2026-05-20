using KampusRotaUI;
using CommunityToolkit.Maui; // Toolkit için
using Microsoft.Extensions.Logging;

namespace KampusRotaUI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            // 1. Toolkit'i burada aktif ediyoruz (XAML hatalarını engeller)
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

         builder.Services.AddSingleton<HttpClient>();


#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}