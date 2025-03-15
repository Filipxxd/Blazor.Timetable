namespace Timetable.Services.DataExchange.Export;

public interface ITransformer
{
    ExportInfo Transform<TEvent>(IEnumerable<TEvent> records, IList<INamePropertySelector<TEvent>> properties)
        where TEvent : class;
}