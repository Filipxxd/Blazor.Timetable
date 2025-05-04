using Blazor.Timetable.Common.Enums;

namespace Blazor.Timetable.Models.Grid;

internal sealed class Cell<TEvent> where
    TEvent : class
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required DateTime DateTime { get; init; }
    public string? Title { get; init; }
    public required int RowIndex { get; init; }
    public required CellType Type { get; set; }
    public IList<CellItem<TEvent>> Items { get; set; } = [];
}
