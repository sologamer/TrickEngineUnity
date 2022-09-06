using System;

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
    }
}