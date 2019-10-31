using System;

namespace NBitcoin.Scripting.Miniscript.Types
{
	internal static class Property<T> where T : class, IProperty<T>
	{
		internal static void SanityChecks(T item)
		{
			item.SanityChecks();
		}

		internal static T FromTrue()
		{
			T t = default;
			return t.FromTrue();
		}

		internal static T TypeCheck<TPk>(Terminal<TPk, TPK> fragment, Func<int, T> c)
		where TPk : IMiniscriptKey
		{
			T t = default;
			return t.TypeCheck(fragment, c);
		}
		internal static T CastCheck()
		{
			T t = default;
			return t.CastCheck();
		}
	}
}
