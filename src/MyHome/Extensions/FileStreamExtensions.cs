using System;
using System.IO;

namespace MyHome.Extensions
{
    public static class FileStreamExtensions
    {
        public static void WriteText(this FileStream fs, string text)
        {
            var bytes = text.GetBytes();
            fs.Write(bytes, 0, text.Length);
        }

        public static void WriteText(this FileStream fs, string messageTemplate, params object[] args)
        {
            WriteText(fs, messageTemplate.Format(args));
        }
    }
}
