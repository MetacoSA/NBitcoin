using System;
using System.Linq;
using FsCheck;

namespace NBitcoin.Tests.Generators
{
	public class PrimitiveGenerator
	{
		public static Arbitrary<uint> UIntArb() =>
			Arb.From(UInt32());
		public static Gen<byte> RandomByte() =>
			Gen.Choose(0, 255).Select(i => (byte) i);

		public static Gen<bool> Bool() => Gen.Choose(0, 1).Select(i => i == 0);

		public static Gen<byte[]> RandomBytes() =>
			Gen.NonEmptyListOf(RandomByte()).Select(bs => bs.ToArray());

		public static Gen<byte[]> RandomBytes(int length) =>
			from bytes in Gen.ListOf(length, RandomByte())
			select bytes.ToArray();

		public static Gen<uint> UInt32() => from i in RandomBytes(4)
																				select BitConverter.ToUInt32(i, 0);

		public static Gen<uint[]> UInt32s(int length) =>
			from list in Gen.ListOf(length, UInt32())
			select list.ToArray();

		public static Gen<ulong> UInt64() => from i in RandomBytes(8)
																				 select BitConverter.ToUInt64(i, 0);

	}
}