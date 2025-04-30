using System.Linq.Expressions;
using System.Text;
using Timetable.Common.Helpers;
using Timetable.Services.DataExchange.Export;

namespace Timetable.Services.DataExchange.Import;

public interface INamePropertyMapper<TEvent>
  where TEvent : class
{
    string Name { get; }
    void SetValue(TEvent target, string raw);
}

public sealed class NamePropertyMapper<TEvent, TProperty>
  : INamePropertyMapper<TEvent>
  where TEvent : class
{
    public string Name { get; init; }
    private readonly Action<TEvent, TProperty> _setter;
    private readonly Func<string, TProperty> _parser;

    public NamePropertyMapper(
      string name,
      Expression<Func<TEvent, TProperty>> selector,
      Func<string, TProperty>? parser = null)
    {
        Name = name;
        // you’ll need your own PropertyHelper.CreateSetter(...)
        _setter = PropertyHelper.CreateSetter(selector);
        _parser = parser ?? (s => (TProperty)Convert.ChangeType(s, typeof(TProperty)));
    }

    public void SetValue(TEvent target, string raw)
    {
        var value = _parser(raw);
        _setter(target, value);
    }
}

public interface IImportTransformer<TEvent>
  where TEvent : class
{
    IEnumerable<TEvent> Transform(Stream stream);
}

internal sealed class CsvImportTransformer<TEvent>
  : IImportTransformer<TEvent>
  where TEvent : class
{
    private readonly IList<INamePropertyMapper<TEvent>> _mappers;

    public CsvImportTransformer(IList<INamePropertyMapper<TEvent>> mappers)
    {
        _mappers = mappers ??
          throw new ArgumentNullException(nameof(mappers));
    }

    public IEnumerable<TEvent> Transform(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: false);

        // 1) read headers
        var headerLine = reader.ReadLine();
        if (headerLine is null) yield break;

        var headers = headerLine
                        .Split(CsvGenerator.Separator)
                        .Select(h => h.Trim())
                        .ToArray();

        // 2) map column‐index → mapper
        var indexMap = new Dictionary<int, INamePropertyMapper<TEvent>>();
        for (var i = 0; i < headers.Length; i++)
        {
            var colName = headers[i];
            var mapper = _mappers.FirstOrDefault(m => m.Name == colName);
            if (mapper != null)
                indexMap[i] = mapper;
        }

        // 3) parse each data row
        string? row;
        while ((row = reader.ReadLine()) != null)
        {
            // create instance via Activator
            var entity = Activator.CreateInstance<TEvent>()
                         ?? throw new InvalidOperationException(
                              $"Cannot create instance of {typeof(TEvent)}");

            var fields = row.Split(CsvGenerator.Separator);

            foreach (var kv in indexMap)
            {
                // split + unescape
                var raw = fields.Length > kv.Key
                          ? CsvGenerator.Unescape(fields[kv.Key])
                          : string.Empty;

                // drive setter via the mapper
                kv.Value.SetValue(entity, raw);
            }

            yield return entity;
        }
    }
}