using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Linq;

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

		private int Tag;
		private NonTerm(int tag) => Tag = tag;
		public static NonTerm Expression { get; } = new NonTerm(Tags.Expression);
		public static NonTerm MaybeSwap { get; } = new NonTerm(Tags.MaybeSwap);
		public static NonTerm MaybeAndV { get; } = new NonTerm(Tags.MaybeAndV);
		public static NonTerm Alt { get; } = new NonTerm(Tags.Alt);
		public static NonTerm Check  { get; } = new NonTerm(Tags.Check);
		public static NonTerm DupIf { get; } = new NonTerm(Tags.DupIf);
		public static NonTerm Verify { get; } = new NonTerm(Tags.Verify);
		public static NonTerm NonZero { get; } = new NonTerm(Tags.NonZero);
		public static NonTerm ZeroNotEqual { get; } = new NonTerm(Tags.ZeroNotEqual);
		public static NonTerm AndV { get; } = new NonTerm(Tags.AndV);
		public static NonTerm AndB { get; } = new NonTerm(Tags.AndB);
		public static NonTerm Tern { get; } = new NonTerm(Tags.Tern);
		public static NonTerm OrB { get; } = new NonTerm(Tags.OrB);
		public static NonTerm OrD { get; } = new NonTerm(Tags.OrD);
		public static NonTerm OrC { get; } = new NonTerm(Tags.OrC);
		public static NonTerm EndIf { get; } = new NonTerm(Tags.EndIf);
		public static NonTerm EndIfNotIf { get; } = new NonTerm(Tags.EndIfNotIf);
		public static NonTerm EndIfElse { get; } = new NonTerm(Tags.EndIfElse);

		public class ThreshW : NonTerm
		{
			public ulong K;
			public ulong N;

			public ThreshW(ulong k, ulong n) : base(Tags.ThreshW)
			{
				N = n;
				K = k;
			}
		}

		public class ThreshE : NonTerm
		{
			public ulong K;
			public ulong N;

			public ThreshE(ulong k, ulong n) : base(Tags.ThreshW)
			{
				N = n;
				K = k;
			}
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

		private int Tag;

		private Terminal(int tag) => Tag = tag;
		public static Terminal True { get; } = new Terminal(Tags.True);
		public static Terminal False { get; } = new Terminal(Tags.False);

		internal class Pk : Terminal
		{
			readonly public PubKey Item;
			public Pk(PubKey pk) : base(Tags.Pk) => Item = pk;
		}

		internal class PkH : Terminal
		{
			readonly public uint160 Item;
			public PkH(uint160 item) : base(Tags.PkH) => Item = item;
		}

		internal class After : Terminal
		{
			readonly public uint Item;
			public After(uint item) : base(Tags.After) => Item = item;
		}

		internal class Older : Terminal
		{
			readonly public uint Item;
			public Older(uint item) : base(Tags.Older) => Item = item;
		}

		internal class Sha256 : Terminal
		{
			readonly public uint256 Item;
			public Sha256(uint256 item) : base(Tags.Sha256) => Item = item;
		}
		internal class Hash256 : Terminal
		{
			readonly public uint256 Item;
			public Hash256(uint256 item) : base(Tags.Hash256) => Item = item;
		}

		internal class Ripemd160 : Terminal
		{
			readonly public uint160 Item;
			public Ripemd160(uint160 item) : base(Tags.Ripemd160) => Item = item;
		}

		internal class Hash160 : Terminal
		{
			readonly public uint160 Item;
			public Hash160(uint160 item) : base(Tags.Hash160) => Item = item;
		}

		internal class Alt : Terminal
		{
			readonly public Miniscript Item;
			public Alt(Miniscript item) : base(Tags.Alt) => Item = item;
		}

		internal class Swap : Terminal
		{
			readonly public Miniscript Item;
			public Swap(Miniscript item): base(Tags.Swap) => Item = item;
		}

		internal class Check : Terminal
		{
			readonly public Miniscript Item;
			public Check(Miniscript item): base(Tags.Check) => Item = item;
		}
		internal class DupIf : Terminal
		{
			readonly public Miniscript Item;
			public DupIf (Miniscript item): base(Tags.DupIf ) => Item = item;
		}
		internal class Verify : Terminal
		{
			readonly public Miniscript Item;
			public Verify(Miniscript item): base(Tags.Verify) => Item = item;
		}
		internal class NonZero : Terminal
		{
			readonly public Miniscript Item;
			public NonZero(Miniscript item): base(Tags.NonZero) => Item = item;
		}
		internal class ZeroNotEqual : Terminal
		{
			readonly public Miniscript Item;
			public ZeroNotEqual(Miniscript item): base(Tags.ZeroNotEqual) => Item = item;
		}
		internal class AndV : Terminal
		{
			readonly public Miniscript Item1;
			readonly public Miniscript Item2;
			public AndV(Miniscript item1,Miniscript item2): base(Tags.AndV)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}
		internal class AndB : Terminal
		{
			readonly public Miniscript Item1;
			readonly public Miniscript Item2;
			public AndB(Miniscript item, Miniscript item2): base(Tags.AndB)
			{
				Item1 = item;
				Item2 = item2;
			}
		}
		internal class AndOr : Terminal
		{
			readonly public Miniscript Item1;
			readonly public Miniscript Item2;
			readonly public Miniscript Item3;

			public AndOr(Miniscript item1, Miniscript item2, Miniscript item3) : base(Tags.AndOr)
			{
				Item1 = item1;
				Item2 = item2;
				Item3 = item3;
			}
		}
		internal class OrB : Terminal
		{
			readonly public Miniscript Item1;
			readonly public Miniscript Item2;
			public OrB(Miniscript item1, Miniscript item2): base(Tags.OrB)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}
		internal class OrD : Terminal
		{
			readonly public Miniscript Item1;
			readonly public Miniscript Item2;
			public OrD(Miniscript item1, Miniscript item2): base(Tags.OrD)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}

		internal class OrC : Terminal
		{
			readonly public Miniscript Item1;
			readonly public Miniscript Item2;
			public OrC(Miniscript item1, Miniscript item2): base(Tags.OrC)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}

		internal class OrI : Terminal
		{
			readonly public Miniscript Item1;
			readonly public Miniscript Item2;
			public OrI(Miniscript item1, Miniscript item2): base(Tags.OrI)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}
		internal class Thresh : Terminal
		{
			readonly public ulong Item1;
			readonly public Miniscript[] Item2;
			public Thresh(ulong item1, Miniscript[] item2): base(Tags.Thresh)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}
		internal class ThreshM : Terminal
		{
			readonly public ulong Item1;
			readonly public Pk[] Item2;
			public ThreshM(ulong item1, Pk[] item2): base(Tags.ThreshM)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}
		public bool Equals(Terminal other)
		{
			throw new NotImplementedException();
		}

		public Script ToScript() =>
			new Script(this.ToOpList());
		private List<Op> ToOpList()
		{
			var l = new List<Op>();
			switch (this.Tag)
			{
				case Tags.True:
					l.Add(OpcodeType.OP_TRUE);
					return l;
				case Tags.False:
					l.Add(OpcodeType.OP_FALSE);
					return l;
			}
			switch (this)
			{
				case Pk self:
					l.Add(Op.GetPushOp(self.Item.ToBytes()));
					return l;
				case PkH self:
					l.Add(OpcodeType.OP_DUP);
					l.Add(OpcodeType.OP_HASH160);
					l.Add(Op.GetPushOp(self.Item.ToBytes()));
					l.Add(OpcodeType.OP_EQUALVERIFY);
					return l;
				case After self:
					l.Add(Op.GetPushOp(self.Item));
					l.Add(OpcodeType.OP_CHECKLOCKTIMEVERIFY);
					return l;
				case Older self:
					l.Add(Op.GetPushOp(self.Item));
					l.Add(OpcodeType.OP_CHECKSEQUENCEVERIFY);
					return l;
				case Sha256 self:
					l.Add(OpcodeType.OP_SIZE);
					l.Add(Op.GetPushOp(32));
					l.Add(OpcodeType.OP_EQUALVERIFY);
					l.Add(OpcodeType.OP_SHA256);
					l.Add(Op.GetPushOp(self.Item.ToBytes()));
					l.Add(OpcodeType.OP_EQUAL);
					return l;
				case Hash256 self:
					l.Add(OpcodeType.OP_SIZE);
					l.Add(Op.GetPushOp(32));
					l.Add(OpcodeType.OP_EQUALVERIFY);
					l.Add(OpcodeType.OP_HASH256);
					l.Add(Op.GetPushOp(self.Item.ToBytes()));
					l.Add(OpcodeType.OP_EQUAL);
					return l;
				case Ripemd160 self:
					l.Add(OpcodeType.OP_SIZE);
					l.Add(Op.GetPushOp(20));
					l.Add(OpcodeType.OP_EQUALVERIFY);
					l.Add(OpcodeType.OP_RIPEMD160);
					l.Add(Op.GetPushOp(self.Item.ToBytes()));
					l.Add(OpcodeType.OP_EQUAL);
					return l;
				case Hash160 self:
					l.Add(OpcodeType.OP_SIZE);
					l.Add(Op.GetPushOp(20));
					l.Add(OpcodeType.OP_EQUALVERIFY);
					l.Add(OpcodeType.OP_HASH160);
					l.Add(Op.GetPushOp(self.Item.ToBytes()));
					l.Add(OpcodeType.OP_EQUAL);
					return l;
				case Alt self:
					l.Add(OpcodeType.OP_TOALTSTACK);
					l.AddRange(self.Item.Node.ToOpList());
					l.Add(OpcodeType.OP_FROMALTSTACK);
					return l;
				case Swap self:
					l.Add(OpcodeType.OP_SWAP);
					l.AddRange(self.Item.Node.ToOpList());
					return l;
				case Check self:
					l.AddRange(self.Item.Node.ToOpList());
					l.Add(OpcodeType.OP_CHECKSIG);
					return l;
				case DupIf self:
					l.Add(OpcodeType.OP_DUP);
					l.Add(OpcodeType.OP_IF);
					l.AddRange(self.Item.Node.ToOpList());
					l.Add(OpcodeType.OP_ENDIF);
					return l;
				case Verify self:
					l.AddRange(self.Item.Node.ToOpList());
					l.PushVerify();
					return l;
				case NonZero self:
					l.Add(OpcodeType.OP_SIZE);
					l.Add(OpcodeType.OP_0NOTEQUAL);
					l.Add(OpcodeType.OP_IF);
					l.AddRange((self.Item.Node.ToOpList()));
					l.Add(OpcodeType.OP_ENDIF);
					return l;
				case ZeroNotEqual self:
					l.AddRange(self.Item.Node.ToOpList());
					l.Add(OpcodeType.OP_0NOTEQUAL);
					return l;
				case AndV self:
					l.AddRange(self.Item1.Node.ToOpList());
					l.AddRange(self.Item2.Node.ToOpList());
					return l;
				case AndB self:
					l.AddRange(self.Item1.Node.ToOpList());
					l.AddRange(self.Item2.Node.ToOpList());
					l.Add(OpcodeType.OP_BOOLAND);
					return l;
				case AndOr self:
					l.AddRange(self.Item1.Node.ToOpList());
					l.Add(OpcodeType.OP_NOTIF);
					l.AddRange(self.Item3.Node.ToOpList());
					l.Add(OpcodeType.OP_ELSE);
					l.AddRange(self.Item2.Node.ToOpList());
					l.Add(OpcodeType.OP_ENDIF);
					return l;
				case OrB self:
					l.AddRange(self.Item1.Node.ToOpList());
					l.AddRange(self.Item2.Node.ToOpList());
					l.Add(OpcodeType.OP_BOOLOR);
					return l;
				case OrD self:
					l.AddRange(self.Item1.Node.ToOpList());
					l.Add(OpcodeType.OP_IFDUP);
					l.Add(OpcodeType.OP_NOTIF);
					l.AddRange(self.Item2.Node.ToOpList());
					l.Add(OpcodeType.OP_ENDIF);
					return l;
				case OrI self:
					l.Add(OpcodeType.OP_IF);
					l.AddRange(self.Item1.Node.ToOpList());
					l.Add(OpcodeType.OP_ELSE);
					l.AddRange(self.Item2.Node.ToOpList());
					l.Add(OpcodeType.OP_ENDIF);
					return l;
				case Thresh self:
					l.AddRange(self.Item2[0].Node.ToOpList());
					foreach (var sub in self.Item2.Skip(1))
					{
						l.AddRange(sub.Node.ToOpList());
						l.Add(OpcodeType.OP_ADD);
					}
					l.Add(Op.GetPushOp((long)self.Item1));
					l.Add(OpcodeType.OP_EQUAL);
					return l;
				case ThreshM self:
					l.Add(Op.GetPushOp((long)self.Item1));
					foreach (var sub in self.Item2)
					{
						l.Add(Op.GetPushOp(sub.Item.ToBytes()));
					}
					l.Add(Op.GetPushOp(self.Item2.Length));
					l.Add(OpcodeType.OP_CHECKMULTISIG);
					return l;
			}
			throw new Exception(("Unreachable!"));
		}

		public ulong ScriptSize()
		{}
	}

}
