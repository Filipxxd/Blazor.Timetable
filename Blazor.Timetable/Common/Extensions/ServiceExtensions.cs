using Blazor.Timetable.Common.Enums;
using Blazor.Timetable.Models.Configuration;
using Blazor.Timetable.Services;
using Blazor.Timetable.Services.Display;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace Blazor.Timetable.Common.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddBlazorTimetable(this IServiceCollection services)
    {
        services.AddScoped<IDisplayService, DailyService>();
        services.AddScoped<IDisplayService, WeeklyService>();
        services.AddScoped<IDisplayService, MonthlyService>();

        services.AddScoped<ModalService>();

        var resourcesNamespace = typeof(Resources.GlobalResource).FullName
            ?? throw new ArgumentException("Resource namespace not found.");

        services.AddLocalization(options => options.ResourcesPath = "Resources")
                .AddSingleton(s => new Localizer(resourcesNamespace, typeof(Resources.GlobalResource).Assembly));

        return services;
    }

    public static IServiceCollection Localize(this IServiceCollection services, Language language)
    {
        var culture = language switch
        {
            Language.English => "en",
            Language.Czech => "cs",
            _ => throw new ArgumentException("Language is not supported.", nameof(language))
        };

        CultureConfig.SetCulture(new CultureInfo(culture));

        return services;
    }
}
