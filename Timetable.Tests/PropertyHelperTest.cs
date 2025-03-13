using Timetable.Utilities;

namespace Timetable.Tests;

public sealed class PropertyHelperTests
{
    private sealed class TestObject
    {
        public int ValueTypeGetOnly => 1;
        public int ValueType { get; init; } = 1;
        public int? ValueTypeNullable { get; init; }
        public string ReferenceType { get; init; } = "Hello world";
        public string? ReferenceTypeNull { get; init; }
        public NestedObject ComplexObject { get; init; } = new();
        public NestedObject? ComplexObjectNull { get; init; }
        public List<string> ListOfReferenceType { get; init; } = new();
        public List<string>? ListOfReferenceTypeNullable { get; init; }
    }

    private sealed class NestedObject
    {
        public double NonNullableDouble { get; init; }
        public double? NullableDouble { get; init; }
    }

    [Fact]
    public void CreateGetter_GetOnly_Int_ValueType()
    {
        var obj = new TestObject();
        var getter = PropertyHelper.CreateGetter<TestObject, int>(x => x.ValueTypeGetOnly);
        var value = getter(obj);
        Assert.Equal(1, value);
    }

    [Fact]
    public void CreateSetter_GetOnly_Int_ValueType_Nullable()
    {
        Assert.Throws<ArgumentException>(() => PropertyHelper.CreateSetter<TestObject, int>(x => x.ValueTypeGetOnly));
    }
    
    [Fact]
    public void CreateGetter_Int_ValueType()
    {
        var obj = new TestObject { ValueType = 42 };
        var getter = PropertyHelper.CreateGetter<TestObject, int>(x => x.ValueType);
        var value = getter(obj);
        Assert.Equal(42, value);
    }

    [Fact]
    public void CreateSetter_Int_ValueType()
    {
        var obj = new TestObject();
        var setter = PropertyHelper.CreateSetter<TestObject, int>(x => x.ValueType);
        setter(obj, 55);
        Assert.Equal(55, obj.ValueType);
    }

    [Fact]
    public void CreateGetter_NullableInt_ValueTypeNullable()
    {
        var obj = new TestObject { ValueTypeNullable = 42 };
        var getter = PropertyHelper.CreateGetter<TestObject, int?>(x => x.ValueTypeNullable);
        var value = getter(obj);
        Assert.Equal(42, value);
    }

    [Fact]
    public void CreateSetter_NullableInt_ValueTypeNullable()
    {
        var obj = new TestObject();
        var setter = PropertyHelper.CreateSetter<TestObject, int?>(x => x.ValueTypeNullable);
        setter(obj, 34);
        Assert.Equal(34, obj.ValueTypeNullable);
    }

    [Fact]
    public void CreateGetter_String_ReferenceType()
    {
        var obj = new TestObject { ReferenceType = "Hello" };
        var getter = PropertyHelper.CreateGetter<TestObject, string>(x => x.ReferenceType);
        var value = getter(obj);
        Assert.Equal("Hello", value);
    }

    [Fact]
    public void CreateSetter_String_ReferenceType()
    {
        var obj = new TestObject();
        var setter = PropertyHelper.CreateSetter<TestObject, string>(x => x.ReferenceType);
        setter(obj, "Hello World");
        Assert.Equal("Hello World", obj.ReferenceType);
    }

    [Fact]
    public void CreateGetter_NullableString_ReferenceTypeNull()
    {
        var obj = new TestObject { ReferenceTypeNull = "Hello" };
        var getter = PropertyHelper.CreateGetter<TestObject, string?>(x => x.ReferenceTypeNull);
        var value = getter(obj);
        Assert.Equal("Hello", value);
    }

    [Fact]
    public void CreateSetter_NullableString_ReferenceTypeNull()
    {
        var obj = new TestObject();
        var setter = PropertyHelper.CreateSetter<TestObject, string?>(x => x.ReferenceTypeNull);
        setter(obj, "Nullable String");
        Assert.Equal("Nullable String", obj.ReferenceTypeNull);
    }

    [Fact]
    public void CreateGetter_ListOfString_ListOfReferenceType()
    {
        var obj = new TestObject { ListOfReferenceType = ["Item1", "Item2"] };
        var getter = PropertyHelper.CreateGetter<TestObject, List<string>>(x => x.ListOfReferenceType);
        var value = getter(obj);
        Assert.NotNull(value);
        Assert.Equal(2, value.Count);
        Assert.Contains("Item1", value);
        Assert.Contains("Item2", value);
    }

    [Fact]
    public void CreateSetter_ListOfString_ListOfReferenceType()
    {
        var obj = new TestObject();
        var setter = PropertyHelper.CreateSetter<TestObject, List<string>>(x => x.ListOfReferenceType);
        var newList = new List<string> { "NewItem1", "NewItem2" };
        setter(obj, newList);
        Assert.Equal(newList, obj.ListOfReferenceType);
    }

    [Fact]
    public void CreateGetter_ListOfNullableString_ListOfReferenceTypeNullable()
    {
        var obj = new TestObject { ListOfReferenceTypeNullable = new List<string> { "Item1", "Item2" } };
        var getter = PropertyHelper.CreateGetter<TestObject, List<string>?>(x => x.ListOfReferenceTypeNullable);
        var value = getter(obj);
        Assert.NotNull(value);
        Assert.Equal(2, value.Count);
        Assert.Contains("Item1", value);
        Assert.Contains("Item2", value);
    }

    [Fact]
    public void CreateSetter_ListOfNullableString_ListOfReferenceTypeNullable()
    {
        var obj = new TestObject
        {
            ListOfReferenceTypeNullable = ["NewItem1", "NewItem2"]
        };
        var setter = PropertyHelper.CreateSetter<TestObject, List<string>>(x => x.ListOfReferenceTypeNullable);
        setter(obj, null);
        Assert.Null(obj.ListOfReferenceTypeNullable);
    }

    [Fact]
    public void CreateGetter_NestedObject_ComplexObject()
    {
        var nested = new NestedObject { NonNullableDouble = 10.5 };
        var obj = new TestObject { ComplexObject = nested };
        var getter = PropertyHelper.CreateGetter<TestObject, NestedObject>(x => x.ComplexObject);
        var value = getter(obj);
        Assert.NotNull(value);
        Assert.Equal(nested, value);
        Assert.Equal(10.5, value.NonNullableDouble);
    }

    [Fact]
    public void CreateSetter_NestedObject_ComplexObject()
    {
        var obj = new TestObject();
        var setter = PropertyHelper.CreateSetter<TestObject, NestedObject>(x => x.ComplexObject);
        var newNestedObject = new NestedObject { NonNullableDouble = 55.5 };
        setter(obj, newNestedObject);
        Assert.Equal(newNestedObject, obj.ComplexObject);
    }

    [Fact]
    public void CreateGetter_NullableNestedObject_ComplexObjectNull()
    {
        var nested = new NestedObject { NullableDouble = 15.5 };
        var obj = new TestObject { ComplexObjectNull = nested };
        var getter = PropertyHelper.CreateGetter<TestObject, NestedObject?>(x => x.ComplexObjectNull);
        var value = getter(obj);
        Assert.NotNull(value);
        Assert.Equal(nested, value);
        Assert.Equal(15.5, value.NullableDouble);
    }

    [Fact]
    public void CreateSetter_NullableNestedObject_ComplexObjectNull()
    {
        var obj = new TestObject();
        var setter = PropertyHelper.CreateSetter<TestObject, NestedObject?>(x => x.ComplexObjectNull);
        var newNestedObject = new NestedObject { NullableDouble = 60.5 };
        setter(obj, newNestedObject);
        Assert.Equal(newNestedObject, obj.ComplexObjectNull);
    }

    [Fact]
    public void CreateGetter_String_ReferenceType_NullObject()
    {
        var getter = PropertyHelper.CreateGetter<TestObject, string>(x => x.ReferenceType);
        var value = getter(null!);
        Assert.Null(value);
    }
}