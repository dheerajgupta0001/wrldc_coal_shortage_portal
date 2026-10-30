using System.Text.RegularExpressions;

namespace CoalShortagePortal.Application.Features.GenMUs.Helpers
{
    public class StationNameHelper
    {
        public static string NormalizeStationName(string stationName)
        {
            if (string.IsNullOrWhiteSpace(stationName))
                return string.Empty;

            return stationName.Trim().ToUpperInvariant();
        }

        public static string ExtractColumnLetters(string cellReference)
        {
            if (string.IsNullOrWhiteSpace(cellReference))
                return string.Empty;

            var cleaned = cellReference
                .Trim()
                .Replace("$", "")
                .ToUpperInvariant();

            var match = Regex.Match(cleaned, "^[A-Z]+");

            return match.Success ? match.Value : string.Empty;
        }

        public static int ExcelColumnNameToNumber(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                return 0;

            columnName = columnName.Trim().ToUpperInvariant();

            int sum = 0;

            foreach (char c in columnName)
            {
                if (c < 'A' || c > 'Z')
                    return 0;

                sum *= 26;
                sum += c - 'A' + 1;
            }

            return sum;
        }
    }
}