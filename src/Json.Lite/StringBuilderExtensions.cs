using System;
using System.Collections;
using System.Text;
using System.Reflection;

namespace Json.Lite
{
    internal static class StringBuilderExtensions
    {
        private const int IndentSize = 3;

        // ISO Date Format "yyyy-MM-ddTHH:mm:ss.FFFFFFFK" milliseconds have limited support by .Net MF
        private const string IsoDateFormat = "yyyy-MM-ddTHH:mm:ss.fff000+00:00";

        public static void ReplaceAll(this StringBuilder sb, string oldValue, string newValue)
        {
            var str = sb.ToString();
            if (str == null) return;
            int start = str.LastIndexOf(oldValue, 0, str.Length);
            while (start > -1)
            {
                sb.Replace(oldValue, newValue, start, oldValue.Length);
                start = str.LastIndexOf(oldValue, 0, start);
            };
        }

        public static void WriteBoolean(this StringBuilder sb, object value)
        {
            if ((bool)value) sb.Append("true");
            else sb.Append("false");
        }

        public static void WriteColon(this StringBuilder sb)
        {
            sb.Append(':');
        }

        public static void WriteDateTime(this StringBuilder sb, object value)
        {
            var datetime = (DateTime)value;
            WriteQuote(sb);
            sb.Append(datetime.ToString(IsoDateFormat));
            WriteQuote(sb);
        }

        public static void WriteEnumerable(this StringBuilder sb, object value, int indent)
        {
            var collection = (IEnumerable)value;
            var nextIndent = indent + IndentSize;
            WriteLeftSquareBracket(sb);

            int counter = 0;
            foreach (var item in collection)
            {
                if (counter > 0)
                {
                    WriteComma(sb);
                }

                WriteNewLine(sb);
                WriteIndent(sb, nextIndent);
                WriteSomething(sb, item, nextIndent);
                counter++;
            }

            if (counter > 0)
            {
                WriteNewLine(sb);
                WriteIndent(sb, indent);
            }

            WriteRightSquareBracket(sb);
        }

        public static void WriteComma(this StringBuilder sb)
        {
            sb.Append(',');
        }

        public static void WriteHashtable(this StringBuilder sb, object value, int indent)
        {
            var hashtable = (Hashtable)value;
            var nextIndent = indent + IndentSize;
            WriteLeftCurlyBracket(sb);

            int counter = 0;
            foreach (DictionaryEntry item in hashtable)
            {
                if (counter > 0)
                {
                    WriteComma(sb);
                }

                WriteNewLine(sb);
                WriteIndent(sb, nextIndent);
                var keyType = item.Key.GetType();
                if (keyType.IsDateTime())
                {
                    WriteDateTime(sb, item.Key);
                }
                else if (keyType.IsTimeSpan())
                {
                    WriteTimeSpan(sb, item.Key);
                }
                else if (keyType.IsString() || keyType.IsNumeric())
                {
                    WriteString(sb, item.Key);
                }
                else if (keyType.IsBoolean())
                {
                    WriteBoolean(sb, item.Key);
                }
                else
                {
                    throw new InvalidCastException("Unable to print Hashtable key, must be a primitive type.");
                }

                WriteColon(sb);
                WriteSpace(sb);
                WriteSomething(sb, item.Value, nextIndent);
                counter++;
            }

            if (counter > 0)
            {
                WriteNewLine(sb);
                WriteIndent(sb, indent);
            }

            WriteRightCurlyBracket(sb);
        }

        public static void WriteIndent(this StringBuilder sb, int indent)
        {
            for (int index = 0; index < indent; index++)
            {
                WriteSpace(sb);
            }
        }

        public static void WriteLeftCurlyBracket(this StringBuilder sb)
        {
            sb.Append('{');
        }

        public static void WriteLeftSquareBracket(this StringBuilder sb)
        {
            sb.Append('[');
        }

        public static void WriteNewLine(this StringBuilder sb)
        {
            sb.Append("\r\n");
        }

        public static void WriteNumber(this StringBuilder sb, object value)
        {
            sb.Append(value.ToString());
        }

        public static void WriteObject(this StringBuilder sb, Type type, object value, int indent)
        {
            var nextIndent = indent + IndentSize;
            WriteLeftCurlyBracket(sb);

            // .GetMembers() is not supported by .Net MF
            var properties = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            for (int index = 0; index < properties.Length; index++)
            {
                var field = properties[index];
                if (index > 0 && index < properties.Length)
                {
                    WriteComma(sb);
                }

                WriteNewLine(sb);
                WriteIndent(sb, nextIndent);

                var propertyName = field.Name.Substring(0, 1).ToLower() +
                                   field.Name.Substring(1);
                WriteString(sb, propertyName);
                WriteColon(sb);
                WriteSpace(sb);
                WriteSomething(sb, field.GetValue(value), nextIndent);
            }

            if (properties.Length > 0)
            {
                WriteNewLine(sb);
                WriteIndent(sb, indent);
            }

            WriteRightCurlyBracket(sb);
        }

        public static void WriteQuote(this StringBuilder sb)
        {
            sb.Append('"');
        }

        public static void WriteRightCurlyBracket(this StringBuilder sb)
        {
            sb.Append('}');
        }

        public static void WriteRightSquareBracket(this StringBuilder sb)
        {
            sb.Append(']');
        }

        public static void WriteSpace(this StringBuilder sb)
        {
            sb.Append(' ');
        }

        public static void WriteSomething(this StringBuilder sb, object value, int indent)
        {
            if (value == null)
            {
                sb.Append("null");
                return;
            }

            var type = value.GetType();

            if (type.IsBoolean())
            {
                WriteBoolean(sb, value);
            } 
            else if (type.IsDateTime())
            {
                WriteDateTime(sb, value);
            }
            else if (type.IsEnum)
            {
                WriteNumber(sb, value);
            }
            else if (type.IsString())
            {
                WriteString(sb, value);
            }
            else if (type.IsNumeric())
            {
                WriteNumber(sb, value);
            }
            else if (type.IsTimeSpan())
            {
                WriteTimeSpan(sb, value);
            }
            else if (type.IsHashtable())
            {
                WriteHashtable(sb, value, indent);
            }
            else if (type.IsEnumerable())
            {
                WriteEnumerable(sb, value, indent);
            }
            else if (type.IsClass || type.IsValueType) // Is class or struct, relies on other cases being handled above
            {
                WriteObject(sb, type, value, indent);
            }
            else 
            {
                throw new NotImplementedException("JSON Converter: Unhandled object type");
            }
        }

        public static void WriteString(this StringBuilder sb, object value)
        {
            WriteQuote(sb);
            var str = value.ToString();
            var safeStr = str.EscapeString();
            sb.Append(safeStr);
            WriteQuote(sb);
        }

        public static void WriteTimeSpan(this StringBuilder sb, object value)
        {
            var timespan = (TimeSpan)value;
            sb.Append(timespan.Days);
            WriteColon(sb);
            sb.Append(timespan.Minutes);
            WriteColon(sb);
            sb.Append(timespan.Seconds);
        }
    }
}
