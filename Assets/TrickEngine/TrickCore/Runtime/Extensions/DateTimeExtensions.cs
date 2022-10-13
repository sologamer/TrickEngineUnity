using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TrickCore
{
    public static class DateTimeExtensions
    {
        public static DateTime FirstDayOfTheWeek(this DateTime dateTime)
        {
            var date = dateTime.Date;
            int week = WeekOfYearISO8601(date);
            int offset = 0;
            while(week == WeekOfYearISO8601(date.AddDays((--offset))))
            {
	
            }
            return date.AddDays(offset + 1);
        }

        public static DateTime LastDayOfTheWeek(this DateTime dateTime)
        {
            return FirstDayOfTheWeek(dateTime).AddDays(7);
        }

        public static int WeekOfYearISO8601(this DateTime date)
        {
            var day = (int) System.Globalization.CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(date);
            return System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                date.AddDays(4 - (day == 0 ? 7 : day)), System.Globalization.CalendarWeekRule.FirstFourDayWeek,
                DayOfWeek.Monday);
        }
        
        public static DateTime StartDateOfTheWeek(this DateTime date)
        {
            int mod = date.Date.DayOfYear % 7;
            return date.Date.AddDays(-mod);
        }

        public static DateTime NowOrNext(this DateTime from, bool startOfDay, DayOfWeek dayOfWeek)
        {
            int start = (int)from.DayOfWeek;
            int target = (int)dayOfWeek;
            if (start == target && (startOfDay ? DateTime.UtcNow.Date : DateTime.UtcNow) <= from)
                return from;
            if (target <= start)
                target += 7;
            return from.AddDays(target - start);
        }
    
        public static DateTime BetweenNowOrNext(this DateTime from, TimeSpan duration, DayOfWeek dayOfWeek)
        {
            int start = (int)from.DayOfWeek;
            int target = (int)dayOfWeek;
            if (start == target && from.InBetween(from.Add(duration))) return from;
            if (target <= start)
                target += 7;
            return from.AddDays(target - start);
        }
    
        public static DateTime BetweenNowOrNext(this DateTime from, DateTime to, DayOfWeek dayOfWeek)
        {
            int start = (int)from.DayOfWeek;
            int target = (int)dayOfWeek;
            if (start == target && from.InBetween(to)) return from;
            if (target <= start)
                target += 7;
            return from.AddDays(target - start);
        }
    
        public static bool InBetween(this DateTime a, DateTime b)
        {
            return a > b ? DateTime.UtcNow >= b && DateTime.UtcNow < a : DateTime.UtcNow >= a && DateTime.UtcNow < b;
        }
    
        public static bool InBetween(this DateTime a, DateTime b, DateTime currentTime)
        {
            return a > b ? currentTime >= b && currentTime < a : currentTime >= a && currentTime < b;
        }

        public static DateTime Floor(this DateTime dateTime, TimeSpan interval)
        {
            return dateTime.AddTicks(-(dateTime.Ticks % interval.Ticks));
        }

        public static DateTime Ceiling(this DateTime dateTime, TimeSpan interval)
        {
            var overflow = dateTime.Ticks % interval.Ticks;
            return overflow == 0 ? dateTime : dateTime.AddTicks(interval.Ticks - overflow);
        }

        public static DateTime Round(this DateTime dateTime, TimeSpan interval)
        {
            var halfIntervalTicks = (interval.Ticks + 1) >> 1;
            return dateTime.AddTicks(halfIntervalTicks - ((dateTime.Ticks + halfIntervalTicks) % interval.Ticks));
        }
    
        public static TimeSpan Floor(this TimeSpan timeSpan, TimeSpan interval)
        {
            DateTime dateTime = new DateTime() + timeSpan;
            return dateTime.AddTicks(-(dateTime.Ticks % interval.Ticks)).TimeOfDay;
        }

        public static TimeSpan Ceiling(this TimeSpan timeSpan, TimeSpan interval)
        {
            DateTime dateTime = new DateTime() + timeSpan;
            var overflow = dateTime.Ticks % interval.Ticks;
            return overflow == 0 ? dateTime.TimeOfDay : dateTime.AddTicks(interval.Ticks - overflow).TimeOfDay;
        }

        public static TimeSpan Round(this TimeSpan timeSpan, TimeSpan interval)
        {
            DateTime dateTime = new DateTime() + timeSpan;
            var halfIntervalTicks = (interval.Ticks + 1) >> 1;
            return dateTime.AddTicks(halfIntervalTicks - ((dateTime.Ticks + halfIntervalTicks) % interval.Ticks)).TimeOfDay;
        }
        
        /// <summary>
        /// January = Q1
        /// </summary>
        public static int GetQuarter(this DateTime date)
        {
            return (date.Month + 2)/3;
        }
        
        public static void GetNextQuarterDate(this DateTime date, out DateTime firstDayOfQuarter, out DateTime lastDayOfQuarter)
        {
            date = date.AddMonths(3);
            int quarterNumber = (date.Month-1)/3+1;
            firstDayOfQuarter = new DateTime(date.Year, (quarterNumber-1)*3+1,1);
            lastDayOfQuarter = firstDayOfQuarter.AddMonths(3).AddDays(-1);
        }
        
        
        
        /// https://stackoverflow.com/questions/11/calculate-relative-time-in-c-sharp
        private static readonly SortedList<double, Func<TimeSpan, string>> Offsets = 
            new SortedList<double, Func<TimeSpan, string>>
            {
                { 0, x => $"{x.TotalSeconds:F0} seconds"},
                { 45, x => $"{x.TotalMinutes:F0} minutes"},
                { 1440, x => $"{x.TotalHours:F0} hours"},
                { 43200, x => $"{x.TotalDays:F0} days"},
                { 525600, x => $"{x.TotalDays / 30:F0} months"},
                { double.MaxValue, x => $"{x.TotalDays / 365:F0} years"}
            };

        /// <summary>
        /// Prettify string to a relative date string (x minutes ago, x hours ago)
        /// </summary>
        /// <param name="currentDateTime"></param>
        /// <param name="compareTime"></param>
        /// <returns></returns>
        public static string ToRelativeDate(this DateTime currentDateTime, DateTime compareTime)
        {
            TimeSpan x = currentDateTime - compareTime;
            string suffix = x.TotalMinutes > 0 ? " ago" : " from now";
            x = new TimeSpan(Math.Abs(x.Ticks));
            return Offsets.First(n => x.TotalMinutes < n.Key).Value(x) + suffix;
        }

        /// <summary>
        /// Prettify string to a relative date string (x minutes ago, x hours ago)
        /// </summary>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        public static string ToRelativeDate(this TimeSpan timeSpan)
        {
            string suffix = timeSpan.TotalMinutes > 0 ? " ago" : " from now";
            timeSpan = new TimeSpan(Math.Abs(timeSpan.Ticks));
            return Offsets.First(n => timeSpan.TotalMinutes < n.Key).Value(timeSpan) + suffix;
        }
    }
}