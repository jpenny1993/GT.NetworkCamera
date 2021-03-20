using System;
using MyHome.Extensions;

namespace MyHome.Utilities
{
    public static class DateTimeParser
    {
        private static readonly string[] Months = new string[] { "Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec" };

        public static bool TryParse(string str, out DateTime datetime)
        {
            return ISO8601(str, out datetime) ||
                   RFC822(str, out datetime) ||
                   UnixTimestamp(str, out datetime);
        }

        /// <summary>
        /// Parse format yyMMddHHmmss
        /// Handle prefix and suffixes for files
        /// IMG_yyMMddHHmmss.bmp
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static DateTime CustomFormat(string str, string prefix = "", string suffix = "")
        {
            try
            {
                var timestamp = str.TrimStart(prefix).TrimEnd(suffix);
                var year = 2000 + int.Parse(timestamp.Substring(0, 2));
                var month = int.Parse(timestamp.Substring(2, 2));
                var day = int.Parse(timestamp.Substring(4, 2));
                var hour = int.Parse(timestamp.Substring(6, 2));
                var minute = int.Parse(timestamp.Substring(8, 2));
                var second = int.Parse(timestamp.Substring(10, 2));
                return new DateTime(year, month, day, hour, minute, second);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Parse date format
        /// 2010-08-20T15:00:00Z
        /// </summary>
        public static bool ISO8601(string str, out DateTime datetime)
        {
            var parts = str.Split('T');
            int year, month, day, hour, minute, second;

            try
            {
                if (parts.Length > 0)
                {
                    var dateParts = parts[0].Split('/');
                    year = int.Parse(dateParts[0]);
                    month = int.Parse(dateParts[1]);
                    day = int.Parse(dateParts[2]);
                }
                else 
                {
                    throw new InvalidCastException();
                }

                if (parts.Length > 1)
                {
                    var timeParts = parts[1].Split(':');
                    hour = int.Parse(timeParts[0]);
                    minute = int.Parse(timeParts[1]);
                    second = int.Parse(timeParts[2].Substring(0, 2));
                }
                else 
                {
                    hour = 0;
                    minute = 0;
                    second = 0;
                }

                datetime = new DateTime(year, month, day, hour, minute, second);
                return true;
            }
            catch
            {
                datetime = DateTime.MinValue;
            }

            return false;
        }

        /// <summary>
        /// Parse date format 
        /// Sun, 06 Jun 2010 20:07:44 +0000
        /// </summary
        public static bool RFC822(string str, out DateTime datetime)
        {
            try
            {
                int day = int.Parse(str.Substring(5, 2));
                int month = Array.IndexOf(Months, str.Substring(8, 3)) + 1;
                int year = int.Parse(str.Substring(12, 4));

                int hour = int.Parse(str.Substring(17, 2));
                int minute = int.Parse(str.Substring(20, 2));
                int second = int.Parse(str.Substring(23, 2));

                int offsetSgn = (str[26] == '-') ? -1 : 1;
                int offsetHour = int.Parse(str.Substring(27, 2));
                int offsetMinute = int.Parse(str.Substring(29, 2));

                datetime = new DateTime(year, month, day, hour, minute, second);
                return true;
            }
            catch
            {
                datetime = DateTime.MinValue;
            }

            return false;
        }

        /// <summary>
        /// Parses ticks since 1st January 1970
        /// </summary>
        public static bool UnixTimestamp(string str, out DateTime datetime)
        {
            try
            {
                var ticks = long.Parse(str);
                datetime = new DateTime(1970, 1, 1);
                datetime.AddTicks(ticks);
                return true;
            }
            catch
            {
                datetime = DateTime.MinValue;
            }

            return false;
        }
    }
}
