using System.Text.RegularExpressions;

namespace TrickCore
{
    public static class RegexExtensions
    {
        private static readonly Regex GetAttributesInCurlyBracketRegex = new Regex(@"\{(.*?)\}", RegexOptions.Compiled);
        public static string ReplaceAttributesCurly(this string str, MatchEvaluator evaluator) => GetAttributesInCurlyBracketRegex.Replace(str, evaluator);
        
        private static readonly Regex GetAttributesInSquareBracketRegex = new Regex(@"\[(.*?)\]", RegexOptions.Compiled);
        public static string ReplaceAttributesSquare(this string str, MatchEvaluator evaluator) => GetAttributesInSquareBracketRegex.Replace(str, evaluator);
    }
}