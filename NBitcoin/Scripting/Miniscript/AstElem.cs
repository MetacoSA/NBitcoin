using System;
using System.Linq.Expressions;

namespace NBitcoin.Scripting.Miniscript
{
	internal class NonTerm : IEquatable<NonTerm>
	{
		internal static class Tags
		{
			public const int Expression = 0;
			public const int MaybeSwap = 1;
			public const int MaybeAndV = 2;
			public const int Alt = 3;
			public const int Check = 4;
			public const int DupIf = 5;
			public const int Verify = 6;
			public const int NonZero = 7;
			public const int ZeroNotEqual = 8;
			public const int AndV = 9;
			public const int AndB = 10;
			public const int Tern = 11;
			public const int OrB = 12;
			public const int OrD = 13;
			public const int OrC = 14;
			public const int ThreshW = 15;
			public const int ThreshE = 16;
			/// <summary>
			/// Could be or_d, or_c, or_i, d, n
			/// </summary>
			public const int EndIf = 17;

			public const int EndIfNotIf = 18;
			public const int EndIfElse = 19;
		}

		public bool Equals(NonTerm other)
		{
			throw new NotImplementedException("");
		}
	}

	public class Terminal : IEquatable<Terminal>
	{
		internal static class Tags
		{
			public const int True = 0;
			public const int False = 1;
			public const int Pk = 2;
			public const int PkH = 3;
			public const int After = 4;
			public const int Older = 5;
			public const int Sha256 = 6;
			public const int Hash256 = 7;
			public const int Ripemd160 = 8;
			public const int Hash160 = 9;
			public const int Alt = 10;
			public const int Swap = 11;
			public const int Check = 12;
			public const int DupIf = 13;
			public const int Verify = 14;
			public const int NonZero = 15;
			public const int ZeroNotEqual = 16;
			public const int AndV = 17;
			public const int AndB = 18;
			public const int AndOr = 19;
			public const int OrB = 20;
			public const int OrD = 21;
			public const int OrC = 22;
			public const int OrI = 23;
			public const int Thresh = 24;
			public const int ThreshM = 25;
		}

		internal int Tag { get; }

		private Terminal(int tag) => Tag = tag;
		public static Terminal True { get; } = new Terminal(Tags.True);
		public static Terminal False { get; } = new Terminal(Tags.False);

		internal class Pk : Terminal
		{
			public PubKey Item;
			public Pk(PubKey pk) : base(Tags.Pk) => Item = pk;
		}

		internal class PkH : Terminal
		{
			public uint160 Item;
			public PkH(uint160 item) : base(Tags.PkH) => Item = item;
		}

		internal class After : Terminal
		{
			public uint Item;
			public After(uint item) : base(Tags.After) => Item = item;
		}

		internal class Older : Terminal
		{
			public uint Item;
			public Older(uint item) : base(Tags.Older) => Item = item;
		}

		internal class Sha256 : Terminal
		{
			public uint256 Item;
			public Sha256(uint256 item) : base(Tags.Sha256) => Item = item;
		}
		internal class Hash256 : Terminal
		{
			public uint256 Item;
			public Hash256(uint256 item) : base(Tags.Hash256) => Item = item;
		}

		internal class Ripemd160 : Terminal
		{
			public uint160 Item;
			public Ripemd160(uint160 item) : base(Tags.Ripemd160) => Item = item;
		}

		internal class Hash160 : Terminal
		{
			public uint160 Item;
			public Hash160(uint160 item) : base(Tags.Hash160) => Item = item;
		}

		public bool Equals(Terminal other)
		{
			throw new NotImplementedException();
		}
	}

}
