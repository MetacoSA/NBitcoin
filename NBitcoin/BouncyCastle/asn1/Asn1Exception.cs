using System;
using System.IO;

namespace nStratis.BouncyCastle.asn1
{
	internal class Asn1Exception
		: IOException
	{
		public Asn1Exception()
			: base()
		{
		}

		public Asn1Exception(
			string message)
			: base(message)
		{
		}

		public Asn1Exception(
			string message,
			Exception exception)
			: base(message, exception)
		{
		}
	}
}
