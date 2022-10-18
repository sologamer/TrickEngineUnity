using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TrickCore
{
    public class LocalizationManager : MonoSingleton<LocalizationManager>
    {
        private readonly List<string> _arguments = new List<string>();

        public string Evaluate(string originalText)
        {
            _arguments.Clear();
            var squaredOut = originalText.ReplaceAttributesSquare(SquareEvaluator);
            var processed = squaredOut.ReplaceAttributesCurly(Evaluator);
            var finalProcess = processed.ReplaceAttributesCurly(FormatEvaluator);
            return finalProcess;
        }

        private string FormatEvaluator(Match match)
        {
            return match.Value;
        }

        private string Evaluator(Match match)
        {
            return match.Value;
        }

        private string SquareEvaluator(Match match)
        {
            return match.Value;
        }
    }
    
    public static class LocalizationManagerExtensions
    {
        public static string LocalizationArguments(this string str, params string[] param)
        {
            return str + string.Join("", param.Select(s => $"[{s}]"));
        }
    }
}