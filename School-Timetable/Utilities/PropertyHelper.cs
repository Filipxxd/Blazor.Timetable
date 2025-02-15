using System.Linq.Expressions;
using System.Reflection;

namespace School_Timetable.Utilities;

internal static class PropertyHelper
{
    public static Func<TObject, TProperty> Get<TObject, TProperty>(Expression<Func<TObject, TProperty>> expression)
    {
        if (expression.Body is MemberExpression { Member: PropertyInfo })
            return expression.Compile();
        
        throw new ArgumentException("Expression must point to a property", nameof(expression));
    }

    public static Action<TObject, TProperty> Set<TObject, TProperty>(Expression<Func<TObject, TProperty>> expression)
    {
        if (expression.Body is not MemberExpression { Member: PropertyInfo } member)
            throw new ArgumentException("Expression must point to a property", nameof(expression));

        var paramExpression = Expression.Parameter(typeof(TProperty), "value");
        var assignExpression = Expression.Assign(member, paramExpression);
        var lambda = Expression.Lambda<Action<TObject, TProperty>>(assignExpression, expression.Parameters[0], paramExpression);
        return lambda.Compile();
    }
}
