using Blazor.Timetable.Models.DataExchange;
using Blazor.Timetable.Services.DataExchange.Import;
using FluentAssertions;
using System.Globalization;
using System.Text;

namespace Blazor.Timetable.Tests;

public sealed class CsvImportTransformerTests
{
    private class TestEvent
    {
        public string Property1 { get; set; } = default!;
        public int IntegerProperty { get; set; }
        public double DoubleProperty { get; set; }
        public bool BooleanProperty { get; set; }
        public int? NullableIntegerProperty { get; set; }
        public DateTime DateTimeProperty { get; set; }
    }

    private static MemoryStream ToStream(string s) =>
        new(Encoding.UTF8.GetBytes(s));

    [Fact]
    public void Transform_ShouldReturnEmpty_WhenStreamHasNoLines()
    {
        var mappers = new ISelector<TestEvent>[]
        {
            new Selector<TestEvent, string>("Property1", e => e.Property1)
        }.ToList();

        var transformer = new CsvImportTransformer<TestEvent>(mappers);
        using var ms = ToStream(string.Empty);

        var result = transformer.Transform(ms).ToList();
        result.Should().BeEmpty();
    }

    [Fact]
    public void Transform_ShouldMapSingleRow_ByHeaderNames()
    {
        var csv = new[]
        {
            "Property1;IntegerProperty",
            "Hello;42"
        }.JoinToString(Environment.NewLine);

        var mappers = new ISelector<TestEvent>[]
        {
            new Selector<TestEvent, string>("Property1", e => e.Property1),
            new Selector<TestEvent, int>("IntegerProperty", e => e.IntegerProperty)
        }.ToList();

        var transformer = new CsvImportTransformer<TestEvent>(mappers);

        List<TestEvent> events;
        using (var ms = ToStream(csv))
            events = transformer.Transform(ms).ToList();

        events.Should().HaveCount(1);
        var ev = events[0];
        ev.Property1.Should().Be("Hello");
        ev.IntegerProperty.Should().Be(42);
    }

    [Fact]
    public void Transform_ShouldMapMultipleRows()
    {
        var csv = new[]
        {
            "Property1;Property2",
            "A;B",
            "C;D"
        }.JoinToString(Environment.NewLine);

        var mappers = new ISelector<TestEvent>[]
        {
            new Selector<TestEvent, string>("Property1", e => e.Property1)
        }.ToList();

        var transformer = new CsvImportTransformer<TestEvent>(mappers);

        using var ms = ToStream(csv);
        var result = transformer.Transform(ms).ToList();

        result.Should().HaveCount(2);
        result[0].Property1.Should().Be("A");
        result[1].Property1.Should().Be("C");
    }

    [Fact]
    public void Transform_ShouldHandleVariousDataTypes_AndNullable()
    {
        CultureInfo.CurrentCulture = new CultureInfo("en-US");

        var header = "IntegerProperty;DoubleProperty;BooleanProperty;DateTimeProperty;NullableIntegerProperty";
        var row = "123;456.78;true;10/10/2023 00:00:00;";

        var csv = new[] { header, row }.JoinToString(Environment.NewLine);

        var mappers = new ISelector<TestEvent>[]
        {
            new Selector<TestEvent, int>("IntegerProperty", e => e.IntegerProperty),
            new Selector<TestEvent, double>("DoubleProperty", e => e.DoubleProperty),
            new Selector<TestEvent, bool>("BooleanProperty", e => e.BooleanProperty),
            new Selector<TestEvent, DateTime>("DateTimeProperty", e => e.DateTimeProperty),
            new Selector<TestEvent, int?>("NullableIntegerProperty", e => e.NullableIntegerProperty)
        }.ToList();

        var transformer = new CsvImportTransformer<TestEvent>(mappers);

        using var ms = ToStream(csv);
        var ev = transformer.Transform(ms).Single();

        ev.IntegerProperty.Should().Be(123);
        ev.DoubleProperty.Should().BeApproximately(456.78, 0.001);
        ev.BooleanProperty.Should().BeTrue();
        ev.DateTimeProperty.Should().Be(new DateTime(2023, 10, 10));
        ev.NullableIntegerProperty.Should().BeNull();
    }

    [Fact]
    public void Transform_ShouldUseCustomParser()
    {
        var header = "DoubleProperty;BooleanProperty;DateTimeProperty";
        var row = "456.78;1;10/10";

        var csv = new[] { header, row }.JoinToString(Environment.NewLine);

        var mappers = new ISelector<TestEvent>[]
        {
            new Selector<TestEvent, double>(
                "DoubleProperty", e => e.DoubleProperty,
                parser: s => Math.Floor(double.Parse(s, CultureInfo.InvariantCulture))),
            new Selector<TestEvent, bool>(
                "BooleanProperty", e => e.BooleanProperty,
                parser: s => s == "1"),
            new Selector<TestEvent, DateTime>(
                "DateTimeProperty", e => e.DateTimeProperty,
                parser: s => DateTime.ParseExact(s, "dd/MM", CultureInfo.InvariantCulture))
        }.ToList();

        var transformer = new CsvImportTransformer<TestEvent>(mappers);

        using var ms = ToStream(csv);
        var ev = transformer.Transform(ms).Single();

        ev.DoubleProperty.Should().Be(456);
        ev.BooleanProperty.Should().BeTrue();
        ev.DateTimeProperty.Should().Be(new DateTime(DateTime.Now.Year, 10, 10));
    }

    [Fact]
    public void Transform_ShouldIgnoreUnknownColumns()
    {
        var csv = new[]
        {
            "Property1;Unknown;IntegerProperty",
            "X;Y;7"
        }.JoinToString(Environment.NewLine);

        var mappers = new ISelector<TestEvent>[]
        {
            new Selector<TestEvent, string>("Property1", e => e.Property1),
            new Selector<TestEvent, int>("IntegerProperty", e => e.IntegerProperty)
        }.ToList();

        var transformer = new CsvImportTransformer<TestEvent>(mappers);

        using var ms = ToStream(csv);
        var ev = transformer.Transform(ms).Single();

        ev.Property1.Should().Be("X");
        ev.IntegerProperty.Should().Be(7);
    }
}

internal static class EnumerableExtensions
{
    public static string JoinToString(
        this IEnumerable<string> lines,
        string separator) =>
        string.Join(separator, lines);
}