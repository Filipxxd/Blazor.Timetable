using Timetable.Common.Exceptions;
using Timetable.Services.DataExchange.Export;

namespace Timetable.Configuration;

public sealed class ExportConfig<TEvent> where TEvent : class
{
    /// <summary>
    /// File name for the export. This is the name of the file that will be created.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Transformer to use for the export. This is the class that will handle the transformation of the data.
    /// </summary>
    public required ITransformer Transformer { get; init; }

    /// <summary>
    /// Properties to export. This is a list of properties that will be included in the export.
    /// </summary>
	public IList<INamePropertySelector<TEvent>> Properties { get; init; } = [];

    internal void Validate()
    {
        if (!Properties.Any())
            throw new InvalidSetupException("At least one property must be provided.");

        if (string.IsNullOrWhiteSpace(FileName))
            throw new InvalidSetupException("FileName must be provided.");

        // TODO: Validate filename
    }
}