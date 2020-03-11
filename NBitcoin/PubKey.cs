using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Stealth;
#if !NO_BC
using NBitcoin.BouncyCastle.Math;
using NBitcoin.BouncyCastle.Math.EC;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public enum ScriptPubKeyType
	{
		/// <summary>
		/// Derive P2PKH addresses (P2PKH)
		/// Only use this for legacy code or coins not supporting segwit
		/// </summary>
		Legacy,
		/// <summary>
		/// Derive Segwit (Bech32) addresses (P2WPKH)
		/// This will result in the cheapest fees. This is the recommended choice.
		/// </summary>
		Segwit,
		/// <summary>
		/// Derive P2SH address of a Segwit address (P2WPKH-P2SH)
		/// Use this when you worry that your users do not support Bech address format.
		/// </summary>
		SegwitP2SH
	}
	public class PubKey : IBitcoinSerializable, IDestination, IComparable<PubKey>, IEquatable<PubKey>
	{
		/// <summary>
		/// Create a new Public key from string
		/// </summary>
		public PubKey(string hex)
			: this(Encoders.Hex.DecodeData(hex))
		{

		}

#if HAS_SPAN
		bool compressed;
		internal PubKey(Secp256k1.ECPubKey pubkey, bool compressed)
		{
			if (pubkey == null)
				throw new ArgumentNullException(nameof(pubkey));
			this._ECKey = pubkey;
			this.compressed = compressed;
		}
#endif

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
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes));
#if HAS_SPAN
			if (NBitcoinContext.Instance.TryCreatePubKey(bytes, out compressed, out var p) && p is Secp256k1.ECPubKey)
			{
				_ECKey = p;
			}
			else
			{
				throw new FormatException("Invalid public key");
			}
#else
			if (!Check(bytes, false))
			{
				throw new FormatException("Invalid public key");
			}
			if (@unsafe)
				this.vch = bytes;
			else
			{
				this.vch = bytes.ToArray();
				try
				{
					_ECKey = new ECKey(bytes, false);
				}
				catch (Exception ex)
				{
					throw new FormatException("Invalid public key", ex);
				}
			}
#endif
		}

#if HAS_SPAN
		Secp256k1.ECPubKey _ECKey;
		internal ref readonly Secp256k1.ECPubKey ECKey => ref _ECKey;
#else
		ECKey _ECKey;
		internal ECKey ECKey
		{
			get
			{
				if (_ECKey == null)
					_ECKey = new ECKey(vch, false);
				return _ECKey;
			}
		}
#endif


		public int CompareTo(PubKey other) => BytesComparer.Instance.Compare(this.ToBytes(), other.ToBytes());

		public PubKey Compress()
		{
			if (IsCompressed)
				return this;
#if HAS_SPAN
			return new PubKey(this._ECKey, true);
#else
			return ECKey.GetPubKey(true);
#endif
		}
		public PubKey Decompress()
		{
			if (!IsCompressed)
				return this;
#if HAS_SPAN
			return new PubKey(this._ECKey, false);
#else
			return ECKey.GetPubKey(false);
#endif
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
			if (!deep || !quick)
				return quick;
#if HAS_SPAN
			return NBitcoinContext.Instance.TryCreatePubKey(data.AsSpan().Slice(offset, count), out _);
#else
			try
			{
				new ECKey(data.SafeSubarray(offset, count), false);
				return true;
			}
			catch
			{
				return false;
			}
#endif
		}

#if HAS_SPAN
		KeyId _ID;
		public KeyId Hash
		{
			get
			{
				if (_ID == null)
				{
					Span<byte> tmp = stackalloc byte[65];
					_ECKey.WriteToSpan(compressed, tmp, out int len);
					tmp = tmp.Slice(0, len);
					_ID = new KeyId(Hashes.Hash160(tmp));
				}
				return _ID;
			}
		}
		WitKeyId _WitID;
		public WitKeyId WitHash
		{
			get
			{
				if (_WitID == null)
				{
					Span<byte> tmp = stackalloc byte[65];
					_ECKey.WriteToSpan(compressed, tmp, out int len);
					tmp = tmp.Slice(0, len);
					_WitID = new WitKeyId(Hashes.Hash160(tmp));
				}
				return _WitID;
			}
		}
#else
		byte[] vch = new byte[0];
		KeyId _ID;
		public KeyId Hash
		{
			get
			{
				if (_ID == null)
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
				if (_WitID == null)
				{
					_WitID = new WitKeyId(Hashes.Hash160(vch, 0, vch.Length));
				}
				return _WitID;
			}
		}
#endif
		public bool IsCompressed
		{
			get
			{
#if HAS_SPAN
				return this.compressed;
#else
				if (this.vch.Length == 65)
					return false;
				if (this.vch.Length == 33)
					return true;
				throw new NotSupportedException("Invalid public key size");
#endif
			}
		}

		public BitcoinAddress GetAddress(ScriptPubKeyType type, Network network)
		{
			switch (type)
			{
				case ScriptPubKeyType.Legacy:
					return this.Hash.GetAddress(network);
				case ScriptPubKeyType.Segwit:
					if (!network.Consensus.SupportSegwit)
						throw new NotSupportedException("This network does not support segwit");
					return this.WitHash.GetAddress(network);
				case ScriptPubKeyType.SegwitP2SH:
					if (!network.Consensus.SupportSegwit)
						throw new NotSupportedException("This network does not support segwit");
					return this.WitHash.ScriptPubKey.Hash.GetAddress(network);
				default:
					throw new NotSupportedException("Unsupported ScriptPubKeyType");
			}
		}

		[Obsolete("Use GetAddress(ScriptPubKeyType.Legacy, network) instead")]
		public BitcoinPubKeyAddress GetAddress(Network network)
		{
			return (BitcoinPubKeyAddress)GetAddress(ScriptPubKeyType.Legacy, network);
		}

		public BitcoinScriptAddress GetScriptAddress(Network network)
		{
			var redeem = PayToPubkeyTemplate.Instance.GenerateScriptPubKey(this);
			return new BitcoinScriptAddress(redeem.Hash, network);
		}

		public HDFingerprint GetHDFingerPrint()
		{
			return new HDFingerprint(this.Hash.ToBytes(true), 0);
		}


		public bool Verify(uint256 hash, SchnorrSignature sig)
		{
			if (sig == null)
				throw new ArgumentNullException(nameof(sig));
			if (hash == null)
				throw new ArgumentNullException(nameof(hash));
#if HAS_SPAN
			Span<byte> msg = stackalloc byte[32];
			hash.ToBytes(msg);
			return ECKey.SigVerify(sig.secpShnorr, msg);
#else
			SchnorrSigner signer = new SchnorrSigner();
			return signer.Verify(hash, this, sig);
#endif
		}

		public bool Verify(uint256 hash, ECDSASignature sig)
		{
			if (sig == null)
				throw new ArgumentNullException(nameof(sig));
			if (hash == null)
				throw new ArgumentNullException(nameof(hash));
#if HAS_SPAN
			Span<byte> msg = stackalloc byte[32];
			hash.ToBytes(msg);
			return _ECKey.SigVerify(sig.ToSecpECDSASignature(), msg);
#else
			return ECKey.Verify(hash, sig);
#endif
		}
		public bool Verify(uint256 hash, byte[] sig)
		{
			return Verify(hash, ECDSASignature.FromDER(sig));
		}

		public Script GetScriptPubKey(ScriptPubKeyType type)
		{
			switch (type)
			{
				case ScriptPubKeyType.Legacy:
					return Hash.ScriptPubKey;
				case ScriptPubKeyType.Segwit:
					return WitHash.ScriptPubKey;
				case ScriptPubKeyType.SegwitP2SH:
					return WitHash.ScriptPubKey.Hash.ScriptPubKey;
				default:
					throw new NotSupportedException();
			}
		}
#if HAS_SPAN
		public string ToHex()
		{
			Span<byte> tmp = stackalloc byte[65];
			this._ECKey.WriteToSpan(compressed, tmp, out var l);
			tmp = tmp.Slice(0, l);
			return Encoders.Hex.EncodeData(tmp);
		}
#else
		public string ToHex()
		{
			return Encoders.Hex.EncodeData(vch);
		}
#endif

#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
#if HAS_SPAN
			if (stream.Serializing)
			{
				Span<byte> tmp = stackalloc byte[65];
				_ECKey.WriteToSpan(compressed, tmp, out var l);
				tmp = tmp.Slice(0, l);
				stream.ReadWrite(ref tmp);
			}
			else
			{
				Span<byte> tmp = stackalloc byte[compressed ? 33 : 65];
				stream.ReadWrite(ref tmp);
				if (NBitcoinContext.Instance.TryCreatePubKey(tmp, out var p) && p is Secp256k1.ECPubKey)
				{
					_ECKey = p;
				}
				else
				{
					throw new FormatException("Deserializing invalid pubkey");
				}
			}
#else
			stream.ReadWrite(ref vch);
			if (!stream.Serializing)
				_ECKey = new ECKey(vch, false);
#endif
		}

#endregion

		public byte[] ToBytes()
		{
#if HAS_SPAN
			return _ECKey.ToBytes(compressed);
#else
			return vch.ToArray();
#endif
		}
#if HAS_SPAN
		public void ToBytes(Span<byte> output, out int length)
		{
			_ECKey.WriteToSpan(compressed, output, out length);
		}
#endif
		public byte[] ToBytes(bool @unsafe)
		{
#if HAS_SPAN
			return ToBytes();
#else
			if (@unsafe)
				return vch;
			else
				return vch.ToArray();
#endif
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
#if HAS_SPAN
			var messageToSign = Utils.FormatMessageForSigning(message);
			var hash = Hashes.Hash256(messageToSign);
			Span<byte> msg = stackalloc byte[32];
			hash.ToBytes(msg);
			return _ECKey.SigVerify(signature.ToSecpECDSASignature(), msg);
#else
			var messageToSign = Utils.FormatMessageForSigning(message);
			var hash = Hashes.Hash256(messageToSign);
			return ECKey.Verify(hash, signature);
#endif
		}

#if HAS_SPAN
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
			if (Secp256k1.SecpECDSASignature.TryCreateFromCompact(signatureEncoded.AsSpan().Slice(1), out var sig) && sig is Secp256k1.SecpECDSASignature)
			{
				return new ECDSASignature(sig);
			}
			return new ECDSASignature(Secp256k1.Scalar.Zero, Secp256k1.Scalar.Zero);
		}
#else
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
#pragma warning disable 618
			var sig = new ECDSASignature(r, s);
#pragma warning restore 618
			return sig;
		}
#endif
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
#if HAS_SPAN
			if (signatureEncoded.Length != 65)
				throw new ArgumentException(paramName: nameof(signatureEncoded), message: "Signature truncated, expected 65");
			Span<byte> msg = stackalloc byte[32];
			hash.ToBytes(msg);
			var s = signatureEncoded.AsSpan();
			int recid = (s[0] - 27) & 3;
			bool fComp = ((s[0] - 27) & 4) != 0;
			Secp256k1.ECPubKey pubkey;
			Secp256k1.SecpRecoverableECDSASignature sig;
			if (Secp256k1.SecpRecoverableECDSASignature.TryCreateFromCompact(s.Slice(1), recid, out sig) && sig is Secp256k1.SecpRecoverableECDSASignature &&
				Secp256k1.ECPubKey.TryRecover(NBitcoinContext.Instance, sig, msg, out pubkey) && pubkey is Secp256k1.ECPubKey)
			{
				return new PubKey(pubkey, fComp);
			}
			throw new InvalidOperationException("Impossible to recover the public key");
#else
			if (signatureEncoded.Length < 65)
				throw new ArgumentException("Signature truncated, expected 65 bytes and got " + signatureEncoded.Length);


			int header = signatureEncoded[0];

			// The header byte: 0x1B = first key with even y, 0x1C = first key with odd y,
			//                  0x1D = second key with even y, 0x1E = second key with odd y

			if (header < 27 || header > 34)
				throw new ArgumentException("Header byte out of range: " + header);

			var sig = DecodeSig(signatureEncoded);
			bool compressed = false;

			if (header >= 31)
			{
				compressed = true;
				header -= 4;
			}
			int recId = header - 27;

			ECKey key = ECKey.RecoverFromSignature(recId, sig, hash, compressed);
			return key.GetPubKey(compressed);
#endif
		}


		public PubKey Derivate(byte[] cc, uint nChild, out byte[] ccChild)
		{
			if (!IsCompressed)
				throw new InvalidOperationException("The pubkey must be compressed");
			if ((nChild >> 31) != 0)
				throw new InvalidOperationException("A public key can't derivate an hardened child");
#if HAS_SPAN
			Span<byte> vout = stackalloc byte[64];
			vout.Clear();
			Span<byte> pubkey = stackalloc byte[33];
			this.ToBytes(pubkey, out _);
			Hashes.BIP32Hash(cc, nChild, pubkey[0], pubkey.Slice(1), vout);
			ccChild = new byte[32]; ;
			vout.Slice(32, 32).CopyTo(ccChild);
			return new PubKey(this.ECKey.AddTweak(vout.Slice(0, 32)), true);
#else
			byte[] lr = null;
			byte[] l = new byte[32];
			byte[] r = new byte[32];
			var pubKey = ToBytes();
			lr = Hashes.BIP32Hash(cc, nChild, pubKey[0], pubKey.Skip(1).ToArray());
			Array.Copy(lr, l, 32);
			Array.Copy(lr, 32, r, 0, 32);
			ccChild = r;

			BigInteger N = ECKey.CURVE.N;
			BigInteger parse256LL = new BigInteger(1, l);

			if (parse256LL.CompareTo(N) >= 0)
				throw new InvalidOperationException("You won a prize ! this should happen very rarely. Take a screenshot, and roll the dice again.");

			var q = ECKey.CURVE.G.Multiply(parse256LL).Add(ECKey.GetPublicKeyParameters().Q);
			if (q.IsInfinity)
				throw new InvalidOperationException("You won the big prize ! this would happen only 1 in 2^127. Take a screenshot, and roll the dice again.");

			q = q.Normalize();
			var p = new NBitcoin.BouncyCastle.Math.EC.FpPoint(ECKey.CURVE.Curve, q.XCoord, q.YCoord, true);
			return new PubKey(p.GetEncoded());
#endif
		}

		public override bool Equals(object obj)
		{
			PubKey item = obj as PubKey;
			if (item == null)
				return false;
			return Equals(item);
		}
#if HAS_SPAN
		public bool Equals(PubKey pk) => this == pk;
		public static bool operator ==(PubKey a, PubKey b)
		{
			if (a is PubKey aa && b is PubKey bb)
			{
				return aa.ECKey == bb.ECKey && aa.compressed == bb.compressed;
			}
			return a is null && b is null;
		}
#else
		public bool Equals(PubKey pk) => pk != null && Utils.ArrayEqual(vch, pk.vch);
		public static bool operator ==(PubKey a, PubKey b)
		{
			if (a?.vch is byte[] avch && b?.vch is byte[] bvch)
			{
				return Utils.ArrayEqual(avch, bvch);
			}
			return a is null && b is null;
		}
#endif

		public static bool operator !=(PubKey a, PubKey b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
#if HAS_SPAN
			unchecked
			{
				var hash = this._ECKey.GetHashCode();
				hash = hash * 23 + (compressed ? 0 : 1);
				return hash;
			}
#else
			return ToHex().GetHashCode();
#endif
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
			if (priv == null)
				throw new ArgumentNullException(nameof(priv));
			if (pub == null)
				throw new ArgumentNullException(nameof(pub));
#if HAS_SPAN
			Span<byte> tmp = stackalloc byte[33];
			pub._ECKey.GetSharedPubkey(priv._ECKey).WriteToSpan(true, tmp, out _);
			var c = NBitcoinContext.Instance.CreateECPrivKey(Hashes.SHA256(tmp));
			//Q' = Q + cG
			var qprime = Secp256k1.EC.G.MultConst(c.sec, 256).Add(this.ECKey.Q);
			return new PubKey(new Secp256k1.ECPubKey(qprime.ToGroupElement(), this._ECKey.ctx), this.IsCompressed);
#else
			var curve = ECKey.Secp256k1;
			var hash = GetStealthSharedSecret(priv, pub);
			//Q' = Q + cG
			var qprim = curve.G.Multiply(new BigInteger(1, hash)).Add(curve.Curve.DecodePoint(this.ToBytes()));
			return new PubKey(qprim.GetEncoded()).Compress(this.IsCompressed);
#endif
		}

		internal static byte[] GetStealthSharedSecret(Key priv, PubKey pub)
		{
			if (priv == null)
				throw new ArgumentNullException(nameof(priv));
			if (pub == null)
				throw new ArgumentNullException(nameof(pub));
#if HAS_SPAN
			Span<byte> tmp = stackalloc byte[33];
			pub._ECKey.GetSharedPubkey(priv._ECKey).WriteToSpan(true, tmp, out _);
			return Hashes.SHA256(tmp);
#else
			var curve = ECKey.Secp256k1;
			var pubec = curve.Curve.DecodePoint(pub.ToBytes());
			var p = pubec.Multiply(new BigInteger(1, priv.ToBytes()));
			var pBytes = new PubKey(p.GetEncoded()).Compress().ToBytes();
			var hash = Hashes.SHA256(pBytes);
			return hash;
#endif
		}

		public PubKey Compress(bool compression)
		{
			if (IsCompressed == compression)
				return this;
			if (compression)
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
				if (_ScriptPubKey == null)
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
#if HAS_SPAN
			if (key == null)
				throw new ArgumentNullException(nameof(key));
			return new PubKey(ECKey.GetSharedPubkey(key._ECKey), true);
#else
			var pub = _ECKey.GetPublicKeyParameters();
			var privKey = key._ECKey.PrivateKey;
			if (!pub.Parameters.Equals(privKey.Parameters))
				throw new InvalidOperationException("ECDH public key has wrong domain parameters");
			ECPoint q = pub.Q.Multiply(privKey.D).Normalize();
			if (q.IsInfinity)
				throw new InvalidOperationException("Infinity is not a valid agreement value for ECDH");
			var pubkey = ECKey.Secp256k1.Curve.CreatePoint(q.XCoord.ToBigInteger(), q.YCoord.ToBigInteger());
			pubkey = pubkey.Normalize();
			return new ECKey(pubkey.GetEncoded(true), false).GetPubKey(true);
#endif
		}

		public string Encrypt(string message)
		{
			if (string.IsNullOrEmpty(message))
				throw new ArgumentNullException(nameof(message));

			var bytes = Encoding.UTF8.GetBytes(message);
			return Encoders.Base64.EncodeData(Encrypt(bytes));
		}

		public byte[] Encrypt(byte[] message)
		{
			if (message is null)
				throw new ArgumentNullException(nameof(message));
			var ephemeral = new Key();
			var sharedKey = Hashes.SHA512(GetSharedPubkey(ephemeral).ToBytes());
			var iv = sharedKey.SafeSubarray(0, 16);
			var encryptionKey = sharedKey.SafeSubarray(16, 16);
			var hashingKey = sharedKey.SafeSubarray(32);

			var aes = new AesBuilder().SetKey(encryptionKey).SetIv(iv).IsUsedForEncryption(true).Build();
			var cipherText = aes.Process(message, 0, message.Length);
			var ephemeralPubkeyBytes = ephemeral.PubKey.ToBytes();
			var encrypted = Encoders.ASCII.DecodeData("BIE1").Concat(ephemeralPubkeyBytes, cipherText);
			var hashMAC = Hashes.HMACSHA256(hashingKey, encrypted);
			return encrypted.Concat(hashMAC);
		}

		public BitcoinWitPubKeyAddress GetSegwitAddress(Network network)
		{
			return new BitcoinWitPubKeyAddress(WitHash, network);
		}

#endregion
	}
}
