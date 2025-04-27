using System.Linq.Expressions;

namespace Timetable.Models;

public sealed class EventProperty<TEvent> where TEvent : class
{
    public Expression<Func<TEvent, object?>> Selector { get; }

    public EventProperty(Expression<Func<TEvent, object?>> selector)
    {
        Selector = selector;
    }
}
