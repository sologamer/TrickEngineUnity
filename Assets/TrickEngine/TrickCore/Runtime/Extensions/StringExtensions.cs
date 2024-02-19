using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
#if !NO_UNITY
using UnityEngine;
#endif

namespace TrickCore
{
    public static class StringExtensions
    {
        private static readonly Regex CjkCharRegex = new Regex(@"\p{IsCJKUnifiedIdeographs}");

        public static bool AnyEquals(this IEnumerable<string> enumerable, string str) => enumerable != null && enumerable.Any(s => string.Equals(s, str));

        public static bool AnyEquals(this IEnumerable<string> enumerable, string str, StringComparison comparisonType) =>
            enumerable != null && enumerable.Any(s => string.Equals(s, str, comparisonType));

        public static bool AnyEquals(this IEnumerable<string> enumerable, List<string> strings) =>
            strings != null && strings.All(s1 => enumerable != null && enumerable.Any(s => string.Equals(s, s1)));

        public static bool AnyEquals(this IEnumerable<string> enumerable, List<string> strings, StringComparison comparisonType) =>
            strings != null && strings.All(s1 => enumerable != null && enumerable.Any(s => string.Equals(s, s1, comparisonType)));

        public static string CombineIfNotNullOrEmpty(this string text, string other, string delimiter = "")
        {
            if (string.IsNullOrEmpty(other)) return text;
            return text + delimiter + other;
        }

        // https://stackoverflow.com/questions/5154970/how-do-i-create-a-hashcode-in-net-c-for-a-string-that-is-safe-to-store-in-a
        public static int GetStableHashCode(this string str)
        {
            if (string.IsNullOrEmpty(str)) return 0;
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1 || str[i + 1] == '\0')
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }

        public static string FirstCharToUpper(this string input) =>
            input switch
            {
                null => null,
                "" => input,
                _ => input[0].ToString().ToUpper() + input.Substring(1, input.Length - 1)
            };

        // https://stackoverflow.com/questions/8809354/replace-first-occurrence-of-pattern-in-a-string
        public static string ReplaceFirst(this string text, string search, string replace)
        {
            int pos = text.IndexOf(search, StringComparison.Ordinal);
            return pos < 0 ? text : text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        private static readonly Regex SplitRegex = new Regex(@"(?<=\p{L})(?=\p{N})", RegexOptions.Compiled);

        /// <summary>
        /// Split a string like "Name404" to ((string)'Name', (int)404)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static (string first, int last) SplitToStringAndNumber(this string input)
        {
            var s1 = SplitRegex.Split(input);
            var first = s1.FirstOrDefault()?.Trim('+', '-');
            var last = s1.LastOrDefault()?.Trim('+', '-');
            return (first, int.TryParse(last, out var lastValue) ? lastValue : 0);
        }

        public static bool LogicalOperationMatch(this string wildcardPattern, string input)
        {
            // First, handle wildcards in patterns that also contain numeric ranges
            if (wildcardPattern.Contains("*") && wildcardPattern.Contains("-"))
            {
                string modifiedPattern = wildcardPattern.Replace("*", ".*"); // Replace wildcard with regex equivalent
                var rangeMatch = Regex.Match(modifiedPattern, @"(.*)\D(\d+)-(\d+)(.*)");
                if (rangeMatch.Success)
                {
                    string regexPattern = $"^{rangeMatch.Groups[1].Value}\\D*(\\d+){rangeMatch.Groups[4].Value}$";
                    var match = Regex.Match(input, regexPattern);
                    if (match.Success)
                    {
                        int num = int.Parse(match.Groups[1].Value);
                        int startRange = int.Parse(rangeMatch.Groups[2].Value);
                        int endRange = int.Parse(rangeMatch.Groups[3].Value);
                        if (num >= startRange && num <= endRange)
                        {
                            return true; // Numeric part of input is within the specified range
                        }
                    }
                }
            }

            // Handling wildcard patterns explicitly
            if (wildcardPattern.Contains("*"))
            {
                // Simple startswith/endswith checks before using regex
                bool startsWithWildcard = wildcardPattern.StartsWith("*");
                bool endsWithWildcard = wildcardPattern.EndsWith("*");
                if (startsWithWildcard && endsWithWildcard)
                {
                    var corePattern = wildcardPattern.Trim('*');
                    if (input.Contains(corePattern)) return true;
                }
                else if (startsWithWildcard)
                {
                    var corePattern = wildcardPattern.TrimStart('*');
                    if (input.EndsWith(corePattern)) return true;
                }
                else if (endsWithWildcard)
                {
                    var corePattern = wildcardPattern.TrimEnd('*');
                    if (input.StartsWith(corePattern)) return true;
                }
                else
                {
                    // Fallback to regex for complex patterns
                    string regexPattern = "^" + Regex.Escape(wildcardPattern).Replace("\\*", ".*") + "$";
                    if (Regex.IsMatch(input, regexPattern)) return true;
                }
            }

            // Numeric and range comparison
            var baseAndNumber = ExtractBaseAndNumber(wildcardPattern);
            if (baseAndNumber.basePart != null && input.StartsWith(baseAndNumber.basePart))
            {
                int inputNumber = ExtractNumber(input.Replace(baseAndNumber.basePart, ""));
                if (wildcardPattern.EndsWith("+") && inputNumber >= baseAndNumber.number)
                {
                    return true;
                }
                else if (wildcardPattern.EndsWith("-") && inputNumber <= baseAndNumber.number)
                {
                    return true;
                }
                else
                {
                    var range = ExtractExplicitRange(wildcardPattern);
                    if (range.HasValue && (inputNumber >= range.Value.start && inputNumber <= range.Value.end))
                    {
                        return true;
                    }
                }
            }

            // Fallback for direct comparison
            return wildcardPattern.Equals(input, StringComparison.OrdinalIgnoreCase);
        }
        
        private static readonly Regex BaseAndNumberRegex = new Regex(@"([a-zA-Z]+)(\d+)", RegexOptions.Compiled);
        private static readonly Regex ExplicitRangeRegex = new Regex(@"(\d+)-(\d+)", RegexOptions.Compiled);
        private static readonly Regex ExtractNumberRegex = new Regex(@"\d+", RegexOptions.Compiled);

        private static (string basePart, int number) ExtractBaseAndNumber(string str)
        {
            var match = BaseAndNumberRegex.Match(str);
            if (match.Success)
            {
                return (match.Groups[1].Value, int.Parse(match.Groups[2].Value));
            }

            return (null, 0);
        }

        private static int ExtractNumber(string str)
        {
            var match = ExtractNumberRegex.Match(str);
            return match.Success ? int.Parse(match.Value) : 0;
        }

        private static (int start, int end)? ExtractExplicitRange(string str)
        {
            var match = ExplicitRangeRegex.Match(str);
            if (match.Success)
            {
                return (int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
            }

            return null;
        }

        public static bool WildcardMatch(string pattern, string input)
        {
            // Converts a wildcard pattern into a regex pattern and checks if the input matches
            var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            return Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// If the value has a decimal, we round it by the amount of decimalPlaces, otherwise just the full integer
        /// </summary>
        /// <param name="value"></param>
        /// <param name="decimalPlaces"></param>
        /// <returns></returns>
        public static string RoundIfDecimal(this float value, int decimalPlaces = 2)
        {
            return value.ToString(value % 1 == 0 ? "F0" : $"0.{new string('#', decimalPlaces)}");
        }

        public static string NoRichText(this string line)
        {
            return Regex.Replace(line, "<.*?>", string.Empty);
        }

        public static bool IsChinese(this char c)
        {
            return CjkCharRegex.IsMatch(c.ToString());
        }

        public static bool IsChinese(this string c)
        {
            return CjkCharRegex.IsMatch(c);
        }

        public static string PrefixKey(this string key)
        {
#if NO_UNITY
            return key;
#else
            return $"{Application.cloudProjectId}_{key}";
#endif
        }

        public static string AddSpacesToSentence(this string text, bool preserveAcronyms)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;
            StringBuilder newText = new StringBuilder(text.Length * 2);
            newText.Append(text[0]);
            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]))
                    if ((text[i - 1] != ' ' && !char.IsUpper(text[i - 1])) ||
                        (preserveAcronyms && char.IsUpper(text[i - 1]) &&
                         i < text.Length - 1 && !char.IsUpper(text[i + 1])))
                        newText.Append(' ');
                newText.Append(text[i]);
            }

            return newText.ToString();
        }

        public static string ToStringColor(this string str, Color color)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{str}</color>";
        }

        public static string ToStringColor(this float str, Color color, string prefix = "", string affix = "", bool flex = true, int roundBy = 0)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{prefix}{(roundBy == 0 ? Mathf.CeilToInt(str) : str).ToString(flex ? roundBy.RoundByFormatCache() : $"F{roundBy}")}{affix}</color>";
        }

        public static string ToStringColor(this int str, Color color)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{str}</color>";
        }

        private static readonly Dictionary<int, string> RoundByCache = new Dictionary<int, string>();

        public static string RoundByFormatCache(this int roundBy)
        {
            if (RoundByCache.TryGetValue(roundBy, out var cached)) return cached;
            RoundByCache.Add(roundBy, cached = $"0.{new string('#', roundBy)}");
            return cached;
        }

        public class StringNumericComparer : IComparer<string>
        {
            public int Compare(string s1, string s2)
            {
                if (s1 == null)
                    return 0;

                if (s2 == null)
                    return 0;

                int len1 = s1.Length;
                int len2 = s2.Length;
                int marker1 = 0;
                int marker2 = 0;

                // Walk through two the strings with two markers.
                while (marker1 < len1 && marker2 < len2)
                {
                    char ch1 = s1[marker1];
                    char ch2 = s2[marker2];

                    // Some buffers we can build up characters in for each chunk.
                    char[] space1 = new char[len1];
                    int loc1 = 0;
                    char[] space2 = new char[len2];
                    int loc2 = 0;

                    // Walk through all following characters that are digits or
                    // characters in BOTH strings starting at the appropriate marker.
                    // Collect char arrays.
                    do
                    {
                        space1[loc1++] = ch1;
                        marker1++;

                        if (marker1 < len1)
                        {
                            ch1 = s1[marker1];
                        }
                        else
                        {
                            break;
                        }
                    } while (char.IsDigit(ch1) == char.IsDigit(space1[0]));

                    do
                    {
                        space2[loc2++] = ch2;
                        marker2++;

                        if (marker2 < len2)
                        {
                            ch2 = s2[marker2];
                        }
                        else
                        {
                            break;
                        }
                    } while (char.IsDigit(ch2) == char.IsDigit(space2[0]));

                    // If we have collected numbers, compare them numerically.
                    // Otherwise, if we have strings, compare them alphabetically.
                    string str1 = new string(space1);
                    string str2 = new string(space2);

                    int result;

                    if (char.IsDigit(space1[0]) && char.IsDigit(space2[0]))
                    {
                        int thisNumericChunk = int.Parse(str1);
                        int thatNumericChunk = int.Parse(str2);
                        result = thisNumericChunk.CompareTo(thatNumericChunk);
                    }
                    else
                    {
                        result = string.Compare(str1, str2, StringComparison.Ordinal);
                    }

                    if (result != 0)
                    {
                        return result;
                    }
                }

                return len1 - len2;
            }
        }

        private static readonly Dictionary<Regex, string> PasswordRules = new Dictionary<Regex, string>
        {
            { new Regex(".{8,}"), "Password length must be at least 8 characters in length." }, // Password is eight or more characters long
            { new Regex("\\d"), "Password needs to contain a number." }, // Password contains numbers
            { new Regex("[a-z].*?[A-Z]|[A-Z].*?[a-z]"), "Password needs to contain a mixed case." }, // Password is mixed case
            { new Regex("[!@#$%^&*?_~-£() ]"), "Password needs to contain a special character." }, // Password has special characters
        };

        public static (int Score, int MaxScore, List<(string Feedback, bool Success)> MatchResults) TrickCheckPasswordScore(this string password, Dictionary<Regex, string> customRules = null)
        {
            customRules ??= PasswordRules;
            var matches = customRules.Select(rule => (rule.Key.Match(password), rule.Value)).ToList();
            return (matches.Sum(m => m.Item1.Success ? 1 : 0), customRules.Count, matches.Select((tuple, i) => (tuple.Item2, tuple.Item1.Success)).ToList());
        }
    }
}