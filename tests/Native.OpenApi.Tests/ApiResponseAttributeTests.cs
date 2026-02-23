namespace Native.OpenApi.Tests;

public class ApiResponseAttributeTests
{
    [Fact]
    public void Constructor_WithAllParameters_ShouldStoreAllProperties()
    {
        // Arrange & Act
        var attribute = new ApiResponseAttribute(200, typeof(string), "application/xml");

        // Assert
        attribute.StatusCode.Should().Be(200);
        attribute.ResponseType.Should().Be(typeof(string));
        attribute.ContentType.Should().Be("application/xml");
    }

    [Fact]
    public void Constructor_WithStatusCodeOnly_ShouldUseDefaults()
    {
        // Arrange & Act
        var attribute = new ApiResponseAttribute(404);

        // Assert
        attribute.StatusCode.Should().Be(404);
        attribute.ResponseType.Should().BeNull();
        attribute.ContentType.Should().Be("application/json");
    }

    [Fact]
    public void Constructor_WithStatusCodeAndType_ShouldUseDefaultContentType()
    {
        // Arrange & Act
        var attribute = new ApiResponseAttribute(201, typeof(int));

        // Assert
        attribute.StatusCode.Should().Be(201);
        attribute.ResponseType.Should().Be(typeof(int));
        attribute.ContentType.Should().Be("application/json");
    }

    [Fact]
    public void Constructor_WithNullType_ShouldAllowNullResponseType()
    {
        // Arrange & Act
        var attribute = new ApiResponseAttribute(204, null);

        // Assert
        attribute.StatusCode.Should().Be(204);
        attribute.ResponseType.Should().BeNull();
        attribute.ContentType.Should().Be("application/json");
    }

    [Fact]
    public void AttributeUsage_ShouldAllowMultipleOnMethod()
    {
        // Arrange
        var attributeUsage = typeof(ApiResponseAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage!.AllowMultiple.Should().BeTrue("multiple response types should be supported");
        attributeUsage.ValidOn.Should().HaveFlag(AttributeTargets.Method);
        attributeUsage.Inherited.Should().BeFalse("response attributes should not be inherited");
    }

    [Theory]
    [InlineData(200, "application/json")]
    [InlineData(201, "application/json")]
    [InlineData(204, "application/json")]
    [InlineData(400, "application/problem+json")]
    [InlineData(404, "application/problem+json")]
    [InlineData(500, "application/problem+json")]
    public void Constructor_WithVariousStatusCodes_ShouldWork(int statusCode, string contentType)
    {
        // Arrange & Act
        var attribute = new ApiResponseAttribute(statusCode, typeof(object), contentType);

        // Assert
        attribute.StatusCode.Should().Be(statusCode);
        attribute.ContentType.Should().Be(contentType);
    }

    [Fact]
    public void Constructor_WithCustomContentType_ShouldStoreCustomValue()
    {
        // Arrange
        var customContentType = "text/plain";

        // Act
        var attribute = new ApiResponseAttribute(200, typeof(string), customContentType);

        // Assert
        attribute.ContentType.Should().Be(customContentType);
    }

    [Fact]
    public void MultipleAttributes_ShouldBeAllowed()
    {
        // This test verifies the AttributeUsage allows multiple instances
        // by checking the attribute's metadata
        var type = typeof(TestClass);
        var method = type.GetMethod(nameof(TestClass.TestMethod));
        var attributes = method?.GetCustomAttributes(typeof(ApiResponseAttribute), false);

        // Assert
        attributes.Should().NotBeNull();
        attributes!.Length.Should().Be(3);
    }

    private class TestClass
    {
        [ApiResponse(200, typeof(string))]
        [ApiResponse(404)]
        [ApiResponse(500, typeof(Exception), "application/problem+json")]
        public void TestMethod() { }
    }
}
