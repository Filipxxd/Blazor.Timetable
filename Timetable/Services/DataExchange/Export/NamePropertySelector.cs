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
    public string Name { get; init; }
    public Func<TEvent, TProperty> Getter { get; init; }

    public NamePropertySelector(string name, Expression<Func<TEvent, TProperty>> selector)
    {
        Name = name;
        Getter = PropertyHelper.CreateGetter(selector!)!;
    }

    public string GetStringValue(TEvent entity)
    {
        var value = Getter(entity);

        return value?.ToString() ?? string.Empty;
    }
}
