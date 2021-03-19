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

            foreach (var item in source)
            {
                if (!item.IsNullOrEmpty() &&
                    item.ToUpper() == str.ToUpper())
                {
                    return true;
                }
            }

            return false;
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
