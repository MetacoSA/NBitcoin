using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace TomanuExtensions
{
    [DebuggerStepThrough]
    public static class ConstructorInfoExtensions
    {
        public static Object Invoke(this ConstructorInfo a_ci)
        {
            return a_ci.Invoke(null);
        }

        public static Object Invoke(this ConstructorInfo a_ci, params Object[] a_params)
        {
            return a_ci.Invoke(a_params);
        }
    }
}