using Blazor.Timetable.Common.Enums;

namespace Blazor.Timetable.Models.Actions;

public sealed class ImportAction<TEvent> where TEvent : class
{
    public IList<TEvent> Events { get; set; } = [];
    public ImportType Type { get; set; } = ImportType.Append;
}
