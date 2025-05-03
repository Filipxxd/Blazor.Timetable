using System.Globalization;
using System.Linq.Expressions;
using Timetable.Common.Helpers;

namespace Timetable.Services.DataExchange.Import;

public interface IImportSelector<TEvent>
    where TEvent : class
{
    string Name { get; }
    void SetValue(TEvent target, string raw);
}

public sealed class ImportSelector<TEvent, TProperty> : IImportSelector<TEvent>
        where TEvent : class
{
    private readonly Action<TEvent, TProperty> _setter;
    private readonly Func<string, TProperty> _parser;
    public string Name { get; init; }

    public ImportSelector(string name, Expression<Func<TEvent, TProperty?>> selector, Func<string, TProperty>? parser = null)
    {
        Name = name;
        _setter = PropertyHelper.CreateSetter(selector);
        _parser = parser ?? ImportSelector<TEvent, TProperty>.CreateDefaultParser();
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
        else
        {
            return raw =>
            {
                if (string.IsNullOrWhiteSpace(raw))
                    return default!;

                var converted = Convert.ChangeType(raw, targetType, CultureInfo.CurrentCulture);
                return (TProperty)converted!;
            };
        }
    }
}
