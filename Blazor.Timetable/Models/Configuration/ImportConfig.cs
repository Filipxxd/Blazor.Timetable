using Blazor.Timetable.Common.Exceptions;
using Blazor.Timetable.Models.DataExchange;
using Blazor.Timetable.Services.DataExchange.Import;

namespace Blazor.Timetable.Models.Configuration;

public sealed class ImportConfig<TEvent>
  where TEvent : class
{
    /// <summary>e.g. new[]{ "csv" }</summary>
    public string[] AllowedExtensions { get; init; } = ["csv"];

    /// <summary>in bytes; default e.g. 10MB</summary>
    public long MaxFileSizeBytes { get; init; } = 10_485_760;

    /// <summary>Implement this interface to parse a Stream into a sequence of TEvent.</summary>
    public IImportTransformer<TEvent> Transformer { get; init; } = new CsvImportTransformer<TEvent>();

    /// <summary>
    /// The list of selectors to map the CSV columns to the properties of TEvent.
    /// </summary>
    public required IList<ISelector<TEvent>> Selectors { get; init; }

    internal void Validate()
    {
        if (!Selectors.Any())
            throw new InvalidSetupException("At least one property selector must be provided.");

        if (Transformer is null)
            throw new InvalidSetupException("Transformer must be provided.");

        if (MaxFileSizeBytes < 10_485_760)
            throw new InvalidSetupException("Max Size in bytes must be at least 10MB.");
    }
}
