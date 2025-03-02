using Microsoft.Extensions.DependencyInjection;
using Timetable.Services.Display;

namespace Timetable.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddSchoolTimetable(this IServiceCollection services)
    {
        services.AddScoped<IDisplayService, DailyService>();
        services.AddScoped<IDisplayService, WeeklyService>();
        services.AddScoped<IDisplayService, MonthlyService>();
        EnsureNoDuplicateDisplayTypes(services);

        return services;
    }
    
    private static void EnsureNoDuplicateDisplayTypes(IServiceCollection services)
    {
        using var serviceProvider = services.BuildServiceProvider();
        var displayTypeServices = serviceProvider.GetServices<IDisplayService>();
        var duplicateDisplayTypes = displayTypeServices
            .GroupBy(service => service.DisplayType)
            .Any(group => group.Count() > 1);

        if (duplicateDisplayTypes)
            throw new InvalidOperationException("Duplicate DisplayType found");
    }
}