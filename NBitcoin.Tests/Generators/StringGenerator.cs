using System;
using System.Linq;
using FsCheck;

namespace NBitcoin.Tests.Generators
{
	public class StringGenerator
	{
		private const string ValidHex = "0123456789abcdef";
		private readonly static char[] ValidHexChars;

		static StringGenerator()
		{
			ValidHexChars = ValidHex.ToCharArray();
		}
		public static Gen<char> HexChar() => Gen.Choose(0, ValidHexChars.Length - 1).Select(i => ValidHexChars[i]);

		public static Gen<string> HexString(int length)
		{
			var res = from i in Gen.Choose(0, length)
								where (i % 2 == 0)
								from cl in Gen.ListOf(i, HexChar())
								select String.Join("", cl.ToList());
			return res;
		}

		public static Gen<string> HexString()
		{
			return Gen.Choose(0, 100).SelectMany(n => HexString(n));
		}
	}
}