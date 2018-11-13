using System;
using System.Linq;
using FsCheck;

namespace NBitcoin.Tests.Generators
{
  public class StringGenerator
  {
    private const string validHex = "0123456789abcdef";
    private readonly static char[] validHexChars;

    static StringGenerator()
    {
      validHexChars = validHex.ToCharArray();
    }
    public static Gen<char> hexChar() => Gen.Choose(0, validHexChars.Length - 1).Select(i => validHexChars[i]);

    public static Gen<string> hexString(int length)
    {
      var res = from i in Gen.Choose(0, length)
                where (i % 2 == 0)
                from cl in Gen.ListOf(i, hexChar())
                select String.Join("", cl.ToList());
      return res;
    }

    public static Gen<string> hexString()
    {
      return Gen.Choose(0, 100).SelectMany(n => hexString(n));
    }
  }
}