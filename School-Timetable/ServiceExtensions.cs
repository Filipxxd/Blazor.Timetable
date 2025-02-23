using Microsoft.Extensions.DependencyInjection;
using School_Timetable.Services.DisplayTypeServices;

namespace School_Timetable;

public static class ServiceExtensions
{
    public static IServiceCollection AddSchoolTimetable(this IServiceCollection services)
    {
        services.AddTransient<IDisplayTypeService, WeeklyService>();
        
        return services;
    }
}