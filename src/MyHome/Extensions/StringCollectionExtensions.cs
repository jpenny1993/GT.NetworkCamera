using System;
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
    }
}
