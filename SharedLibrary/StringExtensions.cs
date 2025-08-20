using System.Text.RegularExpressions;

namespace SharedLibrary.Extensions;

    public static class StringExtensions
    {
        public static string Normalize(this string input, string separator = "_")
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            return Regex.Replace(input.Trim(), @"\s+", "_").ToLowerInvariant();
        }
    }

