using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NBitcoin.Scripting.Parser;

namespace NBitcoin.Scripting.Miniscript.Policy
{
	public enum PolicyError
	{
		/// <summary>
		/// `And` fragments only support two args
		/// </summary>
		NonBinaryArgAnd,
		/// <summary>
		/// `Or` fragments only support two args
		/// </summary>
		NonBinaryArgOr,

		/// <summary>
		/// `Thresh` fragment can only have `1 <= k <= n`
		/// </summary>
		IncorrectThresh,

		/// <summary>
		/// `older` or `after` fragment can only have `n = 0`
		/// </summary>
		ZeroTime,

		/// <summary>
		/// `after` fragment can only have `n < 2^31`
		/// </summary>
		TimeTooFar
	}
	public class ConcretePolicy<TPk, TPKh> : IEquatable<ConcretePolicy<TPk, TPKh>>, ILiftable<TPk, TPKh>
		where TPk : IMiniscriptKey<TPKh>
		where TPKh : IMiniscriptKeyHash
	{
		#region Subtype definitions


		private static class Tags
		{
			internal const int Key = 0;
			internal const int After = 1;
			internal const int Older = 2;
			internal const int Sha256 = 3;
			internal const int Hash256 = 4;
			internal const int Ripemd160 = 5;
			internal const int Hash160 = 6;
			internal const int And = 7;
			internal const int Or = 8;
			internal const int Threshold = 9;
		}

		private int Tag { get; }

		private ConcretePolicy(int tag) => Tag = tag;

		internal class Key : ConcretePolicy<TPk, TPKh>
		{
			public TPk Item;
			public Key(TPk item) : base(Tags.Key) => Item = item;
		}

		internal class After : ConcretePolicy<TPk, TPKh>
		{
			public uint Item;

			public After(uint item) : base(Tags.After) => Item = item;
		}

		internal class Older : ConcretePolicy<TPk, TPKh>
		{
			public uint Item;

			public Older(uint item) : base(Tags.Older) => Item = item;
		}

		internal class Sha256 : ConcretePolicy<TPk, TPKh>
		{
			public uint256 Item;

			public Sha256(uint256 item) : base(Tags.Sha256) => Item = item;
		}

		internal class Hash256 : ConcretePolicy<TPk, TPKh>
		{
			public uint256 Item;

			public Hash256(uint256 item) : base(Tags.Hash256) => Item = item;
		}

		internal class Ripemd160 : ConcretePolicy<TPk, TPKh>
		{
			public uint160 Item;

			public Ripemd160(uint160 item) : base(Tags.Ripemd160) => Item = item;
		}

		internal class Hash160 : ConcretePolicy<TPk, TPKh>
		{
			public uint160 Item;
			public Hash160(uint160 item) : base(Tags.Hash160) => Item = item;
		}

		internal class And : ConcretePolicy<TPk, TPKh>
		{
			public List<ConcretePolicy<TPk, TPKh>> Item;

			public And(List<ConcretePolicy<TPk, TPKh>> item) : base(Tags.And)
			{
				Item = item;
			}
		}

		internal class Or : ConcretePolicy<TPk, TPKh>
		{
			public List<Tuple<uint, ConcretePolicy<TPk, TPKh>>> Item;

			public Or(List<Tuple<uint, ConcretePolicy<TPk, TPKh>>> item) : base(Tags.Or)
			{
				Item = item;
			}
		}

		internal class Threshold : ConcretePolicy<TPk, TPKh>
		{
			public uint Item1;
			public List<ConcretePolicy<TPk, TPKh>> Item2;

			public Threshold(uint item1, List<ConcretePolicy<TPk, TPKh>> item2) : base(Tags.Threshold)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}

		public static ConcretePolicy<TPk, TPKh> NewKey(TPk pk) => new Key(pk);
		public static ConcretePolicy<TPk, TPKh> NewAfter(uint time) => new After(time);
		public static ConcretePolicy<TPk, TPKh> NewOlder(uint item) => new Older(item);
		public static ConcretePolicy<TPk, TPKh> NewSha256(uint256 item) => new Sha256(item);
		public static ConcretePolicy<TPk, TPKh> NewHash256(uint256 item) => new Hash256(item);
		public static ConcretePolicy<TPk, TPKh> NewRipemd160(uint160 item) => new Ripemd160(item);
		public static ConcretePolicy<TPk, TPKh> NewHash160(uint160 item) => new Hash160(item);
		public static ConcretePolicy<TPk, TPKh> NewAnd(IEnumerable<ConcretePolicy<TPk, TPKh>> subs) => new And(subs.ToList());
		public static ConcretePolicy<TPk, TPKh> NewOr(IEnumerable<Tuple<uint, ConcretePolicy<TPk, TPKh>>> item) => new Or(item.ToList());
		public static ConcretePolicy<TPk, TPKh> NewThreshold(uint k, IEnumerable<ConcretePolicy<TPk, TPKh>> subs) => new Threshold(k, subs.ToList());

		#endregion


		#region Equatable members

		public bool Equals(ConcretePolicy<TPk, TPKh> other)
		{
			if (other == null || this.Tag != other.Tag)
				return false;
			switch (this)
			{
				case Key self:
					return self.Item.Equals(((Key)other).Item);
				case After self:
					return self.Item.Equals(((After)other).Item);
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
				case And self:
					var andSet = new HashSet<ConcretePolicy<TPk, TPKh>>(self.Item);
					return andSet.SetEquals(((And)other).Item);
				case Or self:
					var orSet = new HashSet<Tuple<uint, ConcretePolicy<TPk, TPKh>>>(self.Item);
					return orSet.SetEquals(((Or)other).Item);
				case Threshold self:
					var thres = (Threshold) other;
					var threshSet = new HashSet<ConcretePolicy<TPk, TPKh>>(self.Item2);
					return self.Item1 == thres.Item1 && threshSet.SetEquals(thres.Item2);
			}
			throw new Exception("Unreachable!");
		}

		public override int GetHashCode()
		{
			int num = 0;
			switch (this)
			{
				case Key self:
					num = Tags.Key;
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
				case And self:
					num = Tags.And;
					foreach (var a in self.Item)
					{
						num = -1640531527 + self.Item.GetHashCode() + ((num << 6) + (num >> 2));
					}
					return num;
				case Or self:
					num = Tags.Or;
					foreach (var a in self.Item)
					{
						num = -1640531527 + a.Item1.GetHashCode() + ((num << 6) + (num >> 2));
						num = -1640531527 + a.Item2.GetHashCode() + ((num << 6) + (num >> 2));
					}
					return num;
				case Threshold self:
					num = Tags.Threshold;
					num = -1640531527 + self.Item1.GetHashCode() + ((num << 6) + (num >> 2));
					foreach (var policy in self.Item2)
					{
						num = -1640531527 + policy.GetHashCode() + ((num << 6) + (num >> 2));
					}

					return num;
			}
			throw new Exception("Unreachable!");
		}

		public AbstractPolicy<TPk, TPKh> Lift()
		{
			AbstractPolicy<TPk, TPKh> res = null;
			switch (this)
			{
				case Key self:
					res = AbstractPolicy<TPk, TPKh>.NewKeyHash(self.Item.ToPubKeyHash());
					break;
				case After self:
					res = AbstractPolicy<TPk, TPKh>.NewAfter(self.Item);
					break;
				case Older self:
					res = AbstractPolicy<TPk, TPKh>.NewOlder(self.Item);
					break;
				case Sha256 self:
					res = AbstractPolicy<TPk, TPKh>.NewSha256(self.Item);
					break;
				case Hash256 self:
					res = AbstractPolicy<TPk, TPKh>.NewHash256(self.Item);
					break;
				case Ripemd160 self:
					res = AbstractPolicy<TPk, TPKh>.NewRipemd160(self.Item);
					break;
				case Hash160 self:
					res = AbstractPolicy<TPk, TPKh>.NewHash160(self.Item);
					break;
				case And self:
					res = AbstractPolicy<TPk, TPKh>.NewAnd(self.Item.Select(sub => sub.Lift()).ToList());
					break;
				case Or self:
					res = AbstractPolicy<TPk, TPKh>.NewOr(self.Item.Select(sub => sub.Item2.Lift()).ToList());
					break;
				case Threshold self:
					res = AbstractPolicy<TPk, TPKh>.NewThreshold(self.Item1, self.Item2.Select(sub => sub.Lift()).ToList());
					break;
			}

			return res.Normalize();

		}

		public override bool Equals(object obj) =>
			Equals(obj as ConcretePolicy<TPk, TPKh>);

		#endregion

		/// <summary>
		/// This returns whether the given policy is valid or not. It maybe possible that the policy
		/// contains Non-two argument `and`, `or` a `0` arg thresh.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="PolicyException"></exception>
		public bool IsValid()
		{
			switch (this)
			{
				case And self:
					if (self.Item.Count != 2)
						throw PolicyException.NonBinaryArgAnd;
					return self.Item.All(sub => sub.IsValid());
				case Or self:
					if (self.Item.Count != 2)
						throw PolicyException.IncorrectThresh;
					return self.Item.All(sub => sub.Item2.IsValid());
				case Threshold self:
					if (self.Item1 <= 0 || self.Item1 > self.Item2.Count)
						throw PolicyException.IncorrectThresh;
					return self.Item2.All(sub => sub.IsValid());
				case After self:
					if (self.Item == 0)
						throw PolicyException.ZeroTime;
					else if (self.Item > Math.Pow(2u, 31))
						throw PolicyException.TimeTooFar;
					return true;
				case Older self:
					if (self.Item == 0)
						throw PolicyException.ZeroTime;
					else if (self.Item > Math.Pow(2u, 31))
						throw PolicyException.TimeTooFar;
					return true;

			}

			return true;
		}

		/// <summary>
		/// This returns whether any possible compilation of the policy could be
		/// compiled as non-malleable and safe. Note that this returns a tuple
		/// (safe, non-malleable) to avoid because the non-malleability depends on
		/// safety and we would like to cache results
		/// </summary>
		/// <returns></returns>
		public Tuple<bool, bool> IsSafeNonMalleable()
		{
			switch (this)
			{
				case Key _ : return Tuple.Create(true, true);
				case Sha256 _:
				case Hash256 _:
				case Ripemd160 _:
				case Hash160 _:
				case After _:
				case Older _:
					return Tuple.Create(false, true);
				case Threshold self:
					var res =
						self.Item2
							.Select((sub) => sub.IsSafeNonMalleable())
							.Aggregate(Tuple.Create(0, 0), (acc, sub) =>
							{
								var safeCount = acc.Item1 + (sub.Item1 ? 1: 0);
								var nonMallCount = acc.Item2 + (sub.Item2 ? 1 : 0);
								return Tuple.Create(safeCount, nonMallCount);
							});
					var hasEnoughSafeSubs = (res.Item1 >= (self.Item2.Count - self.Item1 + 1));
					var hasEnoughNonMallCount = (res.Item2 == self.Item2.Count && res.Item2 >= (self.Item2.Count - self.Item1));
					return Tuple.Create(hasEnoughSafeSubs, hasEnoughNonMallCount);
				case And self:
					return
						self.Item
							.Select(sub => sub.IsSafeNonMalleable())
							.Aggregate(
								Tuple.Create(false, true),
								(acc, x) =>
								{
									var l = acc.Item1 || x.Item1; // true if it has at least one safe sub.
									var r = acc.Item2 && x.Item2; // true if all subs are non malleable
									return Tuple.Create(l, r);
								});
				case Or self:
					var resOr =
						self.Item
							.Select(sub => sub.Item2.IsSafeNonMalleable())
							.Aggregate(
								Tuple.Create(true, false, true),
								(acc, x) =>
								{
									var one = acc.Item1 && x.Item1; // all subs are safe
									var two = acc.Item2 || x.Item1; // at least one is safe
									var three = acc.Item3 && x.Item2; // all non malleable
									return Tuple.Create(one, two, three);
								}
							);
					return Tuple.Create(resOr.Item1, resOr.Item2 && resOr.Item3);

			}
			throw new Exception("Unreachable!");
		}

		public override string ToString() =>
			ToStringCore(new StringBuilder()).ToString();

		private StringBuilder ToStringCore(StringBuilder sb)
		{
			switch (this)
			{
				case Key self:
					return sb.Append($"pk({self.Item.ToHex()})");
				case After self:
					return sb.Append($"after({self.Item.ToString()})");
				case Older self:
					return sb.Append($"older({self.Item.ToString()})");
				case Sha256 self:
					return sb.Append($"sha256({self.Item})");
				case Hash256 self:
					return sb.Append($"hash256({self.Item})");
				case Ripemd160 self:
					return sb.Append($"ripemd160({self.Item})");
				case Hash160 self:
					return sb.Append($"hash160({self.Item})");
				case And self:
					sb.Append("and(");
					if (self.Item.Count != 0)
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
					if (self.Item.Count != 0)
					{
						sb.Append(self.Item[0].Item1.ToString());
						sb.Append("@");
						self.Item[0].Item2.ToStringCore(sb);
						foreach (var sub in self.Item)
						{
							sb.Append(",");
							sb.Append(sub.Item1.ToString());
							sb.Append("@");
							sub.Item2.ToStringCore(sb);
						}
					}
					return sb.Append("(");
				case Threshold self:
					sb.Append($"thresh({self.Item1.ToString()}");
					foreach (var sub in self.Item2)
					{
						sb.Append(",");
						sub.ToStringCore(sb);
					}

					return sb.Append(")");
			}
			throw new Exception("Unreachable!");
		}

		public static ConcretePolicy<TPk, TPKh> FromTree()
		{

			throw new NotImplementedException("");
		}

		private static Tuple<uint, ConcretePolicy<TPk, TPKh>> FromTreeProb<T>(Tree<T> top, bool allowProb)
		{
			int fragProb = default;
			string fragName = default;
			var nameSplit = top.Name.Split('@');
			if (nameSplit.Length == 0)
			{
				fragProb = 1;
				fragName = "";
			}
			else if (nameSplit.Length == 1)
			{
				fragProb = 1;
				fragName = nameSplit[0];
			}
			else if (nameSplit.Length == 2)
			{
				if (!allowProb)
					throw new ParsingException("we found @ in outside of `or` expression");
				fragProb = Int32.Parse(nameSplit[0]);
				fragName = nameSplit[1];
			}
			else
			{
				throw new ParsingException("We found more than one @ in expression");
			}

			throw new NotImplementedException();
			// if (fragName == "pk" && top.Args.Length == 1)

		}
	}
}
