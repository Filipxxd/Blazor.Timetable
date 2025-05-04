using Microsoft.JSInterop;

namespace Blazor.Timetable.Services.DataExchange.Export;

public interface IExportInfo
{
    DotNetStreamReference StreamReference { get; init; }
    string FileExtension { get; init; }
}

internal sealed class ExportInfo : IExportInfo
{
    public required DotNetStreamReference StreamReference { get; init; }
    public required string FileExtension { get; init; }
}
