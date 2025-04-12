using System.Linq.Expressions;

namespace Timetable.Services.DataExchange.Export;

public interface INamePropertySelector<in TEvent>
{
    string Name { get; }
    string GetStringValue(TEvent entity);
}

public class NamePropertySelector<TEvent, TProperty> : INamePropertySelector<TEvent>
    where TEvent : class
{
    public string Name { get; init; }
    internal Func<TEvent, TProperty> Getter { get; init; }

    public NamePropertySelector(string name, Expression<Func<TEvent, TProperty>> selector)
    {
        Name = name;
        Getter = selector.Compile();
    }

    public string GetStringValue(TEvent entity)
    {
        var value = Getter(entity);

        return value?.ToString() ?? string.Empty;
    }
}
