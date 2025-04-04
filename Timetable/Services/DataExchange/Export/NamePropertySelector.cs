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
    public Expression<Func<TEvent, TProperty>> Selector { get; init; }

    public NamePropertySelector(string name, Expression<Func<TEvent, TProperty>> selector)
    {
        Name = name;
        Selector = selector;
    }

    public string GetStringValue(TEvent entity)
    {
        var compiledSelector = Selector.Compile();
        var value = compiledSelector(entity);

        return value?.ToString() ?? string.Empty;
    }
}
