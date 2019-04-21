using System;
using System.Linq;

namespace NBitcoin.Miniscript
{
	public class AbstractPolicy<T>
	{
		public enum Tags
		{
			CheckSig,
			Multi,
			Hash,
			Time,
			Threshold,
			Or,
			And,
			AsymmetricOr
		}

		public static PubKey CheckSig { get; }
		public Tuple<UInt32, PubKey[]> Multi { get; }
		public uint256 Hash { get; }
		public UInt32 Time { get; }
		public Tuple<UInt32, AbstractPolicy<T>[]> Threshold { get; }
		public Tuple<AbstractPolicy<T>, AbstractPolicy<T>> Or { get; }
		public Tuple<AbstractPolicy<T>, AbstractPolicy<T>> And { get; }
		public Tuple<AbstractPolicy<T>, AbstractPolicy<T>> AsymmetricOr { get; }

		public Tags Tag { get; }

		public override string ToString()
		{
			switch (this.Tag)
			{
				case Tags.CheckSig:
					return $"pk({this.CheckSig.ToHex()})";
				case Tags.Multi:
					var pks = string.Join(",", this.Multi.Item2.Select(t => t.ToHex()));
					return $"multi({this.Multi.Item1},{pks})";
				case Tags.Hash:
					return $"hash({this.Hash})";
				case Tags.Time:
					return $"time({this.Time})";
				case Tags.Threshold:
					var subs = string.Join(",", this.Threshold.Item2.Select(t => t.ToString()));
					return $"thres({this.Threshold.Item1},{subs})";
				case Tags.Or:
					return $"or({this.Or.Item1},{this.Or.Item2})";
			}
		}
	}

	internal class MiniscriptDSLParsers<TIn> : CharParsers<TIn>
	{
		public MiniscriptDSLParsers()
		{
	}
}