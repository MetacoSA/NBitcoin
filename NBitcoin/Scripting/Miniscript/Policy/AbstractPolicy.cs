using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NBitcoin.BouncyCastle.Asn1;
using NBitcoin.Scripting.Parser;


namespace NBitcoin.Scripting.Miniscript.Policy
{
	public class AbstractPolicy<TPk, TPKh> : IEquatable<AbstractPolicy<TPk, TPKh>>
		where TPk : class, IMiniscriptKey<TPKh>, new()
		where TPKh : class, IMiniscriptKeyHash, new()
	{
		#region Subtype definitions
		internal static class Tags
		{
			public const int Unsatisfiable = 0;
			public const int Trivial = 1;
			public const int KeyHash = 2;
			public const int After = 3;
			public const int Older = 4;
			public const int Sha256 = 5;
			public const int Hash256 = 6;
			public const int Ripemd160 = 7;
			public const int Hash160 = 8;
			public const int And = 9;
			public const int Or = 10;
			public const int Threshold = 11;
		}

		private int Tag;
		private AbstractPolicy(int tag) => Tag = tag;

		public static AbstractPolicy<TPk, TPKh> UnSatisfiable { get; } = new AbstractPolicy<TPk, TPKh>(Tags.Unsatisfiable);
		public static AbstractPolicy<TPk, TPKh> Trivial { get; } = new AbstractPolicy<TPk, TPKh>(Tags.Trivial);

		internal class KeyHash : AbstractPolicy<TPk, TPKh>
		{
			internal TPKh Item;
			internal KeyHash(TPKh item) : base(Tags.KeyHash)
			{
				Item = item;
			}
		}

		internal class After : AbstractPolicy<TPk, TPKh>
		{
			internal uint Item;

			public After(uint item) : base (Tags.After)
			{
				Item = item;
			}
		}

		internal class Older : AbstractPolicy<TPk, TPKh>
		{
			internal uint Item;

			public Older(uint item) : base (Tags.Older)
			{
				Item = item;
			}
		}

		internal class Sha256 : AbstractPolicy<TPk, TPKh>
		{
			internal uint256 Item;

			public Sha256(uint256 item) :base(Tags.Sha256)
			{
				Item = item;
			}
		}

		internal class Hash256 : AbstractPolicy<TPk, TPKh>
		{
			internal uint256 Item;

			public Hash256(uint256 item) :base(Tags.Hash256)
			{
				Item = item;
			}
		}

		internal class Ripemd160 : AbstractPolicy<TPk, TPKh>
		{
			internal uint160 Item;

			public Ripemd160(uint160 item) :base(Tags.Ripemd160)
			{
				Item = item;
			}
		}

		internal class Hash160 : AbstractPolicy<TPk, TPKh>
		{
			internal uint160 Item;

			public Hash160(uint160 item) :base(Tags.Hash160)
			{
				Item = item;
			}
		}

		internal class And : AbstractPolicy<TPk, TPKh>
		{
			internal List<AbstractPolicy<TPk, TPKh>> Item;

			public And(List<AbstractPolicy<TPk, TPKh>> item) : base(Tags.And)
			{
				Item = item;
			}
		}

		internal class Or : AbstractPolicy<TPk, TPKh>
		{
			internal List<AbstractPolicy<TPk, TPKh>> Item;

			public Or(List<AbstractPolicy<TPk, TPKh>> item) : base(Tags.Or)
			{
				Item = item;
			}
		}

		internal class Threshold : AbstractPolicy<TPk, TPKh>
		{
			internal uint Item1;
			internal List<AbstractPolicy<TPk, TPKh>> Item2;

			public Threshold(uint item1, List<AbstractPolicy<TPk, TPKh>> item2) : base (Tags.Threshold)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}

		public static AbstractPolicy<TPk, TPKh> NewUnsatisfiable() => UnSatisfiable;
		public static AbstractPolicy<TPk, TPKh> NewTrivial() => Trivial;
		public static AbstractPolicy<TPk, TPKh> NewKeyHash(TPKh item) => new KeyHash(item);
		public static AbstractPolicy<TPk, TPKh> NewAfter(uint item) => new After(item);
		public static AbstractPolicy<TPk, TPKh> NewOlder(uint item) => new Older(item);
		public static AbstractPolicy<TPk, TPKh> NewSha256(uint256 item) => new Sha256(item);
		public static AbstractPolicy<TPk, TPKh> NewHash256(uint256 item) => new Hash256(item);
		public static AbstractPolicy<TPk, TPKh> NewRipemd160(uint160 item) => new Ripemd160(item);
		public static AbstractPolicy<TPk, TPKh> NewHash160(uint160 item) => new Hash160(item);
		public static AbstractPolicy<TPk, TPKh> NewAnd(IEnumerable<AbstractPolicy<TPk, TPKh>> item) => new And(item.ToList());
		public static AbstractPolicy<TPk, TPKh> NewOr(IEnumerable<AbstractPolicy<TPk, TPKh>> item) => new Or(item.ToList());
		public static AbstractPolicy<TPk, TPKh> NewThreshold(uint item1, IEnumerable<AbstractPolicy<TPk, TPKh>> item2) => new Threshold(item1, item2.ToList());

		#endregion

		#region Equatable members

		public override int GetHashCode()
		{
			switch (Tag)
			{
				case Tags.Unsatisfiable: return Tags.Unsatisfiable;
				case Tags.Trivial: return Tags.Trivial;
			}

			int num = 0;
			switch (this)
			{
				case KeyHash self:
				{
					num = Tags.KeyHash;
					return -1640531527 + self.Item.GetHashCode() + ((num << 6) + (num >> 2));
				}
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
				case And self:
					num = Tags.And;
					foreach (var subPolicy in self.Item)
					{
						num = -1640531527 + subPolicy.GetHashCode() + ((num << 6) + (num >> 2));
					}
					return num;
				case Or self:
					num = Tags.Or;
					foreach (var subPolicy in self.Item)
					{
						num = -1640531527 + subPolicy.GetHashCode() + ((num << 6) + (num >> 2));
					}
					return num;
				case Threshold self:
					num = Tags.Threshold;
					num = -1640531527 + self.Item1.GetHashCode() + ((num << 6) + (num >> 2));
					foreach (var subPolicy in self.Item2)
					{
						num = -1640531527 + subPolicy.GetHashCode() + ((num << 6) + (num >> 2));
					}

					return num;
			}

			throw new Exception("Unreachable!");
		}


		public bool Equals(AbstractPolicy<TPk, TPKh> other)
		{
			if (other == null)
				return false;
			if (this.Tag != other.Tag)
				return false;
			switch (this.Tag)
			{
				case (Tags.Unsatisfiable): return true;
				case (Tags.Trivial): return true;
			}
			switch (this)
			{
				case KeyHash self:
					return self.Item.Equals(((KeyHash) other).Item);
				case After self:
					return self.Item.Equals(((After)other).Item);
				case Older self:
					return self.Item.Equals(((Older)other).Item);
				case Sha256 self:
					return self.Item.Equals(((Sha256)other).Item);
				case Hash256 self:
					return self.Item.Equals(((Hash256)other).Item);
				case Ripemd160 self:
					return self.Item.Equals(((Ripemd160)other).Item);
				case Hash160 self:
					return self.Item.Equals(((Hash160)other).Item);
				case And self:
					var andSet = new HashSet<AbstractPolicy<TPk, TPKh>>(self.Item);
					return andSet.SetEquals(((And)other).Item);
				case Or self:
					var orSet = new HashSet<AbstractPolicy<TPk, TPKh>>(self.Item);
					return orSet.SetEquals(((Or)other).Item);
				case Threshold self:
					var subSet = new HashSet<AbstractPolicy<TPk, TPKh>>(self.Item2);
					var o = (Threshold) other;
					return (self.Item1 == o.Item1) && (subSet.SetEquals(o.Item2));
			}
			throw new Exception("Unreachable!");
		}

		#endregion

		public static AbstractPolicy<TPk, TPKh> FromTree(Tree top)
		{
			var n = top.Name;
			var l = top.Args.Count;
			if (n == "UNSATISFIABLE" && l == 0)
				return UnSatisfiable;
			if (n == "TRIVIAL" && l == 0)
				return Trivial;
			if (n == "pkh" && l == 1)
				return Tree.Terminal(top.Args[0], pk => NewKeyHash(MiniscriptFragmentParser<TPk, TPKh>.ParseHash(pk)));
			if (n == "after" && l == 1)
				return Tree.Terminal(top.Args[0], x => NewAfter(UInt32.Parse(x)));
			if (n == "older" && l == 1)
				return Tree.Terminal(top.Args[0], x => NewOlder(UInt32.Parse(x)));
			if (n == "sha256" && l == 1)
				return Tree.Terminal(top.Args[0], x => NewSha256(uint256.Parse(x)));
			if (n == "hash256" && l == 1)
				return Tree.Terminal(top.Args[0], x => NewHash256(uint256.Parse(x)));
			if (n == "ripemd160" && l == 1)
				return Tree.Terminal(top.Args[0], x => NewRipemd160(uint160.Parse(x)));
			if (n == "hash160" && l == 1)
				return Tree.Terminal(top.Args[0], x => NewHash160(uint160.Parse(x)));
			if (n == "and")
			{
				if (!top.Args.Any())
					throw new ParsingException("and without args");
				var subsAnd = top.Args.Select(FromTree);
				return NewAnd(subsAnd);
			}
			if (n == "or")
			{
				if (!top.Args.Any())
					throw new ParsingException("and without args");
				var subsAnd = top.Args.Select(FromTree);
				return NewOr(subsAnd);
			}

			if (n == "thresh")
			{
				if (!top.Args[0].Args.Any())
					throw new ParsingException($"Unexpected {top.Args[0].Args[0].Name}");
				var thresh = UInt32.Parse(top.Args[0].Name);
				if (thresh >= l)
					throw new ParsingException(
						$"number of sub policy in threshold ({l}), exceeded k ({top.Args[0].Name})");
				return NewThreshold(thresh, top.Args.Skip(1).Select(FromTree));
			}

			throw new ParsingException($"Unexpected {n}. with {l} argument");
		}

		/// <summary>
		/// Flatten out trees of `And`s and `Or`s; eliminate `Trivial` and
		/// `Unsatisfiable`'s Does not reorder any branches; use `.sort`
		/// </summary>
		/// <returns></returns>
		public AbstractPolicy<TPk, TPKh> Normalize()
		{
			switch (this)
			{
				case And self:
					var retSubsAnd = new List<AbstractPolicy<TPk, TPKh>>();
					foreach (var sub in self.Item)
					{
						var s = sub.Normalize();
						if (s == Trivial)
						{
						}
						else if (s == UnSatisfiable)
						{
							return UnSatisfiable;
						}
						else if (s is And subAnd)
						{
							retSubsAnd.AddRange(subAnd.Item);
						}
						else
						{
							retSubsAnd.Add(s);
						}
					}

					if (retSubsAnd.Count == 0)
						return Trivial;
					else if (retSubsAnd.Count == 1)
						return retSubsAnd.First();
					else
						return NewAnd(retSubsAnd);

				case Or self:
					var retSubsOr = new List<AbstractPolicy<TPk, TPKh>>();
					foreach (var sub in self.Item)
					{
						if (sub == Trivial) return Trivial;
						else if (sub == UnSatisfiable) {}
						else if (sub is Or orSub) retSubsOr.AddRange(orSub.Item);
						else retSubsOr.Add(sub);
					}

					if (retSubsOr.Count == 0) return Trivial;
					else if (retSubsOr.Count == 1) return retSubsOr.First();
					else return NewOr(retSubsOr);
			}

			return this;
		}

		public override string ToString()
			=> ToStringCore(new StringBuilder()).ToString();

		private StringBuilder ToStringCore(StringBuilder sb)
		{
			switch (this.Tag)
			{
				case Tags.Unsatisfiable: return sb.Append("UNSATISFIABLE");
				case Tags.Trivial: return sb.Append("TRIVIAL");
			}
			switch (this)
			{
				case KeyHash self:
					return sb.AppendFormat("pkh({0})", self.Item.ToHex());
				case After self:
					return sb.AppendFormat("after({0})", self.Item);
				case Older self:
					return sb.AppendFormat("older({0})", self.Item);
				case Sha256 self:
					return sb.AppendFormat("sha256({0})", self.Item);
				case Hash256 self:
					return sb.AppendFormat("hash256({0})", self.Item);
				case Ripemd160 self:
					return sb.AppendFormat("ripemd160({0})", self.Item);
				case Hash160 self:
					return sb.AppendFormat("hash160({0})", self.Item);
				case And self:
					sb.Append("and(");
					if (self.Item.Any())
					{
						self.Item[0].ToStringCore(sb);
						foreach (var sub in self.Item.Skip(1))
						{
							sb.Append(",");
							sub.ToStringCore(sb);
						}
					}
					return sb.Append(")");
				case Or self:
					sb.Append("or(");
					if (self.Item.Any())
					{
						self.Item[0].ToStringCore(sb);
						foreach (var sub in self.Item.Skip(1))
						{
							sb.Append(",");
							sub.ToStringCore(sb);
						}
					}
					return sb.Append(")");
				case Threshold self:
					sb.AppendFormat("thresh({0}",self.Item1);
					foreach (var sub in self.Item2)
					{
						sb.Append(",");
						sub.ToStringCore(sb);
					}
					return sb.Append(")");
			}
			throw new Exception("Unreachable!");
		}
	}
}
