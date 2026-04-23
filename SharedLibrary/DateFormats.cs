namespace SharedLibrary.Constants
{
    public static class DateFormats
    {
        public static readonly string[] AcceptedFormats = 
        { 
            "yyyy-MM-dd", "MM/dd/yyyy", "M/d/yyyy", "dd-MMM-yy", "d-MMM-yy", "MMM d, yyyy",
            "MMM yyyy" // Added to support 'Jun 2017'
        };
        
        public const string DisplayFormats = "YYYY-MM-DD, MM/DD/YYYY, M/D/YYYY, D-MMM-YY, MMM DD, YYYY, or MMM YYYY";
    }
}