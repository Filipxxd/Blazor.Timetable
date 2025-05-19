using Blazor.Timetable.Models.DataExchange;
using Blazor.Timetable.Services.DataExchange.Export;
using System.Text;

namespace Blazor.Timetable.Services.DataExchange.Import;

public sealed class CsvImportTransformer<TEvent> : IImportTransformer<TEvent>
    where TEvent : class
{
    public IList<TEvent> Transform(Stream stream, IList<ISelector<TEvent>> selectors)
    {
        var eventsList = new List<TEvent>();

        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: false);

        var headerLine = reader.ReadLine();
        if (headerLine is null) return eventsList;

        var headers = headerLine
                        .Split(CsvGenerator.Separator)
                        .Select(h => h.Trim())
                        .ToArray();

        var indexMap = new Dictionary<int, ISelector<TEvent>>();
        for (var i = 0; i < headers.Length; i++)
        {
            var colName = headers[i];
            var mapper = selectors.FirstOrDefault(m => m.Name == colName);
            if (mapper != null)
                indexMap[i] = mapper;
        }

        string? row;
        while ((row = reader.ReadLine()) != null)
        {
            var entity = Activator.CreateInstance<TEvent>()
                            ?? throw new InvalidOperationException(
                                $"Cannot create instance of {typeof(TEvent)}");

            var fields = row.Split(CsvGenerator.Separator);
            foreach (var kv in indexMap)
            {
                var value = fields.Length > kv.Key
                            ? CsvGenerator.Unescape(fields[kv.Key])
                            : string.Empty;
                kv.Value.SetValue(entity, value);
            }

            eventsList.Add(entity);
        }

        return eventsList;
    }
}
