using Microsoft.JSInterop;

namespace Timetable.Services.DataExchange.Export;

public sealed class ExportInfo
{
    public required DotNetStreamReference StreamReference { get; init; }
    public required string FileExtension { get; init; }
}
