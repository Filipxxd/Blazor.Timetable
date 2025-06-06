﻿using Blazor.Timetable.Common.Enums;
using Blazor.Timetable.Common.Extensions;
using Blazor.Timetable.Models.Configuration;

namespace Blazor.Timetable.Common.Helpers;

internal static class DateTimeHelper
{
    public static DateOnly GetNextValidDate(DateOnly currentDate, IList<DayOfWeek> days, IList<Month> months)
    {
        while (!currentDate.IsValidFor(days, months))
        {
            currentDate = currentDate.AddDays(1);
        }

        return currentDate;
    }

    public static DateOnly GetStartOfWeekDate(DateOnly currentDate, DayOfWeek firstDayOfWeek)
    {
        var currentDayOfWeek = (int)currentDate.DayOfWeek;
        var targetDayOfWeek = (int)firstDayOfWeek;

        var diff = (7 + (currentDayOfWeek - targetDayOfWeek)) % 7;

        return currentDate.AddDays(-diff);
    }

    public static DateOnly GetDateForDay(DateOnly currentDate, DayOfWeek targetDay, DayOfWeek firstDayOfWeek)
    {
        var weekStartDate = GetStartOfWeekDate(currentDate, firstDayOfWeek);

        while (weekStartDate.DayOfWeek != targetDay)
        {
            weekStartDate = weekStartDate.AddDays(1);
        }

        return weekStartDate;
    }

    public static string GetLocalizedName(DayOfWeek dayOfWeek)
    {
        var today = DateTime.Today;
        var daysToAdd = ((int)dayOfWeek - (int)today.DayOfWeek + 7) % 7;

        return today.AddDays(daysToAdd).ToString("dddd", CultureConfig.CultureInfo);
    }

    public static string FormatHour(int hour, bool use24HourFormat)
        => use24HourFormat
            ? $"{hour}:00"
            : $"{hour % 12} {(hour < 12 ? "AM" : "PM")}";
}