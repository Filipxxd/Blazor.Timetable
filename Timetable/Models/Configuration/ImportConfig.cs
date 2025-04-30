using Timetable.Services.DataExchange.Import;

namespace Timetable.Models.Configuration;

public sealed class ImportConfig<TEvent>
  where TEvent : class
{
    /// <summary>e.g. new[]{ "csv" }</summary>
    public string[] AllowedExtensions { get; init; } = [];
    /// <summary>in bytes; default e.g. 10MB</summary>
    public long MaxFileSizeBytes { get; init; } = 10_485_760;
    /// <summary>Implement this interface to parse a Stream into a sequence of TEvent.</summary>
    public required IImportTransformer<TEvent> Transformer { get; init; }
}
