using System.Text;
using Microsoft.JSInterop;

namespace Timetable.Services.DataExchange.Export;

internal sealed class CsvTransformer : ITransformer
{
    private const char Separator = ';';

    public ExportInfo Transform<TEvent>(IEnumerable<TEvent> records, IList<INamePropertySelector<TEvent>> properties) 
        where TEvent : class
    {
        var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, Encoding.UTF8, 1024, true);
        
        var headerLine = string.Join(Separator, properties.Select(p =>
        {
            if (p.Name.Contains(Separator))
                throw new ArgumentException($"Property name '{p.Name}' cannot contain the separator character '{Separator}'.");
                
            return p.Name.Trim();
        }));

        writer.WriteLine(headerLine);
        
        foreach (var record in records)
        {
            var line = string.Join(Separator, properties.Select(s =>
            {

                var value =                 s.GetStringValue(record);
                var escapedValue = EscapeCsvValue(value).Trim();

                if (escapedValue.Contains(Separator))
                    throw new InvalidOperationException($"Property value '{escapedValue}' cannot contain the separator character '{Separator}'.");

                return escapedValue;
            }));

            writer.WriteLine(line);
        }

        writer.Flush();
        memoryStream.Position = 0;

        return new ExportInfo
        {
            StreamReference = new DotNetStreamReference(memoryStream, true),
            FileExtension = "csv"
        };
    }

    private static string EscapeCsvValue(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Contains(',') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}