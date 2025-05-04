using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using Timetable.Models.Configuration;
using Timetable.Services;
using Timetable.Services.Display;

namespace Timetable.Common.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddBlazorTimetable(this IServiceCollection services)
    {
        services.AddScoped<IDisplayService, DailyService>();
        services.AddScoped<IDisplayService, WeeklyService>();
        services.AddScoped<IDisplayService, MonthlyService>();

        services.AddScoped<ModalService>();

        return services;
    }

    public static IServiceCollection UseCulture(this IServiceCollection services, CultureInfo cultureInfo)
    {
        CultureConfig.SetCulture(cultureInfo);

        return services;
    }
}