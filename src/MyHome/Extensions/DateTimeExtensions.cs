using System;
using Microsoft.SPOT;

namespace MyHome.Extensions
{
    public static class DateTimeExtensions
    {
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
    }
}