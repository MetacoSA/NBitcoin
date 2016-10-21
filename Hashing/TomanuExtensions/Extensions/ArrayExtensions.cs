using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using TomanuExtensions.Utils;

namespace TomanuExtensions
{
    [DebuggerStepThrough]
    public static class ArrayExtensions
    {
        /// <summary>
        /// /// True if array are exactly the same.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a_ar1"></param>
        /// <param name="a_ar2"></param>
        /// <returns></returns>
        public static bool AreSame<T>(this T[] a_ar1, T[] a_ar2)
        {
            if (Object.ReferenceEquals(a_ar1, a_ar2))
                return true;

            if (a_ar1.Length != a_ar2.Length)
                return false;

            for (int i = 0; i < a_ar1.Length; i++)
            {
                if (!a_ar1[i].Equals(a_ar2[i]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// /// True if array are exactly the same.
        /// </summary>
        /// <param name="a_ar1"></param>
        /// <param name="a_ar2"></param>
        /// <returns></returns>
        public static bool AreSame(this byte[] a_ar1, byte[] a_ar2)
        {
            if (Object.ReferenceEquals(a_ar1, a_ar2))
                return true;

            if (a_ar1.Length != a_ar2.Length)
                return false;

            for (int i = 0; i < a_ar1.Length; i++)
            {
                if (a_ar1[i] != a_ar2[i])
                    return false;
            }

            return true;
        }

        /// <summary>
        /// True if array are exactly the same.
        /// </summary>
        /// <param name="a_ar1"></param>
        /// <param name="a_ar2"></param>
        /// <returns></returns>
        public static bool AreSame(this byte[,] a_ar1, byte[,] a_ar2)
        {
            if (Object.ReferenceEquals(a_ar1, a_ar2))
                return true;

            if (a_ar1.GetLength(0) != a_ar2.GetLength(1))
                return false;

            for (int x = 0; x < a_ar1.GetLength(0); x++)
            {
                for (int y = 0; y < a_ar1.GetLength(1); y++)
                {
                    if (a_ar1[x, y] != a_ar2[x, y])
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// True if array are exactly the same.
        /// </summary>
        /// <param name="a_ar1"></param>
        /// <param name="a_ar2"></param>
        /// <returns></returns>
        public static bool AreSame(this ushort[] a_ar1, ushort[] a_ar2)
        {
            if (Object.ReferenceEquals(a_ar1, a_ar2))
                return true;

            if (a_ar1.Length != a_ar2.Length)
                return false;

            for (int i = 0; i < a_ar1.Length; i++)
            {
                if (a_ar1[i] != a_ar2[i])
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Return hash code for array. Result is xor sum of elements GetHashCode() functions.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a_ar"></param>
        /// <returns></returns>
        public static int GetHashCode<T>(T[] a_ar)
        {
            int sum = 0;

            for (int i = 0; i < a_ar.Length; i++)
                sum ^= a_ar[i].GetHashCode();

            return sum;
        }

        /// <summary>
        /// Check that this is valid index.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a_array"></param>
        /// <param name="a_index"></param>
        /// <returns></returns>
        public static bool InRange<T>(this T[] a_array, int a_index)
        {
            return (a_index >= a_array.GetLowerBound(0)) && (a_index <= a_array.GetUpperBound(0));
        }

        /// <summary>
        /// Clear array with zeroes.
        /// </summary>
        /// <param name="a_array"></param>
        public static void Clear<T>(this T[] a_array, T a_value = default(T))
        {
            for (int i = 0; i < a_array.Length; i++)
                a_array[i] = a_value;
        }

        /// <summary>
        /// Clear array with zeroes.
        /// </summary>
        /// <param name="a_array"></param>
        public static void Clear<T>(this T[,] a_array, T a_value = default(T))
        {
            for (int x = 0; x < a_array.GetLength(0); x++)
            {
                for (int y = 0; y < a_array.GetLength(1); y++)
                {
                    a_array[x, y] = a_value;
                }
            }
        }

        /// <summary>
        /// Return array stated from a_index and with a_count legth.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a_array"></param>
        /// <param name="a_index"></param>
        /// <param name="a_count"></param>
        /// <returns></returns>
        public static T[] SubArray<T>(this T[] a_array, int a_index, int a_count = -1)
        {
            if (a_count == -1)
                a_count = a_array.Length - a_index;

            T[] result = new T[a_count];
            Array.Copy(a_array, a_index, result, 0, a_count);
            return result;
        }

        /// <summary>
        /// Find index of a_element within a_array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a_array"></param>
        /// <param name="a_element"></param>
        /// <returns>
        /// Index of element or -1 if not find.
        /// </returns>
        public static int IndexOf<T>(this T[] a_array, T a_element)
        {
            for (int i = 0; i < a_array.Length; i++)
                if (Object.ReferenceEquals(a_element, a_array[i]))
                    return i;
            return -1;
        }

        /// <summary>
        /// Return first occurence of a_sun_array in a_array.
        /// </summary>
        /// <param name="a_array"></param>
        /// <param name="a_sub_array"></param>
        /// <returns></returns>
        public static int FindArrayInArray(this byte[] a_array, byte[] a_sub_array)
        {
            int i, j;

            for (j = 0; j < a_array.Length - a_sub_array.Length; j++)
            {
                for (i = 0; i < a_sub_array.Length; i++)
                {
                    if (a_array[j + i] != a_sub_array[i])
                        break;
                }

                if (i == a_sub_array.Length)
                    return j;
            }

            return -1;
        }

        public static T[] Shuffle<T>(this T[] a_array)
        {
            return Shuffle(a_array, Environment.TickCount);
        }

        public static T[] Shuffle<T>(this T[] a_array, int a_seed)
        {
            MersenneTwister mt = new MersenneTwister((uint)a_seed);

            return (from gr in
                        from el in a_array
                        select new { index = mt.NextInt(), el }
                    orderby gr.index
                    select gr.el).ToArray();
        }

        public static void Fill<T>(this T[,] a_ar, T a_value)
        {
            for (int x = 0; x < a_ar.GetLength(0); x++)
            {
                for (int y = 0; y < a_ar.GetLength(1); y++)
                {
                    a_ar[x, y] = a_value;
                }
            }
        }

        public static void Fill<T>(this T[] a_ar, T a_value)
        {
            for (int i = 0; i < a_ar.GetLength(0); i++)
                a_ar[i] = a_value;
        }

        public static IEnumerable<T> ToEnumerable<T>(this T[,] a_ar)
        {
            foreach (var el in a_ar)
                yield return el;
        }
    }
}