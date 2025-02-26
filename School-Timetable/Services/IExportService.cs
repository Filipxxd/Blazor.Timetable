namespace School_Timetable.Services;

public interface IExportService
{
    MemoryStream Export<TEvent>(IEnumerable<TEvent> records);
}