using System;

namespace NBitcoin.WalletPolicies;

public partial class Miniscript
{
	#region checksum
	/** The character set for the checksum itself (same as bech32). */
	static readonly char[] CHECKSUM_CHARSET = "qpzry9x8gf2tvdw0s3jn54khce6mua7l".ToCharArray();
	static readonly string INPUT_CHARSET_STRING =
		"0123456789()[],'/*abcdefgh@:$%{}" +
		"IJKLMNOPQRSTUVWXYZ&+-.;<=>?!^_|~" +
		"ijklmnopqrstuvwxyzABCDEFGH`#\"\\ ";

	static readonly char[] INPUT_CHARSET = INPUT_CHARSET_STRING.ToCharArray();

	public static string AddChecksum(string desc) => $"{desc}#{GetCheckSum(desc)}";
	public static string GetCheckSum(string desc)
	{
		if (desc is null)
			throw new ArgumentNullException(nameof(desc));
		ulong c = 1;
		int cls = 0;
		int clscount = 0;
		foreach(var ch in desc.ToCharArray())
		{
			var pos = INPUT_CHARSET_STRING.IndexOf(ch);
			if (pos == -1)
				return "";
			c = PolyMod(c, pos & 31);
			cls = cls * 3 + (pos >> 5);
			if (++clscount == 3)
			{
				c = PolyMod(c, cls);
				cls = 0;
				clscount = 0;
			}
		}
		if (clscount > 0) c = PolyMod(c, cls);
		for (int j = 0; j < 8; ++j) c = PolyMod(c, 0);
		c ^= 1;
		var result = new char[8];
		for (int j = 0; j < 8; ++j)
		{
			result[j] = CHECKSUM_CHARSET[(c >> (5 * (7 - j))) & 31];
		}
		return new String(result);
	}
	static ulong PolyMod(ulong c, int val)
	{
		ulong c0 = c >> 35;
		c = ((c & 0x7ffffffffUL) << 5) ^ (ulong)val;
		if ((c0 & 1UL) != 0) c ^= 0xf5dee51989;
		if ((c0 & 2UL) != 0) c ^= 0xa9fdca3312;
		if ((c0 & 4UL) != 0) c ^= 0x1bab10e32d;
		if ((c0 & 8) != 0) c ^= 0x3706b1677a;
		if ((c0 & 16) != 0) c ^= 0x644d626ffd;
		return c;
	}

	#endregion
}
