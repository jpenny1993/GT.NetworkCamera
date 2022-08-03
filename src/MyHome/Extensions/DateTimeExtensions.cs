using System;

namespace MyHome.Extensions
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Returns a timestamp as a string in the following format; yyMMdd.
        /// </summary>
        public static string Datestamp(this DateTime datetime)
        {
            return datetime.ToString("yyMMdd");
        }

        /// <summary>
        /// Returns a timestamp as a string in the following format; yyMM.
        /// </summary>
        public static string Monthstamp(this DateTime datetime)
        {
            return datetime.ToString("yyMM");
        }

        /// <summary>
        /// Returns a a string in the following format; HH:mm.
        /// </summary>
        public static string TimeOfDay(this DateTime datetime)
        {
            return datetime.ToString("HH:mm");
        }

        /// <summary>
        /// Returns true if the current date is British Summer Time.
        /// Always changes on the last Sunday of March and last Sunday of October.
        /// </summary>
        public static bool IsBST(this DateTime datetime)
        {
            // November to February are GMT
            if (datetime.Month < 3 || datetime.Month > 10) { return false; }

            // April to September are BST
            if (datetime.Month > 3 && datetime.Month < 10) { return true; }

            // March and October both have 31 days in the month
            var lastSundayOfMonth = new DateTime(datetime.Year, datetime.Month, 31);
            if (lastSundayOfMonth.DayOfWeek != DayOfWeek.Sunday)
            {
                lastSundayOfMonth = lastSundayOfMonth.AddDays(-(int)lastSundayOfMonth.DayOfWeek);
            }

            if (datetime.Month == 3)
            {
                // In the UK the clocks go forward 1 hour at 1am on the last Sunday in March
                lastSundayOfMonth = lastSundayOfMonth.AddHours(1);
                return datetime > lastSundayOfMonth;
            }
            else if (datetime.Month == 10)
            {
                // and back 1 hour at 2am on the last Sunday in October.
                lastSundayOfMonth = lastSundayOfMonth.AddHours(2);
                return datetime < lastSundayOfMonth;
            }

            // This should never happen
            return false;
        }

        /// <summary>
        /// Returns true if the TimeOfDay is within the given time range.
        /// </summary>
        public static bool IsInRange(this DateTime datetime, TimeSpan rangeStart, TimeSpan rangeEnd)
        {
            if (rangeStart == rangeEnd)
                return false; // Same times means disabled

            var isFromEarlyTillLate = rangeStart < rangeEnd;
            if (isFromEarlyTillLate)
            {
                 // Allow morning till evening
                return datetime.TimeOfDay >= rangeStart &&
                       datetime.TimeOfDay <= rangeEnd;
            }
            else
            {
                // Allow evening till afternoon
                return datetime.TimeOfDay >= rangeStart ||
                       datetime.TimeOfDay <= rangeEnd;
            }
        }

        /// <summary>
        /// Returns <see langword="true"/> if datetime is within the grace time of the target date time.
        /// </summary>
        public static bool IsInRange(this DateTime datetime, DateTime target, int graceTimeInMins)
        {
            var diffTicksAbs = Math.Abs(target.Ticks - datetime.Ticks);
            var graceTicks = graceTimeInMins * TimeSpan.TicksPerMinute;
            return diffTicksAbs <= graceTicks;
        }

        /// <summary>
        /// Returns a timestamp as a string in the following format; yyMMddHHmmss.
        /// </summary>
        public static string Timestamp(this DateTime datetime)
        {
            return datetime.ToString("yyMMddHHmmss");
        }

        /// <summary>
        /// Returns a datetime as a string in a sortable datetime format.
        /// </summary>
        public static string SortableDateTime(this DateTime datetime, bool includeSeconds = true)
        {
            return includeSeconds
                ? datetime.ToString("yyyy-MM-dd HH:mm:ss")
                : datetime.ToString("yyyy-MM-dd HH:mm");
        }
    }
}
