using FluentAssertions;
using Timetable.Common.Enums;
using Timetable.Models;
using Timetable.Models.Configuration;
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

    private readonly PropertyAccessors<TestEvent> _props =
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

        grid.Columns.Should().HaveCount(expectedColumnCount);
        grid.Columns.Select(c => c.DayOfWeek).Should().HaveCount(days.Length);
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

        grid.RowTitles.Should().HaveCount(expectedRowCount);
    }

    [Theory]
    [InlineData(1, 0, 2, 0)]
    [InlineData(8, 0, 16, 0)]
    [InlineData(10, 0, 20, 0)]
    [InlineData(0, 15, 23, 45)]
    [InlineData(9, 15, 10, 30)]
    public void CreateGrid_ShouldReturnCorrectCellCountPerColumn(int hourFrom, int minuteFrom, int hourTo, int minuteTo)
    {
        var start = new TimeOnly(hourFrom, minuteFrom);
        var end = new TimeOnly(hourTo, minuteTo);
        var expectedCellCount = (int)(end.ToTimeSpan() - start.ToTimeSpan()).TotalMinutes / 15 + 1;

        var config = new TimetableConfig
        {
            Days = [DayOfWeek.Monday, DayOfWeek.Tuesday],
            TimeFrom = start,
            TimeTo = end,
            Is24HourFormat = true
        };

        var grid = _weeklyService.CreateGrid(Array.Empty<TestEvent>(), config, _currentDate, _props);

        foreach (var column in grid.Columns)
        {
            column.Cells.Should().HaveCount(expectedCellCount);
            column.Cells.Where(c => c.Type != CellType.Header)
                .All(c =>
                {
                    var cellTime = c.DateTime.TimeOfDay;
                    return cellTime >= start.ToTimeSpan() && cellTime < end.ToTimeSpan();
                }).Should().BeTrue();
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

        var events = new List<TestEvent>
        {
            new() { StartTime = new DateTime(2023, 10, 30, 10, 0, 0), EndTime = new DateTime(2023, 10, 30, 11, 0, 0), Title = "Event1" },
            new() { StartTime = new DateTime(2023, 11, 1, 10, 0, 0), EndTime = new DateTime(2023, 11, 1, 11, 0, 0), Title = "Event2" }
        };

        var grid = _weeklyService.CreateGrid(events, config, _currentDate, _props);

        var includedEvents = grid.Columns
            .SelectMany(col => col.Cells.SelectMany(cell => cell.Items))
            .Select(i => i.EventWrapper.Event)
            .ToArray();

        includedEvents.Should().ContainSingle().Which.Should().Be(events[0]);
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
            new TestEvent { StartTime = new DateTime(2023, 10, 30, 6, 0, 0), EndTime = new DateTime(2023, 10, 30, 11, 0, 0), Title = "Early" },
            new TestEvent { StartTime = new DateTime(2023, 10, 30, 7, 0, 0), EndTime = new DateTime(2023, 10, 30, 8, 0, 0), Title = "Early" },
            new TestEvent { StartTime = new DateTime(2023, 10, 30, 8, 0, 0), EndTime = new DateTime(2023, 10, 30, 9, 0, 0), Title = "Early" },
            new TestEvent { StartTime = new DateTime(2023, 10, 30, 9, 0, 0), EndTime = new DateTime(2023, 10, 30, 19, 0, 0), Title = "Out" },
            new TestEvent { StartTime = new DateTime(2023, 10, 30, 17, 0, 0), EndTime = new DateTime(2023, 10, 30, 19, 0, 0), Title = "Late" },
            new TestEvent { StartTime = new DateTime(2023, 10, 30, 21, 0, 0), EndTime = new DateTime(2023, 10, 30, 23, 0, 0), Title = "Late" }
        };

        var grid = _weeklyService.CreateGrid(events, config, _currentDate, _props);

        var headerEvents = grid.Columns
            .SelectMany(col => col.Cells.Where(cell => cell.Type == CellType.Header))
            .SelectMany(cell => cell.Items)
            .Select(i => i.EventWrapper.Event)
            .ToArray();

        headerEvents.Should().HaveCount(6);
        headerEvents.Should().Contain(events[0]);
        headerEvents.Should().Contain(events[1]);
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

        var events = new List<TestEvent>
        {
            new() { StartTime = new DateTime(2023, 10, 30, 0, 0, 0), EndTime = new DateTime(2023, 10, 31, 0, 0, 0), Title = "WholeDay" }
        };

        var grid = _weeklyService.CreateGrid(events, config, _currentDate, _props);

        var headerEvents = grid.Columns
            .SelectMany(col => col.Cells.Where(cell => cell.Type == CellType.Header))
            .SelectMany(cell => cell.Items.Select(i => i.EventWrapper.Event))
            .ToArray();

        headerEvents.Should().ContainSingle().Which.Should().Be(events[0]);
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

        var events = new List<TestEvent>
        {
            new() { StartTime = new DateTime(2023, 10, 30, 10, 0, 0), EndTime = new DateTime(2023, 10, 30, 12, 0, 0), Title = "Event1" },
            new() { StartTime = new DateTime(2023, 10, 30, 11, 0, 0), EndTime = new DateTime(2023, 10, 30, 13, 0, 0), Title = "Event2" }
        };

        var grid = _weeklyService.CreateGrid(events, config, _currentDate, _props);

        var includedEvents = grid.Columns
            .SelectMany(col => col.Cells.SelectMany(cell => cell.Items))
            .Select(i => i.EventWrapper.Event)
            .ToArray();

        includedEvents.Should().HaveCount(events.Count);
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

        var events = new List<TestEvent>
        {
            new() { StartTime = new DateTime(2023, 10, 30, 9, 0, 0), EndTime = new DateTime(2023, 10, 30, 10, 0, 0), Title = "StartEdge" },
            new() { StartTime = new DateTime(2023, 10, 30, 16, 45, 0), EndTime = new DateTime(2023, 10, 30, 17, 0, 0), Title = "EndEdge" }
        };

        var grid = _weeklyService.CreateGrid(events, config, _currentDate, _props);

        var includedEvents = grid.Columns
            .SelectMany(col => col.Cells.Where(cell => cell.DateTime.Hour < config.TimeTo.Hour))
            .SelectMany(cell => cell.Items)
            .Select(i => i.EventWrapper.Event)
            .ToArray();

        includedEvents.Should().HaveCount(events.Count);
    }

    [Theory]
    [InlineData(12, 0, 13, 0, 4)]
    [InlineData(9, 0, 17, 0, 32)]
    [InlineData(10, 0, 13, 0, 12)]
    [InlineData(8, 0, 10, 0, 8)]
    public void CreateGrid_HeaderEvent_SpanShouldBeCorrect(int hourFrom, int minuteFrom, int hourTo, int minuteTo, int expectedSpan)
    {
        var config = new TimetableConfig
        {
            Days = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday],
            TimeFrom = new TimeOnly(8, 0),
            TimeTo = new TimeOnly(17, 0)
        };

        var evt = new TestEvent
        {
            StartTime = new DateTime(2023, 10, 30, hourFrom, minuteFrom, 0),
            EndTime = new DateTime(2023, 10, 30, hourTo, minuteTo, 0),
            Title = "SpanningEvent"
        };

        var grid = _weeklyService.CreateGrid([evt], config, _currentDate, _props);

        var actualSpan = grid.Columns.SelectMany(col => col.Cells.SelectMany(cell => cell.Items)).First().Span;

        actualSpan.Should().Be(expectedSpan);
    }
}