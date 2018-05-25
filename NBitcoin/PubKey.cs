using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Stealth;
using NBitcoin.BouncyCastle.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBitcoin.BouncyCastle.Math.EC;

namespace NBitcoin
{
	public class PubKey : IBitcoinSerializable, IDestination
	{
		/// <summary>
		/// Create a new Public key from string
		/// </summary>
		public PubKey(string hex)
			: this(Encoders.Hex.DecodeData(hex))
		{

		}

		/// <summary>
		/// Create a new Public key from byte array
		/// </summary>
		public PubKey(byte[] bytes)
			: this(bytes, false)
		{
		}

		/// <summary>
		/// Create a new Public key from byte array
		/// </summary>
		/// <param name="bytes">byte array</param>
		/// <param name="unsafe">If false, make internal copy of bytes and does perform only a costly check for PubKey format. If true, the bytes array is used as is and only PubKey.Check is used for validating the format. </param>	 
		public PubKey(byte[] bytes, bool @unsafe)
		{
			if(bytes == null)
				throw new ArgumentNullException(nameof(bytes));
			if(!Check(bytes, false))
			{
				throw new FormatException("Invalid public key");
			}
			if(@unsafe)
				this.vch = bytes;
			else
			{
				this.vch = bytes.ToArray();
				try
				{
					_ECKey = new ECKey(bytes, false);
				}
				catch(Exception ex)
				{
					throw new FormatException("Invalid public key", ex);
				}
			}
		}

		ECKey _ECKey;
		ECKey ECKey
		{
			get
			{
				if(_ECKey == null)
					_ECKey = new ECKey(vch, false);
				return _ECKey;
			}
		}

		public PubKey Compress()
		{
			if(IsCompressed)
				return this;
			return ECKey.GetPubKey(true);
		}
		public PubKey Decompress()
		{
			if(!IsCompressed)
				return this;
			return ECKey.GetPubKey(false);
		}

		/// <summary>
		/// Check on public key format.
		/// </summary>
		/// <param name="data">bytes array</param>
		/// <param name="deep">If false, will only check the first byte and length of the array. If true, will also check that the ECC coordinates are correct.</param>
		/// <returns>true if byte array is valid</returns>
		public static bool Check(byte[] data, bool deep)
		{
			return Check(data, 0, data.Length, deep);
		}

		public static bool Check(byte[] data, int offset, int count, bool deep)
		{
			var quick = data != null &&
					(
						(count == 33 && (data[offset + 0] == 0x02 || data[offset + 0] == 0x03)) ||
						(count == 65 && (data[offset + 0] == 0x04 || data[offset + 0] == 0x06 || data[offset + 0] == 0x07))
					);
			if(!deep || !quick)
				return quick;
			try
			{
				new ECKey(data.SafeSubarray(offset, count), false);
				return true;
			}
			catch
			{
				return false;
			}
		}

		byte[] vch = new byte[0];
		KeyId _ID;
		public KeyId Hash
		{
			get
			{
				if(_ID == null)
				{
					_ID = new KeyId(Hashes.Hash160(vch, 0, vch.Length));
				}
				return _ID;
			}
		}
		WitKeyId _WitID;
		public WitKeyId WitHash
		{
			get
			{
				if(_WitID == null)
				{
					_WitID = new WitKeyId(Hashes.Hash160(vch, 0, vch.Length));
				}
				return _WitID;
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

		public BitcoinPubKeyAddress GetAddress(Network network)
		{
			return network.CreateBitcoinAddress(this.Hash);
		}

		public BitcoinScriptAddress GetScriptAddress(Network network)
		{
			var redeem = PayToPubkeyTemplate.Instance.GenerateScriptPubKey(this);
			return new BitcoinScriptAddress(redeem.Hash, network);
		}


		public bool Verify(uint256 hash, ECDSASignature sig)
		{
			return ECKey.Verify(hash, sig);
		}
		public bool Verify(uint256 hash, byte[] sig)
		{
			return Verify(hash, ECDSASignature.FromDER(sig));
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
				_ECKey = new ECKey(vch, false);
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

		/// <summary>
		/// Verify message signed using signmessage from bitcoincore
		/// </summary>
		/// <param name="message">The message</param>
		/// <param name="signature">The signature</param>
		/// <returns>True if signatures is valid</returns>
		public bool VerifyMessage(string message, string signature)
		{
			return VerifyMessage(Encoding.UTF8.GetBytes(message), signature);
		}

		/// <summary>
		/// Verify message signed using signmessage from bitcoincore
		/// </summary>
		/// <param name="message">The message</param>
		/// <param name="signature">The signature</param>
		/// <returns>True if signatures is valid</returns>
		public bool VerifyMessage(byte[] messageBytes, string signature)
		{
			return VerifyMessage(messageBytes, DecodeSigString(signature));
		}

		/// <summary>
		/// Verify message signed using signmessage from bitcoincore
		/// </summary>
		/// <param name="message">The message</param>
		/// <param name="signature">The signature</param>
		/// <returns>True if signatures is valid</returns>
		public bool VerifyMessage(byte[] message, ECDSASignature signature)
		{
			var messageToSign = Utils.FormatMessageForSigning(message);
			var hash = Hashes.Hash256(messageToSign);
			return ECKey.Verify(hash, signature);
		}

		/// <summary>
		/// Decode signature from bitcoincore verify/signing rpc methods
		/// </summary>
		/// <param name="signature"></param>
		/// <returns></returns>
		private static ECDSASignature DecodeSigString(string signature)
		{
			var signatureEncoded = Encoders.Base64.DecodeData(signature);
			return DecodeSig(signatureEncoded);
		}
		private static ECDSASignature DecodeSig(byte[] signatureEncoded)
		{
			BigInteger r = new BigInteger(1, signatureEncoded.SafeSubarray(1, 32));
			BigInteger s = new BigInteger(1, signatureEncoded.SafeSubarray(33, 32));
			var sig = new ECDSASignature(r, s);
			return sig;
		}

		//Thanks bitcoinj source code
		//http://bitcoinj.googlecode.com/git-history/keychain/core/src/main/java/com/google/bitcoin/core/Utils.java
		public static PubKey RecoverFromMessage(string messageText, string signatureText)
		{
			return RecoverFromMessage(Encoding.UTF8.GetBytes(messageText), signatureText);
		}

		public static PubKey RecoverFromMessage(byte[] messageBytes, string signatureText)
		{
			var signatureEncoded = Encoders.Base64.DecodeData(signatureText);
			var message = Utils.FormatMessageForSigning(messageBytes);
			var hash = Hashes.Hash256(message);
			return RecoverCompact(hash, signatureEncoded);
		}

		public static PubKey RecoverFromMessage(byte[] messageBytes, byte[] signatureEncoded)
		{
			var message = Utils.FormatMessageForSigning(messageBytes);
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

			var sig = DecodeSig(signatureEncoded);
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
				throw new InvalidOperationException("A public key can't derivate an hardened child");
			}
			Array.Copy(lr, l, 32);
			Array.Copy(lr, 32, r, 0, 32);
			ccChild = r;


			BigInteger N = ECKey.CURVE.N;
			BigInteger parse256LL = new BigInteger(1, l);

			if(parse256LL.CompareTo(N) >= 0)
				throw new InvalidOperationException("You won a prize ! this should happen very rarely. Take a screenshot, and roll the dice again.");

			var q = ECKey.CURVE.G.Multiply(parse256LL).Add(ECKey.GetPublicKeyParameters().Q);
			if(q.IsInfinity)
				throw new InvalidOperationException("You won the big prize ! this would happen only 1 in 2^127. Take a screenshot, and roll the dice again.");

			q = q.Normalize();
			var p = new NBitcoin.BouncyCastle.Math.EC.FpPoint(ECKey.CURVE.Curve, q.XCoord, q.YCoord, true);
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
			var curve = ECKey.Secp256k1;
			var hash = GetStealthSharedSecret(priv, pub);
			//Q' = Q + cG
			var qprim = curve.G.Multiply(new BigInteger(1, hash)).Add(curve.Curve.DecodePoint(this.ToBytes()));
			return new PubKey(qprim.GetEncoded()).Compress(this.IsCompressed);
		}

		internal static byte[] GetStealthSharedSecret(Key priv, PubKey pub)
		{
			var curve = ECKey.Secp256k1;
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
			return new BitcoinPubKeyAddress(this.Hash, network).ToString();
		}

		#region IDestination Members

		Script _ScriptPubKey;
		public Script ScriptPubKey
		{
			get
			{
				if(_ScriptPubKey == null)
				{
					_ScriptPubKey = PayToPubkeyTemplate.Instance.GenerateScriptPubKey(this);
				}
				return _ScriptPubKey;
			}
		}

		/// <summary>
		/// Exchange shared secret through ECDH
		/// </summary>
		/// <param name="key">Private key</param>
		/// <returns>Shared secret</returns>
		[Obsolete("Use GetSharedPubkey instead")]
		public byte[] GetSharedSecret(Key key)
		{
			return Hashes.SHA256(GetSharedPubkey(key).ToBytes());
		}

		/// <summary>
		/// Exchange shared secret through ECDH
		/// </summary>
		/// <param name="key">Private key</param>
		/// <returns>Shared pubkey</returns>
		public PubKey GetSharedPubkey(Key key)
		{
			var pub = _ECKey.GetPublicKeyParameters();
			var privKey = key._ECKey.PrivateKey;
			if(!pub.Parameters.Equals(privKey.Parameters))
				throw new InvalidOperationException("ECDH public key has wrong domain parameters");
			ECPoint q = pub.Q.Multiply(privKey.D).Normalize();
			if(q.IsInfinity)
				throw new InvalidOperationException("Infinity is not a valid agreement value for ECDH");
			var pubkey = ECKey.Secp256k1.Curve.CreatePoint(q.XCoord.ToBigInteger(), q.YCoord.ToBigInteger());
			pubkey = pubkey.Normalize();
			return new ECKey(pubkey.GetEncoded(true), false).GetPubKey(true);
		}

		public BitcoinWitPubKeyAddress GetSegwitAddress(Network network)
		{
			return new BitcoinWitPubKeyAddress(WitHash, network);
		}

		#endregion
	}
}
