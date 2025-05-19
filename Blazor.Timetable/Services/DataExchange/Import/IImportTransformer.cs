using Blazor.Timetable.Models.DataExchange;

namespace Blazor.Timetable.Services.DataExchange.Import;

public interface IImportTransformer<TEvent>
    where TEvent : class
{
    IList<TEvent> Transform(Stream stream, IList<ISelector<TEvent>> selectors);
}
