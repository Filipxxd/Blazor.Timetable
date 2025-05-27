using Blazor.Timetable.Models.DataExchange;

namespace Blazor.Timetable.Services.DataExchange.Export;

public interface IExportTransformer
{
    IExportInfo Transform<TEvent>(
            IEnumerable<TEvent> records,
            IList<ISelector<TEvent>> properties
        ) where TEvent : class;
}