using System;
using System.IO;

namespace NBitcoin.BouncyCastle.Utilities.IO.Pem
{
	public interface PemObjectParser
	{
		/// <param name="obj">
		/// A <see cref="PemObject"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Object"/>
		/// </returns>
		/// <exception cref="IOException"></exception>
		object ParseObject(PemObject obj);
	}
}
