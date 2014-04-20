using System;
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
			_NameToType = new Dictionary<string,Type>();
			_TypeToName = new Dictionary<Type,string>();
			foreach(var pair in typeof(PayloadAttribute).Assembly
				.GetTypes()
				.Where(t => t.Namespace == typeof(PayloadAttribute).Namespace)
				.Where(t => t.IsDefined(typeof(PayloadAttribute), true))
				.Select(t =>
					new
					{
						Attr = t.GetCustomAttributes(typeof(PayloadAttribute), true).OfType<PayloadAttribute>().First(),
						Type = t
					}))
			{
				_NameToType.Add(pair.Attr.Name, pair.Type);
				_TypeToName.Add(pair.Type, pair.Attr.Name);
			}
		}

		public static string GetCommandName<T>()
		{
			return GetCommandName(typeof(T));
		}
		public static Type GetCommandType(string commandName)
		{
			Type result;
			if(!_NameToType.TryGetValue(commandName, out result))
				throw new ArgumentException(commandName + " is not a valid command");
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
			if(!_TypeToName.TryGetValue(type, out result))
				throw new ArgumentException(type.FullName + " is not a payload");
			return result;
		}
	}
}
