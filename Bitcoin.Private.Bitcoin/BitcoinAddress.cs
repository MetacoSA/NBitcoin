using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitcoin.Private.Bitcoin
{
	public class BitcoinAddress : Base58Data
	{
		public BitcoinAddress(string address)
		{
			this.SetString(address);
		}

		protected override bool IsValid
		{
			get
			{
				bool fCorrectSize = vchData.Length == 20;
				bool fKnownVersion = vchVersion[0] == 0 || vchVersion[0] == 5;
				return fCorrectSize && fKnownVersion;
			}
		}



		public KeyId ID
		{
			get
			{
				if(vchVersion[0] == 0)
					return new KeyId(vchData);
				return null;
			}
		}

		//Thanks bitcoinj source code
		//http://bitcoinj.googlecode.com/git-history/keychain/core/src/main/java/com/google/bitcoin/core/Utils.java
		public PubKey RecoverFromSignature(string messageText, string signatureText)
		{
			var signatureEncoded = Convert.FromBase64String(signatureText);
			if(signatureEncoded.Length < 65)
				throw new ArgumentException("Signature truncated, expected 65 bytes and got " + signatureEncoded.Length);


			int header = signatureEncoded[0];

			// The header byte: 0x1B = first key with even y, 0x1C = first key with odd y,
			//                  0x1D = second key with even y, 0x1E = second key with odd y

			if(header < 27 || header > 34)
				throw new ArgumentException("Header byte out of range: " + header);

			BigInteger r = new BigInteger(1, signatureEncoded.Skip(1).Take(32).ToArray());
			BigInteger s = new BigInteger(1, signatureEncoded.Skip(33).Take(32).ToArray());
			var sig = new ECDSASignature(r, s);

			var message = Utils.FormatMessageForSigning(messageText);

			var hash = Utils.Hash(message);

			bool compressed = false;

			if(header >= 31)
			{
				compressed = true;
				header -= 4;
			}
			int recId = header - 27;

			ECKey key = ECKey.RecoverFromSignature(recId, sig, hash, compressed);
			return key.GetPubKey(false);
		}


		public bool VerifyMessage(string message, string signature)
		{
			var key = RecoverFromSignature(message, signature);
			return key.VerifyMessage(message, signature);
		}
	}
}
