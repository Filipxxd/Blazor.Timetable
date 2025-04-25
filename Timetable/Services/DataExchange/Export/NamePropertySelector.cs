using System.Linq.Expressions;
using Timetable.Common.Helpers;

namespace Timetable.Services.DataExchange.Export;

public interface INamePropertySelector<in TEvent>
{
    string Name { get; }
    string GetStringValue(TEvent entity);
}

internal sealed class NamePropertySelector<TEvent, TProperty> : INamePropertySelector<TEvent>
    where TEvent : class
{
    private readonly Func<TProperty, string> _toStringConverter;
    public string Name { get; init; }
    public Func<TEvent, TProperty> Getter { get; init; }

    public NamePropertySelector(string name, Expression<Func<TEvent, TProperty>> selector, Func<TProperty, string>? toStringConverter = null)
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
