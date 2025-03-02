using System.Reflection;
using System.Text;

namespace School_Timetable.Services.Export;

internal sealed class CsvExportService : IExportService
{
    public MemoryStream Export<T>(IEnumerable<T> records)
    {
        var memoryStream = new MemoryStream();

        using var writer = new StreamWriter(memoryStream, Encoding.UTF8, 1024, true);
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        var headerLine = string.Join(',', properties.Select(p => p.Name));
        writer.WriteLine(headerLine);
        
        foreach (var record in records)
        {
            var line = string.Join(',', properties.Select(p => EscapeCsvValue(p.GetValue(record, null)?.ToString())));
            writer.WriteLine(line);
        }

        memoryStream.Position = 0;
        return memoryStream;
    }
    
    private static string EscapeCsvValue(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        return value.Contains(',') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}