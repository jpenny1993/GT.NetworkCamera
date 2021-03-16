using System;
using System.Collections;
using System.Reflection;

namespace Json.Lite
{
    internal static class TypeExtensions
    {
        public static bool IsBoolean(this Type type)
        {
            return type == typeof(Boolean);
        }

        public static bool IsDateTime(this Type type)
        {
            return type == typeof(DateTime);
        }

        public static bool IsEnumerable(this Type type)
        {
            // interfaceType.IsAssignableFrom(type) is not supported in .Net MF
            return type.IsArray || type == typeof(ArrayList);
        }

        public static bool IsHashtable(this Type type)
        {
            return type == typeof(Hashtable);
        }

        public static bool IsNumeric(this Type type)
        {
            // Type.GetTypeCode() is not supported in .Net MF
            // Neither is System.Decimal
            return type == typeof(Byte)    ||
                   type == typeof(SByte)   ||
                   type == typeof(UInt16)  ||
                   type == typeof(UInt32)  ||
                   type == typeof(UInt64)  ||
                   type == typeof(Int16)   ||
                   type == typeof(Int32)   ||
                   type == typeof(Int64)   ||
                   type == typeof(Double)  ||
                   type == typeof(Single);
        }

        public static bool IsString(this Type type)
        {
            return type == typeof(String) ||
                   type == typeof(Char);
        }

        public static bool IsTimeSpan(this Type type)
        {
            return type == typeof(TimeSpan);
        }
    }
}
