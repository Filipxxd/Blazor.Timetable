using Blazor.Timetable.Common.Helpers;
using System.Linq.Expressions;

namespace Blazor.Timetable.Models;

public sealed class PropertyAccessors<TEvent> where TEvent : class
{
    internal Func<TEvent, DateTime> GetDateFrom { get; }
    internal Action<TEvent, DateTime> SetDateFrom { get; }
    internal Func<TEvent, DateTime> GetDateTo { get; }
    internal Action<TEvent, DateTime> SetDateTo { get; }
    internal Func<TEvent, string> GetTitle { get; }
    internal Action<TEvent, string> SetTitle { get; }
    internal Func<TEvent, string?> GetGroupId { get; }
    internal Action<TEvent, string?> SetGroupId { get; }

    internal IList<(Func<TEvent, object?> Getter, Action<TEvent, object?> Setter)> AdditionalProperties { get; } = [];


    internal PropertyAccessors(Expression<Func<TEvent, DateTime>> dateFromSelector,
        Expression<Func<TEvent, DateTime>> dateToSelector, Expression<Func<TEvent, string>> titleSelector,
        Expression<Func<TEvent, string?>> groupIdSelector, IEnumerable<Expression<Func<TEvent, object?>>> additionalProps)
    {
        GetTitle = PropertyHelper.CreateGetter(titleSelector!)!;
        SetTitle = PropertyHelper.CreateSetter(titleSelector!);
        GetDateFrom = PropertyHelper.CreateGetter(dateFromSelector);
        SetDateFrom = PropertyHelper.CreateSetter(dateFromSelector);
        GetDateTo = PropertyHelper.CreateGetter(dateToSelector);
        SetDateTo = PropertyHelper.CreateSetter(dateToSelector);
        GetGroupId = PropertyHelper.CreateGetter(groupIdSelector);
        SetGroupId = PropertyHelper.CreateSetter(groupIdSelector);

        foreach (var expression in additionalProps)
        {
            var getter = PropertyHelper.CreateGetter(expression);
            var setter = PropertyHelper.CreateSetter(expression);

            AdditionalProperties.Add((getter, setter));
        }
    }
}