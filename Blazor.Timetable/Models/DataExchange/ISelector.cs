namespace Blazor.Timetable.Models.DataExchange;

public interface ISelector<TEvent> where TEvent : class
{
    string Name { get; }
    void SetValue(TEvent entity, string raw);
    string GetValue(TEvent entity);
}
