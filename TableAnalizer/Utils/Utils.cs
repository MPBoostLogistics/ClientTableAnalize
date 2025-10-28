using System.Text.RegularExpressions;

namespace TableAnalizer.Utils
{
    public static class RegexUtils
    {
        public static Regex[] CreateRegexArray(this IEnumerable<string> patterns)
        {
            return [.. patterns
            .ToList()
            .Select(p => new Regex(p, RegexOptions.None, TimeSpan.FromSeconds(1)))];
        }
    }
}
