using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NBitcoin.Scripting.Miniscript.Types;
using NBitcoin.Scripting.Parser;

namespace NBitcoin.Scripting.Miniscript
{
	internal class NonTerm : IEquatable<NonTerm>
	{
		#region Subtype definitions

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
		#endregion

		#region Equatable members
		public bool Equals(NonTerm other)
		{
			throw new NotImplementedException("");
		}
		#endregion
	}

	[DebuggerDisplay("{" + nameof(ToDebugString) + "()}")]
	public partial class Terminal<TPk, TPKh> : IEquatable<Terminal<TPk, TPKh>>
		where TPk : class, IMiniscriptKey<TPKh>, new()
		where TPKh : class, IMiniscriptKeyHash, new()
	{
		private static DataEncoders.HexEncoder Hex = new DataEncoders.HexEncoder();
		# region Subtype definitions
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

		internal int Tag;

		private Terminal(int tag) => Tag = tag;
		public static Terminal<TPk, TPKh> True { get; } = new Terminal<TPk, TPKh>(Tags.True);
		public static Terminal<TPk, TPKh> False { get; } = new Terminal<TPk, TPKh>(Tags.False);

		internal class Pk : Terminal<TPk, TPKh>
		{
			readonly public TPk Item;
			public Pk(TPk pk) : base(Tags.Pk) => Item = pk;
		}

		internal class PkH : Terminal<TPk, TPKh>
		{
			readonly public TPKh Item;
			public PkH(TPKh item) : base(Tags.PkH) => Item = item;
		}

		internal class After : Terminal<TPk, TPKh>
		{
			readonly public uint Item;
			public After(uint item) : base(Tags.After) => Item = item;
		}

		internal class Older : Terminal<TPk, TPKh>
		{
			readonly public uint Item;
			public Older(uint item) : base(Tags.Older) => Item = item;
		}

		internal class Sha256 : Terminal<TPk, TPKh>
		{
			readonly public uint256 Item;
			public Sha256(uint256 item) : base(Tags.Sha256) => Item = item;
		}
		internal class Hash256 : Terminal<TPk, TPKh>
		{
			readonly public uint256 Item;
			public Hash256(uint256 item) : base(Tags.Hash256) => Item = item;
		}

		internal class Ripemd160 : Terminal<TPk, TPKh>
		{
			readonly public uint160 Item;
			public Ripemd160(uint160 item) : base(Tags.Ripemd160) => Item = item;
		}

		internal class Hash160 : Terminal<TPk, TPKh>
		{
			readonly public uint160 Item;
			public Hash160(uint160 item) : base(Tags.Hash160) => Item = item;
		}

		internal class Alt : Terminal<TPk, TPKh>
		{
			readonly public Miniscript<TPk, TPKh> Item;
			public Alt(Miniscript<TPk, TPKh> item) : base(Tags.Alt) => Item = item;
		}

		internal class Swap : Terminal<TPk, TPKh>
		{
			readonly public Miniscript<TPk, TPKh> Item;
			public Swap(Miniscript<TPk, TPKh> item): base(Tags.Swap) => Item = item;
		}

		internal class Check : Terminal<TPk, TPKh>
		{
			readonly public Miniscript<TPk, TPKh> Item;
			public Check(Miniscript<TPk, TPKh> item): base(Tags.Check) => Item = item;
		}
		internal class DupIf : Terminal<TPk, TPKh>
		{
			readonly public Miniscript<TPk, TPKh> Item;
			public DupIf (Miniscript<TPk, TPKh> item): base(Tags.DupIf ) => Item = item;
		}
		internal class Verify : Terminal<TPk, TPKh>
		{
			readonly public Miniscript<TPk, TPKh> Item;
			public Verify(Miniscript<TPk, TPKh> item): base(Tags.Verify) => Item = item;
		}
		internal class NonZero : Terminal<TPk, TPKh>
		{
			readonly public Miniscript<TPk, TPKh> Item;
			public NonZero(Miniscript<TPk, TPKh> item): base(Tags.NonZero) => Item = item;
		}
		internal class ZeroNotEqual : Terminal<TPk, TPKh>
		{
			readonly public Miniscript<TPk, TPKh> Item;
			public ZeroNotEqual(Miniscript<TPk, TPKh> item): base(Tags.ZeroNotEqual) => Item = item;
		}
		internal class AndV : Terminal<TPk, TPKh>
		{
			readonly public Miniscript<TPk, TPKh> Item1;
			readonly public Miniscript<TPk, TPKh> Item2;
			public AndV(Miniscript<TPk, TPKh> item1,Miniscript<TPk, TPKh> item2): base(Tags.AndV)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}
		internal class AndB : Terminal<TPk, TPKh>
		{
			readonly public Miniscript<TPk, TPKh> Item1;
			readonly public Miniscript<TPk, TPKh> Item2;
			public AndB(Miniscript<TPk, TPKh> item, Miniscript<TPk, TPKh> item2): base(Tags.AndB)
			{
				Item1 = item;
				Item2 = item2;
			}
		}
		internal class AndOr : Terminal<TPk, TPKh>
		{
			readonly public Miniscript<TPk, TPKh> Item1;
			readonly public Miniscript<TPk, TPKh> Item2;
			readonly public Miniscript<TPk, TPKh> Item3;

			public AndOr(Miniscript<TPk, TPKh> item1, Miniscript<TPk, TPKh> item2, Miniscript<TPk, TPKh> item3) : base(Tags.AndOr)
			{
				Item1 = item1;
				Item2 = item2;
				Item3 = item3;
			}
		}
		internal class OrB : Terminal<TPk, TPKh>
		{
			readonly public Miniscript<TPk, TPKh> Item1;
			readonly public Miniscript<TPk, TPKh> Item2;
			public OrB(Miniscript<TPk, TPKh> item1, Miniscript<TPk, TPKh> item2): base(Tags.OrB)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}
		internal class OrD : Terminal<TPk, TPKh>
		{
			readonly public Miniscript<TPk, TPKh> Item1;
			readonly public Miniscript<TPk, TPKh> Item2;
			public OrD(Miniscript<TPk, TPKh> item1, Miniscript<TPk, TPKh> item2): base(Tags.OrD)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}

		internal class OrC : Terminal<TPk, TPKh>
		{
			readonly public Miniscript<TPk, TPKh> Item1;
			readonly public Miniscript<TPk, TPKh> Item2;
			public OrC(Miniscript<TPk, TPKh> item1, Miniscript<TPk, TPKh> item2): base(Tags.OrC)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}

		internal class OrI : Terminal<TPk, TPKh>
		{
			readonly public Miniscript<TPk, TPKh> Item1;
			readonly public Miniscript<TPk, TPKh> Item2;
			public OrI(Miniscript<TPk, TPKh> item1, Miniscript<TPk, TPKh> item2): base(Tags.OrI)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}
		internal class Thresh : Terminal<TPk, TPKh>
		{
			readonly public uint Item1;
			readonly public Miniscript<TPk, TPKh>[] Item2;
			public Thresh(uint item1, Miniscript<TPk, TPKh>[] item2): base(Tags.Thresh)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}
		internal class ThreshM : Terminal<TPk, TPKh>
		{
			readonly public uint Item1;
			readonly public TPk[] Item2;
			public ThreshM(uint item1, TPk[] item2): base(Tags.ThreshM)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}

		public static Terminal<TPk, TPKh> NewTrue() => Terminal<TPk, TPKh>.True;
		public static Terminal<TPk, TPKh> NewFalse() => Terminal<TPk, TPKh>.False;
		public static Terminal<TPk, TPKh> NewPk(TPk item)
		{
			if (item is null)
				throw new ArgumentNullException(nameof(item));

			return new Pk(item);
		}

		public static Terminal<TPk, TPKh> NewPkH(TPKh item) => new PkH(item);
		public static Terminal<TPk, TPKh> NewAfter(uint item) => new After(item);
		public static Terminal<TPk, TPKh> NewOlder(uint item) => new Older(item);
		public static Terminal<TPk, TPKh> NewSha256(uint256 item)
			=>  new Sha256(item);
		public static Terminal<TPk, TPKh> NewHash256(uint256 item)
			=> new Hash256(item);
		public static Terminal<TPk, TPKh> NewRipemd160(uint160 item)
			=> new Ripemd160(item);
		public static Terminal<TPk, TPKh> NewHash160(uint160 item)
			=> new Hash160(item);
		public static Terminal<TPk, TPKh> NewAlt(Miniscript<TPk, TPKh> item) => new Terminal<TPk, TPKh>.Alt(item);
		public static Terminal<TPk, TPKh> NewSwap(Miniscript<TPk, TPKh> item) => new Terminal<TPk, TPKh>.Swap(item);
		public static Terminal<TPk, TPKh> NewCheck(Miniscript<TPk, TPKh> item)
		{
			if (item is null)
				throw new ArgumentNullException(nameof(item));

			return new Check(item);
		}

		public static Terminal<TPk, TPKh> NewDupIf(Miniscript<TPk, TPKh> item) => new DupIf(item);
		public static Terminal<TPk, TPKh> NewVerify(Miniscript<TPk, TPKh> item) => new Verify(item);
		public static Terminal<TPk, TPKh> NewNonZero(Miniscript<TPk, TPKh> item) => new NonZero(item);
		public static Terminal<TPk, TPKh> NewZeroNotEqual(Miniscript<TPk, TPKh> item) => new ZeroNotEqual(item);

		public static Terminal<TPk, TPKh> NewAndV(Miniscript<TPk, TPKh> item1, Miniscript<TPk, TPKh> item2)
			=> new AndV(item1, item2);

		public static Terminal<TPk, TPKh> NewAndB(Miniscript<TPk, TPKh> item1, Miniscript<TPk, TPKh> item2)
			=> new AndB(item1, item2);

		public static Terminal<TPk, TPKh> NewAndOr(Miniscript<TPk, TPKh> item1, Miniscript<TPk, TPKh> item2, Miniscript<TPk, TPKh> item3)
			=> new AndOr(item1, item2, item3);

		public static Terminal<TPk, TPKh> NewOrB(Miniscript<TPk, TPKh> item1, Miniscript<TPk, TPKh> item2)
		{
			if (item1 is null)
				throw new ArgumentNullException(nameof(item1));

			if (item2 is null)
				throw new ArgumentNullException(nameof(item2));

			return new OrB(item1, item2);
		}

		public static Terminal<TPk, TPKh> NewOrD(Miniscript<TPk, TPKh> item1, Miniscript<TPk, TPKh> item2)
			=> new OrD(item1, item2);
		public static Terminal<TPk, TPKh> NewOrC(Miniscript<TPk, TPKh> item1, Miniscript<TPk, TPKh> item2)
			=> new OrC(item1, item2);
		public static Terminal<TPk, TPKh> NewOrI(Miniscript<TPk, TPKh> item1, Miniscript<TPk, TPKh> item2)
			=> new OrI(item1, item2);

		public static Terminal<TPk, TPKh> NewThresh(uint item1, IEnumerable<Miniscript<TPk, TPKh>> item2)
			=> new Thresh(item1, item2.ToArray());
		public static Terminal<TPk, TPKh> NewThreshM(uint item1, IEnumerable<TPk> item2)
			=> new ThreshM(item1, item2.ToArray());
		#endregion

		#region Equatable members
		public bool Equals(Terminal<TPk, TPKh> other)
		{
			if (other == null)
				return false;
			if (this.Tag != other.Tag)
				return false;

			switch (this.Tag)
			{
				case Tags.True: return true;
				case Tags.False: return true;
			}

			switch (this)
			{
				case Pk self:
					return self.Item.Equals(((Pk) other).Item);
				case PkH self:
					return self.Item.Equals(((PkH) other).Item);
				case After self:
					return self.Item.Equals(((After) other).Item);
				case Older self:
					return self.Item.Equals(((Older) other).Item);
				case Sha256 self:
					return self.Item.Equals(((Sha256) other).Item);
				case Hash256 self:
					return self.Item.Equals(((Hash256) other).Item);
				case Ripemd160 self:
					return self.Item.Equals(((Ripemd160) other).Item);
				case Hash160 self:
					return self.Item.Equals(((Hash160) other).Item);
				case Alt self:
					return self.Item.Equals(((Alt) other).Item);
				case Swap self:
					return self.Item.Equals(((Swap) other).Item);
				case Check self:
					return self.Item.Equals(((Check) other).Item);
				case DupIf self:
					return self.Item.Equals(((DupIf) other).Item);
				case Verify self:
					return self.Item.Equals(((Verify) other).Item);
				case NonZero self:
					return self.Item.Equals(((NonZero) other).Item);
				case ZeroNotEqual self:
					return self.Item.Equals(((ZeroNotEqual) other).Item);
				case AndV self:
					var andv = (AndV) other;
					return self.Item1.Equals(andv.Item1) && self.Item2.Equals(andv.Item2);
				case AndB self:
					var andb = (AndB) other;
					return self.Item1.Equals(andb.Item1) && self.Item2.Equals(andb.Item2);
				case AndOr self:
					var andOr = (AndOr) other;
					return self.Item1.Equals(andOr.Item1) && self.Item2.Equals(andOr.Item2) && self.Item3.Equals(andOr.Item3);
				case OrB self:
					var orb = (OrB) other;
					return self.Item1.Equals(orb.Item1) && self.Item2.Equals(orb.Item2);
				case OrD self:
					var ord = (OrD) other;
					return self.Item1.Equals(ord.Item1) && self.Item2.Equals(ord.Item2);
				case OrC self:
					var orc = (OrC) other;
					return self.Item1.Equals(orc.Item1) && self.Item2.Equals(orc.Item2);
				case OrI self:
					var ori = (OrI) other;
					return self.Item1.Equals(ori.Item1) && self.Item2.Equals(ori.Item2);
				case Thresh self:
					var t = (Thresh) other;
					return self.Item1.Equals(t.Item1) && self.Item2.SequenceEqual(t.Item2);
				case ThreshM self:
					var tm = (ThreshM) other;
					return self.Item1.Equals(tm.Item1) && self.Item2.SequenceEqual(tm.Item2);
			}
			throw new Exception("Unreachable!");
		}

		public override int GetHashCode()
		{

			int num = 0;
			switch (this.Tag)
			{
				case Tags.True: return Tags.True;
				case Tags.False: return Tags.False;
			}

			switch (this)
			{
				case Pk self:
					num = Tags.Pk;
					return -1640531527 + self.Item.GetHashCode() + ((num << 6) + (num >> 2));
				case PkH self:
					num = Tags.PkH;
					return -1640531527 + self.Item.GetHashCode() + ((num << 6) + (num >> 2));
				case After self:
					num = Tags.After;
					return -1640531527 + self.Item.GetHashCode() + ((num << 6) + (num >> 2));
				case Older self:
					num = Tags.Older;
					return -1640531527 + self.Item.GetHashCode() + ((num << 6) + (num >> 2));
				case Sha256 self:
					num = Tags.Sha256;
					return -1640531527 + self.Item.GetHashCode() + ((num << 6) + (num >> 2));
				case Hash256 self:
					num = Tags.Hash256;
					return -1640531527 + self.Item.GetHashCode() + ((num << 6) + (num >> 2));
				case Ripemd160 self:
					num = Tags.Ripemd160;
					return -1640531527 + self.Item.GetHashCode() + ((num << 6) + (num >> 2));
				case Hash160 self:
					num = Tags.Hash160;
					return -1640531527 + self.Item.GetHashCode() + ((num << 6) + (num >> 2));
				case Alt self:
					num = Tags.Alt;
					return -1640531527 + self.Item.GetHashCode() + ((num << 6) + (num >> 2));
				case Swap self:
					num = Tags.Swap;
					return -1640531527 + self.Item.GetHashCode() + ((num << 6) + (num >> 2));
				case Check self:
					num = Tags.Check;
					return -1640531527 + self.Item.GetHashCode() + ((num << 6) + (num >> 2));
				case DupIf self:
					num = Tags.DupIf;
					return -1640531527 + self.Item.GetHashCode() + ((num << 6) + (num >> 2));
				case Verify self:
					num = Tags.Verify;
					return -1640531527 + self.Item.GetHashCode() + ((num << 6) + (num >> 2));
				case NonZero self:
					num = Tags.NonZero;
					return -1640531527 + self.Item.GetHashCode() + ((num << 6) + (num >> 2));
				case ZeroNotEqual self:
					num = Tags.ZeroNotEqual;
					return -1640531527 + self.Item.GetHashCode() + ((num << 6) + (num >> 2));
				case AndV self:
					num = Tags.AndV;
					num = -1640531527 + self.Item1.GetHashCode() + ((num << 6) + (num >> 2));
					return -1640531527 + self.Item2.GetHashCode() + ((num << 6) + (num >> 2));
				case AndB self:
					num = Tags.AndB;
					num = -1640531527 + self.Item1.GetHashCode() + ((num << 6) + (num >> 2));
					return -1640531527 + self.Item2.GetHashCode() + ((num << 6) + (num >> 2));
				case AndOr self:
					num = Tags.AndOr;
					num = -1640531527 + self.Item1.GetHashCode() + ((num << 6) + (num >> 2));
					num = -1640531527 + self.Item2.GetHashCode() + ((num << 6) + (num >> 2));
					return -1640531527 + self.Item3.GetHashCode() + ((num << 6) + (num >> 2));
				case OrB self:
					num = Tags.OrB;
					num = -1640531527 + self.Item1.GetHashCode() + ((num << 6) + (num >> 2));
					return -1640531527 + self.Item2.GetHashCode() + ((num << 6) + (num >> 2));
				case OrD self:
					num = Tags.OrD;
					num = -1640531527 + self.Item1.GetHashCode() + ((num << 6) + (num >> 2));
					return -1640531527 + self.Item2.GetHashCode() + ((num << 6) + (num >> 2));
				case OrC self:
					num = Tags.OrC;
					num = -1640531527 + self.Item1.GetHashCode() + ((num << 6) + (num >> 2));
					return -1640531527 + self.Item2.GetHashCode() + ((num << 6) + (num >> 2));
				case OrI self:
					num = Tags.OrI;
					num = -1640531527 + self.Item1.GetHashCode() + ((num << 6) + (num >> 2));
					return -1640531527 + self.Item2.GetHashCode() + ((num << 6) + (num >> 2));
				case Thresh self:
					num = Tags.Thresh;
					num = -1640531527 + self.Item1.GetHashCode() + ((num << 6) + (num >> 2));
					foreach (var sub in self.Item2)
						num = -1640531527 + sub.GetHashCode() + ((num << 6) + (num >> 2));
					return num;
				case ThreshM self:
					num = Tags.ThreshM;
					num = -1640531527 + self.Item1.GetHashCode() + ((num << 6) + (num >> 2));
					foreach (var sub in self.Item2)
						num = -1640531527 + sub.GetHashCode() + ((num << 6) + (num >> 2));
					return num;
			}
			throw new Exception("Unreachable!");
		}
		#endregion

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
					l.Add(Op.GetPushOp(self.Item.ToPublicKey().ToBytes()));
					return l;
				case PkH self:
					l.Add(OpcodeType.OP_DUP);
					l.Add(OpcodeType.OP_HASH160);
					l.Add(Op.GetPushOp(self.Item.ToHash160().ToBytes()));
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
					l.Add(Op.GetPushOp(32));
					l.Add(OpcodeType.OP_EQUALVERIFY);
					l.Add(OpcodeType.OP_RIPEMD160);
					l.Add(Op.GetPushOp(self.Item.ToBytes()));
					l.Add(OpcodeType.OP_EQUAL);
					return l;
				case Hash160 self:
					l.Add(OpcodeType.OP_SIZE);
					l.Add(Op.GetPushOp(32));
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
				case OrC self:
					l.AddRange(self.Item1.Node.ToOpList());
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
						l.Add(Op.GetPushOp(sub.ToPublicKey().ToBytes()));
					}
					l.Add(Op.GetPushOp(self.Item2.Length));
					l.Add(OpcodeType.OP_CHECKMULTISIG);
					return l;
			}
			throw new Exception(($"Unreachable! {this}"));
		}

		public int ScriptSize()
		{
			switch (this.Tag)
			{
				case Tags.True:
					return 1;
				case Tags.False:
					return 1;
			}

			switch (this)
			{
				case Pk self:
					return self.Item.SerializedLength();
				case PkH _:
					return 24;
				case After self:
					return Utils.ScriptNumSize(self.Item) + 1;
				case Older self:
					return Utils.ScriptNumSize(self.Item) + 1;
				case Sha256 _:
					return 33 + 6;
				case Hash256 _:
					return 33 + 6;
				case Ripemd160 _:
					return 21 + 6;
				case Hash160 _:
					return 21 + 6;
				case Alt self:
					return self.Item.Node.ScriptSize() + 2;
				case Swap self:
					return self.Item.Node.ScriptSize() + 1;
				case Check self:
					return self.Item.Node.ScriptSize() + 1;
				case DupIf self:
					return self.Item.Node.ScriptSize() + 3;
				case Verify self:
					return self.Item.Node.ScriptSize() + (self.Item.Ext.HasVerifyForm ? 0 : 1);
				case NonZero self:
					return self.Item.Node.ScriptSize() + 4;
				case ZeroNotEqual self:
					return self.Item.Node.ScriptSize() + 1;
				case AndV self:
					return self.Item1.Node.ScriptSize() + self.Item2.Node.ScriptSize();
				case AndB self:
					return self.Item1.Node.ScriptSize() + self.Item2.Node.ScriptSize() + 1;
				case AndOr self:
					return
						self.Item1.Node.ScriptSize() +
						self.Item2.Node.ScriptSize() +
						self.Item3.Node.ScriptSize() + 3;
				case OrB self:
					return
						self.Item1.Node.ScriptSize() + self.Item2.Node.ScriptSize() + 1;
				case OrD self:
					return
						self.Item1.Node.ScriptSize() + self.Item2.Node.ScriptSize() + 3;
				case OrC self:
					return
						self.Item1.Node.ScriptSize() + self.Item2.Node.ScriptSize() + 2;
				case OrI self:
					return
						self.Item1.Node.ScriptSize() + self.Item2.Node.ScriptSize() + 3;
				case Thresh self:
					Debug.Assert(self.Item2.Length != 0);
					return
						Utils.ScriptNumSize(self.Item1) + // k
						1 + // EQUAL
						self.Item2.Select(s => s.Node.ScriptSize()).Sum() +
						self.Item2.Length // ADD
                        - 1; // no ADD on first element
				case ThreshM self:
					return
						Utils.ScriptNumSize(self.Item1) +
						1 +
						Utils.ScriptNumSize(self.Item2.Length) +
						self.Item2.Select(x => x.SerializedLength()).Sum();
			}

			throw new Exception("Unreachable!");
		}

		public int? MaxDissatisfactionWitnessElements()
		{
			switch (this.Tag)
			{
				case (Tags.False):
					return 0;
			}
			switch (this)
			{
				case Pk _:
					return 1;
				case PkH _:
					return 2;
				case Alt self:
					return self.Item.Node.MaxDissatisfactionWitnessElements();
				case Swap self:
					return self.Item.Node.MaxDissatisfactionWitnessElements();
				case Check self:
					return self.Item.Node.MaxDissatisfactionWitnessElements();
				case DupIf _:
					return 1;
				case NonZero _:
					return 1;
				case AndB self:
					return
						self.Item1.Node.MaxDissatisfactionWitnessElements() +
						self.Item2.Node.MaxDissatisfactionWitnessElements();
				case AndOr self:
					return
						self.Item1.Node.MaxDissatisfactionWitnessElements() +
						self.Item3.Node.MaxDissatisfactionWitnessElements();
				case OrB self:
					return
						self.Item1.Node.MaxDissatisfactionWitnessElements() +
						self.Item2.Node.MaxDissatisfactionWitnessElements();
				case OrI self:
					var l = self.Item1.Node.MaxDissatisfactionWitnessElements();
					var r = self.Item2.Node.MaxDissatisfactionWitnessElements();
					if (!l.HasValue && r.HasValue)
						return 1 + r.Value;
					else if (l.HasValue && !r.HasValue)
						return 1 + l.Value;
					else if (!(l.HasValue) && (!r.HasValue))
						return null;
					throw new Exception($"tried to dissatisfy or_i with both branches being dissatisfiable");
				case Thresh self:
					var sum = 0;
					foreach (var sub in self.Item2)
					{
						var s = sub.Node.MaxDissatisfactionWitnessElements();
						if (s.HasValue)
							sum += s.Value;
						else
							return null;
					}
					return sum;
				case ThreshM self:
					return 1 + (int)self.Item1;
			}

			return null;
		}

		public int? MaxDissatisfactionSize(int oneCost)
		{
			switch (this.Tag)
			{
				case (Tags.False):
					return 0;
			}

			switch (this)
			{
				case Pk _:
					return 1;
				case PkH _:
					return 35;
				case Alt self:
					return self.Item.Node.MaxDissatisfactionSize(oneCost);
				case Swap self:
					return self.Item.Node.MaxDissatisfactionSize(oneCost);
				case Check self:
					return self.Item.Node.MaxDissatisfactionSize(oneCost);
				case DupIf _:
					return 1;
				case NonZero _:
					return 1;
				case AndB self:
					return
						self.Item1.Node.MaxDissatisfactionSize(oneCost) +
						self.Item2.Node.MaxDissatisfactionSize(oneCost);
				case AndOr self:
					return
						self.Item1.Node.MaxDissatisfactionSize(oneCost) +
						self.Item3.Node.MaxDissatisfactionSize(oneCost);
				case OrB self:
					return
						self.Item1.Node.MaxDissatisfactionSize(oneCost) +
						self.Item2.Node.MaxDissatisfactionSize(oneCost);
				case OrD self:
					return
						self.Item1.Node.MaxDissatisfactionSize(oneCost) +
						self.Item2.Node.MaxDissatisfactionSize(oneCost);
				case OrI self:
					var l = self.Item1.Node.MaxDissatisfactionSize(oneCost);
					var r = self.Item2.Node.MaxDissatisfactionSize(oneCost);
					if (!l.HasValue && r.HasValue)
						return 1 + r.Value;
					else if (l.HasValue && !r.HasValue)
						return oneCost + l.Value;
					else if (!(l.HasValue) && (!r.HasValue))
						return null;
					throw new Exception($"tried to dissatisfy or_i with both branches being dissatisfiable");

				case Thresh self:
					var sum = 0;
					foreach (var sub in self.Item2)
					{
						var s = sub.Node.MaxDissatisfactionSize(oneCost);
						if (s.HasValue)
							sum += s.Value;
						else
							return null;
					}
					return sum;
				case ThreshM self:
					return 1 + (int)self.Item1;
			}

			return null;
		}

		public int MaxSatisfactionWitnessElements()
		{
			switch (this.Tag)
			{
				case Tags.True: return 0;
				case Tags.False: return 0;
			}

			switch (this)
			{
				case Pk _:
					return 1;
				case PkH _: return 2;
				case After _: return 0;
				case Older _: return 0;
				case Sha256 _: return 1;
				case Hash256 _: return 1;
				case Ripemd160 _: return 1;
				case Hash160 _: return 1;
				case Alt self:
					return self.Item.Node.MaxSatisfactionWitnessElements();
				case Swap self:
					return self.Item.Node.MaxSatisfactionWitnessElements();
				case Check self:
					return self.Item.Node.MaxSatisfactionWitnessElements();
				case DupIf self:
					return 1 + self.Item.Node.MaxSatisfactionWitnessElements();
				case Verify self:
					return self.Item.Node.MaxSatisfactionWitnessElements();
				case NonZero self:
					return self.Item.Node.MaxSatisfactionWitnessElements();
				case ZeroNotEqual self:
					return self.Item.Node.MaxSatisfactionWitnessElements();
				case AndV self:
					return
						self.Item1.Node.MaxSatisfactionWitnessElements() +
						self.Item2.Node.MaxSatisfactionWitnessElements();
				case AndB self:
					return
						self.Item1.Node.MaxSatisfactionWitnessElements() +
						self.Item2.Node.MaxSatisfactionWitnessElements();
				case AndOr self:
					var aSat = self.Item1.Node.MaxSatisfactionWitnessElements();
					var aDissat = self.Item1.Node.MaxDissatisfactionWitnessElements();
					return
						Math.Max(
							(aSat + self.Item3.Node.MaxSatisfactionWitnessElements()),
							 aDissat.Value + self.Item2.Node.MaxSatisfactionWitnessElements());
				case OrB self:
					return
						Math.Max(
							(self.Item1.Node.MaxSatisfactionWitnessElements() +
							 self.Item2.Node.MaxDissatisfactionWitnessElements().Value),
							(self.Item1.Node.MaxDissatisfactionWitnessElements().Value +
							 self.Item2.Node.MaxSatisfactionWitnessElements())
						);
				case OrD self:
					return
						Math.Max(
							self.Item1.Node.MaxSatisfactionWitnessElements(),
							self.Item1.Node.MaxDissatisfactionWitnessElements().Value + self.Item2.Node.MaxSatisfactionWitnessElements()
							);
				case OrC self:
					return
						Math.Max(
							self.Item1.Node.MaxSatisfactionWitnessElements(),
							self.Item1.Node.MaxDissatisfactionWitnessElements().Value + self.Item2.Node.MaxSatisfactionWitnessElements()
							);
				case OrI self:
					return
						1 + Math.Max(
							self.Item1.Node.MaxSatisfactionWitnessElements(),
							self.Item2.Node.MaxSatisfactionWitnessElements());
				case Thresh self:
					return
						self.Item2
							.Select(sub =>
								Tuple.Create(sub.Node.MaxSatisfactionWitnessElements(),
									sub.Node.MaxDissatisfactionWitnessElements().Value)
							)
							.OrderBy(t => t.Item1 - t.Item2)
							.Reverse()
							.Select((t, i) => i < self.Item1 ? t.Item1 : t.Item2)
							.Sum();
						;
				case ThreshM self: return 1 + (int)self.Item1;
			}

			throw new Exception("Unreachable!");
		}

		/// <summary>
		/// Maximum size, in bytes, of a satisfying witness.
		/// </summary>
		/// <returns></returns>
		public int MaxSatisfactionSize(int oneCost)
		{
			switch (Tag)
			{
				case Tags.True: return 0;
				case Tags.False: return 0;
			}

			switch (this)
			{
				case Pk _: return 73;
				case PkH _: return 34 + 73;
				case After _: return 0;
				case Older _: return 0;
				case Sha256 _: return 33;
				case Hash256 _: return 33;
				case Ripemd160 _: return 33;
				case Hash160 _: return 33;
				case Alt self: return self.Item.Node.MaxSatisfactionSize(oneCost);
				case Swap self: return self.Item.Node.MaxSatisfactionSize(oneCost);
				case Check self: return self.Item.Node.MaxSatisfactionSize(oneCost);
				case DupIf self: return oneCost + self.Item.Node.MaxSatisfactionSize(oneCost);
				case Verify self: return self.Item.Node.MaxSatisfactionSize(oneCost);
				case NonZero self: return self.Item.Node.MaxSatisfactionSize(oneCost);
				case ZeroNotEqual self: return self.Item.Node.MaxSatisfactionSize(oneCost);
				case AndV self:
					return self.Item1.Node.MaxSatisfactionSize(oneCost) + self.Item2.Node.MaxSatisfactionSize(oneCost);
				case AndB self:
					return self.Item1.Node.MaxSatisfactionSize(oneCost) + self.Item2.Node.MaxSatisfactionSize(oneCost);
				case AndOr self:
					return
						Math.Max(
							self.Item1.Node.MaxSatisfactionSize(oneCost) + self.Item3.Node.MaxSatisfactionSize(oneCost),
							self.Item1.Node.MaxDissatisfactionSize(oneCost).Value +
							self.Item2.Node.MaxSatisfactionSize(oneCost)
						);
				case OrB self:
					return
						Math.Max(
							self.Item1.Node.MaxSatisfactionSize(oneCost) +
							self.Item2.Node.MaxDissatisfactionSize(oneCost).Value,
							self.Item1.Node.MaxDissatisfactionSize(oneCost).Value +
							self.Item2.Node.MaxSatisfactionSize(oneCost)
						);
				case OrD self:
					return
						Math.Max(
							self.Item1.Node.MaxSatisfactionSize(oneCost),
							self.Item1.Node.MaxDissatisfactionSize(oneCost).Value +
							self.Item2.Node.MaxSatisfactionSize(oneCost)
						);
				case OrI self:
					return
						Math.Max(
							oneCost + self.Item1.Node.MaxSatisfactionSize(oneCost),
							1 + self.Item2.Node.MaxSatisfactionSize(oneCost)
						);
				case Thresh self:
					return
						self.Item2
							.Select(sub =>
								new {
									Sat = sub.Node.MaxSatisfactionSize(oneCost),
									Dissat = sub.Node.MaxDissatisfactionSize(oneCost).Value
								})
							.OrderBy(v => v.Sat - v.Dissat)
							.Reverse()
							.Select((v, i) => i < self.Item1 ? v.Sat : v.Dissat)
							.Sum();
				case ThreshM self : return 1 + 73 + (int)self.Item1;
			}
			throw new Exception("unreachable!");
		}

		public override string ToString()
			=> ToStringCore(new StringBuilder()).ToString();

		/// <summary>
		/// Internal helper function for displaying wrapper types;
		/// append a character to display before the `:` as well as a reference
		/// to the wrapped type to allow easy recursion.
		/// </summary>
		/// <returns></returns>
		private Tuple<char, Miniscript<TPk, TPKh>> WrapChar()
		{
			switch (this)
			{
				case Alt self: return Tuple.Create('a', self.Item);
				case Swap self: return Tuple.Create('s', self.Item);
				case Check self: return Tuple.Create('c', self.Item);
				case DupIf self: return Tuple.Create('d', self.Item);
				case Verify self: return Tuple.Create('v', self.Item);
				case NonZero self: return Tuple.Create('j', self.Item);
				case ZeroNotEqual self: return Tuple.Create('n', self.Item);
				case AndV self:
					return (self.Item2.Node == True) ? Tuple.Create('t', self.Item1) : null;
				case OrI self:
					return
						(self.Item2.Node == False)
							? Tuple.Create('u', self.Item1) :
						(self.Item1.Node == False)
							? Tuple.Create('l', self.Item2) : null;
						;
			}
			return null;
		}

		private StringBuilder ToStringCore(StringBuilder sb)
		{
			switch (this.Tag)
			{
				case Tags.True: return sb.Append("1");
				case Tags.False: return sb.Append("0");
			}

			switch (this)
			{
				case Pk self:
					return sb.Append($"pk({self.Item})");
				case PkH self:
					return sb.Append($"pk_h({self.Item})");
				case After self:
					return sb.Append($"after({self.Item})");
				case Older self:
					return sb.Append($"older({self.Item})");
				case Sha256 self:
					return sb.Append($"sha256({self.Item})");
				case Hash256 self:
					return sb.Append($"hash256({self.Item})");
				case Ripemd160 self:
					return sb.Append($"ripemd160({self.Item})");
				case Hash160 self:
					return sb.Append($"hash160({self.Item})");
				case AndV self when (!self.Item2.Node.Equals(True)):
					sb.Append("and_v(");
					self.Item1.Node.ToStringCore(sb);
					sb.Append(",");
					self.Item2.Node.ToStringCore(sb);
					sb.Append(")");
					return sb;
				case AndB self:
					sb.Append("and_b(");
					self.Item1.Node.ToStringCore(sb);
					sb.Append(",");
					self.Item2.Node.ToStringCore(sb);
					return sb.Append(")");
				case AndOr self:
					if (self.Item3.Node == False)
					{
						sb.Append("and_n(");
						self.Item1.Node.ToStringCore(sb);
						sb.Append(",");
						self.Item2.Node.ToStringCore(sb);
						return sb.Append(")");
					}

					sb.Append("andor(");
					self.Item1.Node.ToStringCore(sb);
					sb.Append(",");
					self.Item2.Node.ToStringCore(sb);
					sb.Append(",");
					self.Item3.Node.ToStringCore(sb);
					return sb.Append(")");
				case OrB self:
					sb.Append("or_b(");
					self.Item1.Node.ToStringCore(sb);
					sb.Append(",");
					self.Item2.Node.ToStringCore(sb);
					return sb.Append(")");
				case OrD self:
					sb.Append("or_d(");
					self.Item1.Node.ToStringCore(sb);
					sb.Append(",");
					self.Item2.Node.ToStringCore(sb);
					return sb.Append(")");
				case OrC self:
					sb.Append("or_c(");
					self.Item1.Node.ToStringCore(sb);
					sb.Append(",");
					self.Item2.Node.ToStringCore(sb);
					return sb.Append(")");
				case OrI self:
					if (self.Item1.Node != False && self.Item2.Node != False)
					{
						sb.Append("or_i(");
						self.Item1.Node.ToStringCore(sb);
						sb.Append(",");
						self.Item2.Node.ToStringCore(sb);
						sb.Append(")");
					}
					return sb;
				case Thresh self:
					sb.Append("thresh(");
					sb.Append(self.Item1);
					foreach (var sub in self.Item2)
					{
						sb.Append(",");
						sub.Node.ToStringCore(sb);
					}
					return sb.Append(")");
				case ThreshM self:
					sb.Append("thresh_m(");
					sb.Append(self.Item1);
					foreach (var key in self.Item2)
					{
						sb.Append(",");
						sb.Append(key.ToHex());
					}
					return sb.Append(")");
			}
			var t = WrapChar();
			if (t is null)
				throw new Exception("Unreachable!");
			sb.Append(t.Item1);
			if (t.Item2.Node.WrapChar() is null)
				sb.Append(':');
			return t.Item2.Node.ToStringCore(sb);
		}

		public string ToDebugString() =>
			ToDebugStringCore(new StringBuilder()).ToString();

		private StringBuilder ToDebugStringCore(StringBuilder sb)
		{
			var errors = new List<FragmentPropertyException>();
			sb.Append("[");
			if (Property<MiniscriptFragmentType, TPk, TPKh>.TypeCheck(this, out var typeMap,
				errors))
			{
				sb.Append(typeMap.Correctness.Base);
				sb.Append("/");
				sb.Append(typeMap.Correctness.Input.DebugPrint());
				if (typeMap.Correctness.DisSatisfiable)
					sb.Append('d');
				if (typeMap.Correctness.Unit)
					sb.Append('u');
				sb.Append(typeMap.Malleability.Dissat.DebugPrint());
				if (typeMap.Malleability.Safe)
					sb.Append('s');
				if (typeMap.Malleability.NonMalleable)
					sb.Append('m');
			}
			else
			{
				sb.Append($"TYPECHECK FAILED {errors.Flatten()}");
			}
			sb.Append("]");

			var t = WrapChar();
			if (!(t is null))
			{
				sb.Append(t.Item1);
				var sub = t.Item2;
				if (sub.Node.WrapChar() is null)
					sb.Append(":");
				return sub.Node.ToDebugStringCore(sb);
			}

			switch (Tag)
			{
				case Tags.True : return sb.Append("1");
				case Tags.False : return sb.Append("0");
			}

			switch (this)
			{
				case Pk self:
					return sb.Append($"pk({self.Item})");
				case PkH self:
					return sb.Append($"pk_h({self.Item})");
				case After self:
					return sb.Append($"after({self.Item})");
				case Older self:
					return sb.Append($"older({self.Item})");
				case Sha256 self:
					return sb.Append($"sha256({self.Item})");
				case Hash256 self:
					return sb.Append($"hash256({self.Item})");
				case Ripemd160 self:
					return sb.Append($"ripemd160({self.Item})");
				case Hash160 self:
					return sb.Append($"hash160({self.Item})");
				case AndV self:
					sb.Append("and_v(");
					self.Item1.Node.ToDebugStringCore(sb);
					sb.Append(",");
					self.Item2.Node.ToDebugStringCore(sb);
					return sb.Append(")");
				case AndB self:
					sb.Append("and_b(");
					self.Item1.Node.ToDebugStringCore(sb);
					sb.Append(",");
					self.Item2.Node.ToDebugStringCore(sb);
					return sb.Append(")");
				case AndOr self:
					if (self.Item3.Node == False)
					{
						sb.Append("and_n(");
						self.Item1.Node.ToDebugStringCore(sb);
						sb.Append(",");
						self.Item2.Node.ToDebugStringCore(sb);
						return sb.Append(")");
					}

					sb.Append("andor(");
					self.Item1.Node.ToDebugStringCore(sb);
					sb.Append(",");
					self.Item2.Node.ToDebugStringCore(sb);
					sb.Append(",");
					self.Item3.Node.ToDebugStringCore(sb);
					return sb.Append(")");
				case OrB self:
					sb.Append("or_b(");
					self.Item1.Node.ToDebugStringCore(sb);
					sb.Append(",");
					self.Item2.Node.ToDebugStringCore(sb);
					return sb.Append(")");
				case OrD self:
					sb.Append("or_d(");
					self.Item1.Node.ToDebugStringCore(sb);
					sb.Append(",");
					self.Item2.Node.ToDebugStringCore(sb);
					return sb.Append(")");
				case OrC self:
					sb.Append("or_c(");
					self.Item1.Node.ToDebugStringCore(sb);
					sb.Append(",");
					self.Item2.Node.ToDebugStringCore(sb);
					return sb.Append(")");
				case OrI self:
					if (self.Item1.Node != False && self.Item2.Node != False)
					{
						sb.Append("or_i(");
						self.Item1.Node.ToDebugStringCore(sb);
						sb.Append(",");
						self.Item2.Node.ToDebugStringCore(sb);
						sb.Append(")");
					}
					return sb;
				case Thresh self:
					sb.Append("thresh(");
					sb.Append(self.Item1);
					foreach (var sub in self.Item2)
					{
						sb.Append(",");
						sub.Node.ToDebugStringCore(sb);
					}
					return sb.Append(")");
				case ThreshM self:
					sb.Append("thresh_m(");
					sb.Append(self.Item1);
					foreach (var key in self.Item2)
					{
						sb.Append(",");
						sb.Append(key.ToHex());
					}
					return sb.Append(")");
			}

			throw new Exception("Unreachable!");
		}

		internal static Terminal<TPk, TPKh> FromTree(Tree top)
		{
			string fragWrap = String.Empty;
			string fragName = String.Empty;
			var nameSplit = top.Name.Split(':');
			if (nameSplit.Length == 1)
			{
				fragName = nameSplit[0];
			}
			else if (nameSplit.Length == 2)
			{
				if (nameSplit[0] == String.Empty)
					throw new ParsingException($"unexpected fragment name {top.Name}");
				fragWrap = nameSplit[0];
				fragName = nameSplit[1];
			}
			else throw new ParsingException($"Found more `:` in fragment {top.Name}");

			Terminal<TPk, TPKh> unwrapped = null;
			switch (fragName)
			{
				case "pk":
					if (top.Args.Count == 1)
						unwrapped = Tree.Terminal(top.Args[0], x => NewPk(MiniscriptFragmentParser<TPk, TPKh>.ParseKey(x)));
					break;
				case "pk_h":
					if (top.Args.Count == 1)
						unwrapped = Tree.Terminal(top.Args[0], x => NewPkH(MiniscriptFragmentParser<TPk, TPKh>.ParseHash(x)));
					break;
				case "after":
					if (top.Args.Count == 1)
						unwrapped = Tree.Terminal(top.Args[0], x => NewAfter(UInt32.Parse(x)));
					break;
				case "older":
					if (top.Args.Count == 1)
						unwrapped = Tree.Terminal(top.Args[0], x => NewOlder(UInt32.Parse(x)));
					break;
				case "sha256":
					if (top.Args.Count == 1)
						unwrapped = Tree.Terminal(top.Args[0], x => NewSha256(new uint256(Hex.DecodeData(x), true)));
					break;
				case "hash256":
					if (top.Args.Count == 1)
						unwrapped = Tree.Terminal(top.Args[0], x => NewHash256(new uint256(Hex.DecodeData(x), true)));
					break;
				case "ripemd160":
					if (top.Args.Count == 1)
						unwrapped = Tree.Terminal(top.Args[0], x => NewRipemd160(new uint160(Hex.DecodeData(x), true)));
					break;
				case "hash160":
					if (top.Args.Count == 1)
						unwrapped = Tree.Terminal(top.Args[0], x => NewHash160(new uint160(Hex.DecodeData(x), true)));
					break;
				case "1":
					if (top.Args.Count == 0)
						unwrapped = Tree.Terminal(top.Args[0], _ => NewTrue());
					break;
				case "0":
					if (top.Args.Count == 0)
						unwrapped = Tree.Terminal(top.Args[0], _ => NewFalse());
					break;
				case "and_v":
					if (top.Args.Count == 2)
					{
						var expr = Tree.Binary<TPk, TPKh>(top, NewAndV);
						if (expr is AndV andv && andv.Item2.Node == True)
							throw new ParsingException($"Non canonical true in {expr}");
						unwrapped = expr;
					}
					break;
				case "and_b":
					if (top.Args.Count == 2)
						unwrapped = Tree.Binary<TPk, TPKh>(top, NewAndB);
					break;
				case "and_n":
					if (top.Args.Count == 2)
						unwrapped =
							Tree.Binary<TPk, TPKh>(
								top,
								(a, b) => NewAndOr(a, b, Miniscript<TPk, TPKh>.FromAst(NewFalse()))
							);
					break;
				case "andor":
					if (top.Args.Count == 3)
						unwrapped = Tree.Ternary<TPk, TPKh>(top, NewAndOr);
					break;
				case "or_b":
					if (top.Args.Count == 2)
						unwrapped = Tree.Binary<TPk, TPKh>(top, NewOrB);
					break;
				case "or_d":
					if (top.Args.Count == 2)
						unwrapped = Tree.Binary<TPk, TPKh>(top, NewOrD);
					break;
				case "or_c":
					if (top.Args.Count == 2)
						unwrapped = Tree.Binary<TPk, TPKh>(top, NewOrC);
					break;
				case "or_i":
					if (top.Args.Count == 2)
					{
						var expr = Tree.Binary<TPk, TPKh>(top, NewOrI);
						if (expr is OrI ori && (ori.Item1.Node == False || ori.Item2.Node == False))
							throw new ParsingException($"Non canonical false in {expr}");
						unwrapped = expr;
					}
					break;
				case "thresh":
					var k = Tree.Terminal(top.Args[0], UInt32.Parse);
					var n = top.Args.Count;
					if (n == 0 || k > n - 1)
						throw new ParsingException($"threshold ({k}) is higher than the number of sub exprs ({n})");
					if (n == 1) throw new ParsingException("Empty thresholds not allowed in descriptors");
					var subs = top.Args.Skip(1).Select(Miniscript<TPk, TPKh>.FromTree);
					unwrapped = NewThresh(k, subs);
					break;
				case "thresh_m":
					var km = Tree.Terminal(top.Args[0], UInt32.Parse);
					var nm = top.Args.Count;
					if (nm == 0 || km > nm - 1)
						throw new ParsingException($"");
					var subsm = top.Args.Skip(1).Select(sub => Tree.Terminal(sub, MiniscriptFragmentParser<TPk, TPKh>.ParseKey));
					unwrapped = NewThreshM(km, subsm);
					break;
			}
			if (unwrapped is null)
				throw new ParsingException($"{top.Name}({top.Args.Count} args) while parsing Miniscript");

			return fragWrap.ToCharArray().Reverse().Aggregate(unwrapped, (acc, ch) => WrapExpression(ch, acc));
		}

		private static Terminal<TPk, TPKh> WrapExpression(char wrapChar, Terminal<TPk, TPKh> t)
		{
			switch (wrapChar)
			{
				case 'a':
					return NewAlt(Miniscript<TPk, TPKh>.FromAst(t));
				case 's':
					return NewSwap(Miniscript<TPk, TPKh>.FromAst(t));
				case 'c':
					return NewCheck(Miniscript<TPk, TPKh>.FromAst(t));
				case 'd':
					return NewDupIf(Miniscript<TPk, TPKh>.FromAst(t));
				case 'v':
					return NewVerify(Miniscript<TPk, TPKh>.FromAst(t));
				case 'j':
					return NewNonZero(Miniscript<TPk, TPKh>.FromAst(t));
				case 'n':
					return NewZeroNotEqual(Miniscript<TPk, TPKh>.FromAst(t));
				case 't':
					return NewAndV(
						Miniscript<TPk, TPKh>.FromAst(t),
						Miniscript<TPk, TPKh>.FromAst(NewTrue())
						);
				case 'u':
					return NewOrI(
						Miniscript<TPk, TPKh>.FromAst(t),
						Miniscript<TPk, TPKh>.FromAst(NewFalse())
						);
				case 'l':
					if (t == False)
						throw new ParsingException($"Encountered l:o which is syntactically equal to `u:o` except stupid");
					return NewOrI(
						Miniscript<TPk, TPKh>.FromAst(NewFalse()),
						Miniscript<TPk, TPKh>.FromAst(t)
						);
				default: throw new ParsingException($"Unknown wrapper {wrapChar}");
			}
		}
	}
}
