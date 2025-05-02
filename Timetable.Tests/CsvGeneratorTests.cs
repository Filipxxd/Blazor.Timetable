using FluentAssertions;
using System.Globalization;
using Timetable.Services.DataExchange.Export;

namespace Timetable.Tests;

public sealed class CsvGeneratorTests
{
    private class TestEvent
    {
        public string Property1 { get; set; } = default!;
        public string Property2 { get; set; } = default!;
        public int IntegerProperty { get; set; }
        public double DoubleProperty { get; set; }
        public bool BooleanProperty { get; set; }
        public int? NullableIntegerProperty { get; set; }
        public DateTime DateTimeProperty { get; set; }
    }

    [Fact]
    public void CreateCsvContent_ShouldCreateCorrectHeaderRow()
    {
        var testRecords = new List<TestEvent>
        {
            new() { Property1 = "Value1", Property2 = "Value2" }
        };
        var propertySelectors = new List<INamePropertySelector<TestEvent>>
        {
            new NamePropertySelector<TestEvent, string>("Property1", e => e.Property1),
            new NamePropertySelector<TestEvent, string>("Property2", e => e.Property2)
        };
        var result = CsvGenerator.CreateCsvContent(testRecords, propertySelectors);
        result.Should().HaveCount(2);
        result[0].Should().Equal(["Property1", "Property2"]);
    }

    [Fact]
    public void CreateCsvContent_ShouldCreateCorrectDataRows()
    {
        var testRecords = new List<TestEvent>
        {
            new() { Property1 = "Value1", Property2 = "Value2" },
            new() { Property1 = "Value3", Property2 = "Value4" }
        };
        var propertySelectors = new List<INamePropertySelector<TestEvent>>
        {
            new NamePropertySelector<TestEvent, string>("Property1", e => e.Property1),
            new NamePropertySelector<TestEvent, string>("Property2", e => e.Property2)
        };

        var result = CsvGenerator.CreateCsvContent(testRecords, propertySelectors);

        result.Should().HaveCount(3);
        result[1].Should().Equal(["Value1", "Value2"]);
        result[2].Should().Equal(["Value3", "Value4"]);
    }

    [Fact]
    public void CreateCsvContent_ShouldHandleVariousDataTypes()
    {
        var cultureInfo = new CultureInfo("en-US");
        CultureInfo.CurrentCulture = cultureInfo;

        var testRecords = new List<TestEvent>
        {
            new()
            {
                Property1 = "Value1", Property2 = "Value2", IntegerProperty = 123,
                DoubleProperty = 456.78, BooleanProperty = true,
                DateTimeProperty = new DateTime(2023, 10, 10)
            }
        };
        var propertySelectors = new List<INamePropertySelector<TestEvent>>
        {
            new NamePropertySelector<TestEvent, string>("Property1", e => e.Property1),
            new NamePropertySelector<TestEvent, int>("IntegerProperty", e => e.IntegerProperty),
            new NamePropertySelector<TestEvent, double>("DoubleProperty", e => e.DoubleProperty),
            new NamePropertySelector<TestEvent, bool>("BooleanProperty", e => e.BooleanProperty),
            new NamePropertySelector<TestEvent, DateTime>("DateTimeProperty", e => e.DateTimeProperty),
            new NamePropertySelector<TestEvent, int?>("NullableIntegerProperty", e => e.NullableIntegerProperty)
        };

        var result = CsvGenerator.CreateCsvContent(testRecords, propertySelectors);

        result.Should().HaveCount(2);
        result[1].Should().Equal(["Value1", "123", "456.78", "True", "10/10/2023 12:00:00 AM", ""]);
    }

    [Fact]
    public void CreateCsvContent_ShouldAllowCustomToString()
    {
        var cultureInfo = new CultureInfo("en-US");
        CultureInfo.CurrentCulture = cultureInfo;

        var testRecords = new List<TestEvent>
        {
            new()
            {
                DoubleProperty = 456.78,
                BooleanProperty = true,
                DateTimeProperty = new DateTime(2023, 10, 10)
            }
        };
        var propertySelectors = new List<INamePropertySelector<TestEvent>>
        {
            new NamePropertySelector<TestEvent, double>("DoubleProperty", e => e.DoubleProperty, doubleProperty => ((int)Math.Floor(doubleProperty)).ToString()),
            new NamePropertySelector<TestEvent, bool>("BooleanProperty", e => e.BooleanProperty, boolProperty => "hardcoded"),
            new NamePropertySelector<TestEvent, DateTime>("DateTimeProperty", e => e.DateTimeProperty, dateTimeProperty => dateTimeProperty.ToString("dd/MM"))
        };

        var result = CsvGenerator.CreateCsvContent(testRecords, propertySelectors);

        result.Should().HaveCount(2);
        result[1].Should().Equal(["456", "hardcoded", "10/10"]);
    }

    [Fact]
    public void CreateCsvContent_ShouldThrowExceptionForInvalidPropertyName_WithSeparator()
    {
        var testRecords = new List<TestEvent>
        {
            new() { Property1 = "Value1", Property2 = "Value2" }
        };
        var propertySelectors = new List<INamePropertySelector<TestEvent>>
        {
            new NamePropertySelector<TestEvent, string>("Invalid;Property", e => e.Property1)
        };

        Action act = () => CsvGenerator.CreateCsvContent(testRecords, propertySelectors);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateCsvContent_ShouldNotThrowForValidPropertyName()
    {
        var testRecords = new List<TestEvent>
        {
            new() { Property1 = "NormalValue", Property2 = "Value2" }
        };
        var propertySelectors = new List<INamePropertySelector<TestEvent>>
        {
            new NamePropertySelector<TestEvent, string>("NormalProperty", e => e.Property1)
        };

        Action act = () => CsvGenerator.CreateCsvContent(testRecords, propertySelectors);

        act.Should().NotThrow();
    }
}