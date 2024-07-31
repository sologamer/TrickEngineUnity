using System.Linq;

namespace TrickCore
{
    public static class LocalizationManagerExtensions
    {
        public static string LocalizationArguments(this string str, params string[] param)
        {
            return str + string.Join("", param.Select(s => $"[{s}]"));
        }
    }
}