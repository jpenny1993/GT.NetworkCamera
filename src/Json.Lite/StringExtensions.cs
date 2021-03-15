using System;
using System.Text;

namespace Json.Lite
{
    internal static class StringExtensions
    {
        private const string Backslash = "\\";
        private static readonly char[] EscapableJsonChars = new char[] { '\\', '"' };
        private static readonly char[] InvalidJsonChars = new char[] { '\a', '\b', '\f', '\n', '\r', '\t', '\v' };

        public static string EscapeString(this string str)
        {
            var sb = new StringBuilder(str);

            // Remove invalid chars
            foreach (char c in InvalidJsonChars)
            {
                sb.ReplaceAll(c.ToString(), string.Empty);
            }

            // Replace unescaped chars
            foreach (char c in EscapableJsonChars)
            {
                sb.ReplaceAll(c.ToString(), string.Concat(Backslash, c));
            }

            return sb.ToString();
        }
    }
}
