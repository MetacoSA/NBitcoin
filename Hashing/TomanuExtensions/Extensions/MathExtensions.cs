using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TomanuExtensions
{
    public static class MathExtensions
    {
        public const double PI = 3.1415926535897932384626433832795;
        public const double SQRT2 = 1.4142135623730950488016887242097;
        public const double SQRT3 = 1.7320508075688772935274463415059;

        private static int[][] s_pascal_triangle;

        static MathExtensions()
        {
            s_pascal_triangle = new int [10][];
            s_pascal_triangle[0] = new int[] { 1 };

            for (int i = 1; i < s_pascal_triangle.Length; i++)
                s_pascal_triangle[i] = PascalTriangle(s_pascal_triangle[i - 1]);
        }

        public static int[] PascalTriangle(int a_row)
        {
            if (a_row <= s_pascal_triangle.Length)
                return s_pascal_triangle[a_row - 1];
            else
            {
                int[] result = s_pascal_triangle[s_pascal_triangle.Length - 1];

                for (int i = s_pascal_triangle.Length; i < a_row; i++)
                    result = PascalTriangle(result);

                return result;
            }
        }

        private static int[] PascalTriangle(int[] a_start)
        {
            int[] result = new int[a_start.Length + 1];
            
            result[0] = 1;
            result[result.Length - 1] = 1;

            for (int j = 0; j < a_start.Length - 1; j++)
                result[j+1] = a_start[j] + a_start[j + 1];

            return result;
        }

        public static double ToRad(double a_deg)
        {
            return a_deg * PI / 180;
        }

        public static double ToDeg(double a_rad)
        {
            return a_rad * 180 / PI;
        }

        public static double Hypot(double a_x, double a_y)
        {
            return Math.Sqrt(a_x * a_x + a_y * a_y);
        }
    }
}
