using Timetable.Common.Enums;

namespace Timetable.Structure;

internal sealed class Cell<TEvent> where
    TEvent : class
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required DateTime DateTime { get; init; }
    public string? Title { get; init; }
    public required int RowIndex { get; init; }
    public required CellType Type { get; set; }
    public IList<EventWrapper<TEvent>> Events { get; set; } = [];
}
