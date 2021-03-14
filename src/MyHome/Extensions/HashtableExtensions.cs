using System;
using System.Collections;

namespace MyHome.Extensions
{
    public static class HashtableExtensions
    {
        public static string[] ToStringArray(this Hashtable table)
        {
            var array = new string[table.Count];
            for (int index = 0; index < table.Count; index++)
            {
                array[index] = (string)table[index];
            }

            return array;
        }
    }
}
