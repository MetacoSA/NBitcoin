using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace HashLib
{
    [DebuggerStepThrough]
    internal static class TypeExtensions
    {
        public static bool IsDerivedFrom(this Type a_type, Type a_baseType)
        {
#if !NETCORE
            Debug.Assert(a_type != null);
            Debug.Assert(a_baseType != null);
            Debug.Assert(a_type.IsClass);
            Debug.Assert(a_baseType.IsClass);

            return a_baseType.IsAssignableFrom(a_type);
#else
			return false;
#endif
		}

		public static bool IsImplementInterface(this Type a_type, Type a_interfaceType)
        {
#if !NETCORE
			Debug.Assert(a_type != null);
            Debug.Assert(a_interfaceType != null);
            Debug.Assert(a_type.IsClass || a_type.IsInterface || a_type.IsValueType);
            Debug.Assert(a_interfaceType.IsInterface);

            return a_interfaceType.IsAssignableFrom(a_type);
#else
	        return false;
#endif
		}
    }
}