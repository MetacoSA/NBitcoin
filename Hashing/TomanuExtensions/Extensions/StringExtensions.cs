using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace TomanuExtensions
{
    [DebuggerStepThrough]
    public static class StringExtensions
    {
        public static double ToDouble(this string a_str)
        {
            if (a_str.ToLower() == Double.NegativeInfinity.ToString().ToLower())
                return Double.NegativeInfinity;
            if (a_str.ToLower() == Double.PositiveInfinity.ToString().ToLower())
                return Double.PositiveInfinity;
            if (a_str.ToLower() == Double.NaN.ToString().ToLower())
                return Double.NaN;
            else
                return Double.Parse(a_str, CultureInfo.InvariantCulture);
        }

        public static float ToSingle(this string a_str)
        {
            if (a_str.ToLower() == Single.NegativeInfinity.ToString().ToLower())
                return Single.NegativeInfinity;
            if (a_str.ToLower() == Single.PositiveInfinity.ToString().ToLower())
                return Single.PositiveInfinity;
            if (a_str.ToLower() == Single.NaN.ToString().ToLower())
                return Single.NaN;
            else
                return Single.Parse(a_str, CultureInfo.InvariantCulture);
        }

        public static int ToInt(this string a_str)
        {
            return Int32.Parse(a_str);
        }

        public static bool ToBool(this string a_str)
        {
            return Boolean.Parse(a_str);
        }

        public static String RemoveFromRight(this string a_str, int a_chars)
        {
            return a_str.Remove(a_str.Length - a_chars);
        }

        public static String RemoveFromLeft(this string a_str, int a_chars)
        {
            return a_str.Remove(0, a_chars);
        }

        public static String Left(this string a_str, int a_count)
        {
            return a_str.Substring(0, a_count);
        }

        public static String Right(this string a_str, int a_count)
        {
            return a_str.Substring(a_str.Length - a_count, a_count);
        }

        public static string EnsureStartsWith(this string a_str, string a_prefix)
        {
            return a_str.StartsWith(a_prefix) ? a_str : string.Concat(a_prefix, a_str);
        }

        public static string Repeat(this string a_str, int a_count)
        {
            var sb = new StringBuilder(a_str.Length * a_count);

            for (int i = 0; i < a_count; i++)
                sb.Append(a_str);

            return sb.ToString();
        }

        public static string GetBefore(this string a_str, string a_pattern)
        {
            int index = a_str.IndexOf(a_pattern);
            return (index == -1) ? String.Empty : a_str.Substring(0, index);
        }

        public static string GetAfter(this string a_str, string a_pattern)
        {
            var last_pos = a_str.LastIndexOf(a_pattern);

            if (last_pos == -1)
                return String.Empty;

            int start = last_pos + a_pattern.Length;
            return start >= a_str.Length ? String.Empty : a_str.Substring(start).Trim();
        }

        public static string GetBetween(this string a_str, string a_left, string a_right)
        {
            return a_str.GetBefore(a_right).GetAfter(a_left);
        }

        public static string ToTitleCase(this string value)
        {
            return System.Globalization.CultureInfo.CurrentUICulture.TextInfo.ToTitleCase(value);
        }

        public static string FindUniqueName(this string a_pattern,
            IEnumerable<string> a_names)
        {
            if (!a_names.Contains(a_pattern))
                return a_pattern;

            string[] ar = a_pattern.Split(new char[] { ' ' });

            string left;
            uint index;
            if (!UInt32.TryParse(ar.Last(), out index))
            {
                index = 1;
                left = a_pattern + " ";
            }
            else
            {
                left = String.Join(" ", ar.SkipLast(1)) + " ";
                index++;
            }

            for (; ; )
            {
                string result = (left + index.ToString()).Trim();

                if (a_names.Contains(result))
                {
                    index++;
                    continue;
                }

                return result;
            }
        }

        public static IEnumerable<string> Split(this string a_str, string a_split)
        {
            int start_index = 0;

            for (; ; )
            {
                int split_index = a_str.IndexOf(a_split, start_index);

                if (split_index == -1)
                    break;

                yield return a_str.Substring(start_index, split_index - start_index);

                start_index = split_index + a_split.Length;
            }

            yield return a_str.Substring(start_index);
        }
    }
}