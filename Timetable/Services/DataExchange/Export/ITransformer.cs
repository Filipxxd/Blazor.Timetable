namespace Timetable.Services.DataExchange.Export;

public interface ITransformer
{
    IExportInfo Transform<TEvent>(IEnumerable<TEvent> records, IList<INamePropertySelector<TEvent>> properties)
        where TEvent : class;
}