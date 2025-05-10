using Blazor.Timetable.Models.DataExchange;
using Blazor.Timetable.Services.DataExchange.Export;
using System.Text;

namespace Blazor.Timetable.Services.DataExchange.Import;

internal sealed class CsvImportTransformer<TEvent> : IImportTransformer<TEvent>
    where TEvent : class
{
    private readonly IList<ISelector<TEvent>> _mappers;

    public CsvImportTransformer(IList<ISelector<TEvent>> mappers)
    {
        _mappers = mappers ??
          throw new ArgumentNullException(nameof(mappers));
    }

    public IList<TEvent> Transform(Stream stream)
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
            var mapper = _mappers.FirstOrDefault(m => m.Name == colName);
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
                var raw = fields.Length > kv.Key
                            ? CsvGenerator.Unescape(fields[kv.Key])
                            : string.Empty;
                kv.Value.SetValue(entity, raw);
            }

            eventsList.Add(entity);
        }

        return eventsList;
    }
}
