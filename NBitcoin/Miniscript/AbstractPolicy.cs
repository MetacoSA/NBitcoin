
using System;
using System.Collections;
using System.Linq;

namespace NBitcoin.Miniscript
{
	/// <summary>
	/// High level representation of Miniscript
	/// </summary>
	public abstract class AbstractPolicy : IEquatable<AbstractPolicy>
	{
		public static class Tags
		{
			public const int CheckSig = 0;
			public const int Multi = 1;
			public const int Hash = 2;
			public const int Time = 3;
			public const int Threshold = 4;
			public const int Or = 5;
			public const int And = 6;
			public const int AsymmetricOr = 7;
		}

		private AbstractPolicy(int tag)
		{
			this.Tag = tag;
		}

		#region subclasses
		public class CheckSig : AbstractPolicy
		{
			public PubKey Item { get; }
			internal CheckSig(PubKey item) : base(0)
			{
				this.Item = item;
			}
		}

		public class Multi : AbstractPolicy
		{
			public UInt32 Item1 { get; }
			public PubKey[] Item2 { get; }
			internal Multi(UInt32 item1, PubKey[] item2) : base(1)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}

		public class Hash : AbstractPolicy
		{
			public uint256 Item { get; }
			internal Hash(uint256 item) : base(2) => Item = item;
		}

		public class Time : AbstractPolicy
		{
			public UInt32 Item { get; }
			internal Time(UInt32 item) : base(3) => Item = item;
		}

		public class Threshold : AbstractPolicy
		{
			public UInt32 Item1 { get; }
			public AbstractPolicy[] Item2 { get; }
			internal Threshold(UInt32 item1, AbstractPolicy[] item2) : base(4)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}

		public class Or : AbstractPolicy
		{
			public AbstractPolicy Item1 { get; }
			public AbstractPolicy Item2 { get; }
			internal Or(AbstractPolicy item1, AbstractPolicy item2) : base(5)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}

		public class And : AbstractPolicy
		{
			public AbstractPolicy Item1 { get; }
			public AbstractPolicy Item2 { get; }
			internal And(AbstractPolicy item1, AbstractPolicy item2) : base(6)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}
		public class AsymmetricOr : AbstractPolicy
		{
			public AbstractPolicy Item1 { get; }
			public AbstractPolicy Item2 { get; }
			internal AsymmetricOr(AbstractPolicy item1, AbstractPolicy item2) : base(7)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}

		#endregion

		public int Tag { get; }

		#region Switcher
		public bool IsCheckSig() => Tag == 0;
		public bool IsMulti() => Tag == 1;
		public bool IsHash() => Tag == 2;
		public bool IsTime() => Tag == 3;
		public bool IsThreshold() => Tag == 4;
		public bool IsOr() => Tag == 5;
		public bool IsAnd() => Tag == 6;
		public bool IsAsymmetricOr() => Tag == 7;
		#endregion

		#region Constructors
		public static AbstractPolicy NewCheckSig(PubKey pubkey) =>
			new CheckSig(pubkey);

		public static AbstractPolicy NewMulti(UInt32 m, PubKey[] pks) =>
			new Multi(m, pks);

		public static AbstractPolicy NewHash(uint256 hash) =>
			new Hash(hash);

		public static AbstractPolicy NewTime(UInt32 time) =>
			new Time(time);

		public static AbstractPolicy NewThreshold(UInt32 threshold, AbstractPolicy[] subPolicies) =>
			new Threshold(threshold, subPolicies);

		public static AbstractPolicy NewOr(AbstractPolicy left, AbstractPolicy right) =>
			new Or(left, right);

		public static AbstractPolicy NewAnd(AbstractPolicy left, AbstractPolicy right) =>
			new And(left, right);

		public static AbstractPolicy NewAsymmetricOr(AbstractPolicy left, AbstractPolicy right) =>
			new AsymmetricOr(left, right);


		#endregion

		public sealed override int GetHashCode()
		{
			if (this != null)
			{
				int num = 0;
				switch (Tag)
				{
					default:
						{
							CheckSig checksig = (CheckSig)this;
							num = 0;
							return -1640531527 + checksig.Item.GetHashCode() + ((num << 6) + (num >> 2));
						}
					case 1:
						{
							Multi multi = (Multi)this;
							num = 1;
							num = -1640531527 + (multi.Item2.GetHashCode()) + ((num << 6) + (num >> 2));
							return -1640531527 + ((int)multi.Item1 + ((num << 6) + (num >> 2)));
						}
					case 2:
						{
							Hash hash = (Hash)this;
							num = 2;
							return -1640531527 + hash.Item.GetHashCode() + ((num << 6) + (num >> 2));
						}
					case 3:
						{
							Time time = (Time)this;
							num = 3;
							return -1640531527 + time.Item.GetHashCode() + ((num << 6) + (num >> 2));
						}
					case 4:
						{
							Threshold threshold = (Threshold)this;
							num = 4;
							num = -1640531527 + threshold.Item2.GetHashCode() + ((num << 6) + (num >> 2));
							return -1640531527 + ((int)threshold.Item1 + ((num << 6) + (num >> 2)));
						}
					case 5:
						{
							Or or = (Or)this;
							num = 5;
							num = -1640531527 + (or.Item2.GetHashCode() + ((num << 6) + (num >> 2)));
							return -1640531527 + (or.Item1.GetHashCode() + ((num << 6) + (num >> 2)));
						}
					case 6:
						{
							And and = (And)this;
							num = 6;
							num = -1640531527 + (and.Item2.GetHashCode() + ((num << 6) + (num >> 2)));
							return -1640531527 + (and.Item1.GetHashCode() + ((num << 6) + (num >> 2)));
						}
					case 7:
						{
							AsymmetricOr asymmetricOr = (AsymmetricOr)this;
							num = 7;
							num = -1640531527 + (asymmetricOr.Item2.GetHashCode() + ((num << 6) + (num >> 2)));
							return -1640531527 + (asymmetricOr.Item1.GetHashCode() + ((num << 6) + (num >> 2)));
						}
				}
			}
			return 0;
		}

		public sealed override bool Equals(object obj)
		{
			AbstractPolicy abstractPolicy = obj as AbstractPolicy;
			if (abstractPolicy != null)
			{
				return Equals(abstractPolicy);
			}
			return false;
		}

		public bool Equals(AbstractPolicy obj)
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
							default:
								{
									CheckSig key = (CheckSig)this;
									CheckSig key2 = (CheckSig)obj;
									return key.Item.Equals(key2.Item);
								}
							case 1:
								{
									Multi multi = (Multi)this;
									Multi multi2 = (Multi)obj;
									if (multi.Item1 == multi2.Item1)
									{
										return multi.Item2.SequenceEqual(multi2.Item2);
									}
									return false;
								}
							case 2:
								{
									Hash hash = (Hash)this;
									Hash hash2 = (Hash)obj;
									return hash.Item.Equals(hash2.Item);
								}
							case 3:
								{
									Time time = (Time)this;
									Time time2 = (Time)obj;
									return time.Item.Equals(time2.Item);
								}
							case 4:
								{
									Threshold threshold = (Threshold)this;
									Threshold threshold2 = (Threshold)obj;
									if (threshold.Item1 == threshold2.Item1)
									{
										return threshold.Item2.SequenceEqual(threshold2.Item2);
									}
									return false;
								}
							case 5:
								{
									Or or = (Or)this;
									Or or2 = (Or)obj;
									return or.Item1.Equals(or.Item1) && or.Item2.Equals(or.Item2);
								}
							case 6:
								{
									And and = (And)this;
									And and2 = (And)obj;
									return and.Item1.Equals(and2.Item1) && and.Item2.Equals(and2.Item2);
								}
							case 7:
								{
									AsymmetricOr asymmetricOr = (AsymmetricOr)this;
									AsymmetricOr asymmetricOr2 = (AsymmetricOr)obj;
									return asymmetricOr.Item1.Equals(asymmetricOr2.Item1) &&
										asymmetricOr.Item2.Equals(asymmetricOr2.Item2);
								}
						}
					}
					return false;
				}
				return false;
			}
			return obj == null;
		}

		public override string ToString()
		{
			switch (this.Tag)
			{
				case Tags.CheckSig:
					return $"pk({((CheckSig)this).Item.ToHex()})";
				case Tags.Multi:
					var pks = string.Join(",", ((Multi)this).Item2.Select(t => t.ToHex()));
					return $"multi({((Multi)this).Item1},{pks})";
				case Tags.Hash:
					return $"hash({((Hash)this).Item})";
				case Tags.Time:
					return $"time({((Time)this).Item})";
				case Tags.Threshold:
					var subs = string.Join(",", ((Threshold)this).Item2.Select(t => t.ToString()));
					return $"thres({((Threshold)this).Item1},{subs})";
				case Tags.And:
					return $"and({((And)this).Item1},{((And)this).Item2})";
				case Tags.Or:
					return $"or({((Or)this).Item1},{((Or)this).Item2})";
				case Tags.AsymmetricOr:
					return $"aor({((AsymmetricOr)this).Item1},{((AsymmetricOr)this).Item2})";
			}
			throw new Exception("unreachable");
		}

	}
}