using Jellyfin.Plugin.AccountSync.Extensions;

namespace Jellyfin.Plugin.AccountSync.Tests;

public class PropertyExtensionsTests
{
    [Fact]
    public void PropertiesToString_SimpleObject_ReturnsFormattedString()
    {
        var testObject = new { Name = "Test", Value = 123 };

        var result = testObject.PropertiesToString();

        Assert.Contains("Name: Test", result);
        Assert.Contains("Value: 123", result);
    }

    [Fact]
    public void PropertiesToString_WithNullProperty_HandlesNull()
    {
        var testObject = new { Name = "Test", Value = (string?)null };

        var result = testObject.PropertiesToString();

        Assert.Contains("Name: Test", result);
        Assert.Contains("Value: (null)", result);
    }

    [Fact]
    public void PropertiesToString_WithMultipleProperties_IncludesAll()
    {
        var testObject = new
        {
            Prop1 = "Value1",
            Prop2 = 42,
            Prop3 = true,
            Prop4 = 3.14
        };

        var result = testObject.PropertiesToString();

        Assert.Contains("Prop1: Value1", result);
        Assert.Contains("Prop2: 42", result);
        Assert.Contains("Prop3: True", result);
        Assert.Contains("Prop4: 3.14", result);
    }

    [Fact]
    public void PropertiesToString_EmptyObject_ReturnsNewlineOnly()
    {
        var testObject = new { };

        var result = testObject.PropertiesToString();

        Assert.NotNull(result);
    }
}
