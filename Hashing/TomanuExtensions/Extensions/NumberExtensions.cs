using System.Diagnostics;

namespace TomanuExtensions
{
    [DebuggerStepThrough]
    public static class NumberExtensions
    {
        /// <summary>
        /// Extension method may cause problem: -d.InRange(a, b) means -(d.InRange(a,b))
        /// </summary>
        /// <param name="a_value"></param>
        /// <param name="a_min_inclusive"></param>
        /// <param name="a_max_inclusive"></param>
        /// <returns></returns>
        public static bool InRange(int a_value, int a_min_inclusive, int a_max_inclusive)
        {
            Debug.Assert(a_min_inclusive <= a_max_inclusive);

            return (a_value >= a_min_inclusive) && (a_value <= a_max_inclusive);
        }

        /// <summary>
        /// Extension method may cause problem: -d.InRange(a, b) means -(d.InRange(a,b))
        /// </summary>
        /// <param name="a_value"></param>
        /// <param name="a_min_inclusive"></param>
        /// <param name="a_max_inclusive"></param>
        /// <returns></returns>
        public static bool InRange(uint a_value, uint a_min_inclusive, uint a_max_inclusive)
        {
            Debug.Assert(a_min_inclusive <= a_max_inclusive);

            return (a_value >= a_min_inclusive) && (a_value <= a_max_inclusive);
        }

        /// <summary>
        /// Extension method may cause problem: -d.InRange(a, b) means -(d.InRange(a,b))
        /// </summary>
        /// <param name="a_value"></param>
        /// <param name="a_min_inclusive"></param>
        /// <param name="a_max_inclusive"></param>
        /// <returns></returns>
        public static bool InRange(byte a_value, byte a_min_inclusive, byte a_max_inclusive)
        {
            Debug.Assert(a_min_inclusive <= a_max_inclusive);

            return (a_value >= a_min_inclusive) && (a_value <= a_max_inclusive);
        }

        /// <summary>
        /// Extension method may cause problem: -d.InRange(a, b) means -(d.InRange(a,b))
        /// </summary>
        /// <param name="a_value"></param>
        /// <param name="a_min_inclusive"></param>
        /// <param name="a_max_inclusive"></param>
        /// <returns></returns>
        public static bool InRange(sbyte a_value, sbyte a_min_inclusive, sbyte a_max_inclusive)
        {
            Debug.Assert(a_min_inclusive <= a_max_inclusive);

            return (a_value >= a_min_inclusive) && (a_value <= a_max_inclusive);
        }

        /// <summary>
        /// Extension method may cause problem: -d.InRange(a, b) means -(d.InRange(a,b))
        /// </summary>
        /// <param name="a_value"></param>
        /// <param name="a_min_inclusive"></param>
        /// <param name="a_max_inclusive"></param>
        /// <returns></returns>
        public static bool InRange(short a_value, short a_min_inclusive, short a_max_inclusive)
        {
            Debug.Assert(a_min_inclusive <= a_max_inclusive);

            return (a_value >= a_min_inclusive) && (a_value <= a_max_inclusive);
        }

        /// <summary>
        /// Extension method may cause problem: -d.InRange(a, b) means -(d.InRange(a,b))
        /// </summary>
        /// <param name="a_value"></param>
        /// <param name="a_min_inclusive"></param>
        /// <param name="a_max_inclusive"></param>
        /// <returns></returns>
        public static bool InRange(ushort a_value, ushort a_min_inclusive, ushort a_max_inclusive)
        {
            Debug.Assert(a_min_inclusive <= a_max_inclusive);

            return (a_value >= a_min_inclusive) && (a_value <= a_max_inclusive);
        }

        /// <summary>
        /// Extension method may cause problem: -d.InRange(a, b) means -(d.InRange(a,b))
        /// </summary>
        /// <param name="a_value"></param>
        /// <param name="a_min_inclusive"></param>
        /// <param name="a_max_inclusive"></param>
        /// <returns></returns>
        public static bool InRange(long a_value, long a_min_inclusive, long a_max_inclusive)
        {
            Debug.Assert(a_min_inclusive <= a_max_inclusive);

            return (a_value >= a_min_inclusive) && (a_value <= a_max_inclusive);
        }

        /// <summary>
        /// Extension method may cause problem: -d.InRange(a, b) means -(d.InRange(a,b))
        /// </summary>
        /// <param name="a_value"></param>
        /// <param name="a_min_inclusive"></param>
        /// <param name="a_max_inclusive"></param>
        /// <returns></returns>
        public static bool InRange(ulong a_value, ulong a_min_inclusive, ulong a_max_inclusive)
        {
            Debug.Assert(a_min_inclusive <= a_max_inclusive);

            return (a_value >= a_min_inclusive) && (a_value <= a_max_inclusive);
        }

        /// <summary>
        /// Extension method may cause problem: -d.Limit(a, b) means -(d.Limit(a,b))
        /// </summary>
        /// <param name="a_d"></param>
        /// <param name="a_min_inclusive"></param>
        /// <param name="a_max_inclusive"></param>
        /// <returns></returns>
        public static int Limit(int a_d, int a_min_inclusive,
            int a_max_inclusive)
        {
            Debug.Assert(a_min_inclusive <= a_max_inclusive);

            if (a_d < a_min_inclusive)
                return a_min_inclusive;
            else if (a_d > a_max_inclusive)
                return a_max_inclusive;
            else
                return a_d;
        }

        /// <summary>
        /// Extension method may cause problem: -d.Limit(a, b) means -(d.Limit(a,b))
        /// </summary>
        /// <param name="a_d"></param>
        /// <param name="a_min_inclusive"></param>
        /// <param name="a_max_inclusive"></param>
        /// <returns></returns>
        public static uint Limit(uint a_d, uint a_min_inclusive,
            uint a_max_inclusive)
        {
            Debug.Assert(a_min_inclusive <= a_max_inclusive);

            if (a_d < a_min_inclusive)
                return a_min_inclusive;
            else if (a_d > a_max_inclusive)
                return a_max_inclusive;
            else
                return a_d;
        }

        /// <summary>
        /// Extension method may cause problem: -d.Limit(a, b) means -(d.Limit(a,b))
        /// </summary>
        /// <param name="a_d"></param>
        /// <param name="a_min_inclusive"></param>
        /// <param name="a_max_inclusive"></param>
        /// <returns></returns>
        public static byte Limit(byte a_d, byte a_min_inclusive,
            byte a_max_inclusive)
        {
            Debug.Assert(a_min_inclusive <= a_max_inclusive);

            if (a_d < a_min_inclusive)
                return a_min_inclusive;
            else if (a_d > a_max_inclusive)
                return a_max_inclusive;
            else
                return a_d;
        }

        /// <summary>
        /// Extension method may cause problem: -d.Limit(a, b) means -(d.Limit(a,b))
        /// </summary>
        /// <param name="a_d"></param>
        /// <param name="a_min_inclusive"></param>
        /// <param name="a_max_inclusive"></param>
        /// <returns></returns>
        public static sbyte Limit(sbyte a_d, sbyte a_min_inclusive,
            sbyte a_max_inclusive)
        {
            Debug.Assert(a_min_inclusive <= a_max_inclusive);

            if (a_d < a_min_inclusive)
                return a_min_inclusive;
            else if (a_d > a_max_inclusive)
                return a_max_inclusive;
            else
                return a_d;
        }

        /// <summary>
        /// Extension method may cause problem: -d.Limit(a, b) means -(d.Limit(a,b))
        /// </summary>
        /// <param name="a_d"></param>
        /// <param name="a_min_inclusive"></param>
        /// <param name="a_max_inclusive"></param>
        /// <returns></returns>
        public static short Limit(short a_d, short a_min_inclusive,
            short a_max_inclusive)
        {
            Debug.Assert(a_min_inclusive <= a_max_inclusive);

            if (a_d < a_min_inclusive)
                return a_min_inclusive;
            else if (a_d > a_max_inclusive)
                return a_max_inclusive;
            else
                return a_d;
        }

        /// <summary>
        /// Extension method may cause problem: -d.Limit(a, b) means -(d.Limit(a,b))
        /// </summary>
        /// <param name="a_d"></param>
        /// <param name="a_min_inclusive"></param>
        /// <param name="a_max_inclusive"></param>
        /// <returns></returns>
        public static ushort Limit(ushort a_d, ushort a_min_inclusive,
            ushort a_max_inclusive)
        {
            Debug.Assert(a_min_inclusive <= a_max_inclusive);

            if (a_d < a_min_inclusive)
                return a_min_inclusive;
            else if (a_d > a_max_inclusive)
                return a_max_inclusive;
            else
                return a_d;
        }

        /// <summary>
        /// Extension method may cause problem: -d.Limit(a, b) means -(d.Limit(a,b))
        /// </summary>
        /// <param name="a_d"></param>
        /// <param name="a_min_inclusive"></param>
        /// <param name="a_max_inclusive"></param>
        /// <returns></returns>
        public static long Limit(long a_d, long a_min_inclusive,
            long a_max_inclusive)
        {
            Debug.Assert(a_min_inclusive <= a_max_inclusive);

            if (a_d < a_min_inclusive)
                return a_min_inclusive;
            else if (a_d > a_max_inclusive)
                return a_max_inclusive;
            else
                return a_d;
        }

        /// <summary>
        /// Extension method may cause problem: -d.Limit(a, b) means -(d.Limit(a,b))
        /// </summary>
        /// <param name="a_d"></param>
        /// <param name="a_min_inclusive"></param>
        /// <param name="a_max_inclusive"></param>
        /// <returns></returns>
        public static ulong Limit(ulong a_d, ulong a_min_inclusive,
            ulong a_max_inclusive)
        {
            Debug.Assert(a_min_inclusive <= a_max_inclusive);

            if (a_d < a_min_inclusive)
                return a_min_inclusive;
            else if (a_d > a_max_inclusive)
                return a_max_inclusive;
            else
                return a_d;
        }
    }
}