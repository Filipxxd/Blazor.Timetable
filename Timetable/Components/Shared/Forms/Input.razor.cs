namespace Timetable.Components.Shared.Forms;

public partial class Input<TEvent, TType> : BaseInput<TEvent, TType>
{
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