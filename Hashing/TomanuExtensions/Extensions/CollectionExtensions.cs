using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TomanuExtensions
{
    [DebuggerStepThrough]
    public static class CollectionExtensions
    {
        public static void RemoveRange<T>(this ICollection<T> a_collection, IEnumerable<T> a_elements)
        {
            foreach (var ele in a_elements)
                a_collection.Remove(ele);
        }

        public static void RemoveAll<T>(this ICollection<T> a_collection, Predicate<T> a_predicate)
        {
            var deleteList = a_collection.Where(child => a_predicate(child)).ToList();
            deleteList.ForEach(t => a_collection.Remove(t));
        }

        public static bool AddUnique<T>(this ICollection<T> a_collection, T a_value)
        {
            if (a_collection.Contains(a_value) == false)
            {
                a_collection.Add(a_value);
                return true;
            }
            return false;
        }

        public static int AddRangeUnique<T>(this ICollection<T> a_collection, IEnumerable<T> a_values)
        {
            var count = 0;
            foreach (var value in a_values)
            {
                if (a_collection.AddUnique(value))
                    count++;
            }
            return count;
        }
    }
}