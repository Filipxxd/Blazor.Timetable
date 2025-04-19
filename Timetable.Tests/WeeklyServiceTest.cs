using Timetable.Common.Enums;
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
    private readonly CompiledProps<TestEvent> _props =
        new(e => e.StartTime, e => e.EndTime, e => e.Title);

    [Theory]
    [InlineData(new[] { DayOfWeek.Monday }, 1)]
    [InlineData(new[] { DayOfWeek.Monday, DayOfWeek.Tuesday }, 2)]
    [InlineData(new[] { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday }, 3)]
    [InlineData(new[] { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday, DayOfWeek.Monday }, 7)]
    public void CreateGrid_ShouldReturnGridWithCorrectColumnCount(DayOfWeek[] days, int expectedColumnCount)
    {
        var currentDate = new DateTime(2023, 10, 30);
        var mockConfig = new TimetableConfig
        {
            Days = days
        };

        var result = _weeklyService.CreateGrid([], mockConfig, currentDate, _props);
        Assert.Equal(expectedColumnCount, result.Columns.Count);
    }

    [Theory]
    [InlineData(1, 2, 1)]
    [InlineData(8, 16, 8)]
    [InlineData(10, 20, 10)]
    [InlineData(0, 23, 23)]
    public void CreateGrid_ShouldReturnGridWithCorrectRowPrependCount(int hourFrom, int hourTo, int expectedRowPrependCount)
    {
        var currentDate = new DateTime(2023, 10, 30);
        var mockConfig = new TimetableConfig
        {
            Days = [DayOfWeek.Monday],
            TimeFrom = new TimeOnly(hourFrom, 0),
            TimeTo = new TimeOnly(hourTo, 0)
        };
        var result = _weeklyService.CreateGrid([], mockConfig, currentDate, _props);
        Assert.Equal(expectedRowPrependCount, result.RowTitles.Count);
    }


    [Theory]
    [InlineData(1, 2, 2)]
    [InlineData(8, 16, 9)]
    [InlineData(10, 20, 11)]
    [InlineData(0, 23, 24)]
    public void CreateGrid_ShouldReturnCorrectCellCountPerColumn(int hourFrom, int hourTo, int expectedColumnCellCount)
    {
        var currentDate = new DateTime(2023, 10, 30);
        var mockConfig = new TimetableConfig
        {
            Days = [DayOfWeek.Monday, DayOfWeek.Tuesday],
            TimeFrom = new TimeOnly(hourFrom, 0),
            TimeTo = new TimeOnly(hourTo, 0)
        };
        var result = _weeklyService.CreateGrid([], mockConfig, currentDate, _props);

        foreach (var column in result.Columns)
        {
            Assert.Equal(expectedColumnCellCount, column.Cells.Count);

            foreach (var cell in column.Cells.Where(cell => cell.Type != CellType.Header))
            {
                Assert.True(cell.DateTime.Hour >= hourFrom);
                Assert.True(cell.DateTime.Hour < hourTo);
            }
        }
    }

    [Fact]
    public void CreateGrid_ShouldOnlyIncludeEventsWithinGridDateRange()
    {
        var currentDate = new DateTime(2023, 10, 30);
        var mockConfig = new TimetableConfig
        {
            Days = [DayOfWeek.Monday, DayOfWeek.Tuesday]
        };
        var events = new List<TestEvent>
        {
            new() { StartTime = new DateTime(2023, 10, 30, 10, 0, 0), EndTime = new DateTime(2023, 10, 30, 11, 0, 0) },
            new() { StartTime = new DateTime(2023, 11, 1, 10, 0, 0), EndTime = new DateTime(2023, 11, 1, 11, 0, 0) }
        };
        var result = _weeklyService.CreateGrid(events, mockConfig, currentDate, _props);
        var includedEvents = result.Columns.SelectMany(col => col.Cells.SelectMany(cell => cell.Events)).Select(e => e.Event).ToList();
        Assert.Single(includedEvents);
        Assert.Equal(events[0], includedEvents[0]);
    }

    [Theory]
    [InlineData((DayOfWeek[])[])]
    public void CreateGrid_WithInvalidConfiguration_ShouldThrow(DayOfWeek[] days)
    {
        var currentDate = DateTime.Now;
        var mockConfig = new TimetableConfig
        {
            Days = days,
            TimeFrom = new TimeOnly(0, 0),
            TimeTo = new TimeOnly(1, 0)
        };
        Assert.Throws<InvalidOperationException>(() => _weeklyService.CreateGrid([], mockConfig, currentDate, _props));
    }

    [Fact]
    public void CreateGrid_WithEventsOutsideConfiguredTime_ShouldBeInHeaderCells()
    {
        var currentDate = new DateTime(2023, 10, 30);
        var mockConfig = new TimetableConfig
        {
            Days = [DayOfWeek.Monday, DayOfWeek.Tuesday],
            TimeFrom = new TimeOnly(9, 0),
            TimeTo = new TimeOnly(17, 0)
        };
        var events = new List<TestEvent>
        {
            new() { StartTime = new DateTime(2023, 10, 30, 8, 0, 0), EndTime = new DateTime(2023, 10, 30, 9, 0, 0) },
            new() { StartTime = new DateTime(2023, 10, 30, 18, 0, 0), EndTime = new DateTime(2023, 10, 30, 19, 0, 0) }
        };
        var result = _weeklyService.CreateGrid(events, mockConfig, currentDate, _props);
        var headerEvents = result.Columns.SelectMany(col => col.Cells.Where(cell => cell.Type == CellType.Header).SelectMany(cell => cell.Events).Select(e => e.Event)).ToList();
        Assert.Equal(events, headerEvents);
    }

    [Fact]
    public void CreateGrid_WithWholeDayEvents_ShouldBeInHeaderCells()
    {
        var currentDate = new DateTime(2023, 10, 30);
        var mockConfig = new TimetableConfig
        {
            Days = [DayOfWeek.Monday, DayOfWeek.Tuesday],
            TimeFrom = new TimeOnly(9, 0),
            TimeTo = new TimeOnly(17, 0)
        };
        var events = new List<TestEvent>
        {
            new() { StartTime = new DateTime(2023, 10, 30, 0, 0, 0), EndTime = new DateTime(2023, 10, 30, 23, 59, 59) }
        };
        var result = _weeklyService.CreateGrid(events, mockConfig, currentDate, _props);
        var headerEvents = result.Columns.SelectMany(col => col.Cells.Where(cell => cell.Type == CellType.Header).SelectMany(cell => cell.Events).Select(e => e.Event)).ToList();
        Assert.Single(headerEvents);
        Assert.Equal(events[0], headerEvents[0]);
    }

    [Fact]
    public void CreateGrid_WithOverlappingEvents_ShouldIncludeAllEvents()
    {
        var currentDate = new DateTime(2023, 10, 30);
        var mockConfig = new TimetableConfig
        {
            Days = [DayOfWeek.Monday],
            TimeFrom = new TimeOnly(9, 0),
            TimeTo = new TimeOnly(17, 0)
        };
        var events = new List<TestEvent>
        {
            new() { StartTime = new DateTime(2023, 10, 30, 10, 0, 0), EndTime = new DateTime(2023, 10, 30, 12, 0, 0) },
            new() { StartTime = new DateTime(2023, 10, 30, 11, 0, 0), EndTime = new DateTime(2023, 10, 30, 13, 0, 0) }
        };
        var result = _weeklyService.CreateGrid(events, mockConfig, currentDate, _props);
        var includedEvents = result.Columns.SelectMany(col => col.Cells.SelectMany(cell => cell.Events)).Select(e => e.Event).ToList();
        Assert.Equal(events.Count, includedEvents.Count);
    }

    [Fact]
    public void CreateGrid_WithEventsOnEdgeBoundaries_ShouldIncludeCorrectly()
    {
        var currentDate = new DateTime(2023, 10, 30);
        var mockConfig = new TimetableConfig
        {
            Days = [DayOfWeek.Monday],
            TimeFrom = new TimeOnly(9, 0),
            TimeTo = new TimeOnly(17, 0)
        };
        var events = new List<TestEvent>
        {
            new() { StartTime = new DateTime(2023, 10, 30, 9, 0, 0), EndTime = new DateTime(2023, 10, 30, 9, 30, 0) },
            new() { StartTime = new DateTime(2023, 10, 30, 17, 0, 0), EndTime = new DateTime(2023, 10, 30, 18, 0, 0) }
        };
        var result = _weeklyService.CreateGrid(events, mockConfig, currentDate, _props);
        var includedEvents = result.Columns.SelectMany(col => col.Cells.Where(cell => cell.DateTime.Hour < mockConfig.TimeTo.Hour).SelectMany(cell => cell.Events)).Select(e => e.Event).ToList();
        Assert.Equal(events.Count, includedEvents.Count);
    }
}