using System;
using System.Text;
using Microsoft.SPOT;

namespace MyHome.Extensions
{
    public static class StringExtensions
    {
        public static bool IsNullOrEmpty(this string source)
        {
            return source == null || source == string.Empty;
        }

        public static string Remove(this string source, string value)
        {
            var index = source.IndexOf(value);
            var start = index > -1 ? index + value.Length : 0;
            var end = source.Length - start;
            return source.Substring(start, end);
        }

        public static string Replace(this string source, char oldValue, char newValue)
        {
            var builder = new StringBuilder(source);
            builder.Replace(oldValue, newValue);

            return builder.ToString();
        }

        public static string Replace(this string source, string oldValue, string newValue)
        {
            if (oldValue == null)
                throw new ArgumentNullException("oldValue");

            if (newValue == null)
                throw new ArgumentNullException("newValue");

            var builder = new StringBuilder(source);
            builder.Replace(oldValue, newValue);

            return builder.ToString();
        }

        public static string ReplaceAll(this string source, char oldValue, char newValue)
        {
            var builder = new StringBuilder(source);
            int index = 0;
            do
            {
                index = source.IndexOf(oldValue, index);
                builder.Replace(oldValue, newValue);
                if (index != -1)
                {
                    index++;
                }
            } while (index > -1 && index < source.Length);

            return builder.ToString();
        }

        public static string ReplaceAll(this string source, string oldValue, string newValue)
        {
            if (oldValue == null)
                throw new ArgumentNullException("oldValue");

            if (newValue == null)
                throw new ArgumentNullException("newValue");

            var builder = new StringBuilder(source);
            int index = 0;
            do
            {
                index = source.IndexOf(oldValue, index);
                builder.Replace(oldValue, newValue);
                if (index != -1)
                {
                    index += newValue.Length;
                }
            } while (index > -1 && index < source.Length);

            return builder.ToString();
        }

        public static bool StartsWith(this string source, string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            return source.IndexOf(value) == 0;
        }
    }
}
