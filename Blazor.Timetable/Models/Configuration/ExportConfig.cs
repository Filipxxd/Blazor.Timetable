﻿using Blazor.Timetable.Common.Exceptions;
using Blazor.Timetable.Models.DataExchange;
using Blazor.Timetable.Services.DataExchange.Export;

namespace Blazor.Timetable.Models.Configuration;

public sealed class ExportConfig<TEvent> where TEvent : class
{
    /// <summary>
    /// File name for the export. This is the name of the file that will be created without extension eg. "EventsExport".
    /// </summary>
    public string FileName { get; init; } = "EventExport";

    /// <summary>
    /// Transformer to use for the export. This is the class that will handle the transformation of the data.
    /// </summary>
    public IExportTransformer Transformer { get; init; } = new CsvExportTransformer();

    /// <summary>
    /// Properties to export. This is a list of properties that will be included in the export.
    /// </summary>
	public required IList<ISelector<TEvent>> Selectors { get; init; } = [];

    internal void Validate()
    {
        if (!Selectors.Any())
            throw new InvalidSetupException("At least one property selector must be provided.");

        if (string.IsNullOrWhiteSpace(FileName))
            throw new InvalidSetupException("FileName must be provided.");

        var invalidChars = Path.GetInvalidFileNameChars();
        if (FileName.IndexOfAny(invalidChars) >= 0)
            throw new InvalidSetupException("FileName contains invalid characters.");

        var reservedFileNames = new List<string> { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4",
            "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(FileName).ToUpperInvariant();
        if (reservedFileNames.Contains(fileNameWithoutExtension))
            throw new InvalidSetupException("FileName cannot be a reserved name.");

        const int maxFileNameLength = 255;
        if (FileName.Length > maxFileNameLength)
            throw new InvalidSetupException("FileName is too long.");

        var duplicateNames = Selectors
            .GroupBy(selector => selector.Name)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateNames.Count != 0)
            throw new InvalidSetupException($"Duplicate selector names found: {string.Join(", ", duplicateNames)}");
    }
}