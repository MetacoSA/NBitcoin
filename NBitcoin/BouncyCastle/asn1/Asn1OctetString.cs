using System;
using System.IO;

using NBitcoin.BouncyCastle.Utilities;

namespace NBitcoin.BouncyCastle.Asn1
{
	internal abstract class Asn1OctetString
		: Asn1Object, Asn1OctetStringParser
	{
		internal byte[] str;

		/**
         * @param string the octets making up the octet string.
         */
		internal Asn1OctetString(
			byte[] str)
		{
			if (str == null)
				throw new ArgumentNullException(nameof(str));

			this.str = str;
		}

		public Stream GetOctetStream()
		{
			return new MemoryStream(str, false);
		}

		public Asn1OctetStringParser Parser
		{
			get
			{
				return this;
			}
		}

		public virtual byte[] GetOctets()
		{
			return str;
		}

		protected override int Asn1GetHashCode()
		{
			return Arrays.GetHashCode(GetOctets());
		}

		protected override bool Asn1Equals(
			Asn1Object asn1Object)
		{
			DerOctetString other = asn1Object as DerOctetString;

			if (other == null)
				return false;

			return Arrays.AreEqual(GetOctets(), other.GetOctets());
		}
	}
}
