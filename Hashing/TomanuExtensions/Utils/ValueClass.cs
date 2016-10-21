using System.Diagnostics;

namespace TomanuExtensions.Utils
{
    [DebuggerDisplay("Value: {Value}")]
    public class ValueClass<T> where T : struct
    {
        public T Value;

        public ValueClass(T a_value)
        {
            Value = a_value;
        }

        public static implicit operator T(ValueClass<T> a_bc)
        {
            return a_bc.Value;
        }

        public static implicit operator ValueClass<T>(T a_value)
        {
            return new ValueClass<T>(a_value);
        }
    }
}