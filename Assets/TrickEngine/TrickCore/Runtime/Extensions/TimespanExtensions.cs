using System;
using System.Text;
using UnityEngine;

namespace TrickCore
{
    public static class TimespanExtensions
    {

        public static string ToReadableString(this TimeSpan span)
        {
            string formatted = string.Format("{0}{1}{2}{3}",
                span.Duration().Days > 0 ? string.Format("{0:0} day{1}, ", span.Days, span.Days == 1 ? string.Empty : "s") : string.Empty,
                span.Duration().Hours > 0 ? string.Format("{0:0} hour{1}, ", span.Hours, span.Hours == 1 ? string.Empty : "s") : string.Empty,
                span.Duration().Minutes > 0 ? string.Format("{0:0} minute{1}, ", span.Minutes, span.Minutes == 1 ? string.Empty : "s") : string.Empty,
                span.Duration().Seconds > 0 ? string.Format("{0:0} second{1}", span.Seconds, span.Seconds == 1 ? string.Empty : "s") : string.Empty);

            if (formatted.EndsWith(", ")) formatted = formatted.Substring(0, formatted.Length - 2);

            if (string.IsNullOrEmpty(formatted)) formatted = "0 seconds";

            return formatted;
        }

        public static string ToReadableStringShort(this TimeSpan span, bool withSeconds, bool fullWords = false)
        {
            string formatted = string.Format("{0}{1}{2}{3}",
                span.Duration().Days > 0 ? $"{span.Days:0}{(fullWords ? " day(s)" : "d")} " : string.Empty,
                span.Duration().Hours > 0 ? $"{span.Hours:0}{(fullWords ? " hr(s)" : "hr")} " : string.Empty,
                span.Duration().Minutes > 0 ? $"{span.Minutes:0}{(fullWords ? " min(s)" : "min")} " : string.Empty,
                span.TotalMinutes < 1.0 || withSeconds && span.Duration().Seconds > 0 ? $"{span.Seconds:0}sec" : string.Empty);

            formatted = formatted.Trim();

            if (string.IsNullOrEmpty(formatted)) formatted = "0sec";

            return formatted;
        }

        public static string ToSmallReadableString(this TimeSpan span, bool shortened = false)
        {
            string formatted = string.Empty;
            if(span.Duration().Days > 0) formatted += $"{span.Days:0} day{(span.Days == 1 ? string.Empty : "s")}";
            else if(span.Duration().Hours > 0) formatted += $"{span.Hours:0} {(shortened ? "hr" : "hour")}{(span.Hours == 1 ? string.Empty : "s")}";
            else if (span.Duration().Minutes > 0) formatted += $"{span.Minutes:0} {(shortened ? "min" : "minute")}{(span.Minutes == 1 ? string.Empty : "s")}";
            else if (span.Duration().Seconds > 0) formatted += $"{span.Seconds:0} {(shortened ? "sec" : "second")}{(span.Seconds == 1 ? string.Empty : "s")}";
            if (string.IsNullOrEmpty(formatted)) formatted = $"0 {(shortened ? "sec" : "second")}";
            return formatted;
        }


        public static string ToLetterReadableStringDecimal(this TimeSpan span)
        {
            string formatted = string.Empty;
            var spanDuration = span.Duration();
            if (spanDuration.Days > 0) formatted += $"{span.Days:0.##}d";
            else if (spanDuration.Hours > 0) formatted += $"{span.Hours:0.##}h";
            else if (spanDuration.Minutes > 0) formatted += $"{span.Minutes:0.##}m";
            else if (spanDuration.TotalSeconds > 0) formatted += $"{span.TotalSeconds:0.##}s";
            if (string.IsNullOrEmpty(formatted)) formatted = "0s";
            return formatted;
        }
        public static string ToLetterReadableString(this TimeSpan span)
        {
            string formatted = string.Empty;
            if (span.Duration().Days > 0) formatted += $"{span.Days:0.##}d";
            else if (span.Duration().Hours > 0) formatted += $"{span.Hours:0.##}h";
            else if (span.Duration().Minutes > 0) formatted += $"{span.Minutes:0.##}m";
            else if (span.Duration().Seconds > 0) formatted += $"{span.Seconds:0.##}s";
            if (string.IsNullOrEmpty(formatted)) formatted = "0s";
            return formatted;
        }

        public static string ToLetterReadableString(this TimeSpan span, Color color)
        {
            string formatted = string.Empty;
            var spanDuration = span.Duration();
            if (spanDuration.Days > 0) formatted += $"{$"{span.Days:0.##}".ToStringColor(color)}d";
            else if (spanDuration.Hours > 0) formatted += $"{$"{span.Hours:0.##}".ToStringColor(color)}h";
            else if (spanDuration.Minutes > 0) formatted += $"{$"{span.Minutes:0.##}".ToStringColor(color)}m";
            else if (spanDuration.Seconds > 0) formatted += $"{$"{span.Seconds:0.##}".ToStringColor(color)}s";
            if (string.IsNullOrEmpty(formatted)) formatted = $"{"0".ToStringColor(color)}s";
            return formatted;
        }
        
        public static string ToLetterReadableStringSeconds(this TimeSpan span, Color color)
        {
            return $"{$"{span.TotalSeconds:0.##}".ToStringColor(color)}s";
        }

        public static string ToTimeSpanClockString(this TimeSpan span, bool includeHour = false)
        {
            StringBuilder sb = new StringBuilder();
            var duration = span.Duration();
            if (duration.Days > 0) sb.Append($"{span.Days:0}:");
            if (includeHour && duration.Hours > 0) sb.Append($"{span.Hours:0,#}:");
            sb.Append($"{span.Minutes:0,#}:");
            sb.Append($"{span.Seconds:0,#}");
            return sb.ToString();
        }
    }
}