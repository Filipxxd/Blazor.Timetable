namespace Blazor.Timetable.Services.DataExchange.Export;

public interface IExportTransformer
{
    IExportInfo Transform<TEvent>(IEnumerable<TEvent> records, IList<IExportSelector<TEvent>> properties)
        where TEvent : class;
}