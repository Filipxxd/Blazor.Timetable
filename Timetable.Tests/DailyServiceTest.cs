using Timetable.Common.Enums;
using Timetable.Configuration;
using Timetable.Services.Display;
using Timetable.Models;

namespace Timetable.Tests;

public sealed class DailyServiceTests
{
    private readonly DailyService _dailyService = new();

    private class TestEvent
    {
        public DateTime StartTime { get; init; }
        public DateTime EndTime { get; init; }
        public string Title { get; init; } = default!;
    }

    private readonly CompiledProps<TestEvent> _props =
        new(e => e.StartTime, e => e.EndTime, e => e.Title);

    [Theory]
    [InlineData(0, 23, 23)]
    [InlineData(8, 16, 8)]
    [InlineData(10, 20, 10)]
    [InlineData(0, 12, 12)]
    public void CreateGrid_ShouldReturnGridWithCorrectRowCount(int hourFrom, int hourTo, int expectedRowCount)
    {
        var currentDate = new DateTime(2023, 10, 30);
        var mockConfig = new TimetableConfig
        {
            TimeFrom = new TimeOnly(hourFrom, 0),
            TimeTo = new TimeOnly(hourTo, 0),
            Is24HourFormat = false
        };

        var result = _dailyService.CreateGrid([], mockConfig, currentDate, _props);

        Assert.Equal(expectedRowCount, result.RowTitles.Count);
    }

    [Theory]
    [InlineData(new[] { 0, 1, 2 }, 0)]
    [InlineData(new[] { 8, 9, 10 }, 8)]
    [InlineData(new[] { 12, 13, 14 }, 12)]
    public void CreateGrid_ShouldSetCorrectStartingHour(int[] configHours, int expectedFirstHour)
    {
        var currentDate = new DateTime(2023, 10, 30);
        var mockConfig = new TimetableConfig
        {
            TimeFrom = new TimeOnly(configHours.First(), 0),
            TimeTo = new TimeOnly(configHours.Last() + 1, 0),
            Is24HourFormat = true
        };

        var result = _dailyService.CreateGrid([], mockConfig, currentDate, _props);

        Assert.Equal(expectedFirstHour, TimeSpan.Parse(result.RowTitles.First()).Hours);
    }

    [Fact]
    public void CreateGrid_ShouldOnlyIncludeEventsOnCurrentDate()
    {
        var currentDate = new DateTime(2023, 10, 30);
        var mockConfig = new TimetableConfig
        {
            TimeFrom = new TimeOnly(0, 0),
            TimeTo = new TimeOnly(23, 59),
            Is24HourFormat = true
        };
        var events = new List<TestEvent>
        {
            new()
            {
                StartTime = new DateTime(2023, 10, 30, 10, 0, 0),
                EndTime = new DateTime(2023, 10, 30, 11, 0, 0),
                Title = "Event 1"
            },
            new()
            {
                StartTime = new DateTime(2023, 10, 29, 10, 0, 0),
                EndTime = new DateTime(2023, 10, 29, 11, 0, 0),
                Title = "Event 2"
            },
            new()
            {
                StartTime = new DateTime(2023, 11, 1, 10, 0, 0),
                EndTime = new DateTime(2023, 11, 1, 11, 0, 0),
                Title = "Event 3"
            }
        };

        var result = _dailyService.CreateGrid(events, mockConfig, currentDate, _props);

        var includedEvents = result.Columns.SelectMany(col => col.Cells.SelectMany(cell => cell.Events)).Select(e => e.Event).ToList();
        Assert.Single(includedEvents);
        Assert.Equal("Event 1", includedEvents.First().Title);
    }

    [Fact]
    public void CreateGrid_WithNoEvents_ShouldReturnEmptyGrid()
    {
        var currentDate = new DateTime(2023, 10, 30);
        var mockConfig = new TimetableConfig
        {
            TimeFrom = new TimeOnly(0, 0),
            TimeTo = new TimeOnly(23, 59),
            Is24HourFormat = true
        };

        var result = _dailyService.CreateGrid([], mockConfig, currentDate, _props);

        foreach (var column in result.Columns)
        {
            foreach (var cell in column.Cells)
            {
                Assert.Empty(cell.Events);
            }
        }
    }

    [Fact]
    public void CreateGrid_WithEventsOutsideConfiguredTime_ShouldBeExcludedOrHandled()
    {
        var currentDate = new DateTime(2023, 10, 30);
        var mockConfig = new TimetableConfig
        {
            TimeFrom = new TimeOnly(9, 0),
            TimeTo = new TimeOnly(17, 0),
            Days = [DayOfWeek.Monday],
        };
        var events = new List<TestEvent>
        {
            new()
            {
                StartTime = new DateTime(2023, 10, 30, 8, 0, 0),
                EndTime = new DateTime(2023, 10, 30, 9, 0, 0),
                Title = "Event 1"
            },
            new()
            {
                StartTime = new DateTime(2023, 10, 30, 16, 0, 0),
                EndTime = new DateTime(2023, 10, 30, 17, 0, 0),
                Title = "Event 2"
            },
            new()
            {
                StartTime = new DateTime(2023, 10, 31, 10, 0, 0),
                EndTime = new DateTime(2023, 10, 31, 11, 0, 0),
                Title = "Event 3"
            }
        };

        var result = _dailyService.CreateGrid(events, mockConfig, currentDate, _props);

        var regularEvents = result.Columns.SelectMany(col => col.Cells.Where(cell => cell.Type != CellType.Header).SelectMany(cell => cell.Events)).Select(e => e.Event).ToList();
        Assert.Single(regularEvents);
        Assert.Equal("Event 2", regularEvents[0].Title);

        var headerEvents = result.Columns.SelectMany(col => col.Cells.Where(cell => cell.Type == CellType.Header).SelectMany(cell => cell.Events)).Select(e => e.Event).ToList();
        Assert.Single(headerEvents);
        Assert.Equal("Event 1", headerEvents[0].Title);

        var allEvents = result.Columns.SelectMany(col => col.Cells.SelectMany(cell => cell.Events)).Select(e => e.Event).ToList();
        Assert.DoesNotContain(allEvents, e => e.Title == "Event 3");
    }

    [Fact]
    public void CreateGrid_WithWholeDayEvent_ShouldIncludeInAllRelevantCells()
    {
        var currentDate = new DateTime(2023, 10, 30);
        var mockConfig = new TimetableConfig
        {
            TimeFrom = new TimeOnly(0, 0),
            TimeTo = new TimeOnly(15, 59)
        };
        var wholeDayEvent = new TestEvent
        {
            StartTime = new DateTime(2023, 10, 30, 0, 0, 0),
            EndTime = new DateTime(2023, 10, 30, 20, 59, 59),
            Title = "All Day Event"
        };
        var events = new List<TestEvent> { wholeDayEvent };

        var result = _dailyService.CreateGrid(events, mockConfig, currentDate, _props);

        var headerCell = result.Columns.SelectMany(x => x.Cells).First(cell => cell.Type == CellType.Header);
        Assert.Contains(headerCell.Events, cellEvent => cellEvent.Title == wholeDayEvent.Title);
    }

    [Fact]
    public void CreateGrid_WithOverlappingEvents_ShouldIncludeAllEvents()
    {
        var currentDate = new DateTime(2023, 10, 30);
        var mockConfig = new TimetableConfig
        {
            TimeFrom = new TimeOnly(9, 0),
            TimeTo = new TimeOnly(17, 0),
            Is24HourFormat = false
        };
        var events = new List<TestEvent>
        {
            new()
            {
                StartTime = new DateTime(2023, 10, 30, 10, 0, 0),
                EndTime = new DateTime(2023, 10, 30, 12, 0, 0),
                Title = "Event 1"
            },
            new()
            {
                StartTime = new DateTime(2023, 10, 30, 11, 0, 0),
                EndTime = new DateTime(2023, 10, 30, 13, 0, 0),
                Title = "Event 2"
            }
        };

        var result = _dailyService.CreateGrid(events, mockConfig, currentDate, _props);

        var allEvents = result.Columns.SelectMany(col => col.Cells.SelectMany(cell => cell.Events)).Select(e => e.Event.Title).ToList();
        Assert.Contains("Event 1", allEvents);
        Assert.Contains("Event 2", allEvents);
    }

    [Fact]
    public void CreateGrid_WithEventsOnEdgeBoundaries_ShouldIncludeCorrectly()
    {
        var currentDate = new DateTime(2023, 10, 30);
        var mockConfig = new TimetableConfig
        {
            TimeFrom = new TimeOnly(9, 0),
            TimeTo = new TimeOnly(17, 0),
            Is24HourFormat = false
        };
        var events = new List<TestEvent>
        {
            new()
            {
                StartTime = new DateTime(2023, 10, 30, 9, 0, 0),
                EndTime = new DateTime(2023, 10, 30, 9, 30, 0),
                Title = "Start Boundary Event"
            },
            new()
            {
                StartTime = new DateTime(2023, 10, 30, 17, 0, 0),
                EndTime = new DateTime(2023, 10, 30, 17, 30, 0),
                Title = "End Boundary Event"
            }
        };

        var result = _dailyService.CreateGrid(events, mockConfig, currentDate, _props);

        var startEventIncluded = result.Columns
            .SelectMany(col => col.Cells)
            .Any(cell => cell.Events.Any(e => e.Event.Title == "Start Boundary Event"));
        Assert.True(startEventIncluded);

        var endEventIncluded = result.Columns
            .SelectMany(col => col.Cells)
            .Any(cell => cell.Events.Any(e => e.Event.Title == "End Boundary Event"));
        Assert.False(endEventIncluded);
    }
}
