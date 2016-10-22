using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HashLib
{
    [DebuggerStepThrough]
    internal static class ArrayExtensions
    {
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
        public static T[] SubArray<T>(this T[] a_array, int a_index, int a_count)
        {
            T[] result = new T[a_count];
            Array.Copy(a_array, a_index, result, 0, a_count);
            return result;
        }
    }
}