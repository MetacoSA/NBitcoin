using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NBitcoin.Scripting.Parser
{
	internal class StringInput : IInput<char>
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
			Memos = new Dictionary<object, object>();
		}

		public bool AtEnd { get { return Position == Source.Length; } }
		public char GetCurrent() => Source[Position];

		public IInput<char> Advance()
		{
			if (AtEnd)
				throw new InvalidOperationException("The input is already at the end of the source");
			return new StringInput(Source, Position + 1);
		}

		public IEnumerator<char> GetEnumerator()
		{
			var arr = (IEnumerable<char>)(Source).ToCharArray();
			return arr.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return (Source).ToCharArray().GetEnumerator();
		}

		public IDictionary<object, object> Memos { get; }
	}
}