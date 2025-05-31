namespace Web.Components.Pages;

public sealed class EventGenerator
{
    private int _id = 1;
    public List<Subject> GenerateHardcodedEvents()
    {
        var now = DateTime.Now.Date.AddDays(1);

        var events = new List<Subject>
        {
            CreateEvent("Math Class", now.AddDays(-5).AddHours(9), 1.5),
            CreateEvent("English Class", now.AddDays(-4).AddHours(10), 1),
            CreateEvent("Science Class", now.AddDays(-3).AddHours(11), 2),
            CreateEvent("History Class", now.AddDays(-2).AddHours(13), 1),
            CreateEvent("Art Class", now.AddDays(-1).AddHours(14), 1.5),

            CreateEvent("Biology Class", now.AddHours(8), 2),
            CreateEvent("Chemistry Class", now.AddHours(11), untilEndOfDay: true),
            CreateEvent("Music Class", now.AddDays(1).AddHours(9), 1),
            CreateEvent("Physical Education", now.AddDays(2).AddHours(10), 2),
            CreateEvent("Drama Class", now.AddDays(3).AddHours(12), 1.5)
        };

        // preformance test
        //var i = 0;
        //while (i < 10_000)
        //{
        //    events.Add(CreateEvent("test lesson", now.AddDays(-6).AddHours(2), 1));
        //    i++;
        //}

        return events;
    }

    private Subject CreateEvent(string title, DateTime start, double? durationHours = null, bool untilEndOfDay = false)
    {
        var end = untilEndOfDay
            ? new DateTime(start.Year, start.Month, start.Day, 23, 59, 59)
            : start.AddHours(durationHours ?? 1);

        var ev = new Subject
        {
            Id = _id++,
            Name = title,
            StartTime = start,
            EndTime = end,
            Description = "Scheduled school class"
        };
        return ev;
    }
}