using Timetable.Exceptions;
using Timetable.Services.DataExchange.Export;

namespace Timetable.Configuration;

public sealed class ExportConfig<TEvent> where TEvent : class
{
    public required string FileName { get; init; }
    public required ITransformer Transformer { get; init; }
    public IList<INamePropertySelector<TEvent>> Properties { get; init; } = [];

    internal void Validate()
    {
        if (!Properties.Any())
            throw new InvalidSetupException("At least one property must be provided.");

        if (string.IsNullOrWhiteSpace(FileName))
            throw new InvalidSetupException("FileName must be provided.");
    }
}