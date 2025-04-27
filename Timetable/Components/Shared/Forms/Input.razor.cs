using Microsoft.AspNetCore.Components;
using System.Linq.Expressions;
using System.Reflection;
using Timetable.Components.Shared.Modals;
using Timetable.Models;

namespace Timetable.Components.Shared.Forms;

public partial class Input<TEvent, TType> : BaseInput<TEvent, TType>
{
    [CascadingParameter] public UpdateEventModal<TEvent>? ParentModal { get; set; }

    protected override void OnInitialized()
    {
        // If ParentModal is available via cascading, register the field info.
        if (ParentModal != null && Selector.Body is MemberExpression memberExp && memberExp.Member is PropertyInfo prop)
        {
            var fieldInfo = new AdditionalFieldInfo<TEvent>
            {
                PropertyName = prop.Name,
                Selector = Expression.Lambda<Func<TEvent, object?>>(
                    Expression.Convert(memberExp, typeof(object)), Selector.Parameters)
            };
            ParentModal.AdditionalFieldInfos.Add(fieldInfo);
        }
    }
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        // If ParentModal is available via cascading, register the field info.
        if (ParentModal != null && Selector.Body is MemberExpression memberExp && memberExp.Member is PropertyInfo prop)
        {
            var fieldInfo = new AdditionalFieldInfo<TEvent>
            {
                PropertyName = prop.Name,
                Selector = Expression.Lambda<Func<TEvent, object?>>(
                    Expression.Convert(memberExp, typeof(object)), Selector.Parameters)
            };
            ParentModal.AdditionalFieldInfos.Add(fieldInfo);
        }
    }

    private static string GetInputType()
    {
        var numberTypes = new[] { typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal) };
        if (Array.Exists(numberTypes, type => type == typeof(TType)))
            return "number";

        return typeof(TType) switch
        {
            Type type when type == typeof(string) => "text",
            Type type when type == typeof(bool) => "checkbox",
            _ => "text"
        };
    }
}