using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TomanuExtensions
{
    [DebuggerStepThrough]
    public static class EnumExtensions
    {
        public static T Parse<T>(string a_str)
        {
            return (T)Enum.Parse(typeof(T), a_str);
        }

        public static IEnumerable<T> GetValues<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }
    }
}