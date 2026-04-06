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
            
            // Check for isError flag
            if (doc.RootElement.ValueKind == JsonValueKind.Object && 
                doc.RootElement.TryGetProperty("isError", out var isErr) && 
                isErr.ValueKind == JsonValueKind.True)
            {
                return null;
            }

            if (doc.RootElement.ValueKind == JsonValueKind.Number)
            {
                decimal directVal = doc.RootElement.GetDecimal();
                if (IsValidPrice(directVal))
                    return directVal;
            }

            if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("result", out var resultElement))
            {
                string text = resultElement.ToString();
                
                // If it's just a number, return it
                if (decimal.TryParse(text, out decimal directVal))
                {
                    if (IsValidPrice(directVal))
                        return directVal;
                }

                return ExtractPriceFromText(text);
            }
            else
            {
                // Fallback for JSON without 'result' property
                // Try to extract from the whole JSON string, but be VERY conservative
                return ExtractPriceFromText(responseJson);
            }
        }
        catch (JsonException)
        {
            // If it's not valid JSON, treat it as raw text
            if (decimal.TryParse(responseJson, out decimal directVal))
            {
                if (IsValidPrice(directVal))
                    return directVal;
            }
            
            return ExtractPriceFromText(responseJson);
        }
        catch
        {
        }

        return null;
    }

    private static bool IsValidPrice(decimal val)
    {
        // Ignore 2.0 (often a default or placeholder in some tools)
        // Ignore values > 1,000,000 (likely not a single comic price)
        // Ignore very small values < 0.1 (likely not a price)
        // Ignore specific suspicious values like 1143 (a known GUID fragment)
        if (val == 2.0m || val > 1000000m || val < 0.1m) return false;
        if ((int)val == 1143) return false; 
        
        return true;
    }

    private static decimal? ExtractPriceFromText(string text)
    {
        // Try to find something that looks like a price ($12.34 or 12.34)
        // Avoid extracting from GUIDs or long numeric strings
        var matches = System.Text.RegularExpressions.Regex.Matches(text, @"\$?\s*(\d+(\.\d{2}))");
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            if (decimal.TryParse(match.Groups[1].Value, out decimal val))
            {
                if (IsValidPrice(val))
                    return val;
            }
        }
        
        // Fallback to simpler decimal if no $ or .XX found, but be more restrictive
        var simpleMatches = System.Text.RegularExpressions.Regex.Matches(text, @"\b(\d+(\.\d+)?)\b");
        foreach (System.Text.RegularExpressions.Match match in simpleMatches)
        {
            if (decimal.TryParse(match.Value, out decimal val))
            {
                // If it's an integer, only accept it if it's within a reasonable range for a comic
                if (val % 1 == 0)
                {
                    if (val > 5m && val < 5000m && IsValidPrice(val))
                        return val;
                }
                else if (IsValidPrice(val))
                {
                    return val;
                }
            }
        }

        return null;
    }
}
