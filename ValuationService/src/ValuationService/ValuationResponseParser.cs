using System.Text.Json;
using Microsoft.Extensions.Options;
using ValuationService.Infrastructure;

namespace ValuationService.Service;

public interface IValuationResponseParser
{
    decimal? ParseValueFromMcpResponse(string responseJson);
}

public class ValuationResponseParser : IValuationResponseParser
{
    private readonly ParserOptions _options;

    public ValuationResponseParser(IOptions<ParserOptions> options)
    {
        _options = options.Value;
    }

    public decimal? ParseValueFromMcpResponse(string responseJson)
    {
        if (string.IsNullOrWhiteSpace(responseJson)) return null;

        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            if (doc.RootElement.ValueKind == JsonValueKind.Number)
            {
                decimal directVal = doc.RootElement.GetDecimal();
                if (IsValidValue(directVal))
                    return directVal;
            }

            if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("result", out var resultElement))
            {
                if (doc.RootElement.TryGetProperty("isError", out var isError) && isError.ValueKind == JsonValueKind.True)
                {
                    return null;
                }

                string text = resultElement.ToString();
                
                // If it's just a number, return it
                if (decimal.TryParse(text, out decimal directVal))
                {
                    if (IsValidValue(directVal))
                        return directVal;
                }

                var matches = System.Text.RegularExpressions.Regex.Matches(text, @"(\d+(\.\d+)?)");
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (decimal.TryParse(match.Value, out decimal val))
                    {
                        if (IsValidValue(val))
                            return val;
                    }
                }
            }
            else
            {
                // Fallback for JSON without 'result' property
                // Try to extract from the whole JSON string
                // But first check if it looks like a log message we should ignore
                if (ShouldIgnore(responseJson))
                {
                    return null;
                }

                var matches = System.Text.RegularExpressions.Regex.Matches(responseJson, @"(\d+(\.\d+)?)");
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (decimal.TryParse(match.Value, out decimal val))
                    {
                        if (IsValidValue(val))
                            return val;
                    }
                }
            }
        }
        catch (JsonException)
        {
            // If it's not valid JSON, treat it as raw text
            // But first check if it looks like a log message we should ignore
            if (ShouldIgnore(responseJson))
            {
                return null;
            }

            if (decimal.TryParse(responseJson, out decimal directVal))
            {
                if (IsValidValue(directVal))
                    return directVal;
            }
            
            var matches = System.Text.RegularExpressions.Regex.Matches(responseJson, @"(\d+(\.\d+)?)");
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (decimal.TryParse(match.Value, out decimal val))
                {
                    if (IsValidValue(val))
                        return val;
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private bool IsValidValue(decimal val)
    {
        if (val > _options.MaxValidValue) return false;
        if (_options.IgnoredValues.Contains(val)) return false;
        return true;
    }

    private bool ShouldIgnore(string response)
    {
        foreach (var marker in _options.IgnoredLogMarkers)
        {
            if (response.Contains(marker)) return true;
        }
        return false;
    }
}
