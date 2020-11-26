#if NULLABLE_SHIMS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Diagnostics.CodeAnalysis
{
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	internal sealed class MaybeNullWhenAttribute : Attribute
	{
		public MaybeNullWhenAttribute(bool returnValue) { ReturnValue = returnValue; }

		public bool ReturnValue { get; }
	}
}
#endif
