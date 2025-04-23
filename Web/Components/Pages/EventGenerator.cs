using System.Collections.ObjectModel;

namespace Web.Components.Pages;

public sealed class EventGenerator
{
    private int _currentEventId = 1;

    public ObservableCollection<TimetableEvent> GenerateHardcodedEvents()
    {
        var now = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day + 4, 6, 0, 0);
        var events = new ObservableCollection<TimetableEvent>
        {
            CreateEvent("Math Class", now.AddDays(-10).AddHours(9), 1),
            CreateEvent("Science Class", now.AddDays(-8).AddHours(14), 2),

            CreateEvent("History Class", now.AddDays(-1).Date, 48),
            CreateEvent("Art Class", now.AddHours(3), 2),

            CreateEvent("English Class", now.AddDays(3).AddHours(11), 25),
            CreateEvent("Biology Class", now.AddDays(5).Date, 72),
            CreateEvent("Chemistry Class", now.AddDays(8).AddHours(15), 2),

            CreateEvent("Football Practice", now.AddDays(1).AddHours(16),152),
            CreateEvent("Football Practice", now.AddDays(1).AddHours(16),72),
            CreateEvent("Guitar Lesson", now.AddDays(-6).AddHours(1),2),
            CreateEvent("Yoga Session", now.AddDays(-3).AddHours(1), 1),
            CreateEvent("Drums Session", now.AddDays(-3).AddHours(4), 1),
            CreateEvent("Other Session", now.AddDays(-3).AddHours(7), 2),
            CreateEvent("Different Session", now.AddDays(-3).AddHours(7), 4),
            CreateEvent("Second Different Session", now.AddDays(-3).AddHours(7), 3),
            new() {
                Id = _currentEventId++,
                Title = "test lesson",
                StartTime = now.AddDays(-6).AddHours(2),
                EndTime = now.AddDays(-6).AddHours(2).AddMinutes(15),
                Description = "Hardcoded event"
            }
        };

        return events;
    }

    private TimetableEvent CreateEvent(string title, DateTime start, int durationHours)
    {
        var ev = new TimetableEvent
        {
            Id = _currentEventId++,
            Title = title,
            StartTime = start,
            EndTime = start.AddHours(durationHours),
            Description = "Hardcoded event"
        };

        return ev;
    }
}