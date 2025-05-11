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

    public static IServiceCollection UseCulture(this IServiceCollection services, CultureInfo cultureInfo)
    {
        CultureConfig.SetCulture(cultureInfo);

        return services;
    }
}
