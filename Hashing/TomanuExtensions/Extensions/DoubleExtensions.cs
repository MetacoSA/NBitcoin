using System;
using System.Diagnostics;

namespace TomanuExtensions
{
    [DebuggerStepThrough]
    public static class DoubleExtensions
    {
        public static bool IsNumber(this double a_d)
        {
            return !Double.IsInfinity(a_d) && !Double.IsNaN(a_d);
        }

        public static double Fraction(this double a_d)
        {
            return a_d - Math.Truncate(a_d);
        }

        public static int Round(this double a_d)
        {
            return (int)Math.Round(a_d);
        }

        public static int Ceiling(this double a_d)
        {
            return (int)Math.Ceiling(a_d);
        }

        public static int Floor(this double a_d)
        {
            return (int)Math.Floor(a_d);
        }

        public static bool IsAlmostRelativeEquals(this double a_d1, double a_d2, double a_precision)
        {
            double mid = Math.Max(Math.Abs(a_d1), Math.Abs(a_d2));

            if (Double.IsInfinity(mid))
                return false;
            
            if (mid > a_precision)
                return Math.Abs(a_d1 - a_d2) <= a_precision * mid;
            else
                return a_d1 < a_precision;
        }

        /// <summary>
        /// Extension method may cause problem: -d.Limit(a, b) means -(d.Limit(a,b))
        /// </summary>
        /// <param name="a_d"></param>
        /// <param name="a_min_inclusive"></param>
        /// <param name="a_max_inclusive"></param>
        /// <returns></returns>
        public static double Limit(double a_d, double a_min_inclusive,
            double a_max_inclusive)
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
        public static bool InRange(double a_d, double a_min_inclusive,
            double a_max_inclusive)
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
        public static bool AlmostRelvativeInRange(double a_d, double a_min_inclusive,
            double a_max_inclusive, double a_precision)
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
        public static bool AlmostInRange(double a_d, double a_min_inclusive,
            double a_max_inclusive, double a_precision)
        {
            Debug.Assert(a_min_inclusive <= a_max_inclusive);

            return a_d.IsAlmostGreaterOrEqualThen(a_min_inclusive, a_precision) &&
                a_d.IsAlmostLessOrEqualThen(a_max_inclusive, a_precision);
        }

        public static bool IsAlmostRelativeLessThen(this double a_d1, double a_d2, double a_precision)
        {
            if (IsAlmostRelativeEquals(a_d1, a_d2, a_precision))
                return true;

            return a_d1 < a_d2;
        }

        public static bool IsAlmostRelativeLessOrEqualThen(this double a_d1, double a_d2, double a_precision)
        {
            if (IsAlmostRelativeEquals(a_d1, a_d2, a_precision))
                return true;

            return a_d1 <= a_d2;
        }

        public static bool IsAlmostRelativeGreaterThen(this double a_d1, double a_d2, double a_precision)
        {
            if (IsAlmostRelativeEquals(a_d1, a_d2, a_precision))
                return true;

            return a_d1 > a_d2;
        }

        public static bool IsAlmostRelativeGreaterOrEqualThen(this double a_d1, double a_d2, double a_precision)
        {
            if (IsAlmostRelativeEquals(a_d1, a_d2, a_precision))
                return true;

            return a_d1 >= a_d2;
        }

        public static double Min(double a_d1, double a_d2, double a_d3)
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

        public static double Min(double a_d1, double a_d2, double a_d3, double a_d4)
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

        public static double Max(double a_d1, double a_d2, double a_d3)
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

        public static double Max(double a_d1, double a_d2, double a_d3, double a_d4)
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

        public static bool IsAlmostLessThen(this double a_d1, double a_d2, double a_precision)
        {
            return a_d1 < a_d2 + a_precision;
        }

        public static bool IsAlmostLessOrEqualThen(this double a_d1, double a_d2, double a_precision)
        {
            return a_d1 <= a_d2 + a_precision;
        }

        public static bool IsAlmostGreaterThen(this double a_d1, double a_d2, double a_precision)
        {
            return a_d1 > a_d2 - a_precision;
        }

        public static bool IsAlmostGreaterOrEqualThen(this double a_d1, double a_d2, double a_precision)
        {
            return a_d1 >= a_d2 - a_precision;
        }

        public static bool IsAlmostEquals(this double a_d1, double a_d2, double a_precision)
        {
            return Math.Abs(a_d1 - a_d2) < a_precision;
        }
    }
}