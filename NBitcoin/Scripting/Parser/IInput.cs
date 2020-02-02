using System;
using System.Collections.Generic;

namespace NBitcoin.Scripting.Parser
{
	internal interface IInput<out T> : IEnumerable<T>
	{
		IInput<T> Advance();

		T GetCurrent();

		bool AtEnd { get; }
		int Position { get; }
		IDictionary<object, object> Memos { get; }
	}
}