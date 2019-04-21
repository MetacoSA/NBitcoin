using System;

namespace NBitcoin.Miniscript.Parser
{
    public interface IInput
    {
		IInput Advance();

		string Source { get; }

		char Current { get; }

		bool AtEnd { get; }
		int Position { get; }
	}
}