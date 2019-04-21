using System;

namespace NBitcoin.Miniscript.Parser
{
	public class StringInput : IInput
	{
		public StringInput(string source) : this(source, 0) { }
		public string Source { get; }
		public int Position { get; }

		internal StringInput(string source, int position)
		{
			if (source == null)
				throw new System.ArgumentNullException(nameof(source));
			Source = source;
			Position = position;
		}

		public bool AtEnd { get { return Position == Source.Length; } }
		public char Current { get { return Source[Position]; } }

		public IInput Advance()
		{
			if (AtEnd)
				throw new InvalidOperationException("The input is already at the end of the source");
			return new StringInput(Source, Position + 1);
		}
	}
}