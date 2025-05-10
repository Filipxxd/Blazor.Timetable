using Microsoft.JSInterop;

namespace Blazor.Timetable.Models.DataExchange;

/// <summary>
/// Represents the data stream for export. StreamReference must be disposed after use.
/// </summary>
public interface IExportInfo
{
    DotNetStreamReference StreamReference { get; init; }
    string FileExtension { get; init; }
}
