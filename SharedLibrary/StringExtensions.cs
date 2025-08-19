namespace SharedLibrary.SharedLibrary.Extensions
{
    public static class StringExtensions
    {
        public static string Normalize(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            return Regex.Replace(input.Trim(), @"\s+", "_").ToLowerInvariant();
        }
    }
}
