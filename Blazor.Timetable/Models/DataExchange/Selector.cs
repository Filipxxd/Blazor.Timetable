using Blazor.Timetable.Common.Helpers;
using System.Globalization;
using System.Linq.Expressions;

namespace Blazor.Timetable.Models.DataExchange;

public sealed class Selector<TEvent, TProperty> : ISelector<TEvent>
    where TEvent : class
{
    private readonly Func<TEvent, TProperty?> _getter;
    private readonly Action<TEvent, TProperty> _setter;
    private readonly Func<string, TProperty> _parser;
    private readonly Func<TProperty?, string> _toStringConverter;

    public string Name { get; private set; }

    public Selector(string name,
        Expression<Func<TEvent, TProperty?>> selector,
        Func<TProperty?, string>? toStringConverter = null,
        Func<string, TProperty>? parser = null)
    {
        Name = name;
        _getter = PropertyHelper.CreateGetter(selector);
        _setter = PropertyHelper.CreateSetter(selector);
        _toStringConverter = toStringConverter ?? (value => value?.ToString() ?? string.Empty);
        _parser = parser ?? CreateDefaultParser();
    }

    public string GetValue(TEvent entity)
    {
        var value = _getter(entity);
        return _toStringConverter(value);
    }

    public void SetValue(TEvent target, string raw)
    {
        var value = _parser(raw);
        _setter(target, value);
    }

    private static Func<string, TProperty> CreateDefaultParser()
    {
        var targetType = typeof(TProperty);

        if (Nullable.GetUnderlyingType(targetType) is Type underlying)
        {
            return raw =>
            {
                if (string.IsNullOrWhiteSpace(raw))
                    return default!;

                var converted = Convert.ChangeType(raw, underlying, CultureInfo.CurrentCulture);
                return (TProperty)converted!;
            };
        }

        return raw =>
        {
            if (string.IsNullOrWhiteSpace(raw))
                return default!;

            var converted = Convert.ChangeType(raw, targetType, CultureInfo.CurrentCulture);
            return (TProperty)converted!;
        };
    }
}
