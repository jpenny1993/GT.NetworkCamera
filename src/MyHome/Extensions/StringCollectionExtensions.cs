using System;
using System.Collections;
using Microsoft.SPOT;

namespace MyHome.Extensions
{
    public static class StringCollectionExtensions
    {
        public static bool Contains(this string[] source, string str)
        {
            if (source == null) 
            {
                throw new ArgumentNullException("source", "String[] cannot be null.");
            }

            foreach (var item in source)
            {
                if (item == str)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ContainsCaseInsensitive(this string[] source, string str)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source", "String[] cannot be null.");
            }

            var uStr = !str.IsNullOrEmpty()
                ? str.ToUpper()
                : str;
            foreach (var item in source)
            {
                var uItem = !item.IsNullOrEmpty()
                    ? item.ToUpper()
                    : item;
                if (uItem == uStr)
                {
                    return true;
                }
            }

            return false;
        }

        public static int IndexOfCaseInsensitive(this string[] source, string str)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source", "String[] cannot be null.");
            }
            var uStr = !str.IsNullOrEmpty()
                ? str.ToUpper()
                : str;
            for (var i = 0; i < source.Length; i++)
            {
                var item = source[i];
                var uItem = !item.IsNullOrEmpty()
                    ? item.ToUpper()
                    : item;
                if (uItem == uStr)
                {
                    return i;
                }
            }

            return -1;
        }

        public static string[] ToStringArray(this ArrayList source)
        {
            var array = new string[source.Count];
            for (var index = 0; index < source.Count; index++)
            {
                array[index] = (string)source[index];
            }
            return array;
        }
    }
}
