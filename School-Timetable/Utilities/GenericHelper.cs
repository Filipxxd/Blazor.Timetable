using System.Reflection;

namespace School_Timetable.Utilities;

internal static class GenericHelper<T> where T : class
{
    public static TType Get<TType>(T item, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(item);

        var property = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property is null)
            throw new InvalidOperationException($"Property '{propertyName}' not found on type '{typeof(T).Name}'.");

        var value = property.GetValue(item);

        if (value is null && !default(TType)!.Equals(value))
            throw new InvalidOperationException($"Property '{propertyName}' is expected to be non-null but is null.");

        // if (value is not TType typedValue)
        //     return typedValue.ToString();
        //     //throw new InvalidCastException($"Property '{propertyName}' is not of type '{typeof(TType).Name}'.");
        //
        
        if (typeof(TType) == typeof(string))
        {
            return (TType)(object)value.ToString()!;
        }
            
        return (TType)value;
    }
}