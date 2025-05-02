using Timetable.Common.Enums;

namespace Timetable.Models.Props;

public class ImportProps<TEvent> where TEvent : class
{
    public IList<TEvent> Events { get; set; } = [];
    public ImportType Type { get; set; } = ImportType.Append;
}
