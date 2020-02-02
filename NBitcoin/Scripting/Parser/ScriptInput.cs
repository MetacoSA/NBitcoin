using System.Collections.Generic;
using System;
using System.Collections;

namespace NBitcoin.Scripting.Parser
{
	internal class ScriptInput : IInput<ScriptToken>
	{
		public ScriptInput(Script source) : this(source.ToTokens(), 0) { }

		public ScriptInput(ScriptToken[] source) : this(source, 0) { }

		public ScriptToken[] Source { get; }
		public int Position { get; }

		internal ScriptInput(ScriptToken[] source, int position)
		{
			if (source == null)
				throw new System.ArgumentNullException(nameof(source));
			Source = source;
			Position = position;
			Memos = new Dictionary<object, object>();
		}

		public bool AtEnd { get { return Position == Source.Length; } }
		public ScriptToken GetCurrent() => Source[Position];

		public IInput<ScriptToken> Advance()
		{
			if (AtEnd)
				throw new InvalidOperationException("The input is already at the end of the source");
			return new ScriptInput(Source, Position + 1);
		}

		public IEnumerator<ScriptToken> GetEnumerator()
		{
			return ((IEnumerable<ScriptToken>)Source).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.Source.GetEnumerator();
		}

		public IDictionary<object, object> Memos { get; }
	}
}