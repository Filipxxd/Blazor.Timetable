using FluentAssertions;
using Blazor.Timetable.Models;
using Blazor.Timetable.Models.Configuration;
using Blazor.Timetable.Services.Display;

namespace Blazor.Timetable.Tests;

public sealed class MonthlyServiceTests
{
    private readonly MonthlyService _sut = new();
    private class TestEvent
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? GroupId { get; set; }
    }

    private readonly PropertyAccessors<TestEvent> _props =
        new(
          e => e.StartTime,
          e => e.EndTime,
          e => e.Title,
          e => e.GroupId,
          []);

    private readonly DateOnly _oct15 = new(2023, 10, 15);

    private TimetableConfig MakeConfig() =>
        new()
        {
            Days =
            [
                DayOfWeek.Sunday,
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday,
                DayOfWeek.Saturday
            ],
            TimeFrom = new TimeOnly(0, 0),
            TimeTo = new TimeOnly(23, 59)
        };

    [Fact]
    public void CreateGrid_SingleWeekSpan_ShouldComputeExactDays()
    {
        var ev = new TestEvent
        {
            StartTime = new DateTime(2023, 10, 1),
            EndTime = new DateTime(2023, 10, 3)
        };

        var grid = _sut.CreateGrid([ev], MakeConfig(), _oct15, _props);

        var sundayCol = grid.Columns.First(c => c.DayOfWeek == DayOfWeek.Sunday);
        var cellOct1 = sundayCol.Cells.Single(c => c.DateTime.Date == new DateTime(2023, 10, 1));
        cellOct1.Items.Should().ContainSingle()
               .Which.Span.Should().Be(3);
    }

    [Fact]
    public void CreateGrid_SpanCappedByEndOfRow_MaxSpanAdjustment()
    {
        var ev = new TestEvent
        {
            StartTime = new DateTime(2023, 10, 30),
            EndTime = new DateTime(2023, 10, 31)
        };

        var grid = _sut.CreateGrid([ev], MakeConfig(), _oct15, _props);

        var mondayCol = grid.Columns.First(c => c.DayOfWeek == DayOfWeek.Monday);
        var cellOct30 = mondayCol.Cells.Single(c => c.DateTime.Date == new DateTime(2023, 10, 30));
        cellOct30.Items.Should().ContainSingle()
                 .Which.Span.Should().Be(2);
    }

    [Fact]
    public void CreateGrid_EventSpanningTwoWeeks_AppearsOnBothRows()
    {
        var ev = new TestEvent
        {
            StartTime = new DateTime(2023, 10, 2),
            EndTime = new DateTime(2023, 10, 10)
        };

        var grid = _sut.CreateGrid([ev], MakeConfig(), _oct15, _props);

        var monCol = grid.Columns.First(c => c.DayOfWeek == DayOfWeek.Monday);
        var cellOct2 = monCol.Cells.Single(c => c.DateTime.Date == new DateTime(2023, 10, 2));
        cellOct2.Items.Should().ContainSingle()
                .Which.Span.Should().Be(6);

        var sunCol = grid.Columns.First(c => c.DayOfWeek == DayOfWeek.Sunday);
        var cellOct8 = sunCol.Cells.Single(c => c.DateTime.Date == new DateTime(2023, 10, 8));
        cellOct8.Items.Should().ContainSingle()
                .Which.Span.Should().Be(3);
    }
}