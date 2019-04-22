using System;
using System.Collections.Generic;

namespace NBitcoin.Miniscript.Parser
{
    public interface IInput
    {
		IInput Advance();

		string Source { get; }

		char Current { get; }

		bool AtEnd { get; }
		int Position { get; }
		IDictionary<object, object> Memos { get; }
	}
}