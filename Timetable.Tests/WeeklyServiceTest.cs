using Timetable.Configuration;
using Timetable.Services.Display;
using Timetable.Structure;

namespace Timetable.Tests;

public sealed class WeeklyServiceTests
{
    private readonly WeeklyService _weeklyService = new();

    private class TestEvent
    {
        public DateTime StartTime { get; init; }
        public DateTime EndTime { get; init; }
        public string Title { get; init; } = default!;
    }

    private readonly EventProps<TestEvent> _props =
        new(e => e.StartTime, e => e.EndTime, e => e.Title);

    [Fact]
    public void CreateGrid_ShouldReturnGridWithCorrectHeaderRowCount()
    {
        var mockConfig = new TimetableConfig
        {
            CurrentDate = new DateTime(2023, 10, 30),
            Days = [DayOfWeek.Monday, DayOfWeek.Tuesday]
        };

        var result = _weeklyService.CreateGrid([], mockConfig, _props);

        Assert.Equal(mockConfig.Days.Count(), result.HeaderRow.Cells.Count);
    }

    [Fact]
    public void CreateGrid_ShouldReturnGridWithCorrectRowCount()
    {
        var mockConfig = new TimetableConfig
        {
            CurrentDate = new DateTime(2023, 10, 30),
            Days = [DayOfWeek.Monday, DayOfWeek.Tuesday],
            TimeFrom = new TimeOnly(9, 0),
            TimeTo = new TimeOnly(17, 0)
        };

        var result = _weeklyService.CreateGrid([], mockConfig, _props);

        Assert.Equal(mockConfig.TimeTo.Hour - mockConfig.TimeFrom.Hour + 1, result.Rows.Count);
    }

    [Fact]
    public void CreateGrid_ShouldReturnGridWithCorrectCellCountInRows()
    {
        var mockConfig = new TimetableConfig
        {
            CurrentDate = new DateTime(2023, 10, 30),
            Days = [DayOfWeek.Monday, DayOfWeek.Tuesday],
            TimeFrom = new TimeOnly(9, 0),
            TimeTo = new TimeOnly(17, 0)
        };

        var result = _weeklyService.CreateGrid([], mockConfig, _props);

        foreach (var row in result.Rows)
        {
            Assert.Equal(mockConfig.Days.Count(), row.Cells.Count);
        }
    }

    [Fact]
    public void CreateGrid_ShouldOnlyIncludeEventsWithinWeekTimeframe()
    {
        var mockConfig = new TimetableConfig
        {
            CurrentDate = new DateTime(2023, 10, 30),
            Days = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday },
            TimeFrom = new TimeOnly(9, 0),
            TimeTo = new TimeOnly(17, 0)
        };

        var events = new List<TestEvent>
        {
            new() { StartTime = new DateTime(2023, 10, 30, 10, 0, 0), EndTime = new DateTime(2023, 10, 30, 11, 0, 0) },
            new() { StartTime = new DateTime(2023, 11, 1, 10, 0, 0), EndTime = new DateTime(2023, 11, 1, 11, 0, 0) }
        };

        var result = _weeklyService.CreateGrid(events, mockConfig, _props);

        var includedEvents = result.Rows.SelectMany(row => row.Cells.SelectMany(cell => cell.Events)).ToList();
        Assert.Single(includedEvents);
        Assert.Equal(events[0], includedEvents[0].Event);
    }

    [Fact]
    public void CreateGrid_WithEmptyConfiguration_ShouldThrow()
    {
        var mockConfig = new TimetableConfig
        {
            CurrentDate = DateTime.Now,
            Days = [],
            TimeFrom = new TimeOnly(0, 0),
            TimeTo = new TimeOnly(0, 0)
        };

        Assert.Throws<InvalidOperationException>(() => _weeklyService.CreateGrid(new List<TestEvent>(), mockConfig, _props));
    }

    [Fact]
    public void CreateGrid_WithEventsOutsideConfiguredTime_ShouldBeInHeaderRow()
    {
        var mockConfig = new TimetableConfig
        {
            CurrentDate = new DateTime(2023, 10, 30),
            Days = [DayOfWeek.Monday, DayOfWeek.Tuesday],
            TimeFrom = new TimeOnly(9, 0),
            TimeTo = new TimeOnly(17, 0)
        };

        var events = new List<TestEvent>
        {
            new() {
                StartTime = new DateTime(2023, 10, 30, 8, 0, 0), EndTime = new DateTime(2023, 10, 30, 9, 0, 0)
            },
            new() {
                StartTime = new DateTime(2023, 10, 30, 18, 0, 0), EndTime = new DateTime(2023, 10, 30, 19, 0, 0)
            }
        };

        var result = _weeklyService.CreateGrid(events, mockConfig, _props);
        var header = result.HeaderRow;
        var actual = header.Cells.SelectMany(x => x.Events.Select(x => x.Event));

        Assert.Equal(events, actual);
    }

    [Fact]
    public void CreateGrid_WithWholeDayEvents_ShouldBeInHeaderRow()
    {
        var mockConfig = new TimetableConfig
        {
            CurrentDate = new DateTime(2023, 10, 30),
            Days = [DayOfWeek.Monday, DayOfWeek.Tuesday],
            TimeFrom = new TimeOnly(9, 0),
            TimeTo = new TimeOnly(17, 0)
        };

        var events = new List<TestEvent>
        {
            new() {
                StartTime = new DateTime(2023, 10, 30, 0, 0, 0), EndTime = new DateTime(2023, 10, 30, 23, 59, 59)
            }
        };

        var result = _weeklyService.CreateGrid(events, mockConfig, _props);
        var header = result.HeaderRow;
        var actual = header.Cells.SelectMany(x => x.Events.Select(x => x.Event));

        Assert.Equal(events, actual);
    }

    [Fact]
    public void CreateGrid_WithOverlappingEvents_ShouldIncludeAllEvents()
    {
        var mockConfig = new TimetableConfig
        {
            CurrentDate = new DateTime(2023, 10, 30),
            Days = [DayOfWeek.Monday],
            TimeFrom = new TimeOnly(9, 0),
            TimeTo = new TimeOnly(17, 0)
        };

        var events = new List<TestEvent>
        {
            new()
                { StartTime = new DateTime(2023, 10, 30, 10, 0, 0), EndTime = new DateTime(2023, 10, 30, 12, 0, 0) },
            new()
                { StartTime = new DateTime(2023, 10, 30, 11, 0, 0), EndTime = new DateTime(2023, 10, 30, 13, 0, 0) }
        };

        var result = _weeklyService.CreateGrid(events, mockConfig, _props);

        var includedEvents = result.Rows.SelectMany(row => row.Cells.SelectMany(x => x.Events)).Select(x => x.Event);
        Assert.Equal(events.Count, includedEvents.Count());
    }

    [Fact]
    public void CreateGrid_WithEventsOnEdgeBoundaries_ShouldIncludeCorrectly()
    {
        var mockConfig = new TimetableConfig
        {
            CurrentDate = new DateTime(2023, 10, 30),
            Days = [DayOfWeek.Monday],
            TimeFrom = new TimeOnly(9, 0),
            TimeTo = new TimeOnly(17, 0)
        };

        var events = new List<TestEvent>
        {
            new TestEvent
                { StartTime = new DateTime(2023, 10, 30, 9, 0, 0), EndTime = new DateTime(2023, 10, 30, 9, 30, 0) },
            new TestEvent
                { StartTime = new DateTime(2023, 10, 30, 17, 0, 0), EndTime = new DateTime(2023, 10, 30, 18, 0, 0) }
        };

        var result = _weeklyService.CreateGrid(events, mockConfig, _props);

        var includedEvents = result.Rows.SelectMany(row => row.Cells.SelectMany(x => x.Events)).Select(x => x.Event);
        Assert.Equal(events.Count, includedEvents.Count());
    }
}
