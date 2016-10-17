using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TomanuExtensions
{
    [DebuggerStepThrough]
    public static class IListExtensions
    {
        public static int IndexOf<T>(this IList<T> a_list, T a_element)
        {
            for (int i = 0; i < a_list.Count; i++)
            {
                if (a_element.Equals(a_list[i]))
                    return i;
            }

            return -1;
        }

        public static int IndexOf<T>(this IList<T> a_list, T a_element, IEqualityComparer<T> a_comparer)
        {
            for (int i = 0; i < a_list.Count; i++)
            {
                if (a_comparer.Equals(a_list[i], a_element))
                    return i;
            }

            return -1;
        }

        public static T RemoveLast<T>(this IList<T> a_list)
        {
            T last = a_list.Last();
            a_list.RemoveAt(a_list.Count - 1);
            return last;
        }

        public static T RemoveFirst<T>(this IList<T> a_list)
        {
            T first = a_list.First();
            a_list.RemoveAt(0);
            return first;
        }

        public static T Last<T>(this IList<T> a_list)
        {
            return a_list[a_list.Count - 1];
        }

        public static void RemoveRange<T>(this IList<T> a_list, IEnumerable<T> a_elements)
        {
            foreach (var ele in a_elements)
                a_list.Remove(ele);
        }

        public static int GetHashCode<T>(IList<T> a_list)
        {
            int hash = 0;

            foreach (var el in a_list)
                hash ^= el.GetHashCode();

            return hash;
        }

        public static bool Replace<T>(this IList<T> a_list, T a_old, T a_new)
        {
            int index = a_list.IndexOf(a_old);

            if (index == -1)
                return false;

            a_list[index] = a_new;
            return true;
        }
    }
}