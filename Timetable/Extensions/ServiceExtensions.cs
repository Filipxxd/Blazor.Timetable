using Microsoft.Extensions.DependencyInjection;
using Timetable.Services.Display;

namespace Timetable.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddSchoolTimetable(this IServiceCollection services)
    {
        services.AddScoped<DailyService>();
        services.AddScoped<WeeklyService>();
        services.AddScoped<MonthlyService>();

        return services;
    }
}