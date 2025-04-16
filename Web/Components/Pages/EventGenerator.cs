namespace Web.Components.Pages
{
    public sealed class EventGenerator
    {
        private int _currentEventId = 1;

        public List<TimetableEvent> GenerateHardcodedEvents()
        {
            var now = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day + 4, 6, 0, 0);
            var events = new List<TimetableEvent>
            {
                // Past events
                CreateEvent("Math Class", now.AddDays(-10).AddHours(9), 1),
                CreateEvent("Science Class", now.AddDays(-8).AddHours(14), 2),
                CreateEvent("XXXD Class", now.AddDays(-8).AddHours(14), 2),

                // Present events (closer to current date)
                CreateEvent("History Class", now.AddDays(-1), 2),
                CreateEvent("Art Class", now.AddHours(3), 2),

                // Future events
                CreateEvent("English Class", now.AddDays(1).AddHours(11), 1),
                CreateEvent("Biology Class", now.AddDays(5).Date, 48),
                CreateEvent("Chemistry Class", now.AddDays(8).AddHours(15), 2),

                // Mixed durations, some spanning today
                CreateEvent("Music Class", now.AddDays(3).AddHours(10), 3),
                CreateEvent("Physical Education", now.AddDays(-2).AddHours(17), 1),
                CreateEvent("Philosophy Class", now.AddDays(7).Date, 24),
                CreateEvent("Drama Rehearsal", now.AddHours(-5), 1),
                CreateEvent("Football Practice", now.AddDays(4).AddHours(16), 2),
                CreateEvent("Guitar Lesson", now.AddDays(3).AddHours(19), 1),
                CreateEvent("Yoga Session", now.AddDays(-3).AddHours(1), 1),
                CreateEvent("Poggers Session", now.AddHours(7), 2),
                CreateEvent("Grc Session", now.AddDays(-3).AddHours(7), 2),
                CreateEvent("Hahahah Session", now.AddDays(-3).AddHours(7), 3),
                CreateEvent("Cooking Class", now.AddDays(6).AddHours(11), 2),
                CreateEvent("Photography Workshop", now.AddDays(10).AddHours(9), 3),
                CreateEvent("Dance Class", now.AddDays(12).AddHours(18), 2),

                // Events spanning multiple days
               // CreateEvent("Creative Writing", now.AddDays(-4).AddHours(14), 48),
				//CreateEvent("Computer Science", now.AddDays(15).AddHours(20), 27),
				//CreateEvent("Economics Lecture", now.AddDays(9).AddHours(15), 28)
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
}