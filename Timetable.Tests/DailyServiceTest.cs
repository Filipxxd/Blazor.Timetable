using FluentAssertions;
using Timetable.Common.Enums;
using Timetable.Models;
using Timetable.Models.Configuration;
using Timetable.Services.Display;

namespace Timetable.Tests;

public sealed class DailyServiceTests
{
    private readonly DailyService _dailyService = new();

    private class TestEvent
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? GroupId { get; set; }
    }

    private readonly PropertyAccessors<TestEvent> _props =
        new(e => e.StartTime, e => e.EndTime, e => e.Title, e => e.GroupId, []);

    private readonly DateOnly _currentDate = new(2023, 10, 30);

    [Theory]
    [InlineData(0, 23, 23)]
    [InlineData(8, 16, 8)]
    [InlineData(10, 20, 10)]
    [InlineData(0, 12, 12)]
    public void CreateGrid_ShouldReturnGridWithCorrectRowCount(int hourFrom, int hourTo, int expectedRowCount)
    {
        var mockConfig = new TimetableConfig
        {
            TimeFrom = new TimeOnly(hourFrom, 0),
            TimeTo = new TimeOnly(hourTo, 0),
            Is24HourFormat = false
        };

        var result = _dailyService.CreateGrid([], mockConfig, _currentDate, _props);

        result.RowTitles.Count().Should().Be(expectedRowCount);
    }

    [Theory]
    [InlineData(0, 2, 0)]
    [InlineData(8, 10, 8)]
    [InlineData(12, 14, 12)]
    public void CreateGrid_ShouldSetCorrectStartingHour(int startHour, int endHour, int expectedFirstHour)
    {
        var mockConfig = new TimetableConfig
        {
            TimeFrom = new TimeOnly(startHour, 0),
            TimeTo = new TimeOnly(endHour + 1, 0),
            Is24HourFormat = true
        };

        var result = _dailyService.CreateGrid([], mockConfig, _currentDate, _props);

        var firstRowTitle = result.RowTitles.First();
        TimeSpan.Parse(firstRowTitle).Hours.Should().Be(expectedFirstHour);
    }

    [Fact]
    public void CreateGrid_WithEventsOutsideConfiguredTime_ShouldBeExcludedOrHandled()
    {
        var mockConfig = new TimetableConfig
        {
            TimeFrom = new TimeOnly(9, 0),
            TimeTo = new TimeOnly(17, 0),
            Is24HourFormat = false
        };

        var events = new List<TestEvent>
        {
            new() { StartTime = new DateTime(2023, 10, 30, 8, 0, 0), EndTime = new DateTime(2023, 10, 30, 9, 0, 0), Title = "Event 1" },
            new() { StartTime = new DateTime(2023, 10, 30, 16, 0, 0), EndTime = new DateTime(2023, 10, 30, 17, 0, 0), Title = "Event 2" },
            new() { StartTime = new DateTime(2023, 10, 31, 10, 0, 0), EndTime = new DateTime(2023, 10, 31, 11, 0, 0), Title = "Event 3" }
        };

        var result = _dailyService.CreateGrid(events, mockConfig, _currentDate, _props);

        var regularEvents = result.Columns
            .SelectMany(col => col.Cells.Where(cell => cell.Type != CellType.Header)
            .SelectMany(cell => cell.Items))
            .Select(e => e.EventWrapper.Event)
            .ToList();

        regularEvents.Should().ContainSingle().Which.Title.Should().Be("Event 2");

        var headerEvents = result.Columns
            .SelectMany(col => col.Cells.Where(cell => cell.Type == CellType.Header)
            .SelectMany(cell => cell.Items))
            .Select(e => e.EventWrapper.Event)
            .ToList();

        headerEvents.Should().ContainSingle().Which.Title.Should().Be("Event 1");

        var allEvents = result.Columns
            .SelectMany(col => col.Cells.SelectMany(cell => cell.Items))
            .Select(e => e.EventWrapper.Event)
            .ToList();

        allEvents.Should().NotContain(e => e.Title == "Event 3");
    }

    [Fact]
    public void CreateGrid_WithWholeDayEvent_ShouldIncludeInAllRelevantCells()
    {
        var mockConfig = new TimetableConfig
        {
            TimeFrom = new TimeOnly(0, 0),
            TimeTo = new TimeOnly(16, 0),
            Is24HourFormat = true
        };

        var wholeDayEvent = new TestEvent
        {
            StartTime = new DateTime(2023, 10, 30, 0, 0, 0),
            EndTime = new DateTime(2023, 10, 30, 20, 59, 59),
            Title = "All Day Event"
        };

        var events = new[] { wholeDayEvent };

        var result = _dailyService.CreateGrid(events, mockConfig, _currentDate, _props);

        var headerCell = result.Columns.SelectMany(x => x.Cells).First(cell => cell.Type == CellType.Header);

        headerCell.Items.Should().Contain(e => e.EventWrapper.Event.Title == wholeDayEvent.Title);
    }

    [Fact]
    public void CreateGrid_WithOverlappingEvents_ShouldIncludeAllEvents()
    {
        var mockConfig = new TimetableConfig
        {
            TimeFrom = new TimeOnly(9, 0),
            TimeTo = new TimeOnly(17, 0),
            Is24HourFormat = false
        };

        var events = new List<TestEvent>
        {
            new() { StartTime = new DateTime(2023, 10, 30, 10, 0, 0), EndTime = new DateTime(2023, 10, 30, 12, 0, 0), Title = "Event 1" },
            new() { StartTime = new DateTime(2023, 10, 30, 11, 0, 0), EndTime = new DateTime(2023, 10, 30, 13, 0, 0), Title = "Event 2" }
        };

        var result = _dailyService.CreateGrid(events, mockConfig, _currentDate, _props);

        var allTitles = result.Columns
            .SelectMany(col => col.Cells.SelectMany(cell => cell.Items))
            .Select(e => e.EventWrapper.Event.Title)
            .ToList();

        allTitles.Should().Contain("Event 1").And.Contain("Event 2");
    }

    [Fact]
    public void CreateGrid_WithEventsOnEdgeBoundaries_ShouldIncludeCorrectly()
    {
        var mockConfig = new TimetableConfig
        {
            TimeFrom = new TimeOnly(9, 0),
            TimeTo = new TimeOnly(17, 0),
            Is24HourFormat = false
        };

        var events = new List<TestEvent>
        {
            new() { StartTime = new DateTime(2023, 10, 30, 9, 0, 0), EndTime = new DateTime(2023, 10, 30, 9, 30, 0), Title = "Start Boundary Event" },
            new() { StartTime = new DateTime(2023, 10, 29, 17, 0, 0), EndTime = new DateTime(2023, 10, 29, 17, 30, 0), Title = "Not Today Event" }
        };

        var result = _dailyService.CreateGrid(events, mockConfig, _currentDate, _props);

        result.Columns.SelectMany(col => col.Cells)
            .Any(cell => cell.Items.Any(e => e.EventWrapper.Event.Title == "Start Boundary Event"))
            .Should().BeTrue();

        result.Columns.SelectMany(col => col.Cells)
            .Any(cell => cell.Items.Any(e => e.EventWrapper.Event.Title == "Not Today Event"))
            .Should().BeFalse();
    }
}