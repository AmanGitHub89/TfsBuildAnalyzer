using System;
using System.Collections.Generic;
using System.Linq;


namespace TfsBuildAnalyzer.Utilities
{
    internal static class UtilityMethods
    {
        public static bool CaseInsensitiveContains(this string mainString, string containsText)
        {
            return mainString.IndexOf(containsText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool CaseInsensitiveContains(this List<string> stringList, string containsText)
        {
            return stringList.Any(x => x.Equals(containsText, StringComparison.OrdinalIgnoreCase));
        }

        public static bool CaseInsensitiveEquals(this string mainString, string compareString)
        {
            return mainString.Equals(compareString, StringComparison.OrdinalIgnoreCase);
        }

        public static List<string> CommaSeparatedStringToList(string input)
        {
            return input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList().Select(x => x.Trim()).ToList();
        }

        public static string ListToCommaSeparatedString(List<string> input)
        {
            return string.Join(",", input);
        }
    }
}
