using System.Linq.Expressions;
using System.Reflection;

namespace School_Timetable.Utilities;

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
        if (expression.Body is MemberExpression memberExpression && memberExpression.Member is PropertyInfo property)
        {
            var targetExpression = expression.Parameters[0]; // The parameter representing the object type
            var valueExpression = Expression.Parameter(typeof(TProperty), "value");

            // Ensure that the value is of the same type as the property
            Expression convertedValueExpression = valueExpression;

            // If property requires boxing/unboxing, perform a conversion
            if (property.PropertyType != typeof(TProperty))
            {
                convertedValueExpression = Expression.Convert(valueExpression, property.PropertyType);
            }

            var assignExpression = Expression.Assign(memberExpression, convertedValueExpression);
            var lambda = Expression.Lambda<Action<TObject, TProperty?>>(assignExpression, targetExpression, valueExpression);

            return lambda.Compile();
        }

        throw new ArgumentException("Expression must point to a property", nameof(expression));
    }
}
