using System;
using System.Text;
using Microsoft.SPOT;

namespace MyHome.Extensions
{
    public static class StringExtensions
    {
        public static bool Contains(this string source, string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            return source.IndexOf(value) > -1;
        }

        public static int Count(this string str, string value)
        {
            int count = 0;
            int start = str.LastIndexOf(value, 0, value.Length);
            while (start > -1)
            {
                start = str.LastIndexOf(value, 0, start);
            };

            return count;
        }

        public static bool EndsWith(this string source, string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            return (source.LastIndexOf(value) + value.Length) == source.Length;
        }

        public static string Format(this string messageTemplate, params object[] args)
        {
            var sb = new StringBuilder(messageTemplate);
            for (var index = 0; index < args.Length; index++)
            {
                var term = string.Concat("{", index, "}");
                sb.Replace(term, args[index].ToString());
            }

            return sb.ToString();
        }

        public static byte[] GetBytes(this string source)
        {
            return Encoding.UTF8.GetBytes(source);
        }

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
            int start = source.LastIndexOf(oldValue, 0, source.Length);
            while (start > -1)
            {
                builder.Replace(oldValue, newValue, start, 1);
                start = source.LastIndexOf(oldValue, 0, start);
            };

            return builder.ToString();
        }

        public static string ReplaceAll(this string source, string oldValue, string newValue)
        {
            if (oldValue == null)
                throw new ArgumentNullException("oldValue");

            if (newValue == null)
                throw new ArgumentNullException("newValue");

            var builder = new StringBuilder(source);
            int start = source.LastIndexOf(oldValue, 0, source.Length);
            while (start > -1)
            {
                builder.Replace(oldValue, newValue, start, oldValue.Length);
                start = source.LastIndexOf(oldValue, 0, start);
            };

            return builder.ToString();
        }

        public static bool StartsWith(this string source, string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            return source.IndexOf(value) == 0;
        }

        public static string TrimEnd(this string source, string value)
        {
            if (EndsWith(source, value))
            {
                return source.Substring(0, source.LastIndexOf(value));
            }

            return source;
        }

        public static string TrimStart(this string source, string value)
        {
            if (StartsWith(source, value))
            {
                return source.Substring(value.Length);
            }

            return source;
        }
    }
}
