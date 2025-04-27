using System.Linq.Expressions;
using Timetable.Common.Helpers;

namespace Timetable.Models;

public sealed class CompiledProps<TEvent> where TEvent : class
{
    internal Func<TEvent, DateTime> GetDateFrom { get; }
    internal Action<TEvent, DateTime> SetDateFrom { get; }
    internal Func<TEvent, DateTime> GetDateTo { get; }
    internal Action<TEvent, DateTime> SetDateTo { get; }
    internal Func<TEvent, string> GetTitle { get; }
    internal Action<TEvent, string> SetTitle { get; }
    internal Func<TEvent, object?> GetGroupId { get; }
    internal Action<TEvent, object?> SetGroupId { get; }

    internal IList<(Func<TEvent, object?> Getter, Action<TEvent, object?> Setter)> AdditionalProperties { get; } = [];


    internal CompiledProps(Expression<Func<TEvent, DateTime>> dateFromSelector,
        Expression<Func<TEvent, DateTime>> dateToSelector, Expression<Func<TEvent, string>> titleSelector,
        Expression<Func<TEvent, object?>> groupIdSelector, IEnumerable<EventProperty<TEvent>> additionalProps)
    {
        GetTitle = PropertyHelper.CreateGetter(titleSelector!)!;
        SetTitle = PropertyHelper.CreateSetter(titleSelector!);
        GetDateFrom = PropertyHelper.CreateGetter(dateFromSelector);
        SetDateFrom = PropertyHelper.CreateSetter(dateFromSelector);
        GetDateTo = PropertyHelper.CreateGetter(dateToSelector);
        SetDateTo = PropertyHelper.CreateSetter(dateToSelector);
        GetGroupId = PropertyHelper.CreateGetter(groupIdSelector);
        SetGroupId = PropertyHelper.CreateSetter(groupIdSelector);

        foreach (var expr in additionalProps)
        {
            var getter = PropertyHelper.CreateGetter(expr.Selector);
            var setter = PropertyHelper.CreateSetter(expr.Selector);

            AdditionalProperties.Add((getter, setter));
        }
    }
}