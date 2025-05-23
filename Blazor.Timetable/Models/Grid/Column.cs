﻿namespace Blazor.Timetable.Models.Grid;

internal sealed class Column<TEvent> where
    TEvent : class
{
    public required DayOfWeek DayOfWeek { get; init; }
    public required int Index { get; init; }
    public List<Cell<TEvent>> Cells { get; init; } = [];
}
