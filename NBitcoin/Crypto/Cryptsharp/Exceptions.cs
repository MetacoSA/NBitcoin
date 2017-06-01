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
	static class Exceptions
	{
		public static ArgumentException Argument
			(string valueName, string message, params object[] args)
		{
			message = string.Format(message, args);
			ArgumentException e = valueName == null
				? new ArgumentException(message)
				: new ArgumentException(message, valueName);
			return e;
		}

		public static ArgumentNullException ArgumentNull(string valueName)
		{
			ArgumentNullException e = valueName == null
				? new ArgumentNullException()
				: new ArgumentNullException(valueName);
			return e;
		}

		public static ArgumentOutOfRangeException ArgumentOutOfRange
			(string valueName, string message, params object[] args)
		{
			message = string.Format(message, args);
			ArgumentOutOfRangeException e = valueName == null
				? new ArgumentOutOfRangeException(message, (Exception)null)
				: new ArgumentOutOfRangeException(valueName, message);
			return e;
		}

		public static NotSupportedException NotSupported()
		{
			return new NotSupportedException();
		}
	}
}
