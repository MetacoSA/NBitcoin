using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace TomanuExtensions
{
    [DebuggerStepThrough]
    public static class MethodInfoExtensions
    {
        /// <summary>
        /// With virtual keyword. Also interface implementations even without virtual keyword.
        /// </summary>
        /// <param name="a_pi"></param>
        /// <returns></returns>
        public static bool IsVirtual(this MethodInfo a_mi)
        {
            return a_mi.IsVirtual && !a_mi.IsAbstract && !a_mi.IsOverriden();
        }

        /// <summary>
        /// With override keyword.
        /// </summary>
        /// <param name="a_pi"></param>
        /// <returns></returns>
        public static bool IsOverriden(this MethodInfo a_mi)
        {
            return a_mi.DeclaringType != a_mi.GetBaseDefinition().DeclaringType;
        }

        public static IEnumerable<MethodInfo> GetBaseDefinitions(this MethodInfo a_mi,
            bool a_with_this = false)
        {
            if (a_with_this)
                yield return a_mi;

            MethodInfo t = a_mi;

            while ((t.GetBaseDefinition() != null) && (t.GetBaseDefinition() != t))
            {
                t = t.GetBaseDefinition();
                yield return t;
            }
        }

        public static bool IsDerivedFrom(this MethodInfo a_mi, MethodInfo a_base,
            bool a_with_this = false)
        {
            if (a_mi.Name != a_base.Name)
                return false;
            if (a_mi.DeclaringType == a_base.DeclaringType)
            {
                if (!a_mi.GetParameters().Select(p => p.ParameterType).SequenceEqual(
                    a_base.GetParameters().Select(p => p.ParameterType)))
                {
                    return false;
                }

                return a_with_this;
            }

            return a_mi.GetBaseDefinitions().Contains(a_base);
        }
    }
}