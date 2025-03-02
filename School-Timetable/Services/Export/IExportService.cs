namespace School_Timetable.Services.Export;

public interface IExportService
{
    MemoryStream Export<TEvent>(IEnumerable<TEvent> records);
}