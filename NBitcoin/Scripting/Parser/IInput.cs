using System;
using System.Collections.Generic;

namespace NBitcoin.Scripting.Parser
{
    public interface IInput<out T>
    {
		IInput<T> Advance();

		T GetCurrent();

		T GetNext();

		bool AtEnd { get; }
		int Position { get; }
		IDictionary<object, object> Memos { get; }
	}
}