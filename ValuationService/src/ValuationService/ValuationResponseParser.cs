using System.Text.Json;

namespace ValuationService.Service;

internal static class ValuationResponseParser
{
    public static decimal? ParseValueFromMcpResponse(string responseJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            if (doc.RootElement.TryGetProperty("result", out var resultElement))
            {
                if (doc.RootElement.TryGetProperty("isError", out var isError) && isError.GetBoolean())
                {
                    return null;
                }

                string text = resultElement.ToString();
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
        }
        catch
        {
        }

        return null;
    }
}
