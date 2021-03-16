using System;
using System.Collections;
using System.Text;
using Microsoft.SPOT;
using MyHome.Extensions;

namespace MyHome.Utilities
{
    public static class Path
    {
        private const char Backslash = '\\';
        private const char Forwardslash = '/';
        private const char Period = '.';
        private const string DoubleBackslash = "\\\\";
        private const string EscapeDirectory = "..";

        public static string Combine(params string[] items)
        {
            var builder = new StringBuilder();
            for (var index = 0; index < items.Length; index++)
            {
                var str = items[index];
                if (str.IsNullOrEmpty()) continue;

                // Replace things that would break the path
                str = str
                    .ReplaceAll(EscapeDirectory, string.Empty)
                    .ReplaceAll(Forwardslash, Backslash)
                    .ReplaceAll(DoubleBackslash, Backslash.ToString());

                // Add directory separators
                if (builder.Length > 0)
                {
                    builder.Append(Backslash);
                }

                builder.Append(str);
            }

            return builder.ToString();
        }

        public static string GetDirectoryName(string path)
        {
            try
            {
                return path.Substring(0, path.LastIndexOf(Backslash));
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string GetFileExtension(string path)
        {
            try
            {
                var index = path.LastIndexOf(Period, (path.LastIndexOf(Backslash) + 1));
                var length = path.Length - index;
                return path.Substring(index, length);
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string GetFilename(string path)
        {
            try
            {
                var start = path.LastIndexOf(Backslash) + 1;
                var end = path.LastIndexOf(Period) - start;
                return path.Substring(start, end);
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string GetFilenameWithExtension(string path)
        {
            try
            {
                var index = path.LastIndexOf(Backslash) + 1;
                var length = path.Length - index;
                return path.Substring(index, length);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
