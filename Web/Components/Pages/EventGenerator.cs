namespace Web.Components.Pages;

public class EventGenerator
{
    private static readonly DateTime TodayMidnight = DateTime.Today;
    private readonly Random _random = new Random();
    private int _currentEventId = 10;
    private readonly List<string> _titles =
    [
        "Math Class", "Science Class", "History Class", "Art Class", "English Class",
        "Biology Class", "Chemistry Class", "Music Class", "Physical Education", "Philosophy Class"
    ];

    private readonly List<string> _groupIds = [];
    
    public List<TimetableEvent> GenerateEvents(int numberOfEvents)
    {
        var events = new List<TimetableEvent>();
        const int daysOffsetMax = 365;

        while (numberOfEvents > 0)
        {
            var eventsInSet = Math.Min(numberOfEvents, _random.Next(1, 4));

            for (var i = 0; i < eventsInSet; i++)
            {
                var start = TodayMidnight.AddDays(_random.Next(-daysOffsetMax, daysOffsetMax))
                    .AddHours(_random.Next(0, 24));
                var durationOptions = new[] { 1, 2, 3, 8, 12, 24 };
                var duration = durationOptions[_random.Next(durationOptions.Length)];
                var eventGroupId = GenerateOrGetGroupId();

                var ev = new TimetableEvent
                {
                    Id = _currentEventId++,
                    Title = _titles[_random.Next(_titles.Count)],
                    StartTime = start,
                    EndTime = start.AddHours(duration),
                    Description = "Randomly generated event",
                    GroupId = eventGroupId
                };

                events.Add(ev);
            }

            numberOfEvents -= eventsInSet;
        }

        return events;
    }
    
    private string? GenerateOrGetGroupId()
    {
        if (_random.Next(0, 10) >= 3) return null;
        
        if (_groupIds.Count > 0 && _random.Next(0, 10) < 5)
        {
            return _groupIds[_random.Next(_groupIds.Count)];
        }
        
        var newGroupId = Guid.NewGuid().ToString();
        _groupIds.Add(newGroupId);
        return newGroupId;
    }
}