using System.Linq.Expressions;
using System.Reflection;

namespace Timetable.Common.Utilities;

internal static class PropertyHelper
{
    public static Func<TObject, TProperty?> CreateGetter<TObject, TProperty>(Expression<Func<TObject, TProperty>> expression)
    {
        if (expression.Body is not MemberExpression 
            && expression.Body is not UnaryExpression { Operand: MemberExpression })
            throw new ArgumentException("Expression must point to a property", nameof(expression));
        
        var compiledExpression = expression.Compile();
            
        return obj => Equals(obj, default(TObject)) ? default : compiledExpression(obj);
    }

    public static Action<TObject, TProperty?> CreateSetter<TObject, TProperty>(Expression<Func<TObject, TProperty?>> expression) where TObject : class
    {
        if (expression.Body is not MemberExpression { Member: PropertyInfo property } memberExpression)
            throw new ArgumentException("Expression must point to a property", nameof(expression));
        
        var targetExpression = expression.Parameters[0];
        var valueExpression = Expression.Parameter(typeof(TProperty), "value");
            
        Expression convertedValueExpression = valueExpression;
            
        if (property.PropertyType != typeof(TProperty))
        {
            convertedValueExpression = Expression.Convert(valueExpression, property.PropertyType);
        }

        var assignExpression = Expression.Assign(memberExpression, convertedValueExpression);
        var lambda = Expression.Lambda<Action<TObject, TProperty?>>(assignExpression, targetExpression, valueExpression);

        return lambda.Compile();
    }
}
