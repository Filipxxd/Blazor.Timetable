using System.Linq.Expressions;
using Blazor.Timetable.Common.Helpers;

namespace Blazor.Timetable.Services.DataExchange.Export;

public interface IExportSelector<in TEvent>
{
    string Name { get; }
    string GetStringValue(TEvent entity);
}

public sealed class ExportSelector<TEvent, TProperty> : IExportSelector<TEvent>
    where TEvent : class
{
    private readonly Func<TProperty, string> _toStringConverter;
    public string Name { get; init; }
    internal Func<TEvent, TProperty> Getter { get; init; }

    public ExportSelector(string name, Expression<Func<TEvent, TProperty>> selector, Func<TProperty, string>? toStringConverter = null)
    {
        Name = name;
        Getter = PropertyHelper.CreateGetter(selector!)!;
        _toStringConverter = toStringConverter ?? (value => value?.ToString() ?? string.Empty);
    }

    public string GetStringValue(TEvent entity)
    {
        var value = Getter(entity);
        return _toStringConverter(value);
    }
}
