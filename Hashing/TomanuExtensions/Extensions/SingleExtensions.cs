using System;
using System.Diagnostics;

namespace TomanuExtensions
{
    [DebuggerStepThrough]
    public static class SingleExtensions
    {
        public static bool IsNumber(this float a_d)
        {
            return !Single.IsInfinity(a_d) && !Single.IsNaN(a_d);
        }

        public static float Fraction(this float a_d)
        {
            return a_d - (float)Math.Truncate(a_d);
        }

        public static int Round(this float a_d)
        {
            return (int)Math.Round(a_d);
        }

        public static int Ceiling(this float a_d)
        {
            return (int)Math.Ceiling(a_d);
        }

        public static int Floor(this float a_d)
        {
            return (int)Math.Floor(a_d);
        }

        public static bool IsAlmostRelativeEquals(this float a_d1, float a_d2, float a_precision)
        {
            double mid = Math.Max(Math.Abs(a_d1), Math.Abs(a_d2));

            if (Double.IsInfinity(mid))
                return false;

            if (mid > a_precision)
                return Math.Abs(a_d1 - a_d2) <= a_precision * mid;
            else
                return a_d1 < a_precision;
        }

        public static bool IsAlmostEquals(this float a_d1, float a_d2, float a_precision)
        {
            return Math.Abs(a_d1 - a_d2) < a_precision;
        }

        /// <summary>
        /// Extension method may cause problem: -d.Limit(a, b) means -(d.Limit(a,b))
        /// </summary>
        /// <param name="a_d"></param>
        /// <param name="a_min_inclusive"></param>
        /// <param name="a_max_inclusive"></param>
        /// <returns></returns>
        public static float Limit(float a_d, float a_min_inclusive,
            float a_max_inclusive)
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
        /// Extension method may cause problem: -d.InRange(a, b) means -(d.InRange(a,b))
        /// </summary>
        /// <param name="a_d"></param>
        /// <param name="a_min_inclusive"></param>
        /// <param name="a_max_inclusive"></param>
        /// <returns></returns>
        public static bool InRange(float a_d, float a_min_inclusive,
            float a_max_inclusive)
        {
            Debug.Assert(a_min_inclusive <= a_max_inclusive);

            return (a_d >= a_min_inclusive) && (a_d <= a_max_inclusive);
        }

        /// <summary>
        /// Extension method may cause problem: -d.InRange(a, b) means -(d.AlmostRelvativeInRange(a,b))
        /// </summary>
        /// <param name="a_d"></param>
        /// <param name="a_min_inclusive"></param>
        /// <param name="a_max_inclusive"></param>
        /// <returns></returns>
        public static bool AlmostRelvativeInRange(float a_d, float a_min_inclusive,
            float a_max_inclusive, float a_precision)
        {
            Debug.Assert(a_min_inclusive <= a_max_inclusive);

            return a_d.IsAlmostRelativeGreaterOrEqualThen(a_min_inclusive, a_precision) &&
                a_d.IsAlmostRelativeLessOrEqualThen(a_max_inclusive, a_precision);
        }

        /// <summary>
        /// Extension method may cause problem: -d.InRange(a, b) means -(d.AlmostInRange(a,b))
        /// </summary>
        /// <param name="a_d"></param>
        /// <param name="a_min_inclusive"></param>
        /// <param name="a_max_inclusive"></param>
        /// <returns></returns>
        public static bool AlmostInRange(float a_d, float a_min_inclusive,
            float a_max_inclusive, float a_precision)
        {
            Debug.Assert(a_min_inclusive <= a_max_inclusive);

            return a_d.IsAlmostGreaterOrEqualThen(a_min_inclusive, a_precision) &&
                a_d.IsAlmostLessOrEqualThen(a_max_inclusive, a_precision);
        }

        public static bool IsAlmostRelativeLessThen(this float a_d1, float a_d2, float a_precision)
        {
            if (IsAlmostRelativeEquals(a_d1, a_d2, a_precision))
                return true;

            return a_d1 < a_d2;
        }

        public static bool IsAlmostRelativeLessOrEqualThen(this float a_d1, float a_d2, float a_precision)
        {
            if (IsAlmostRelativeEquals(a_d1, a_d2, a_precision))
                return true;

            return a_d1 <= a_d2;
        }

        public static bool IsAlmostRelativeGreaterThen(this float a_d1, float a_d2, float a_precision)
        {
            if (IsAlmostRelativeEquals(a_d1, a_d2, a_precision))
                return true;

            return a_d1 > a_d2;
        }

        public static bool IsAlmostRelativeGreaterOrEqualThen(this float a_d1, float a_d2, float a_precision)
        {
            if (IsAlmostRelativeEquals(a_d1, a_d2, a_precision))
                return true;

            return a_d1 >= a_d2;
        }

        public static float Min(float a_d1, float a_d2, float a_d3)
        {
            if (a_d1 < a_d2)
            {
                if (a_d1 < a_d3)
                    return a_d1;
                else
                    return a_d3;
            }
            else
            {
                if (a_d2 < a_d3)
                    return a_d2;
                else
                    return a_d3;
            }
        }

        public static float Min(float a_d1, float a_d2, float a_d3, float a_d4)
        {
            if (a_d1 < a_d2)
            {
                if (a_d1 < a_d3)
                {
                    if (a_d1 < a_d4)
                        return a_d1;
                    else
                        return a_d4;
                }
                else
                {
                    if (a_d3 < a_d4)
                        return a_d3;
                    else
                        return a_d4;
                }
            }
            else
            {
                if (a_d2 < a_d3)
                {
                    if (a_d2 < a_d4)
                        return a_d2;
                    else
                        return a_d4;
                }
                else
                {
                    if (a_d3 < a_d4)
                        return a_d3;
                    else
                        return a_d4;
                }
            }
        }

        public static float Max(float a_d1, float a_d2, float a_d3)
        {
            if (a_d1 > a_d2)
            {
                if (a_d1 > a_d3)
                    return a_d1;
                else
                    return a_d3;
            }
            else
            {
                if (a_d2 > a_d3)
                    return a_d2;
                else
                    return a_d3;
            }
        }

        public static float Max(float a_d1, float a_d2, float a_d3, float a_d4)
        {
            if (a_d1 > a_d2)
            {
                if (a_d1 > a_d3)
                {
                    if (a_d1 > a_d4)
                        return a_d1;
                    else
                        return a_d4;
                }
                else
                {
                    if (a_d3 > a_d4)
                        return a_d3;
                    else
                        return a_d4;
                }
            }
            else
            {
                if (a_d2 > a_d3)
                {
                    if (a_d2 > a_d4)
                        return a_d2;
                    else
                        return a_d4;
                }
                else
                {
                    if (a_d3 > a_d4)
                        return a_d3;
                    else
                        return a_d4;
                }
            }
        }

        public static bool IsAlmostLessThen(this float a_d1, float a_d2, float a_precision)
        {
            return a_d1 < a_d2 + a_precision;
        }

        public static bool IsAlmostLessOrEqualThen(this float a_d1, float a_d2, float a_precision)
        {
            return a_d1 <= a_d2 + a_precision;
        }

        public static bool IsAlmostGreaterThen(this float a_d1, float a_d2, float a_precision)
        {
            return a_d1 > a_d2 - a_precision;
        }

        public static bool IsAlmostGreaterOrEqualThen(this float a_d1, float a_d2, float a_precision)
        {
            return a_d1 >= a_d2 - a_precision;
        }
    }
}