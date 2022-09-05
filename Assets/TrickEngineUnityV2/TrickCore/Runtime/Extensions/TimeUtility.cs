using System;

namespace TrickCore
{
    public static class TimeUtility
    {
        public static readonly DateTime ReferenceDate = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static int GetResetDayIndex(int days, DateTime currentTime)
        {
            var reference = ReferenceDate;
            var offset = GetDayOfReset(days, reference, 1) - GetDayOfReset(days, reference, 0);
            int index = 0;
            while(offset != null && reference < currentTime)
            {
                ++index;
                reference = reference.AddDays(offset.Value.Days);
                offset = GetDayOfReset(days, reference, 1) - GetDayOfReset(days, reference, 0);
                if (offset == null) break;
            }
            return index;
        }
    
        public static DateTime? GetDayOfReset(int days, DateTime dateTime, int offset)
        {
            return dateTime.Date.AddDays(days * offset);
        }

    }
}