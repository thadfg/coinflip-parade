using Xunit;
using ValuationService.Service;

namespace ValuationService.Tests;

public class ValuationResponseParserTests
{
    [Theory]
    [InlineData("{\"result\": \"123.45\"}", 123.45)]
    [InlineData("{\"result\": \"Current price is $15.99 on average\"}", 15.99)]
    [InlineData("{\"result\": \"$25.00\"}", 25.0)]
    [InlineData("{\"result\": \"40.00\"}", 40.0)]
    public void ParseValueFromMcpResponse_ValidResponse_ReturnsValue(string json, decimal expected)
    {
        var result = ValuationResponseParser.ParseValueFromMcpResponse(json);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ParseValueFromMcpResponse_IgnoresGuidFragment()
    {
        // 1143 is a known GUID fragment that was being incorrectly parsed
        var json = "Executed DbCommand (4ms) [Parameters=[@p1='1143...']";
        var result = ValuationResponseParser.ParseValueFromMcpResponse(json);
        Assert.Null(result);
    }

    [Fact]
    public void ParseValueFromMcpResponse_IgnoresVeryLargeNumbers()
    {
        var json = "{\"result\": \"1000001\"}";
        var result = ValuationResponseParser.ParseValueFromMcpResponse(json);
        Assert.Null(result);
    }

    [Fact]
    public void ParseValueFromMcpResponse_ErrorResponse_ReturnsNull()
    {
        var json = "{\"isError\": true, \"result\": \"Error occurred\"}";
        var result = ValuationResponseParser.ParseValueFromMcpResponse(json);
        Assert.Null(result);
    }

    [Theory]
    [InlineData("{\"result\": \"unknown\"}")]
    [InlineData("{\"result\": \"\"}")]
    [InlineData("{}")]
    [InlineData("invalid-json")]
    public void ParseValueFromMcpResponse_InvalidOrEmptyResponse_ReturnsNull(string json)
    {
        var result = ValuationResponseParser.ParseValueFromMcpResponse(json);
        Assert.Null(result);
    }

    [Theory]
    [InlineData("123.45", 123.45)]
    [InlineData("Current price is 15.99 on average", 15.99)]
    [InlineData("25", 25.0)]
    public void ParseValueFromMcpResponse_ValidNonJsonResponse_ReturnsValue(string text, decimal expected)
    {
        var result = ValuationResponseParser.ParseValueFromMcpResponse(text);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("{\"other\": \"123.45\"}", 123.45)]
    public void ParseValueFromMcpResponse_JsonWithoutResult_ReturnsValue(string json, decimal expected)
    {
        var result = ValuationResponseParser.ParseValueFromMcpResponse(json);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ParseValueFromMcpResponse_MultipleNumbers_ReturnsFirstValid()
    {
        // The implementation takes the first valid decimal that isn't filtered
        var json = "{\"result\": \"Prices: 2.0, 45.50, 60.00\"}";
        var result = ValuationResponseParser.ParseValueFromMcpResponse(json);
        Assert.Equal(45.50m, result);
    }
}
