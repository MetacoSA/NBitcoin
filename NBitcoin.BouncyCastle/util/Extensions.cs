using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace NBitcoin.BouncyCastle
{
	public static class Extensions
	{
		public static bool IsInstanceOfType(this Type type, object obj)
		{
			return obj != null && type.GetTypeInfo().IsAssignableFrom(obj.GetType().GetTypeInfo());
		}
	}
}
