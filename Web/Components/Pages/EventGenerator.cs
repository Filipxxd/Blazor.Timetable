using System;
using System.Collections.Generic;

namespace Web.Components.Pages
{
    public class EventGenerator
    {
        // For reproducible behavior, use a constant seed.
        private readonly Random _random = new Random(42);
        private int _currentEventId = 10;

        private readonly List<string> _titles = new List<string>
        {
            "Math Class", "Science Class", "History Class", "Art Class", "English Class",
            "Biology Class", "Chemistry Class", "Music Class", "Physical Education", "Philosophy Class"
        };

        private readonly List<string> _groupIds = [];
        
        private static DateTime GetWeekStart(DateTime dt)
        {
            int diff = dt.DayOfWeek - DayOfWeek.Monday;
            if (diff < 0) diff += 7;
            return dt.Date.AddDays(-diff);
        }

        private static readonly DateTime WeekStart = GetWeekStart(DateTime.Now);

        public List<TimetableEvent> GenerateEvents(int numberOfEvents)
        {
            var events = new List<TimetableEvent>();
            
            while (numberOfEvents > 0)
            {
                var eventsInSet = Math.Min(numberOfEvents, _random.Next(1, 4));

                for (var i = 0; i < eventsInSet; i++)
                {
                    var dayOffset = _random.Next(0, 7);

                    DateTime start;
                    int durationHours;
                    
                    var typeIndicator = _currentEventId % 5;
                    switch (typeIndicator)
                    {
                        case 0:
                            start = WeekStart.AddDays(dayOffset).Date;
                            durationHours = 24;
                            break;
                        case 1:
                            durationHours = 1;
                            start = WeekStart.AddDays(dayOffset).Date.AddHours(8 + _random.Next(0, 4));
                            break;
                        case 2:
                            durationHours = 2;
                            start = WeekStart.AddDays(dayOffset).Date.AddHours(9 + _random.Next(0, 4));
                            break;
                        case 3:
                            durationHours = 3;
                            start = WeekStart.AddDays(dayOffset).Date.AddHours(10 + _random.Next(0, 4));
                            break;
                        case 4:
                            durationHours = 3;
                            start = WeekStart.AddDays(dayOffset).Date.AddHours(23);
                            break;
                        default:
                            durationHours = 1;
                            start = WeekStart.AddDays(dayOffset).Date.AddHours(8);
                            break;
                    }
                    
                    var ev = new TimetableEvent
                    {
                        Id = _currentEventId++,
                        Title = _titles[_random.Next(_titles.Count)],
                        StartTime = start,
                        EndTime = start.AddHours(durationHours),
                        Description = "Randomly generated event",
                        GroupId = GenerateOrGetGroupId()
                    };

                    if (durationHours % 24 == 0)
                        ev.EndTime = ev.EndTime.AddMinutes(-1);
                    
                    events.Add(ev);
                }

                numberOfEvents -= eventsInSet;
            }

            return events;
        }

        private string? GenerateOrGetGroupId()
        {
            if (_random.Next(0, 10) >= 3)
                return null;
            
            if (_groupIds.Count > 0 && _random.Next(0, 10) < 5)
            {
                return _groupIds[_random.Next(_groupIds.Count)];
            }
            
            var newGroupId = Guid.NewGuid().ToString();
            _groupIds.Add(newGroupId);
            return newGroupId;
        }
    }
}