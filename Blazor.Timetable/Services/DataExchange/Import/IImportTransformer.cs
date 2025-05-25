using Blazor.Timetable.Models.DataExchange;

namespace Blazor.Timetable.Services.DataExchange.Import;

public interface IImportTransformer
{
    IList<TEvent> Transform<TEvent>(Stream stream, IList<ISelector<TEvent>> selectors)
        where TEvent : class;
}
