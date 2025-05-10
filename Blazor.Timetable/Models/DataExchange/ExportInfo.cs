using Microsoft.JSInterop;

namespace Blazor.Timetable.Models.DataExchange;

internal sealed class ExportInfo : IExportInfo
{
    public required DotNetStreamReference StreamReference { get; init; }
    public required string FileExtension { get; init; }
}
