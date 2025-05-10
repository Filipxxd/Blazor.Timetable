using Blazor.Timetable.Models.DataExchange;
using Microsoft.JSInterop;
using System.Text;

namespace Blazor.Timetable.Services.DataExchange.Export;

internal sealed class CsvTransformer : IExportTransformer
{
    public IExportInfo Transform<TEvent>(IEnumerable<TEvent> records, IList<ISelector<TEvent>> properties)
        where TEvent : class
    {
        var csvContent = CsvGenerator.CreateCsvContent(records, properties);

        var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, Encoding.UTF8, 1024, true);

        foreach (var line in csvContent)
        {
            writer.WriteLine(string.Join(CsvGenerator.Separator, line));
        }

        writer.Flush();
        memoryStream.Position = 0;

        return new ExportInfo
        {
            StreamReference = new DotNetStreamReference(memoryStream, true),
            FileExtension = "csv"
        };
    }
}