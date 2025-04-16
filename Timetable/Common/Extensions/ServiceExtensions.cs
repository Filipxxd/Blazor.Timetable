using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using Timetable.Configuration;
using Timetable.Services.Display;

namespace Timetable.Common.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddSchoolTimetable(this IServiceCollection services)
    {
        services.AddScoped<DailyService>();
        services.AddScoped<WeeklyService>();
        services.AddScoped<MonthlyService>();

        return services;
    }

    public static IServiceCollection UseCulture(this IServiceCollection services, CultureInfo cultureInfo)
    {
        CultureConfig.SetCulture(cultureInfo);

        return services;
    }
}