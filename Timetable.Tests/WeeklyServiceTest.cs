using Timetable.Common.Enums;
using Timetable.Configuration;
using Timetable.Models;
using Timetable.Services.Display;

namespace Timetable.Tests;

public sealed class WeeklyServiceTests
{
    private readonly WeeklyService _weeklyService = new();

    private class TestEvent
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? GroupId { get; set; }
    }

    private readonly CompiledProps<TestEvent> _props =
        new(e => e.StartTime, e => e.EndTime, e => e.Title, e => e.GroupId, []);

    private readonly DateOnly _currentDate = new(2023, 10, 30);

    [Theory]
    [InlineData(new[] { DayOfWeek.Monday }, 1)]
    [InlineData(new[] { DayOfWeek.Monday, DayOfWeek.Tuesday }, 2)]
    [InlineData(new[] { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday }, 3)]
    [InlineData(new[] { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday, DayOfWeek.Monday }, 7)]
    public void CreateGrid_ShouldReturnGridWithCorrectColumnCount(DayOfWeek[] days, int expectedColumnCount)
    {
        var config = new TimetableConfig
        {
            Days = days,
            TimeFrom = new TimeOnly(0, 0),
            TimeTo = new TimeOnly(23, 0)
        };
        var grid = _weeklyService.CreateGrid([], config, _currentDate, _props);
        var gridColumnDaysOfWeek = grid.Columns.Select(c => c.DayOfWeek);
        Assert.Equal(expectedColumnCount, grid.Columns.Count);
        Assert.Equal(days.Length, gridColumnDaysOfWeek.Count());
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(8, 16)]
    [InlineData(10, 20)]
    [InlineData(0, 23)]
    public void CreateGrid_ShouldReturnGridWithCorrectRowPrependCount(int hourFrom, int hourTo)
    {
        var expectedRowCount = hourTo - hourFrom;
        var config = new TimetableConfig
        {
            Days = [DayOfWeek.Monday],
            TimeFrom = new TimeOnly(hourFrom, 0),
            TimeTo = new TimeOnly(hourTo, 0),
        };
        var grid = _weeklyService.CreateGrid([], config, _currentDate, _props);
        Assert.Equal(expectedRowCount, grid.RowTitles.Count());
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(8, 16)]
    [InlineData(10, 20)]
    [InlineData(0, 23)]
    public void CreateGrid_ShouldReturnCorrectCellCountPerColumn(int hourFrom, int hourTo)
    {
        var hourCount = hourTo - hourFrom;
        var expectedCellCount = hourCount + 1;
        var config = new TimetableConfig
        {
            Days = [DayOfWeek.Monday, DayOfWeek.Tuesday],
            TimeFrom = new TimeOnly(hourFrom, 0),
            TimeTo = new TimeOnly(hourTo, 0),
            Is24HourFormat = true
        };
        var grid = _weeklyService.CreateGrid([], config, _currentDate, _props);
        foreach (var column in grid.Columns)
        {
            Assert.Equal(expectedCellCount, column.Cells.Count);
            foreach (var cell in column.Cells.Where(c => c.Type != CellType.Header))
            {
                Assert.True(cell.DateTime.Hour >= hourFrom);
                Assert.True(cell.DateTime.Hour < hourTo);
            }
        }
    }

    [Fact]
    public void CreateGrid_ShouldOnlyIncludeEventsWithinGridDateRange()
    {
        var config = new TimetableConfig
        {
            Days = [DayOfWeek.Monday, DayOfWeek.Tuesday],
            TimeFrom = new TimeOnly(0, 0),
            TimeTo = new TimeOnly(23, 0),
            Is24HourFormat = true
        };
        var events = new[]
        {
            new TestEvent { StartTime = new DateTime(2023, 10, 30, 10, 0, 0), EndTime = new DateTime(2023, 10, 30, 11, 0, 0), Title = "Event1" },
            new TestEvent { StartTime = new DateTime(2023, 11, 1, 10, 0, 0), EndTime = new DateTime(2023, 11, 1, 11, 0, 0), Title = "Event2" }
        };
        var grid = _weeklyService.CreateGrid(events, config, _currentDate, _props);
        var includedEvents = grid.Columns
                                  .SelectMany(col => col.Cells.SelectMany(cell => cell.Events))
                                  .Select(wrapper => wrapper.Event)
                                  .ToList();
        Assert.Single(includedEvents);
        Assert.Equal(events[0], includedEvents[0]);
    }

    [Fact]
    public void CreateGrid_WithInvalidConfiguration_ShouldThrow()
    {
        var config = new TimetableConfig
        {
            Days = [],
            TimeFrom = new TimeOnly(0, 0),
            TimeTo = new TimeOnly(1, 0)
        };
        Assert.Throws<InvalidOperationException>(() => _weeklyService.CreateGrid([], config, _currentDate, _props));
    }

    [Fact]
    public void CreateGrid_WithEventsOutsideConfiguredTime_ShouldBeInHeaderCells()
    {
        var config = new TimetableConfig
        {
            Days = [DayOfWeek.Monday, DayOfWeek.Tuesday],
            TimeFrom = new TimeOnly(9, 0),
            TimeTo = new TimeOnly(17, 0),
            Is24HourFormat = true
        };
        var events = new[]
        {
            new TestEvent { StartTime = new DateTime(2023, 10, 30, 7, 0, 0), EndTime = new DateTime(2023, 10, 30, 8, 0, 0), Title = "Early" },
            new TestEvent { StartTime = new DateTime(2023, 10, 30, 8, 0, 0), EndTime = new DateTime(2023, 10, 30, 9, 0, 0), Title = "Early" },
            new TestEvent { StartTime = new DateTime(2023, 10, 30, 18, 0, 0), EndTime = new DateTime(2023, 10, 30, 19, 0, 0), Title = "Late" },
            new TestEvent { StartTime = new DateTime(2023, 10, 30, 19, 0, 0), EndTime = new DateTime(2023, 10, 30, 20, 0, 0), Title = "Late" }
        };
        var grid = _weeklyService.CreateGrid(events, config, _currentDate, _props);
        var headerEvents = grid.Columns
                               .SelectMany(col => col.Cells.Where(cell => cell.Type == CellType.Header)
                                                           .SelectMany(cell => cell.Events)
                                                           .Select(wrapper => wrapper.Event));

        Assert.Equal(4, headerEvents.Count());
        Assert.Contains(events[0], headerEvents);
        Assert.Contains(events[1], headerEvents);
    }

    [Fact]
    public void CreateGrid_WithWholeDayEvents_ShouldBeInHeaderCells()
    {
        var config = new TimetableConfig
        {
            Days = [DayOfWeek.Monday, DayOfWeek.Tuesday],
            TimeFrom = new TimeOnly(9, 0),
            TimeTo = new TimeOnly(17, 0),
            Is24HourFormat = true
        };
        var events = new[]
        {
            new TestEvent { StartTime = new DateTime(2023, 10, 30, 0, 0, 0), EndTime = new DateTime(2023, 10, 31, 0, 0, 0), Title = "WholeDay" }
        };
        var grid = _weeklyService.CreateGrid(events, config, _currentDate, _props);
        var headerEvents = grid.Columns
                               .SelectMany(col => col.Cells.Where(cell => cell.Type == CellType.Header)
                                                           .SelectMany(cell => cell.Events)
                                                           .Select(wrapper => wrapper.Event));
        Assert.Single(headerEvents);
        Assert.Equal(events[0], headerEvents.First());
    }

    [Fact]
    public void CreateGrid_WithOverlappingEvents_ShouldIncludeAllEvents()
    {
        var config = new TimetableConfig
        {
            Days = [DayOfWeek.Monday],
            TimeFrom = new TimeOnly(9, 0),
            TimeTo = new TimeOnly(17, 0),
            Is24HourFormat = true
        };
        var events = new[]
        {
            new TestEvent { StartTime = new DateTime(2023, 10, 30, 10, 0, 0), EndTime = new DateTime(2023, 10, 30, 12, 0, 0), Title = "Event1" },
            new TestEvent { StartTime = new DateTime(2023, 10, 30, 11, 0, 0), EndTime = new DateTime(2023, 10, 30, 13, 0, 0), Title = "Event2" }
        };
        var grid = _weeklyService.CreateGrid(events, config, _currentDate, _props);
        var includedEvents = grid.Columns
                                  .SelectMany(col => col.Cells)
                                  .SelectMany(cell => cell.Events)
                                  .Select(wrapper => wrapper.Event)
                                  .ToArray();
        Assert.Equal(events.Length, includedEvents.Length);
    }

    [Fact]
    public void CreateGrid_WithEventsOnEdgeBoundaries_ShouldIncludeCorrectly()
    {
        var config = new TimetableConfig
        {
            Days = [DayOfWeek.Monday],
            TimeFrom = new TimeOnly(9, 0),
            TimeTo = new TimeOnly(17, 0),
            Is24HourFormat = true
        };
        var events = new[]
        {
            //new TestEvent { StartTime = new DateTime(2023, 10, 30, 9, 0, 0), EndTime = new DateTime(2023, 10, 30, 9, 30, 0), Title = "StartEdge" }, TODO: Fix minutes case
            new TestEvent { StartTime = new DateTime(2023, 10, 30, 9, 0, 0), EndTime = new DateTime(2023, 10, 30, 10, 0, 0), Title = "StartEdge" },
            new TestEvent { StartTime = new DateTime(2023, 10, 30, 17, 0, 0), EndTime = new DateTime(2023, 10, 30, 18, 0, 0), Title = "EndEdge" }
        };
        var grid = _weeklyService.CreateGrid(events, config, _currentDate, _props);
        var includedEvents = grid.Columns
                                  .SelectMany(col => col.Cells.Where(cell => cell.DateTime.Hour < config.TimeTo.Hour))
                                  .SelectMany(cell => cell.Events)
                                  .Select(wrapper => wrapper.Event);

        Assert.Equal(events.Length, includedEvents.Count());
    }

    [Theory]
    [InlineData(12, 0, 13, 0, 4)]
    [InlineData(9, 0, 17, 0, 32)]
    [InlineData(10, 0, 13, 0, 12)]
    [InlineData(8, 0, 10, 0, 8)]
    [InlineData(0, 0, 23, 0, 16)]
    public void CreateGrid_HeaderEvent_SpanShouldBeCorrect(int hourFrom, int minuteFrom, int hourTo, int minuteTo, int expectedSpan)
    {
        var config = new TimetableConfig
        {
            Days = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday],
            TimeFrom = new TimeOnly(9, 0),
            TimeTo = new TimeOnly(17, 0)
        };
        var evt = new TestEvent
        {
            StartTime = new DateTime(2023, 10, 30, hourFrom, minuteFrom, 0),
            EndTime = new DateTime(2023, 10, 30, hourTo, minuteTo, 0),
            Title = "SpanningEvent"
        };
        var events = new[] { evt };
        var grid = _weeklyService.CreateGrid(events, config, _currentDate, _props);
        var actualSpan = grid.Columns.SelectMany(col => col.Cells.SelectMany(cell => cell.Events)).First().Span;

        Assert.False(actualSpan == 0);
        Assert.Equal(expectedSpan, actualSpan);
    }
}