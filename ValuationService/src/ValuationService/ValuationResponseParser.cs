using System.Text.Json;

namespace ValuationService.Service;

internal static class ValuationResponseParser
{
    public static decimal? ParseValueFromMcpResponse(string responseJson)
    {
        if (string.IsNullOrWhiteSpace(responseJson)) return null;

        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            if (doc.RootElement.ValueKind == JsonValueKind.Number)
            {
                decimal directVal = doc.RootElement.GetDecimal();
                if (directVal != 2.0m && directVal <= 1000000)
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
                    if (directVal != 2.0m && directVal <= 1000000)
                        return directVal;
                }

                var matches = System.Text.RegularExpressions.Regex.Matches(text, @"(\d+(\.\d+)?)");
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (decimal.TryParse(match.Value, out decimal val))
                    {
                        if (val == 2.0m || val > 1000000) continue;
                        return val;
                    }
                }
            }
            else
            {
                // Fallback for JSON without 'result' property
                // Try to extract from the whole JSON string
                var matches = System.Text.RegularExpressions.Regex.Matches(responseJson, @"(\d+(\.\d+)?)");
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (decimal.TryParse(match.Value, out decimal val))
                    {
                        if (val == 2.0m || val > 1000000) continue;
                        return val;
                    }
                }
            }
        }
        catch (JsonException)
        {
            // If it's not valid JSON, treat it as raw text
            if (decimal.TryParse(responseJson, out decimal directVal))
            {
                if (directVal != 2.0m && directVal <= 1000000)
                    return directVal;
            }
            
            var matches = System.Text.RegularExpressions.Regex.Matches(responseJson, @"(\d+(\.\d+)?)");
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (decimal.TryParse(match.Value, out decimal val))
                {
                    if (val == 2.0m || val > 1000000) continue;
                    return val;
                }
            }
        }
        catch
        {
        }

        return null;
    }
}
