using System.Linq.Expressions;
using System.Reflection;

namespace Blazor.Timetable.Common.Helpers;

internal static class PropertyHelper
{
    public static Func<TObject, TProperty?> CreateGetter<TObject, TProperty>
        (Expression<Func<TObject, TProperty?>> expression) where TObject : class
    {
        if (expression.Body is not MemberExpression
            && expression.Body is not UnaryExpression { Operand: MemberExpression })
            throw new ArgumentException(
                "Expression must point to a property", nameof(expression));

        var compiledExpression = expression.Compile();

        return obj => Equals(obj, default(TObject))
            ? default
            : compiledExpression(obj);
    }

    public static Action<TObject, TProperty?> CreateSetter<TObject, TProperty>
        (Expression<Func<TObject, TProperty?>> expression) where TObject : class
    {
        var memberExpr = expression.Body switch
        {
            MemberExpression me => me,
            UnaryExpression ue when ue.Operand is MemberExpression me => me,
            _ => throw new ArgumentException(
                "Expression must point to a property", nameof(expression)),
        };

        if (memberExpr.Member is not PropertyInfo propertyInfo)
            throw new ArgumentException(
                "Member is not a property", nameof(expression));

        var parameter = expression.Parameters[0];
        var valueParameter = Expression.Parameter(typeof(TProperty), "value");

        var valueToAssign = valueParameter as Expression;

        if (propertyInfo.PropertyType != typeof(TProperty))
            valueToAssign = Expression.Convert(
                valueParameter, propertyInfo.PropertyType);

        var member = Expression.MakeMemberAccess(parameter, propertyInfo);
        var assign = Expression.Assign(member, valueToAssign);
        var lambda = Expression.Lambda<Action<TObject, TProperty?>>(
            assign, parameter, valueParameter);

        return lambda.Compile();
    }
}
