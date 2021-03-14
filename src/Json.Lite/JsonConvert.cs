using System;
using System.Text;

namespace Json.Lite
{
    public static class JsonConvert
    {
        /// <summary>
        /// Converts an object into a JSON string.
        /// Due to limitations in .Net MF the following features are not supported.
        /// - Dictionaries
        /// - Lists
        /// - Member properties
        /// </summary>
        public static string SerializeObject(object value)
        {
            var sb = new StringBuilder();
            sb.WriteSomething(value, 0);
            return sb.ToString();
        }
    }
}
