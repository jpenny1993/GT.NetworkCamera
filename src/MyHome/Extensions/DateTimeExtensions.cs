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
        public static bool IsBST(this DateTime date)
        {
            // December to February are out
            if (date.Month < 3 || date.Month > 10) { return false; }
            // April to September are in
            if (date.Month > 3 && date.Month < 10) { return true; }
            // The earliest possible day for last sunday is 25th
            // we are DST if our previous sunday was on or after the 18th.
            int previousSunday = date.Day - (int)date.DayOfWeek;
            if (date.Month == 3)
            {
                // In march the hour increases 1am
                return previousSunday >= 18 && date.Hour >= 1;
            }

            // In october the hour decreases at 2am
            return previousSunday >= 18 && date.Hour >= 2;
        }
    }
}
