using Timetable.Services.Display;
using Timetable.Structure;
using Timetable.Configuration;

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

    private readonly TimetableEventProps<TestEvent> _props =
        new(e => e.StartTime, e => e.EndTime, e => e.Title);

    [Fact]
    public void CreateGrid_ShouldReturnGridWithCorrectRowCount()
    {
        var mockConfig = new TimetableConfig
        {
            CurrentDate = new DateTime(2023, 10, 30),
            Days = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday },
            TimeFrom = new TimeOnly(9, 0),
            TimeTo = new TimeOnly(17, 0)
        };

        var result = _weeklyService.CreateGrid([], mockConfig, _props);

        Assert.Equal(mockConfig.TimeTo.Hour - mockConfig.TimeFrom.Hour + 1, result.Count);
    }

    [Fact]
    public void CreateGrid_ShouldReturnGridWithCorrectCellCount()
    {
        var mockConfig = new TimetableConfig
        {
            CurrentDate = new DateTime(2023, 10, 30),
            Days = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday },
            TimeFrom = new TimeOnly(9, 0),
            TimeTo = new TimeOnly(17, 0)
        };

        var result = _weeklyService.CreateGrid([], mockConfig, _props);

        var expectedCellCount = (mockConfig.TimeTo.Hour - mockConfig.TimeFrom.Hour + 1) * mockConfig.Days.Count();

        Assert.Equal(expectedCellCount, result.SelectMany(x => x.Cells).Count());
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
            new TestEvent
                { StartTime = new DateTime(2023, 10, 30, 10, 0, 0), EndTime = new DateTime(2023, 10, 30, 11, 0, 0) },
            new TestEvent
                { StartTime = new DateTime(2023, 11, 1, 10, 0, 0), EndTime = new DateTime(2023, 11, 1, 11, 0, 0) }
        };

        var result = _weeklyService.CreateGrid(events, mockConfig, _props);

        var includedEvents = result.SelectMany(row => row.Cells.SelectMany(cell => cell.Events)).ToList();
        Assert.Single(includedEvents);
        Assert.Equal(events[0], includedEvents[0].Event);
    }

    [Fact]
    public void CreateGrid_WithEmptyConfiguration_ShouldThrow()
    {
        var mockConfig = new TimetableConfig
        {
            CurrentDate = DateTime.Now,
            Days = new List<DayOfWeek>(),
            TimeFrom = new TimeOnly(0, 0),
            TimeTo = new TimeOnly(0, 0)
        };
        
        Assert.Throws<InvalidOperationException>(() => _weeklyService.CreateGrid(new List<TestEvent>(), mockConfig, _props));
    }
    
    [Fact]
    public void CreateGrid_WithEventsOutsideConfiguredTime_ShouldExcludeThoseEvents()
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
            new TestEvent
            {
                StartTime = new DateTime(2023, 10, 30, 8, 0, 0), EndTime = new DateTime(2023, 10, 30, 9, 0, 0)
            },
            new TestEvent
            {
                StartTime = new DateTime(2023, 10, 30, 18, 0, 0), EndTime = new DateTime(2023, 10, 30, 19, 0, 0)
            }
        };
        
        var result = _weeklyService.CreateGrid(events, mockConfig, _props);

        var includedEvents = result.SelectMany(row => row.Cells.SelectMany(cell => cell.Events)).ToList();
        Assert.Empty(includedEvents);
    }
    
    [Fact]
    public void CreateGrid_WithOverlappingEvents_ShouldIncludeAllEvents()
    {
        var mockConfig = new TimetableConfig
        {
            CurrentDate = new DateTime(2023, 10, 30),
            Days = [ DayOfWeek.Monday ],
            TimeFrom = new TimeOnly(9, 0),
            TimeTo = new TimeOnly(17, 0)
        };

        var events = new List<TestEvent>
        {
            new TestEvent
                { StartTime = new DateTime(2023, 10, 30, 10, 0, 0), EndTime = new DateTime(2023, 10, 30, 12, 0, 0) },
            new TestEvent
                { StartTime = new DateTime(2023, 10, 30, 11, 0, 0), EndTime = new DateTime(2023, 10, 30, 13, 0, 0) }
        };

        var result = _weeklyService.CreateGrid(events, mockConfig, _props);

        var includedEvents = result.SelectMany(row => row.Cells.SelectMany(cell => cell.Events)).ToList();
        Assert.Equal(2, includedEvents.Count);
    }
    
    [Fact]
    public void CreateGrid_WithEventsOnEdgeBoundaries_ShouldIncludeCorrectly()
    {
        var mockConfig = new TimetableConfig
        {
            CurrentDate = new DateTime(2023, 10, 30),
            Days = new List<DayOfWeek> { DayOfWeek.Monday },
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

        var includedEvents = result.SelectMany(row => row.Cells.SelectMany(cell => cell.Events)).ToList();
        Assert.Equal(2, includedEvents.Count);
    }
}
