using System;
using System.Linq;

namespace NBitcoin.Miniscript
{
	/// <summary>
	/// Internal representation of AbstractPolicy.
	/// Which can cash the compilation result to improve performance.
	/// </summary>
	internal abstract class CompiledNodeContent
	{
		internal static class Tags
		{
			public const int Pk = 0;
			public const int Multi = 1;
			public const int Time = 2;
			public const int Hash = 3;
			public const int And = 4;
			public const int Or = 5;
			public const int Thresh = 6;
		}

		public int Tag { get; }

		private CompiledNodeContent(int tag) => Tag = tag;

		public class Pk : CompiledNodeContent
		{
			public PubKey Item1 { get; }
			public Pk(PubKey item1) : base (0) => Item1 = item1;
		}

		public class Multi : CompiledNodeContent
		{
			public UInt32 Item1 { get; }
			public PubKey[] Item2 { get; }
			public Multi(UInt32 item1, PubKey[] item2) : base (1)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}

		public class Time : CompiledNodeContent
		{
			public UInt32 Item1 { get; }
			public Time(UInt32 item1) : base(2) => Item1 = item1;
		}

		public class Hash : CompiledNodeContent
		{
			public uint256 Item1 { get; }
			public Hash(uint256 item1) : base(3) => Item1 = item1;
		}

		public class And : CompiledNodeContent
		{
			public CompiledNode Item1 { get; }
			public CompiledNode Item2 { get; }
			public And(CompiledNode item1, CompiledNode item2) : base(4)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}

		public class Or : CompiledNodeContent
		{
			public CompiledNode Item1 { get; }
			public CompiledNode Item2 { get; }
			public double Item3 { get; }
			public double Item4 { get; }
			public Or(
				CompiledNode item1,
				CompiledNode item2,
				double item3,
				double item4
				) : base(5)
			{
				Item1 = item1;
				Item2 = item2;
				Item3 = item3;
				Item4 = item4;
			}
		}

		public class Thresh : CompiledNodeContent
		{
			public UInt32 Item1 { get; }
			public CompiledNode[] Item2 { get; }
			public Thresh(UInt32 item1, CompiledNode[] item2) : base(6)
			{
				Item1 = item1;
				Item2 = item2;
			}
		}

		public static CompiledNodeContent FromPolicy(AbstractPolicy policy)
		{
			switch (policy)
			{
				case AbstractPolicy.CheckSig p:
					return new CompiledNodeContent.Pk(p.Item);
				case AbstractPolicy.Multi p:
					return new CompiledNodeContent.Multi(p.Item1, p.Item2);
				case AbstractPolicy.Hash p:
					return new CompiledNodeContent.Hash(p.Item);
				case AbstractPolicy.Time p:
					return new CompiledNodeContent.Time(p.Item);
				case AbstractPolicy.And p:
					return new CompiledNodeContent.And(
						CompiledNode.FromPolicy(p.Item1),
						CompiledNode.FromPolicy(p.Item2)
						);
				case AbstractPolicy.Or p:
					return new CompiledNodeContent.Or(
						CompiledNode.FromPolicy(p.Item1),
						CompiledNode.FromPolicy(p.Item2),
						0.5,
						0.5
						);
				case AbstractPolicy.AsymmetricOr p:
					return new CompiledNodeContent.Or(
						CompiledNode.FromPolicy(p.Item1),
						CompiledNode.FromPolicy(p.Item2),
						127.0 / 128.0,
						1.0 / 128.0
						);
				case AbstractPolicy.Threshold p:
					if (p.Item2.Length == 0)
						throw new Exception("Cannot have empty threshold in a descriptor");
					return new CompiledNodeContent.Thresh(
						p.Item1,
						p.Item2.Select(s => CompiledNode.FromPolicy(s)).ToArray()
					);
			}

			throw new Exception("Unreachable");
		}
	}
}