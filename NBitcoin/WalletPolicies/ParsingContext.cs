#if !NO_RECORDS
#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static NBitcoin.WalletPolicies.MiniscriptError;

namespace NBitcoin.WalletPolicies
{
	class ParsingContext
	{
		internal class Frame : IDisposable
		{
			internal Frame(ParsingContext ctx)
			{
				this.ctx = ctx;
			}
			public int ExpectedParameterCount;
			public List<MiniscriptNode> Parameters = new List<MiniscriptNode>();
			private ParsingContext ctx;

			public int FragmentIndex { get; internal set; }
			public void Dispose()
			{
				ctx._frames.Pop();
			}
		}
		public ParsingContext(string miniscript, MiniscriptParsingSettings settings)
		{
			ArgumentNullException.ThrowIfNull(settings);
			ArgumentNullException.ThrowIfNull(miniscript);
			Miniscript = miniscript;
			this.ParsingSettings = settings;
		}
		public string Miniscript { get; }

		internal MiniscriptParsingSettings ParsingSettings;

		public Network Network => ParsingSettings.Network;
		public int Offset { get; internal set; }
		public bool Advance(int charCount)
		{
			SkipSpaces();
			if (Offset + charCount > Miniscript.Length)
				return false;
			Offset += charCount;
			SkipSpaces();
			return true;
		}
		public void SkipSpaces()
		{
			while (Offset < Miniscript.Length && char.IsWhiteSpace(Miniscript[Offset]))
			{
				Offset++;
			}
		}
		public char NextChar => Miniscript[Offset];
		public bool IsEnd => Offset >= Miniscript.Length;
		public int RemainingChars => Miniscript.Length - Offset;
		public string Remaining => Miniscript[Offset..];
		Stack<Frame> _frames = new();
		public Frame CurrentFrame => _frames.Peek();
		public Frame PushFrame()
		{
			Frame f = new Frame(this);
			_frames.Push(f);
			return f;
		}
		public KeyType? ExpectedKeyType { get; internal set; }

		public bool TrySetExpectedKeyType(KeyType keyType)
		{
			if (ExpectedKeyType is null)
			{
				ExpectedKeyType = keyType;
				return true;
			}
			return ExpectedKeyType == keyType;
		}
		public int FragmentIndex { get; internal set; }
		public KeyType? DefaultKeyType => ParsingSettings.KeyType;

		public bool NetstedMusig { get; internal set; }

		public override string ToString() => Remaining;

		public bool Peek(string str, [MaybeNullWhen(true)] out MiniscriptError error)
		{
			error = null;
			SkipSpaces();
			if (!Remaining.StartsWith(str, StringComparison.Ordinal))
			{
				error = new UnexpectedToken(Offset);
				return false;
			}
			return true;
		}
		public bool Peek(char c, [MaybeNullWhen(true)] out MiniscriptError error)
		{
			if (!Peek(out var c2, out error))
				return false;
			if (c2 != c)
			{
				error = new UnexpectedToken(Offset);
				return false;
			}
			return true;
		}
		public bool Peek(out char c, [MaybeNullWhen(true)] out MiniscriptError error)
		{
			c = default;
			SkipSpaces();
			if (IsEnd)
			{
				error = new IncompleteExpression(Offset - 1);
				return false;
			}
			c = this.NextChar;
			error = null;
			return true;
		}
		public bool Consume(string str, [MaybeNullWhen(true)] out MiniscriptError error)
		{
			error = null;
			SkipSpaces();
			if (!Remaining.StartsWith(str, StringComparison.Ordinal))
			{
				error = new UnexpectedToken(Offset);
				return false;
			}
			Advance(str.Length);
			return true;
		}
		public bool Consume(out char c, [MaybeNullWhen(true)] out MiniscriptError error)
		{
			var r = Peek(out c, out error);
			if (r)
				Advance(1);
			return r;
		}
		public bool Consume(char c, [MaybeNullWhen(true)] out MiniscriptError error)
		{
			SkipSpaces();
			if (!Peek(out var c2, out error))
				return false;
			if (c2 != c)
			{
				error = new UnexpectedToken(Offset);
				return false;
			}
			Advance(1);
			error = null;
			return true;
		}

		public Memento StartMemento(bool commit) => new Memento(this, commit);

		public class Memento : IDisposable
		{
			private ParsingContext context;
			private readonly KeyType? expectedKeyType;
			private readonly bool nestedMusig;
			private bool commit;
			private readonly int offset;

			public Memento(ParsingContext context, bool commit)
			{
				this.context = context;
				this.expectedKeyType = this.context.ExpectedKeyType;
				this.nestedMusig = this.context.NetstedMusig;
				this.offset = context.Offset;
				this.commit = commit;
			}
			public void Commit() => commit = true;
			public void Rollback() => commit = false;


			public void Dispose()
			{
				if (!commit)
				{
					this.context.Offset = offset;
					this.context.ExpectedKeyType = expectedKeyType;
					this.context.NetstedMusig = nestedMusig;
				}
			}
		}
	}
}
#endif
