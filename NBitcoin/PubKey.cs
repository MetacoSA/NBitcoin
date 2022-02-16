#nullable enable
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
#if !NO_BC
using NBitcoin.BouncyCastle.Math;
using NBitcoin.BouncyCastle.Math.EC;
#endif
#if HAS_SPAN
using NBitcoin.Secp256k1;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

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
		SegwitP2SH,
		/// <summary>
		/// Derive the taproot address of this pubkey following BIP86. This public key is used as the internal key, the output key is computed without script path. (The tweak is SHA256(internal_key))
		/// </summary>
#if !HAS_SPAN
		[Obsolete("TaprootBIP86 is unavailable in .net framework")]
#endif
		TaprootBIP86
	}
	public class PubKey : IDestination, IComparable<PubKey>, IEquatable<PubKey>, IPubKey
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
		public static bool TryCreatePubKey(byte[] bytes, [MaybeNullWhen(false)] out PubKey pubKey)
		{
			if(bytes is null)
				throw new ArgumentNullException(nameof(bytes));
			return TryCreatePubKey(bytes.AsSpan(), out pubKey);
		}
		public static bool TryCreatePubKey(ReadOnlySpan<byte> bytes, [MaybeNullWhen(false)] out PubKey pubKey)
		{
			if (NBitcoinContext.Instance.TryCreatePubKey(bytes, out var compressed, out var p))
			{
				pubKey = new PubKey(p, compressed);
				return true;
			}
			pubKey = null;
			return false;
		}
#endif
#if !HAS_SPAN
		public static bool TryCreatePubKey(byte[] bytes, [MaybeNullWhen(false)] out PubKey pubKey)
		{
			if (bytes is null)
				throw new ArgumentNullException(nameof(bytes));

			try
			{
				var eck = new ECKey(bytes, false);
				pubKey = new PubKey(eck, bytes);
				return true;
			}
			catch
			{
				pubKey = null;
				return false;
			}
		}

		PubKey(ECKey eCKey, byte[] bytes)
		{
			_ECKey = eCKey;
			vch = bytes.ToArray();
		}
#endif

		/// <summary>
		/// Create a new Public key from byte array
		/// </summary>
		/// <param name="bytes">byte array</param>
		public PubKey(byte[] bytes)
		{
			if (bytes is null)
				throw new ArgumentNullException(nameof(bytes));
#if HAS_SPAN
			if (NBitcoinContext.Instance.TryCreatePubKey(bytes, out compressed, out var p))
			{
				_ECKey = p;
			}
			else
			{
				throw new FormatException("Invalid public key");
			}
#else
			this.vch = bytes.ToArray();
			try
			{
				_ECKey = new ECKey(bytes, false);
			}
			catch (Exception ex)
			{
				throw new FormatException("Invalid public key", ex);
			}
#endif
		}

#if HAS_SPAN
		/// <summary>
		/// Create a new Public key from byte array
		/// </summary>
		/// <param name="bytes">byte array</param>
		public PubKey(ReadOnlySpan<byte> bytes)
		{
			if (NBitcoinContext.Instance.TryCreatePubKey(bytes, out compressed, out var p) && p is Secp256k1.ECPubKey)
			{
				_ECKey = p;
			}
			else
			{
				throw new FormatException("Invalid public key");
			}
		}

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


		public int CompareTo(PubKey? other) => other is null ? 1 : BytesComparer.Instance.Compare(this.ToBytes(), other.ToBytes());

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
		/// Quick sanity check on public key format. (size + first byte)
		/// </summary>
		/// <param name="data">bytes array</param>
		public static bool SanityCheck(byte[] data)
		{
			return SanityCheck(data, 0, data.Length);
		}

		public static bool SanityCheck(byte[] data, int offset, int count)
		{
			if (data is null)
				throw new ArgumentNullException(nameof(data));
			return
					(
						(count == 33 && (data[offset + 0] == 0x02 || data[offset + 0] == 0x03)) ||
						(count == 65 && (data[offset + 0] == 0x04 || data[offset + 0] == 0x06 || data[offset + 0] == 0x07))
					);
		}

#if HAS_SPAN
		KeyId? _ID;
		public KeyId Hash
		{
			get
			{
				if (_ID is null)
				{
					Span<byte> tmp = stackalloc byte[65];
					_ECKey.WriteToSpan(compressed, tmp, out int len);
					tmp = tmp.Slice(0, len);
					_ID = new KeyId(Hashes.Hash160(tmp));
				}
				return _ID;
			}
		}
		WitKeyId? _WitID;
		public WitKeyId WitHash
		{
			get
			{
				if (_WitID is null)
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
		byte[] vch;
		KeyId? _ID;
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
		WitKeyId? _WitID;
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
#pragma warning disable CS0618 // Type or member is obsolete
				case ScriptPubKeyType.TaprootBIP86:
#pragma warning restore CS0618 // Type or member is obsolete
					if (!network.Consensus.SupportTaproot)
						throw new NotSupportedException("This network does not support taproot");
#if !HAS_SPAN
					throw new NotSupportedException("This feature of taproot is not supported in .NET Framework");
#else
					return GetTaprootFullPubKey().GetAddress(network);
#endif
				default:
					throw new NotSupportedException("Unsupported ScriptPubKeyType");
			}
		}

#if HAS_SPAN
		public TaprootFullPubKey GetTaprootFullPubKey()
		{
			return GetTaprootFullPubKey(null);
		}
		public TaprootFullPubKey GetTaprootFullPubKey(uint256? merkleRoot)
		{
			return TaprootInternalKey.GetTaprootFullPubKey(merkleRoot);
		}

		TaprootInternalPubKey? _InternalKey;
		internal bool Parity => this.ECKey.Q.y.IsOdd;
		public TaprootInternalPubKey TaprootInternalKey
		{
			get
			{
				if (_InternalKey is TaprootInternalPubKey)
				{
					return _InternalKey;
				}
				var xonly = this.ECKey.ToXOnlyPubKey(out _);
				_InternalKey = new TaprootInternalPubKey(xonly);
				return _InternalKey;
			}
		}
#endif
		HDFingerprint? fp;
		public HDFingerprint GetHDFingerPrint()
		{
			if (fp is HDFingerprint f)
				return f;
			f = new HDFingerprint(this.Hash.ToBytes(), 0);
			fp = f;
			return f;
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
			return GetDestination(type).ScriptPubKey;
		}
		public IAddressableDestination GetDestination(ScriptPubKeyType type)
		{
			switch (type)
			{
				case ScriptPubKeyType.Legacy:
					return Hash;
				case ScriptPubKeyType.Segwit:
					return WitHash;
				case ScriptPubKeyType.SegwitP2SH:
					return WitHash.ScriptPubKey.Hash;
#pragma warning disable CS0618 // Type or member is obsolete
				case ScriptPubKeyType.TaprootBIP86:
#pragma warning restore CS0618 // Type or member is obsolete
#if HAS_SPAN
					return GetTaprootFullPubKey();
#else
					throw new NotSupportedException("ScriptPubKeyType.TaprootBIP86 is not supported by .net framework");
#endif
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

		public static PubKey RecoverCompact(uint256 hash, CompactSignature compactSignature)
		{
			if (compactSignature is null)
				throw new ArgumentNullException(nameof(compactSignature));
			if (hash is null)
				throw new ArgumentNullException(nameof(hash));
#if HAS_SPAN
			Span<byte> msg = stackalloc byte[32];
			hash.ToBytes(msg);
			if (Secp256k1.SecpRecoverableECDSASignature.TryCreateFromCompact(compactSignature.Signature, compactSignature.RecoveryId, out var sig) &&
				Secp256k1.ECPubKey.TryRecover(NBitcoinContext.Instance, sig, msg, out var pubkey))
			{
				return new PubKey(pubkey, true);
			}
			throw new InvalidOperationException("Impossible to recover the public key");
#else
			BigInteger r = new BigInteger(1, compactSignature.Signature.SafeSubarray(0, 32));
			BigInteger s = new BigInteger(1, compactSignature.Signature.SafeSubarray(32, 32));
#pragma warning disable 618
			var sig = new ECDSASignature(r, s);
#pragma warning restore 618
			ECKey key = ECKey.RecoverFromSignature(compactSignature.RecoveryId, sig, hash);
			return key.GetPubKey(true);
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
			byte[] l = new byte[32];
			byte[] r = new byte[32];
			var pubKey = ToBytes();
			byte[] lr = Hashes.BIP32Hash(cc, nChild, pubKey[0], pubKey.Skip(1).ToArray());
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

		public override bool Equals(object? obj)
		{
			if (obj is PubKey pk)
				return Equals(pk);
			return false;
		}
#if HAS_SPAN
		public bool Equals(PubKey? pk) => this == pk;
		public static bool operator ==(PubKey? a, PubKey? b)
		{
			if (a is PubKey aa && b is PubKey bb)
			{
				return aa.ECKey == bb.ECKey && aa.compressed == bb.compressed;
			}
			return a is null && b is null;
		}
#else
		public bool Equals(PubKey? pk) => pk is PubKey && Utils.ArrayEqual(vch, pk.vch);
		public static bool operator ==(PubKey? a, PubKey? b)
		{
			if (a?.vch is byte[] avch && b?.vch is byte[] bvch)
			{
				return Utils.ArrayEqual(avch, bvch);
			}
			return a is null && b is null;
		}
#endif

		public static bool operator !=(PubKey? a, PubKey? b)
		{
			return !(a == b);
		}

		int? hashcode;
		public override int GetHashCode()
		{
			if (hashcode is int h)
				return h;
#if HAS_SPAN
			unchecked
			{
				h = this._ECKey.GetHashCode();
				h = h * 23 + (compressed ? 0 : 1);
				hashcode = h;
				return h;
			}
#else
			h = ToHex().GetHashCode();
			hashcode = h;
			return h;
#endif
		}

		#region IDestination Members

		Script? _ScriptPubKey;
		public Script ScriptPubKey
		{
			get
			{
				if (_ScriptPubKey is null)
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
			if (message is null)
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

		#endregion
	}
}
