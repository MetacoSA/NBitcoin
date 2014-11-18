using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Stealth;
using NBitcoin.BouncyCastle.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class PubKey : IBitcoinSerializable
	{
		public PubKey(string hex)
			: this(Encoders.Hex.DecodeData(hex))
		{

		}
		public PubKey(byte[] vch)
		{
			if(!IsValidSize(vch.Length))
			{
				throw new FormatException("Invalid public key size");
			}
			this.vch = vch.ToArray();
			_Key = new ECKey(vch, false);
		}

		public PubKey Compress()
		{
			if(IsCompressed)
				return this;
			return _Key.GetPubKey(true);
		}
		public PubKey Decompress()
		{
			if(!IsCompressed)
				return this;
			return _Key.GetPubKey(false);
		}

		public static bool IsValidSize(long size)
		{
			return size == 65 || size == 33;
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
					_ID = new KeyId(Hashes.Hash160(vch, vch.Length));
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

		public BitcoinAddress GetAddress(Network network)
		{
			return network.CreateBitcoinAddress(this.ID);
		}

		public BitcoinScriptAddress GetScriptAddress(Network network)
		{
			var redeem = PayToPubkeyTemplate.Instance.GenerateScriptPubKey(this);
			return new BitcoinScriptAddress(redeem.ID, network);
		}


		public bool Verify(uint256 hash, ECDSASignature sig)
		{
			return _Key.Verify(hash, sig);
		}
		public bool Verify(uint256 hash, byte[] sig)
		{
			return Verify(hash, ECDSASignature.FromDER(sig));
		}

		Script _HashPaymentScript;
		public Script HashPaymentScript
		{
			get
			{
				if(_HashPaymentScript == null)
				{
					_HashPaymentScript = PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(ID);
				}
				return _HashPaymentScript;
			}
		}
		Script _PaymentScript;
		public Script PaymentScript
		{
			get
			{
				if(_PaymentScript == null)
				{
					_PaymentScript = PayToPubkeyTemplate.Instance.GenerateScriptPubKey(this);
				}
				return _PaymentScript;
			}
		}

		public string ToHex()
		{
			return Encoders.Hex.EncodeData(vch);
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref vch);
			if(!stream.Serializing)
				_Key = new ECKey(vch, false);
		}

		#endregion

		public byte[] ToBytes()
		{
			return vch.ToArray();
		}
		public byte[] ToBytes(bool @unsafe)
		{
			if(@unsafe)
				return vch;
			else
				return vch.ToArray();
		}
		public override string ToString()
		{
			return ToHex();
		}


		public bool VerifyMessage(string message, string signature)
		{
			var key = PubKey.RecoverFromMessage(message, signature);
			return key.ID == ID;
		}

		//Thanks bitcoinj source code
		//http://bitcoinj.googlecode.com/git-history/keychain/core/src/main/java/com/google/bitcoin/core/Utils.java
		public static PubKey RecoverFromMessage(string messageText, string signatureText)
		{
			var signatureEncoded = Convert.FromBase64String(signatureText);
			var message = Utils.FormatMessageForSigning(messageText);
			var hash = Hashes.Hash256(message);
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


		public PubKey Derivate(byte[] cc, uint nChild, out byte[] ccChild)
		{
			byte[] lr = null;
			byte[] l = new byte[32];
			byte[] r = new byte[32];
			if((nChild >> 31) == 0)
			{
				var pubKey = ToBytes();
				lr = Hashes.BIP32Hash(cc, nChild, pubKey[0], pubKey.Skip(1).ToArray());
			}
			else
			{
				throw new InvalidOperationException("Impossible to derivate a child key from a hardened one");
			}
			Array.Copy(lr, l, 32);
			Array.Copy(lr, 32, r, 0, 32);
			ccChild = r;


			BigInteger N = ECKey.CURVE.N;
			BigInteger kPar = new BigInteger(1, this.vch);
			BigInteger parse256LL = new BigInteger(1, l);

			if(parse256LL.CompareTo(N) >= 0)
				throw new InvalidOperationException("You won a prize ! this should happen very rarely. Take a screenshot, and roll the dice again.");

			var q = ECKey.CURVE.G.Multiply(parse256LL).Add(_Key.GetPublicKeyParameters().Q);
			if(q.IsInfinity)
				throw new InvalidOperationException("You won the big prize ! this would happen only 1 in 2^127. Take a screenshot, and roll the dice again.");

			var p = new NBitcoin.BouncyCastle.Math.EC.FpPoint(ECKey.CURVE.Curve, q.X, q.Y, true);
			return new PubKey(p.GetEncoded());
		}

		public override bool Equals(object obj)
		{
			PubKey item = obj as PubKey;
			if(item == null)
				return false;
			return ToHex().Equals(item.ToHex());
		}
		public static bool operator ==(PubKey a, PubKey b)
		{
			if(System.Object.ReferenceEquals(a, b))
				return true;
			if(((object)a == null) || ((object)b == null))
				return false;
			return a.ToHex() == b.ToHex();
		}

		public static bool operator !=(PubKey a, PubKey b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return ToHex().GetHashCode();
		}

		public PubKey UncoverSender(Key ephem, PubKey scan)
		{
			return Uncover(ephem, scan);
		}
		public PubKey UncoverReceiver(Key scan, PubKey ephem)
		{
			return Uncover(scan, ephem);
		}
		public PubKey Uncover(Key priv, PubKey pub)
		{
			var curve = ECKey.CreateCurve();
			var hash = GetStealthSharedSecret(priv, pub);
			//Q' = Q + cG
			var qprim = curve.G.Multiply(new BigInteger(1, hash)).Add(curve.Curve.DecodePoint(this.ToBytes()));
			return new PubKey(qprim.GetEncoded()).Compress(this.IsCompressed);
		}

		internal static byte[] GetStealthSharedSecret(Key priv, PubKey pub)
		{
			var curve = ECKey.CreateCurve();
			var pubec = curve.Curve.DecodePoint(pub.ToBytes());
			var p = pubec.Multiply(new BigInteger(1, priv.ToBytes()));
			var pBytes = new PubKey(p.GetEncoded()).Compress().ToBytes();
			var hash = Hashes.SHA256(pBytes);
			return hash;
		}

		public PubKey Compress(bool compression)
		{
			if(IsCompressed == compression)
				return this;
			if(compression)
				return this.Compress();
			else
				return this.Decompress();
		}

		public BitcoinStealthAddress CreateStealthAddress(PubKey scanKey, Network network)
		{
			return new BitcoinStealthAddress(scanKey, new PubKey[] { this }, 1, null, network);
		}

		public string ToString(Network network)
		{
			return new BitcoinAddress(this.ID, network).ToString();
		}
	}
}
