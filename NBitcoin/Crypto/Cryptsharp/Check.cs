#region License
/*
CryptSharp
Copyright (c) 2013 James F. Bellinger <http://www.zer7.com/software/cryptsharp>

Permission to use, copy, modify, and/or distribute this software for any
purpose with or without fee is hereby granted, provided that the above
copyright notice and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
*/
#endregion

using System;

namespace NBitcoin.Crypto.Internal
{
	static class Check
	{
		public static void Bounds(string valueName, Array value, int offset, int count)
		{
			Check.Null(valueName, value);

			if (offset < 0 || count < 0 || count > value.Length - offset)
			{
				throw Exceptions.ArgumentOutOfRange(valueName,
													"Range [{0}, {1}) is outside array bounds [0, {2}).",
													offset, offset + count, value.Length);
			}
		}

		public static void Length(string valueName, Array value, int minimum, int maximum)
		{
			Check.Null(valueName, value);

			if (value.Length < minimum || value.Length > maximum)
			{
				throw Exceptions.ArgumentOutOfRange(valueName,
													"Length must be in the range [{0}, {1}].",
													minimum, maximum);
			}
		}

		public static void Null<T>(string valueName, T value)
		{
			if (value == null)
			{
				throw Exceptions.ArgumentNull(valueName);
			}
		}

		public static void Range(string valueName, int value, int minimum, int maximum)
		{
			if (value < minimum || value > maximum)
			{
				throw Exceptions.ArgumentOutOfRange(valueName,
													"Value must be in the range [{0}, {1}].",
													minimum, maximum);
			}
		}
	}
}
