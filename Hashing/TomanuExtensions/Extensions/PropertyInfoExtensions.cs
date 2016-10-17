using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace TomanuExtensions
{
    [DebuggerStepThrough]
    public static class PropertyInfoExtensions
    {
        /// <summary>
        /// With virtual keyword. Also interface implementations even without virtual keyword.
        /// </summary>
        /// <param name="a_pi"></param>
        /// <returns></returns>
        public static bool IsVirtual(this PropertyInfo a_pi)
        {
            if (a_pi.GetAccessors(true).Length == 0)
                return false;

            return a_pi.GetAccessors(true)[0].IsVirtual();
        }

        /// <summary>
        /// With abstract keyword.
        /// </summary>
        /// <param name="a_pi"></param>
        /// <returns></returns>
        public static bool IsAbstract(this PropertyInfo a_pi)
        {
            if (a_pi.GetAccessors(true).Length == 0)
                return false;
            return a_pi.GetAccessors(true)[0].IsAbstract;
        }

        /// <summary>
        /// With override keyword.
        /// </summary>
        /// <param name="a_pi"></param>
        /// <returns></returns>
        public static bool IsOverriden(this PropertyInfo a_pi)
        {
            if (a_pi.GetAccessors(true).Length == 0)
                return false;

            return a_pi.GetAccessors(true)[0].IsOverriden();
        }

        public static bool IsDerivedFrom(this PropertyInfo a_pi, PropertyInfo a_base,
            bool a_with_this = false)
        {
            if (a_pi.Name != a_base.Name)
                return false;
            if (a_pi.PropertyType != a_base.PropertyType)
                return false;
            if (!a_pi.GetIndexParameters().Select(p => p.ParameterType).SequenceEqual(
                a_base.GetIndexParameters().Select(p => p.ParameterType)))
            {
                return false;
            }
            if (a_pi.DeclaringType == a_base.DeclaringType)
                return a_with_this;

            MethodInfo m1 = a_pi.GetGetMethod(true);
            MethodInfo m3 = a_base.GetGetMethod(true);

            if ((m1 != null) && (m3 != null))
            {
                if (m1.GetBaseDefinitions().ContainsAny(m3.GetBaseDefinitions(true)))
                    return true;
            }
            else if ((m1 != null) || (m3 != null))
                return false;

            MethodInfo m2 = a_pi.GetSetMethod(true);
            MethodInfo m4 = a_base.GetSetMethod(true);

            if ((m2 != null) && (m4 != null))
            {
                if (m2.GetBaseDefinitions().ContainsAny(m4.GetBaseDefinitions(true)))
                    return true;
            }
            else if ((m2 != null) || (m4 != null))
                return false;

            return false;
        }
    }
}