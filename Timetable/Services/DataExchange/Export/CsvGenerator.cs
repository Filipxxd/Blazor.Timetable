namespace Timetable.Services.DataExchange.Export;

internal static class CsvGenerator
{
    public const char Separator = ';';

    public static string[][] CreateCsvContent<TEvent>(IEnumerable<TEvent> records, IList<INamePropertySelector<TEvent>> properties)
        where TEvent : class
    {
        var csvContent = new List<string[]>();

        var header = properties.Select(p =>
        {
            if (p.Name.Contains(Separator))
                throw new ArgumentException($"Property name '{p.Name}' cannot contain the separator character '{Separator}'.");
            return p.Name.Trim();
        }).ToArray();

        csvContent.Add(header);

        foreach (var record in records)
        {
            var line = properties.Select(s =>
            {
                var value = s.GetStringValue(record);
                var escapedValue = EscapeCsvValue(value).Trim();
                if (escapedValue.Contains(Separator))
                    throw new InvalidOperationException($"Property value '{escapedValue}' cannot contain the separator character '{Separator}'.");
                return escapedValue;
            }).ToArray();

            csvContent.Add(line);
        }

        return [.. csvContent];
    }

    private static string EscapeCsvValue(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Contains(',') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}

