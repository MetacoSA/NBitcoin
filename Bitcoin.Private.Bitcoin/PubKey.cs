using Bitcoin.Private.Bitcoin.DataEncoders;
using Org.BouncyCastle.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitcoin.Private.Bitcoin
{
	public class PubKey
	{
		public PubKey(byte[] vch)
		{
			if(vch.Length != 65 && vch.Length != 33)
			{
				throw new ArgumentException("Invalid public key size");
			}
			this.vch = vch.ToArray();
			_Key = new ECKey(vch, false);
		}
		byte[] vch = new byte[0];
		ECKey _Key = null;
		KeyId _ID;
		public KeyId ID
		{
			get
			{
				if(_ID == null)
				{
					_ID = new KeyId(Utils.Hash160(vch, vch.Length));
				}
				return _ID;
			}
		}

		public bool IsCompressed
		{
			get
			{
				if(this.vch.Length == 65)
					return false;
				if(this.vch.Length == 33)
					return true;
				throw new NotSupportedException("Invalid public key size");
			}
		}

		BitcoinAddress _Address;
		public BitcoinAddress Address
		{
			get
			{
				if(_Address == null)
				{
					var vchList = this.ID.ToBytes().ToList();
					vchList.Insert(0, 0);
					_Address = new BitcoinAddress(Encoders.Base58Check.EncodeData(vchList.ToArray()));
				}
				return _Address;
			}
		}

		public bool Verify(uint256 hash, ECDSASignature sig)
		{
			return _Key.Verify(hash, sig);
		}

		public string ToHex()
		{
			return Encoders.Hex.EncodeData(vch);
		}
		public byte[] ToBytes()
		{
			return vch.ToArray();
		}

		public override string ToString()
		{
			return ToHex();
		}

		public bool VerifyMessage(string message, string signature)
		{
			return this.Address.VerifyMessage(message, signature);
		}
		

		//Thanks bitcoinj source code
		//http://bitcoinj.googlecode.com/git-history/keychain/core/src/main/java/com/google/bitcoin/core/Utils.java
		public static PubKey RecoverFromMessage(string messageText, string signatureText)
		{
			var signatureEncoded = Convert.FromBase64String(signatureText);
			var message = Utils.FormatMessageForSigning(messageText);
			var hash = Utils.Hash(message);
			return RecoverCompact(hash, signatureEncoded);
		}

		public static PubKey RecoverCompact(uint256 hash, byte[] signatureEncoded)
		{
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
			bool compressed = false;

			if(header >= 31)
			{
				compressed = true;
				header -= 4;
			}
			int recId = header - 27;

			ECKey key = ECKey.RecoverFromSignature(recId, sig, hash, compressed);
			return key.GetPubKey(compressed);
		}

	}
}
