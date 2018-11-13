using System;
using System.Linq;
using FsCheck;

namespace NBitcoin.Tests.Generators
{
  public class PrimitiveGenerator
  {
    public static Gen<byte> randomByte() =>
      Gen.Choose(0, 255).Select(i => (byte)i);

    public static Gen<byte[]> randomBytes() =>
      Gen.ListOf(randomByte()).Select(bs => bs.ToArray());

    public static Gen<byte[]> randomBytes(int length) =>
      from bytes in Gen.ListOf(length, randomByte())
      select bytes.ToArray();

    public static Gen<uint> uint32() => from i in randomBytes(4)
                                        select BitConverter.ToUInt32(i);

    public static Gen<uint[]> uint32s(int length) =>
      from list in Gen.ListOf(length, uint32())
      select list.ToArray();
    
    public static Gen<ulong> uint64() => from i in randomBytes(8)
                                         select BitConverter.ToUInt64(i);

  }
}