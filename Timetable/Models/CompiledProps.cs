using System.Linq.Expressions;
using Timetable.Common.Helpers;

namespace Timetable.Models;

public sealed class CompiledProps<TEvent> where TEvent : class
{
    public Func<TEvent, DateTime> GetDateFrom { get; }
    public Action<TEvent, DateTime> SetDateFrom { get; }
    public Func<TEvent, DateTime> GetDateTo { get; }
    public Action<TEvent, DateTime> SetDateTo { get; }
    public Func<TEvent, string> GetTitle { get; }
    public Action<TEvent, string> SetTitle { get; }
    public Func<TEvent, object?> GetGroupId { get; }
    public Action<TEvent, object?> SetGroupId { get; }

    public CompiledProps(Expression<Func<TEvent, DateTime>> dateFromSelector,
        Expression<Func<TEvent, DateTime>> dateToSelector, Expression<Func<TEvent, string>> titleSelector,
        Expression<Func<TEvent, object?>> groupIdSelector)
    {
        GetTitle = PropertyHelper.CreateGetter(titleSelector!)!;
        SetTitle = PropertyHelper.CreateSetter(titleSelector!);
        GetDateFrom = PropertyHelper.CreateGetter(dateFromSelector);
        SetDateFrom = PropertyHelper.CreateSetter(dateFromSelector);
        GetDateTo = PropertyHelper.CreateGetter(dateToSelector);
        SetDateTo = PropertyHelper.CreateSetter(dateToSelector);
        GetGroupId = PropertyHelper.CreateGetter(groupIdSelector);
        SetGroupId = PropertyHelper.CreateSetter(groupIdSelector);
    }
}