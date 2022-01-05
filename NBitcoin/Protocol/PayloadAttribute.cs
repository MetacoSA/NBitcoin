using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	[AttributeUsage(AttributeTargets.Class)]
	public class PayloadAttribute : Attribute
	{
		static Dictionary<string, Type> _NameToType;
		static Dictionary<Type, string> _TypeToName;

		static PayloadAttribute()
		{
			_NameToType = new Dictionary<string, Type>();
			_TypeToName = new Dictionary<Type, string>();
			foreach (var pair in
				GetLoadableTypes(typeof(PayloadAttribute).GetTypeInfo().Assembly)
				.Where(t => t.Namespace == typeof(PayloadAttribute).Namespace)
				.Where(t => t.IsDefined(typeof(PayloadAttribute), true))
				.Select(t =>
					new
					{
						Attr = t.GetCustomAttributes(typeof(PayloadAttribute), true).OfType<PayloadAttribute>().First(),
						Type = t
					}))
			{
				_NameToType.Add(pair.Attr.Name, pair.Type.AsType());
				_TypeToName.Add(pair.Type.AsType(), pair.Attr.Name);
			}
		}

		static IEnumerable<TypeInfo> GetLoadableTypes(Assembly assembly)
		{
			try
			{
				return assembly.DefinedTypes;
			}
			catch (ReflectionTypeLoadException e)
			{
				return e.Types.Where(t => t != null).Select(t => t.GetTypeInfo());
			}
		}

		public static string GetCommandName<T>()
		{
			return GetCommandName(typeof(T));
		}
		public static Type GetCommandType(string commandName)
		{
			Type result;
			if (!_NameToType.TryGetValue(commandName, out result))
				return typeof(UnknownPayload);
			return result;
		}
		public PayloadAttribute(string commandName)
		{
			Name = commandName;
		}
		public string Name
		{
			get;
			set;
		}

		internal static string GetCommandName(Type type)
		{
			string result;
			if (!_TypeToName.TryGetValue(type, out result))
			{
				// try base type too
				if (!_TypeToName.TryGetValue(type.GetTypeInfo().BaseType, out result))
					throw new ArgumentException(type.FullName + " is not a payload");
			}

			return result;
		}
	}
}
