using System;

namespace nStratis.BouncyCastle.crypto
{
	internal class CryptoException
		: Exception
	{
		public CryptoException()
		{
		}

		public CryptoException(
			string message)
			: base(message)
		{
		}

		public CryptoException(
			string message,
			Exception exception)
			: base(message, exception)
		{
		}
	}
}
