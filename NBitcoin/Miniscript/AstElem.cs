using System;
using System.Linq;
using System.Text;

namespace NBitcoin.Miniscript
{
	public abstract class AstElem : IEquatable<AstElem>
	{
		# region tags
 		internal static class Tags
		{
			// -------- wrappers ---------
			// `<V> 1`
			public const int Pk = 0;
			// `TAS <E>  FAS`
			public const int PkV = 1;
			// ``
			public const int PkQ = 2;
			// ``
			public const int PkW = 3;
			// -------- multisig check -------
			// ``
			public const int Multi = 4;
			// ``
			public const int MultiV = 5;
			// ``
			// -------- timelocks -------
			public const int TimeT = 6;
			// ``
			public const int TimeV = 7;
			// ``
			public const int TimeF = 8;
			// ``
			public const int Time = 9;
			// ``
			public const int TimeW = 10;
			// -------- hashlocks -------
			// ``
			public const int HashT = 11;
			// ``
			public const int HashV = 12;
			// ``
			public const int HashW = 13;
			// -------- wrappers -------
			// ``
			public const int True = 14;
			// ``
			public const int Wrap = 15;
			// ``
			public const int Likely = 16;
			// ``
			public const int Unlikely = 17;
			// -------- conjunctions -------
			// ``
			public const int AndCat = 18;
			// ``
			public const int AndBool = 19;
			// ``
			public const int AndCasc = 20;
			// -------- disjunctions --------
			// `<E> <W> BoolOr`
			public const int OrBool = 21;
			// `<E> IFDUP NOTIF <T/E> ENDIF`
			public const int OrCasc = 22;
			// `<E> NOTIF <V> ENDIF`
			public const int OrCont = 23;
			// `IF <Q> ELSE <Q> ENDIF CHECKSIG`
			public const int OrKey = 24;
			// `IF <Q> ELSE <Q> ENDIF CHECKSIGVERIFY`
			public const int OrKeyV = 25;
			// `IF <sub1> ELSE <sub2> ENDIF` for many choices of `sub1` and `sub2`
			public const int OrIf = 26;

			// `IF <T> ELSE <T> ENDIF VERIFY`
			public const int OrIfV= 27;

			// `NOTIF <F> ELSE <E> ENDIF`
			public const int OrNotIf = 28;
			// --------- thresholds ----------
			// `<E> (<W> ADD)* <n> EQUAL`
			public const int Thresh = 29;

			// `<E> (<W> ADD)* <n> EQUALVERIFY`
			public const int ThreshV = 30;

		}
		#endregion
		private AstElem(int Tag)
		{
			this.Tag = Tag;
		}

		public int Tag { get; }

		# region SubClasses
		internal class Pk : AstElem
		{
			public PubKey Item1 { get; }
			internal Pk(PubKey item1) : base(0) => Item1 = item1;

		}
		internal class PkV : AstElem
		{
			public PubKey Item1 { get; }
			internal PkV(PubKey item1) : base(1) => Item1 = item1;

		}
		internal class PkQ : AstElem
		{
			public PubKey Item1 { get; }
			internal PkQ(PubKey item1) : base(2) => Item1 = item1;

		}

		internal class PkW : AstElem
		{
			public PubKey Item1 { get; }
			internal PkW(PubKey item1) : base(3) => Item1 = item1;

		}
		internal class Multi : AstElem
		{
			public UInt32 Item1 { get; }
			public PubKey[] Item2 { get; }
			internal Multi(UInt32 item1, PubKey[] item2) : base(4)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}
		internal class MultiV : AstElem
		{
			public UInt32 Item1 { get; }
			public PubKey[] Item2 { get; }
			internal MultiV(UInt32 item1, PubKey[] item2) : base(5)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}

		internal class TimeT : AstElem
		{
			public UInt32 Item1 { get; }
			internal TimeT(UInt32 item1) : base(6) => Item1 = item1;
		}

		internal class TimeV : AstElem
		{
			public UInt32 Item1 { get; }
			internal TimeV(UInt32 item1) : base(7) => Item1 = item1;
		}
		internal class TimeF : AstElem
		{
			public UInt32 Item1 { get; }
			internal TimeF(UInt32 item1) : base(8) => Item1 = item1;
		}
		internal class Time : AstElem
		{
			public UInt32 Item1 { get; }
			internal Time(UInt32 item1) : base(9) => Item1 = item1;
		}
		internal class TimeW : AstElem
		{
			public UInt32 Item1 { get; }
			internal TimeW(UInt32 item1) : base(10) => Item1 = item1;
		}

		internal class HashT : AstElem
		{
			public uint256 Item1 { get; }
			internal HashT(uint256 item1) : base(11) => Item1 = item1;
		}

		internal class HashV : AstElem
		{
			public uint256 Item1 { get; }
			internal HashV(uint256 item1) : base(12) => Item1 = item1;
		}

		internal class HashW : AstElem
		{
			public uint256 Item1 { get; }
			internal HashW(uint256 item1) : base(13) => Item1 = item1;
		}

		internal class True : AstElem
		{
			public AstElem Item1 { get; }
			internal True(AstElem item1) : base(14) => Item1 = item1;
		}
		internal class Wrap : AstElem
		{
			public AstElem Item1 { get; }
			internal Wrap(AstElem item1) : base(15) => Item1 = item1;
		}
		internal class Likely : AstElem
		{
			public AstElem Item1 { get; }
			internal Likely(AstElem item1) : base(16) => Item1 = item1;
		}
		internal class Unlikely : AstElem
		{
			public AstElem Item1 { get; }
			internal Unlikely(AstElem item1) : base(17) => Item1 = item1;
		}

		internal class AndCat : AstElem
		{
			public AstElem Item1 { get; }
			public AstElem Item2 { get; }
			internal AndCat(AstElem item1, AstElem item2) : base(18)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}
		internal class AndBool : AstElem
		{
			public AstElem Item1 { get; }
			public AstElem Item2 { get; }
			internal AndBool(AstElem item1, AstElem item2) : base(19)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}

		internal class AndCasc : AstElem
		{
			public AstElem Item1 { get; }
			public AstElem Item2 { get; }
			internal AndCasc(AstElem item1, AstElem item2) : base(20)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}

		internal class OrBool : AstElem
		{
			public AstElem Item1 { get; }
			public AstElem Item2 { get; }
			internal OrBool(AstElem item1, AstElem item2) : base(21)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}

		internal class OrCasc : AstElem
		{
			public AstElem Item1 { get; }
			public AstElem Item2 { get; }
			internal OrCasc(AstElem item1, AstElem item2) : base(22)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}
		internal class OrCont : AstElem
		{
			public AstElem Item1 { get; }
			public AstElem Item2 { get; }
			internal OrCont(AstElem item1, AstElem item2) : base(23)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}
		internal class OrKey : AstElem
		{
			public AstElem Item1 { get; }
			public AstElem Item2 { get; }
			internal OrKey(AstElem item1, AstElem item2) : base(24)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}

		internal class OrKeyV : AstElem
		{
			public AstElem Item1 { get; }
			public AstElem Item2 { get; }
			internal OrKeyV(AstElem item1, AstElem item2) : base(25)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}

		internal class OrIf : AstElem
		{
			public AstElem Item1 { get; }
			public AstElem Item2 { get; }
			internal OrIf(AstElem item1, AstElem item2) : base(26)
			{
				// Since this is most generic ast, assert in here for easy debugging.
				if (
					(item1.IsT() && item2.IsT()) ||
					(item1.IsF() && item2.IsF()) ||
					(item1.IsV() && item2.IsV()) ||
					(item1.IsQ() && item2.IsQ()) ||
					(item1.IsE() && item2.IsF())
					)
				{
					Item1 = item1;
					Item2 = item2;
				}
				else
				{
					throw new Exception($"Invalid type for AstElem.OrIf \n: item1: {item1},\n: item2: {item2}");
				}
			}
		}

		internal class OrIfV : AstElem
		{
			public AstElem Item1 { get; }
			public AstElem Item2 { get; }
			internal OrIfV(AstElem item1, AstElem item2) : base(27)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}

		internal class OrNotIf : AstElem
		{
			public AstElem Item1 { get; }
			public AstElem Item2 { get; }
			internal OrNotIf(AstElem item1, AstElem item2) : base(28)
			{
				if (item1.IsF() && item2.IsE())
				{
					Item1 = item1;
					Item2 = item2;
				}
				else
				{
					throw new Exception($"Invalid type for AstElem.OrNotIf \n: item1: {item1},\n: item2: {item2}");
				}
			}
		}

		internal class Thresh : AstElem
		{
			public UInt32 Item1 { get; }
			public AstElem[] Item2 { get; }
			internal Thresh(UInt32 item1, AstElem[] item2) : base(29)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}

		#region constructor

		public static AstElem NewPk(PubKey item) => new Pk(item);
		public static AstElem NewPkV(PubKey item) => new PkV(item);
		public static AstElem NewPkQ(PubKey item) => new PkQ(item);
		public static AstElem NewPkW(PubKey item) => new PkW(item);
		public static AstElem NewMulti(uint m, PubKey[] item) => new Multi(m, item);
		public static AstElem NewMultiV(uint m, PubKey[] item) => new MultiV(m, item);
		public static AstElem NewTimeT(uint item) => new TimeT(item);
		public static AstElem NewTimeV(uint item) => new TimeV(item);
		public static AstElem NewTimeF(uint item) => new TimeF(item);
		public static AstElem NewTime(uint item) => new Time(item);
		public static AstElem NewTimeW(uint item) => new TimeW(item);
		public static AstElem NewHashT(uint256 item) => new HashT(item);
		public static AstElem NewHashV(uint256 item) => new HashV(item);
		public static AstElem NewHashW(uint256 item) => new HashW(item);
		public static AstElem NewTrue(AstElem item) => new True(item);
		public static AstElem NewWrap(AstElem item) => new Wrap(item);
		public static AstElem NewLikely(AstElem item) => new Likely(item);
		public static AstElem NewUnlikely(AstElem item) => new Unlikely(item);
		public static AstElem NewAndCat(AstElem item1, AstElem item2) => new AndCat(item1, item2);
		public static AstElem NewAndBool(AstElem left, AstElem right) => new AndBool(left, right);
		public static AstElem NewAndCasc(AstElem left, AstElem right) => new AndCasc(left, right);
		public static AstElem NewOrBool(AstElem left, AstElem right) => new OrBool(left, right);
		public static AstElem NewOrCasc(AstElem left, AstElem right) => new OrCasc(left, right);
		public static AstElem NewOrCont(AstElem left, AstElem right) => new OrCont(left, right);

		public static AstElem NewOrKey(AstElem left, AstElem right) => new OrKey(left, right);
		public static AstElem NewOrKeyV(AstElem left, AstElem right) => new OrKeyV(left, right);
		public static AstElem NewOrIf(AstElem left, AstElem right) => new OrIf(left, right);
		public static AstElem NewOrIfV(AstElem left, AstElem right) => new OrIfV(left, right);
		public static AstElem NewOrNotIf(AstElem left, AstElem right) => new OrNotIf(left, right);
		public static AstElem NewThresh(uint item1, AstElem[] item2) => new Thresh(item1, item2);
		public static AstElem NewThreshV(uint item1, AstElem[] item2) => new ThreshV(item1, item2);

		#endregion

		#region Equatable members
		public sealed override int GetHashCode()
		{
			if (this != null)
			{
				int num = 0;
				switch (Tag)
				{
					case Tags.Pk:
						{
							Pk pk = (Pk)this;
							num = 0;
							return -1640531527 + pk.Item1.GetHashCode() + ((num << 6) + (num >> 2));
						}
					case Tags.PkV:
						{
							PkV pkv = (PkV)this;
							num = 1;
							return -1640531527 + pkv.Item1.GetHashCode() + ((num << 6) + (num >> 2));
						}
					case Tags.PkQ:
						{
							PkQ pkq = (PkQ)this;
							num = 2;
							return -1640531527 + pkq.Item1.GetHashCode() + ((num << 6) + (num >> 2));
						}
					case Tags.PkW:
						{
							PkW pkw = (PkW)this;
							num = 3;
							return -1640531527 + pkw.Item1.GetHashCode() + ((num << 6) + (num >> 2));
						}
					case Tags.Multi:
						{
							Multi multi = (Multi)this;
							num = 4;
							num = -1640531527 + (multi.Item2.GetHashCode()) + ((num << 6) + (num >> 2));
							return -1640531527 + ((int)multi.Item1 + ((num << 6) + (num >> 2)));
						}
					case Tags.MultiV:
						{
							MultiV multi = (MultiV)this;
							num = 5;
							num = -1640531527 + (multi.Item2.GetHashCode()) + ((num << 6) + (num >> 2));
							return -1640531527 + ((int)multi.Item1 + ((num << 6) + (num >> 2)));
						}
					case Tags.TimeT:
						{
							TimeT timet = (TimeT)this;
							num = 6;
							return -1640531527 + timet.Item1.GetHashCode() + ((num << 6) + (num >> 2));
						}
					case Tags.TimeV:
						{
							TimeV timev = (TimeV)this;
							num = 7;
							return -1640531527 + timev.Item1.GetHashCode() + ((num << 6) + (num >> 2));
						}
					case Tags.TimeF:
						{
							TimeF timef = (TimeF)this;
							num = 8;
							return -1640531527 + timef.Item1.GetHashCode() + ((num << 6) + (num >> 2));
						}
					case Tags.Time:
						{
							Time time = (Time)this;
							num = 9;
							return -1640531527 + time.Item1.GetHashCode() + ((num << 6) + (num >> 2));
						}
					case Tags.TimeW:
						{
							TimeW timew = (TimeW)this;
							num = 10;
							return -1640531527 + timew.Item1.GetHashCode() + ((num << 6) + (num >> 2));
						}
					case Tags.HashT:
						{
							HashT hasht = (HashT)this;
							num = 11;
							return -1640531527 + hasht.Item1.GetHashCode() + ((num << 6) + (num >> 2));
						}
					case Tags.HashV:
						{
							HashV hashv = (HashV)this;
							num = 12;
							return -1640531527 + hashv.Item1.GetHashCode() + ((num << 6) + (num >> 2));
						}
					case Tags.HashW:
						{
							HashW hashw = (HashW)this;
							num = 13;
							return -1640531527 + hashw.Item1.GetHashCode() + ((num << 6) + (num >> 2));
						}
					case Tags.True:
						{
							True self = (True)this;
							num = 14;
							return -1640531527 + self.Item1.GetHashCode() + ((num << 6) + (num >> 2));
						}
					case Tags.Wrap:
						{
							Wrap self = (Wrap)this;
							num = 15;
							return -1640531527 + self.Item1.GetHashCode() + ((num << 6) + (num >> 2));
						}
					case Tags.Likely:
						{
							Likely self = (Likely)this;
							num = 16;
							return -1640531527 + self.Item1.GetHashCode() + ((num << 6) + (num >> 2));
						}
					case Tags.Unlikely:
						{
							Unlikely self = (Unlikely)this;
							num = 17;
							return -1640531527 + self.Item1.GetHashCode() + ((num << 6) + (num >> 2));
						}
					case Tags.AndCat:
						{
							AndCat self = (AndCat)this;
							num = 18;
							num = -1640531527 + (self.Item1.GetHashCode()) + ((num << 6) + (num >> 2));
							return -1640531527 + (self.Item2.GetHashCode() + ((num << 6) + (num >> 2)));
						}
					case Tags.AndBool:
						{
							AndBool self = (AndBool)this;
							num = 19;
							num = -1640531527 + (self.Item1.GetHashCode()) + ((num << 6) + (num >> 2));
							return -1640531527 + (self.Item2.GetHashCode() + ((num << 6) + (num >> 2)));
						}
					case Tags.AndCasc:
						{
							AndCasc self = (AndCasc)this;
							num = 20;
							num = -1640531527 + (self.Item1.GetHashCode()) + ((num << 6) + (num >> 2));
							return -1640531527 + (self.Item2.GetHashCode() + ((num << 6) + (num >> 2)));
						}
					case Tags.OrBool:
						{
							OrBool self = (OrBool)this;
							num = 21;
							num = -1640531527 + (self.Item1.GetHashCode()) + ((num << 6) + (num >> 2));
							return -1640531527 + (self.Item2.GetHashCode() + ((num << 6) + (num >> 2)));
						}
					case Tags.OrCasc:
						{
							OrCasc self = (OrCasc)this;
							num = 22;
							num = -1640531527 + (self.Item1.GetHashCode()) + ((num << 6) + (num >> 2));
							return -1640531527 + (self.Item2.GetHashCode() + ((num << 6) + (num >> 2)));
						}
					case Tags.OrCont:
						{
							OrCont self = (OrCont)this;
							num = 23;
							num = -1640531527 + (self.Item1.GetHashCode()) + ((num << 6) + (num >> 2));
							return -1640531527 + (self.Item2.GetHashCode() + ((num << 6) + (num >> 2)));
						}
					case Tags.OrKey:
						{
							OrKey self = (OrKey)this;
							num = 24;
							num = -1640531527 + (self.Item1.GetHashCode()) + ((num << 6) + (num >> 2));
							return -1640531527 + (self.Item2.GetHashCode() + ((num << 6) + (num >> 2)));
						}
					case Tags.OrKeyV: {
							OrKeyV self = (OrKeyV)this;
							num = 25;
							num = -1640531527 + (self.Item1.GetHashCode()) + ((num << 6) + (num >> 2));
							return -1640531527 + (self.Item2.GetHashCode() + ((num << 6) + (num >> 2)));
						}
					case Tags.OrIf: {
							OrIf self = (OrIf)this;
							num = 26;
							num = -1640531527 + (self.Item1.GetHashCode()) + ((num << 6) + (num >> 2));
							return -1640531527 + (self.Item2.GetHashCode() + ((num << 6) + (num >> 2)));
						}
					case Tags.OrIfV: {
							OrIfV self = (OrIfV)this;
							num = 27;
							num = -1640531527 + (self.Item1.GetHashCode()) + ((num << 6) + (num >> 2));
							return -1640531527 + (self.Item2.GetHashCode() + ((num << 6) + (num >> 2)));
						}
					case Tags.OrNotIf: {
							OrNotIf self = (OrNotIf)this;
							num = 28;
							num = -1640531527 + (self.Item1.GetHashCode()) + ((num << 6) + (num >> 2));
							return -1640531527 + (self.Item2.GetHashCode() + ((num << 6) + (num >> 2)));
						}
					case Tags.Thresh: {
							Thresh self = (Thresh)this;
							num = 29;
							num = -1640531527 + (self.Item2.GetHashCode()) + ((num << 6) + (num >> 2));
							return -1640531527 + ((int)self.Item1 + ((num << 6) + (num >> 2)));
						}
					case Tags.ThreshV: {
							ThreshV self = (ThreshV)this;
							num = 30;
							num = -1640531527 + (self.Item2.GetHashCode()) + ((num << 6) + (num >> 2));
							return -1640531527 + ((int)self.Item1 + ((num << 6) + (num >> 2)));
						}
				}
				throw new Exception("Unreachable");
			}
			return 0;
		}

		public sealed override bool Equals(object obj)
		{
			AstElem astelem = obj as AstElem;
			if (astelem != null)
			{
				return Equals(astelem);
			}
			return false;
		}

		public bool Equals(AstElem obj)
		{
			while (this != null)
			{
				if (obj != null)
				{
					int tag = Tag;
					int tag2 = obj.Tag;
					if (tag == tag2)
					{
						switch (Tag)
						{
							case Tags.Pk:
								return ((Pk)this).Item1.Equals(((Pk)obj).Item1);
							case Tags.PkV:
								return ((PkV)this).Item1.Equals(((PkV)obj).Item1);
							case Tags.PkQ:
								return ((PkQ)this).Item1.Equals(((PkQ)obj).Item1);
							case Tags.PkW:
								return ((PkW)this).Item1.Equals(((PkW)obj).Item1);
							case Tags.Multi:
								var multi = (Multi)this;
								var multi2 = (Multi)obj;
								if (multi.Item1 == multi2.Item1)
								{
									return multi.Item2.SequenceEqual(multi2.Item2);
								}
								return false;
							case Tags.MultiV:
								var multiv = (MultiV)this;
								var multiv2 = (MultiV)obj;
								if (multiv.Item1 == multiv2.Item1)
								{
									return multiv.Item2.SequenceEqual(multiv2.Item2);
								}
								return false;
							case Tags.TimeT:
								return ((TimeT)this).Item1 == ((TimeT)obj).Item1;
							case Tags.TimeV:
								return ((TimeV)this).Item1 == ((TimeV)obj).Item1;
							case Tags.TimeF:
								return ((TimeF)this).Item1 == ((TimeF)obj).Item1;
							case Tags.Time:
								return ((Time)this).Item1 == ((Time)obj).Item1;
							case Tags.TimeW:
								return ((TimeW)this).Item1 == ((TimeW)obj).Item1;
							case Tags.HashT:
								return ((HashT)this).Item1 == ((HashT)obj).Item1;
							case Tags.HashV:
								return ((HashV)this).Item1 == ((HashV)obj).Item1;
							case Tags.HashW:
								return ((HashW)this).Item1 == ((HashW)obj).Item1;
							case Tags.True:
								return ((True)this).Item1.Equals(((True)obj).Item1);
							case Tags.Wrap:
								return ((Wrap)this).Item1.Equals(((Wrap)obj).Item1);
							case Tags.Likely:
								return ((Likely)this).Item1.Equals(((Likely)obj).Item1);
							case Tags.Unlikely:
								return ((Unlikely)this).Item1.Equals(((Unlikely)obj).Item1);
							case Tags.AndCat:
								var andcat = (AndCat)this;
								var andcat2 = (AndCat)obj;
								return andcat.Item1.Equals(andcat2.Item1) &&
									andcat.Item2.Equals(andcat2.Item2);
							case Tags.AndBool:
								var andbool = (AndBool)this;
								var andbool2 = (AndBool)obj;
								return andbool.Item1.Equals(andbool2.Item1) &&
									andbool.Item2.Equals(andbool2.Item2);
							case Tags.AndCasc:
								var andcasc = (AndCasc)this;
								var andcasc2 = (AndCasc)obj;
								return andcasc.Item1.Equals(andcasc2.Item1) &&
									andcasc.Item2.Equals(andcasc2.Item2);
							case Tags.OrBool:
								var orbool = (OrBool)this;
								var orbool2 = (OrBool)obj;
								return orbool.Item1.Equals(orbool2.Item1) &&
									orbool.Item2.Equals(orbool2.Item2);
							case Tags.OrCasc:
								var orcasc = (OrCasc)this;
								var orcasc2 = (OrCasc)obj;
								return orcasc.Item1.Equals(orcasc2.Item1) &&
									orcasc.Item2.Equals(orcasc2.Item2);
							case Tags.OrCont:
								var orcont = (OrCont)this;
								var orcont2 = (OrCont)obj;
								return orcont.Item1.Equals(orcont2.Item1) &&
									orcont.Item2.Equals(orcont2.Item2);
							case Tags.OrKey:
								var orkey = (OrKey)this;
								var orkey2 = (OrKey)obj;
								return orkey.Item1.Equals(orkey2.Item1) &&
									orkey.Item2.Equals(orkey2.Item2);
							case Tags.OrKeyV:
								var orkeyv = (OrKeyV)this;
								var orkeyv2 = (OrKeyV)obj;
								return orkeyv.Item1.Equals(orkeyv2.Item1) &&
									orkeyv.Item2.Equals(orkeyv2.Item2);
							case Tags.OrIf:
								var orif = (OrIf)this;
								var orif2 = (OrIf)obj;
								return orif.Item1.Equals(orif2.Item1) &&
									orif.Item2.Equals(orif2.Item2);
							case Tags.OrIfV:
								var orifv = (OrIfV)this;
								var orifv2 = (OrIfV)obj;
								return orifv.Item1.Equals(orifv2.Item1) &&
									orifv.Item2.Equals(orifv2.Item2);
							case Tags.OrNotIf:
								var ornotif = (OrNotIf)this;
								var ornotif2 = (OrNotIf)obj;
								return ornotif.Item1.Equals(ornotif2.Item1) &&
									ornotif.Item2.Equals(ornotif2.Item2);
							case Tags.Thresh:
								var thresh = (Thresh)this;
								var thresh2 = (Thresh)obj;
								return thresh.Item1 == thresh2.Item1 &&
									thresh.Item2.SequenceEqual(thresh2.Item2);
							case Tags.ThreshV:
								var threshv = (ThreshV)this;
								var threshv2 = (ThreshV)obj;
								return threshv.Item1 == threshv2.Item1 &&
									threshv.Item2.SequenceEqual(threshv2.Item2);
						}
					}
					return false;
				}
				return false;
			}
			return obj == null;
		}
		# endregion

		public class ThreshV : AstElem
		{
			public UInt32 Item1 { get; }
			public AstElem[] Item2 { get; }
			internal ThreshV(UInt32 item1, AstElem[] item2) : base(30)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}
		# endregion
		# region Switcher
		public bool IsPk() => Tag == 0;
		public bool IsPkV() => Tag == 1;
		public bool IsPkQ() => Tag == 2;
		public bool IsPkW() => Tag == 3;
		public bool IsMulti() => Tag == 4;
		public bool IsMultiV() => Tag == 5;
		public bool IsTimeT() => Tag == 6;
		public bool IsTimeV() => Tag == 7;
		public bool IsTimeF() => Tag == 8;
		public bool IsTime() => Tag == 9;
		public bool IsTimeW() => Tag == 10;
		public bool IsHashT() => Tag == 11;
		public bool IsHashV() => Tag == 12;
		public bool IsHashW() => Tag == 13;
		public bool IsTrue() => Tag == 14;
		public bool IsWrap() => Tag == 15;
		public bool IsLikely() => Tag == 16;
		public bool IsUnlikely() => Tag == 17;
		public bool IsAndCat() => Tag == 18;
		public bool IsAndBool() => Tag == 19;
		public bool IsAndCast() => Tag == 20;
		public bool IsOrBool() => Tag == 21;
		public bool IsOrCasc() => Tag == 22;
		public bool IsOrCont() => Tag == 23;
		public bool IsOrKey() => Tag == 24;
		public bool IsOrKeyV() => Tag == 25;
		public bool IsOrIf() => Tag == 26;
		public bool IsOrIfV() => Tag == 27;
		public bool IsOrNotIf() => Tag == 28;
		public bool IsThresh() => Tag == 29;
		public bool IsThreshV() => Tag == 30;

		# endregion

		# region Is[E|Q|W|F|V|T]

		public bool IsE()
		{
			switch (this.Tag)
			{
				case Tags.Pk:
				case Tags.Multi:
				case Tags.Time:
					return true;
				case Tags.Likely:
					return ((Likely)this).Item1.IsF();
				case Tags.Unlikely:
					return ((Unlikely)this).Item1.IsF();
				case Tags.AndBool:
					return ((AndBool)this).Item1.IsE() &&
						((AndBool)this).Item2.IsW();
				case Tags.AndCasc:
					return ((AndCasc)this).Item1.IsE() &&
						((AndCasc)this).Item2.IsF();
				case Tags.OrBool:
					return ((OrBool)this).Item1.IsE() &&
						((OrBool)this).Item2.IsW();
				case Tags.OrCasc:
					return ((OrCasc)this).Item1.IsE() &&
						((OrCasc)this).Item2.IsE();
				case Tags.OrKey:
					return ((OrKey)this).Item1.IsQ() &&
						((OrKey)this).Item2.IsQ();
				case Tags.OrIf:
					return ((OrIf)this).Item1.IsE() &&
						((OrIf)this).Item2.IsF();
				case Tags.OrNotIf:
					return ((OrNotIf)this).Item1.IsF() &&
						((OrNotIf)this).Item2.IsE();
				case Tags.Thresh:
					var subs = ((Thresh)this).Item2;
					return subs.Length != 0 &&
						subs[0].IsE() &&
						subs.Skip(1).All(s => s.IsW());
			};
			return false;
		}

		public bool IsQ()
		{
			switch (this.Tag)
			{
				case Tags.PkQ:
					return true;
				case Tags.AndCat:
					return ((AndCat)this).Item1.IsV() && ((AndCat)this).Item2.IsQ();
				case Tags.OrIf:
					return ((OrIf)this).Item1.IsQ() && ((OrIf)this).Item2.IsQ();
			}
			return false;
		}


		public bool IsW()
		{
			switch(this.Tag)
			{
				case Tags.PkW:
				case Tags.TimeW:
				case Tags.HashW:
					return true;
				case Tags.Wrap:
					return ((Wrap)this).Item1.IsE();
			}
			return false;
		}
		public bool IsF()
		{
			switch (this.Tag)
			{
				case Tags.TimeF:
					return true;
				case Tags.True:
					return ((True)this).Item1.IsV();
				case Tags.AndCat:
					return ((AndCat)this).Item1.IsV() &&
						((AndCat)this).Item2.IsF();
				case Tags.OrIf:
					return ((OrIf)this).Item1.IsF() &&
						((OrIf)this).Item2.IsF();
			}
			return false;
		}
		public bool IsV()
		{
			switch (this.Tag)
			{
				case Tags.PkV:
				case Tags.MultiV:
				case Tags.TimeV:
				case Tags.HashV:
					return true;
				case Tags.AndCat:
					return ((AndCat)this).Item1.IsV() &&
						((AndCat)this).Item2.IsV();
				case Tags.OrCont:
					return ((OrCont)this).Item1.IsE() &&
						((OrCont)this).Item2.IsV();
				case Tags.OrKeyV:
					return ((OrKeyV)this).Item1.IsQ() &&
						((OrKeyV)this).Item2.IsQ();
				case Tags.OrIf:
					return ((OrIf)this).Item1.IsV() &&
						((OrIf)this).Item2.IsV();
				case Tags.OrIfV:
					return ((OrIfV)this).Item1.IsT() &&
						((OrIfV)this).Item2.IsT();
				case Tags.ThreshV:
					var subs = ((ThreshV)this).Item2;
					return subs.Length != 0 &&
						subs[0].IsE() &&
						subs.Skip(1).All(s => s.IsW());
			}
			return false;
		}

		public bool IsT()
		{
			switch (this.Tag)
			{
				case Tags.Pk:
				case Tags.Multi:
				case Tags.TimeT:
				case Tags.HashT:
					return true;
				case Tags.True:
					return ((True)this).Item1.IsV();
				case Tags.AndCat:
					return ((AndCat)this).Item1.IsV() &&
						((AndCat)this).Item2.IsT();
				case Tags.OrBool:
					return ((OrBool)this).Item1.IsE() &&
						((OrBool)this).Item2.IsW();
				case Tags.OrCasc:
					return ((OrCasc)this).Item1.IsE() &&
						((OrCasc)this).Item2.IsT();
				case Tags.OrKey:
					return ((OrKey)this).Item1.IsQ() &&
						((OrKey)this).Item2.IsQ();
				case Tags.OrIf:
					return ((OrIf)this).Item1.IsT() &&
						((OrIf)this).Item2.IsT();
				case Tags.Thresh:
					var subs = ((Thresh)this).Item2;
					return subs.Length != 0 &&
						subs[0].IsE() &&
						subs.Skip(1).All(s => s.IsW());
			};
			return false;
		}
		#endregion

		/// <summary>
		/// Note that this does not assure exact equality against original DSL
		/// Since we have no way to distinguish `or` and `aor`
		/// </summary>
		/// <returns></returns>
		public AbstractPolicy ToPolicy()
		{
			switch(this.Tag)
			{
				case Tags.Pk:
					return AbstractPolicy.NewCheckSig(((Pk)this).Item1);
				case Tags.PkV:
					return AbstractPolicy.NewCheckSig(((PkV)this).Item1);
				case Tags.PkQ:
					return AbstractPolicy.NewCheckSig(((PkQ)this).Item1);
				case Tags.PkW:
					return AbstractPolicy.NewCheckSig(((PkW)this).Item1);
				case Tags.Multi:
					return AbstractPolicy.NewMulti(((Multi)this).Item1, ((Multi)this).Item2);
				case Tags.MultiV:
					return AbstractPolicy.NewMulti(((MultiV)this).Item1, ((MultiV)this).Item2);
				case Tags.TimeT:
					return AbstractPolicy.NewTime(((TimeT)this).Item1);
				case Tags.TimeV:
					return AbstractPolicy.NewTime(((TimeV)this).Item1);
				case Tags.TimeF:
					return AbstractPolicy.NewTime(((TimeF)this).Item1);
				case Tags.Time:
					return AbstractPolicy.NewTime(((Time)this).Item1);
				case Tags.TimeW:
					return AbstractPolicy.NewTime(((TimeW)this).Item1);
				case Tags.HashT:
					return AbstractPolicy.NewHash(((HashT)this).Item1);
				case Tags.HashV:
					return AbstractPolicy.NewHash(((HashV)this).Item1);
				case Tags.HashW:
					return AbstractPolicy.NewHash(((HashW)this).Item1);
				case Tags.True:
					return ((True)this).Item1.ToPolicy();
				case Tags.Wrap:
					return ((Wrap)this).Item1.ToPolicy();
				case Tags.Likely:
					return ((Likely)this).Item1.ToPolicy();
				case Tags.Unlikely:
					return ((Unlikely)this).Item1.ToPolicy();
				case Tags.AndCat:
					return AbstractPolicy.NewAnd(
						((AndCat)this).Item1.ToPolicy(),
						((AndCat)this).Item2.ToPolicy()
					);
				case Tags.AndBool:
					return AbstractPolicy.NewAnd(
						((AndBool)this).Item1.ToPolicy(),
						((AndBool)this).Item2.ToPolicy()
					);
				case Tags.AndCasc:
					return AbstractPolicy.NewAnd(
						((AndCasc)this).Item1.ToPolicy(),
						((AndCasc)this).Item2.ToPolicy()
					);
				case Tags.OrBool:
					return AbstractPolicy.NewOr(
							((OrBool)this).Item1.ToPolicy(),
							((OrBool)this).Item2.ToPolicy()
						);
				case Tags.OrCasc:
					return AbstractPolicy.NewOr(
							((OrCasc)this).Item1.ToPolicy(),
							((OrCasc)this).Item2.ToPolicy()
						);
				case Tags.OrCont:
					return AbstractPolicy.NewOr(
							((OrCont)this).Item1.ToPolicy(),
							((OrCont)this).Item2.ToPolicy()
						);
				case Tags.OrKey:
					return AbstractPolicy.NewOr(
							((OrKey)this).Item1.ToPolicy(),
							((OrKey)this).Item2.ToPolicy()
						);
				case Tags.OrKeyV:
					return AbstractPolicy.NewOr(
							((OrKeyV)this).Item1.ToPolicy(),
							((OrKeyV)this).Item2.ToPolicy()
						);
				case Tags.OrIf:
					return AbstractPolicy.NewOr(
							((OrIf)this).Item1.ToPolicy(),
							((OrIf)this).Item2.ToPolicy()
						);
				case Tags.OrIfV:
					return AbstractPolicy.NewOr(
							((OrIfV)this).Item1.ToPolicy(),
							((OrIfV)this).Item2.ToPolicy()
						);
				case Tags.OrNotIf:
					return AbstractPolicy.NewOr(
							((OrNotIf)this).Item1.ToPolicy(),
							((OrNotIf)this).Item2.ToPolicy()
						);
				case Tags.Thresh:
					return AbstractPolicy.NewThreshold(
						((Thresh)this).Item1,
						((Thresh)this).Item2.Select(i => i.ToPolicy()).ToArray()
					);
				case Tags.ThreshV:
					return AbstractPolicy.NewThreshold(
						((ThreshV)this).Item1,
						((ThreshV)this).Item2.Select(i => i.ToPolicy()).ToArray()
					);
			};

			throw new Exception("Unreachable");
		}

		public Script ToScript()
			=> new Script(Serialize(new StringBuilder()).ToString());

		public override string ToString()
			=> DebugPrint(new StringBuilder()).ToString();

		private StringBuilder DebugPrint(StringBuilder sb)
		{
			switch (this)
			{
				case Pk self:
					return sb.AppendFormat("pk({0})", self.Item1);
				case PkV self:
					return sb.AppendFormat("pk_v({0})", self.Item1);
				case PkQ self:
					return sb.AppendFormat("pk_q({0})", self.Item1);
				case PkW self:
					return sb.AppendFormat("pk_w({0})", self.Item1);
				case Multi self:
					sb.AppendFormat("multi({0}", self.Item1);
					foreach (var pk in self.Item2)
						sb.AppendFormat(",{0}", pk);
					return sb.Append(")");
				case MultiV self:
					sb.AppendFormat("multi_v({0}", self.Item1);
					foreach (var pk in self.Item2)
						sb.AppendFormat(",{0}", pk);
					return sb.Append(")");
				case TimeT self:
					return sb.AppendFormat("time_t({0})", self.Item1);
				case TimeV self:
					return sb.AppendFormat("time_v({0})", self.Item1);
				case TimeF self:
					return sb.AppendFormat("time_f({0})", self.Item1);
				case Time self:
					return sb.AppendFormat("time({0})", self.Item1);
				case TimeW self:
					return sb.AppendFormat("time_w({0})", self.Item1);
				case HashT self:
					return sb.AppendFormat("hash_t({0})", self.Item1);
				case HashV self:
					return sb.AppendFormat("hash_v({0})", self.Item1);
				case HashW self:
					return sb.AppendFormat("hash_w({0})", self.Item1);
				case True self:
					sb.Append("true(");
					self.Item1.DebugPrint(sb);
					return sb.Append(")");
				case Wrap self:
					sb.Append("wrap(");
					self.Item1.DebugPrint(sb);
					return sb.Append(")");
				case Likely self:
					sb.Append("likely(");
					self.Item1.DebugPrint(sb);
					return sb.Append(")");
				case Unlikely self:
					sb.Append("unlikely(");
					self.Item1.DebugPrint(sb);
					return sb.Append(")");
				case AndCat self:
					sb.Append("and_cat(");
					self.Item1.DebugPrint(sb);
					sb.Append(",");
					self.Item2.DebugPrint(sb);
					return sb.Append(")");
				case AndBool self:
					sb.Append("and_bool(");
					self.Item1.DebugPrint(sb);
					sb.Append(",");
					self.Item2.DebugPrint(sb);
					return sb.Append(")");
				case AndCasc self:
					sb.Append("and_casc(");
					self.Item1.DebugPrint(sb);
					sb.Append(",");
					self.Item2.DebugPrint(sb);
					return sb.Append(")");
				case OrBool self:
					sb.Append("or_bool(");
					self.Item1.DebugPrint(sb);
					sb.Append(",");
					self.Item2.DebugPrint(sb);
					return sb.Append(")");
				case OrCasc self:
					sb.Append("or_casc(");
					self.Item1.DebugPrint(sb);
					sb.Append(",");
					self.Item2.DebugPrint(sb);
					return sb.Append(")");
				case OrCont self:
					sb.Append("or_cont(");
					self.Item1.DebugPrint(sb);
					sb.Append(",");
					self.Item2.DebugPrint(sb);
					return sb.Append(")");
				case OrKey self:
					sb.Append("or_key(");
					self.Item1.DebugPrint(sb);
					sb.Append(",");
					self.Item2.DebugPrint(sb);
					return sb.Append(")");
				case OrKeyV self:
					sb.Append("or_key_v(");
					self.Item1.DebugPrint(sb);
					sb.Append(",");
					self.Item2.DebugPrint(sb);
					return sb.Append(")");
				case OrIf self:
					sb.Append("or_if(");
					self.Item1.DebugPrint(sb);
					sb.Append(",");
					self.Item2.DebugPrint(sb);
					return sb.Append(")");
				case OrIfV self:
					sb.Append("or_if_v(");
					self.Item1.DebugPrint(sb);
					sb.Append(",");
					self.Item2.DebugPrint(sb);
					return sb.Append(")");
				case OrNotIf self:
					sb.Append("or_notif(");
					self.Item1.DebugPrint(sb);
					sb.Append(",");
					self.Item2.DebugPrint(sb);
					return sb.Append(")");
				case Thresh self:
					sb.AppendFormat("thresh({0}", self.Item1);
					foreach (var sub in self.Item2)
					{
						sb.Append(",");
						sub.DebugPrint(sb);
					}
					return sb.Append(")");
				case ThreshV self:
					sb.AppendFormat("thresh_v({0}", self.Item1);
					foreach (var sub in self.Item2)
					{
						sb.Append(",");
						sub.DebugPrint(sb);
					}
					return sb.Append(")");
			}

			throw new Exception("Unreachable");
		}
		private StringBuilder Serialize(StringBuilder sb)
		{
			switch (this)
			{
				case Pk self:
					return sb.AppendFormat(" {0} OP_CHECKSIG", self.Item1);
				case PkV self:
					return sb.AppendFormat(" {0} OP_CHECKSIGVERIFY", self.Item1);
				case PkQ self:
					return sb.AppendFormat(" {0}", self.Item1);
				case PkW self:
					return sb.AppendFormat(" OP_SWAP {0} OP_CHECKSIG", self.Item1);
				case Multi self:
					sb.AppendFormat(" {0}", EncodeUInt( self.Item1));
					foreach (var pk in self.Item2)
						sb.AppendFormat(" {0}", pk.ToHex());
					return sb.AppendFormat(" {0} OP_CHECKMULTISIG", EncodeUInt((uint)self.Item2.Length));
				case MultiV self:
					sb.AppendFormat(" {0}", EncodeUInt(self.Item1));
					foreach (var pk in self.Item2)
						sb.AppendFormat(" {0}", pk.ToHex());
					return sb.AppendFormat(" {0} OP_CHECKMULTISIGVERIFY", EncodeUInt((uint)self.Item2.Length));
				case TimeT self:
					return sb.AppendFormat(" {0} OP_CSV", EncodeUInt(self.Item1));
				case TimeV self:
					return sb.AppendFormat(" {0} OP_CSV OP_DROP", EncodeUInt(self.Item1));
				case TimeF self:
					return sb.AppendFormat(" {0} OP_CSV OP_0NOTEQUAL", EncodeUInt(self.Item1));
				case Time self:
					return sb.AppendFormat(" OP_DUP OP_IF {0} OP_CSV OP_DROP OP_ENDIF", EncodeUInt(self.Item1));
				case TimeW self:
					return sb.AppendFormat(" OP_SWAP OP_DUP OP_IF {0} OP_CSV OP_DROP OP_ENDIF", EncodeUInt(self.Item1));
				case HashT self:
					return sb.AppendFormat(" OP_SIZE {0} OP_EQUALVERIFY OP_SHA256 {1} OP_EQUAL", EncodeUInt(32), self.Item1);
				case HashV self:
					return sb.AppendFormat(" OP_SIZE {0} OP_EQUALVERIFY OP_SHA256 {1} OP_EQUALVERIFY", EncodeUInt(32), self.Item1);
				case HashW self:
					return sb.AppendFormat(" OP_SWAP OP_SIZE OP_0NOTEQUAL OP_IF OP_SIZE {0} OP_EQUALVERIFY OP_SHA256 {1} OP_EQUALVERIFY 1 OP_ENDIF", EncodeUInt(32),self.Item1);
				case True self:
					self.Item1.Serialize(sb);
					return sb.Append(" 1");
				case Wrap self:
					sb.Append(" OP_TOALTSTACK");
					self.Item1.Serialize(sb);
					return sb.Append(" OP_FROMALTSTACK");
				case Likely self:
					sb.Append(" OP_NOTIF");
					self.Item1.Serialize(sb);
					return sb.Append(" OP_ELSE 0 OP_ENDIF");
				case Unlikely self:
					sb.Append(" OP_IF");
					self.Item1.Serialize(sb);
					return sb.Append(" OP_ELSE 0 OP_ENDIF");
				case AndCat self:
					self.Item1.Serialize(sb);
					return self.Item2.Serialize(sb);
				case AndBool self:
					self.Item1.Serialize(sb);
					self.Item2.Serialize(sb);
					return sb.Append(" OP_BOOLAND");
				case AndCasc self:
					self.Item1.Serialize(sb);
					sb.Append(" OP_NOTIF 0 OP_ELSE");
					self.Item2.Serialize(sb);
					return sb.Append(" OP_ENDIF");
				case OrBool self:
					self.Item1.Serialize(sb);
					self.Item2.Serialize(sb);
					return sb.Append(" OP_BOOLOR");
				case OrCasc self:
					self.Item1.Serialize(sb);
					sb.Append(" OP_IFDUP OP_NOTIF");
					self.Item2.Serialize(sb);
					return sb.Append(" OP_ENDIF");
				case OrCont self:
					self.Item1.Serialize(sb);
					sb.Append(" OP_NOTIF");
					self.Item2.Serialize(sb);
					return sb.Append(" OP_ENDIF");
				case OrKey self:
					sb.Append(" OP_IF");
					self.Item1.Serialize(sb);
					sb.Append(" OP_ELSE");
					self.Item1.Serialize(sb);
					return sb.Append(" OP_ENDIF OP_CHECKSIG");
				case OrKeyV self:
					sb.Append(" OP_IF");
					self.Item1.Serialize(sb);
					sb.Append(" OP_ELSE");
					self.Item1.Serialize(sb);
					return sb.Append(" OP_ENDIF OP_CHECKSIGVERIFY");
				case OrIf self:
					sb.Append(" OP_IF");
					self.Item1.Serialize(sb);
					sb.Append(" OP_ELSE");
					self.Item2.Serialize(sb);
					return sb.Append(" OP_ENDIF");
				case OrIfV self:
					sb.Append(" OP_IF");
					self.Item1.Serialize(sb);
					sb.Append(" OP_ELSE");
					self.Item2.Serialize(sb);
					return sb.Append(" OP_ENDIF OP_VERIFY");
				case OrNotIf self:
					sb.Append(" OP_NOTIF");
					self.Item1.Serialize(sb);
					sb.Append(" OP_ELSE");
					self.Item2.Serialize(sb);
					return sb.Append(" OP_ENDIF");
				case Thresh self:
					for (int i = 0; i < self.Item2.Length; i++)
					{
						self.Item2[i].Serialize(sb);
						if (i > 0)
							sb.Append(" OP_ADD");
					}
					return sb.AppendFormat(" {0} OP_EQUAL", EncodeUInt(self.Item1));
				case ThreshV self:
					for (int i = 0; i < self.Item2.Length; i++)
					{
						self.Item2[i].Serialize(sb);
						if (i > 0)
							sb.Append(" OP_ADD");
					}
					return sb.AppendFormat(" {0} OP_EQUALVERIFY", EncodeUInt(self.Item1));
			}
			throw new Exception("Unreachable");
		}

		private string EncodeUInt(UInt32 n)
			=> Op.GetPushOp(n).ToString();

	}
}