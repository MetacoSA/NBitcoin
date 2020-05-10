using System;

namespace NBitcoin
{
	public abstract class BitcoinExtKeyBase : Base58Data, IDestination
	{
		protected BitcoinExtKeyBase()
		{

		}
		protected BitcoinExtKeyBase(IBitcoinSerializable key, Network network)
			: base(key.ToBytes(), network)
		{
		}


		#region IDestination Members

		public abstract Script ScriptPubKey
		{
			get;
		}

		#endregion
	}

	/// <summary>
	/// Base58 representation of an ExtKey, within a particular network.
	/// </summary>
	public class BitcoinExtKey : BitcoinExtKeyBase, ISecret, IHDKey
	{
		/// <summary>
		/// Constructor. Creates an extended key from the Base58 representation, checking the expected network.
		/// </summary>
		public BitcoinExtKey(string base58, Network expectedNetwork = null)
		{
			Init<BitcoinExtKey>(base58, expectedNetwork);
		}

		/// <summary>
		/// Constructor. Creates a representation of an extended key, within the specified network.
		/// </summary>
		public BitcoinExtKey(ExtKey key, Network network)
			: base(key, network)
		{
		}

		/// <summary>
		/// Gets whether the data is the correct expected length.
		/// </summary>
		protected override bool IsValid
		{
			get
			{
				return vchData.Length == 74;
			}
		}

		ExtKey _Key;

		/// <summary>
		/// Gets the extended key, converting from the Base58 representation.
		/// </summary>
		public ExtKey ExtKey
		{
			get
			{
				if (_Key == null)
				{
					_Key = new ExtKey();
					_Key.ReadWrite(new BitcoinStream(vchData));
				}
				return _Key;
			}
		}

		/// <summary>
		/// Gets the type of item represented by this Base58 data.
		/// </summary>
		public override Base58Type Type
		{
			get
			{
				return Base58Type.EXT_SECRET_KEY;
			}
		}

		/// <summary>
		/// Gets the script of the hash of the public key corresponing to the private key 
		/// of the extended key of this Base58 item.
		/// </summary>
		public override Script ScriptPubKey
		{
			get
			{
				return ExtKey.ScriptPubKey;
			}
		}

		/// <summary>
		/// Gets the Base58 representation, in the same network, of the neutered extended key.
		/// </summary>
		public BitcoinExtPubKey Neuter()
		{
			return ExtKey.Neuter().GetWif(Network);
		}

		public BitcoinExtKey Derive(uint index)
		{
			return new BitcoinExtKey(ExtKey.Derive(index), Network);
		}

		IHDKey IHDKey.Derive(KeyPath keyPath)
		{
			return Derive(keyPath);
		}

		public BitcoinExtKey Derive(KeyPath keyPath)
		{
			if (keyPath == null)
				throw new ArgumentNullException(nameof(keyPath));
			return new BitcoinExtKey(ExtKey.Derive(keyPath), Network);
		}
		public ExtKey Derive(RootedKeyPath rootedKeyPath)
		{
			if (rootedKeyPath == null)
				throw new ArgumentNullException(nameof(rootedKeyPath));
			if (rootedKeyPath.MasterFingerprint != GetPublicKey().GetHDFingerPrint())
				throw new ArgumentException(paramName: nameof(rootedKeyPath), message: "The rootedKeyPath's fingerprint does not match this ExtKey");
			return Derive(rootedKeyPath.KeyPath);
		}

		public PubKey GetPublicKey()
		{
			return ExtKey.PrivateKey.PubKey;
		}

		bool IHDKey.CanDeriveHardenedPath()
		{
			return true;
		}

		#region ISecret Members

		/// <summary>
		/// Gets the private key of the extended key of this Base58 item.
		/// </summary>
		public Key PrivateKey
		{
			get
			{
				return ExtKey.PrivateKey;
			}
		}

		#endregion

		/// <summary>
		/// Implicit cast from BitcoinExtKey to ExtKey.
		/// </summary>
		public static implicit operator ExtKey(BitcoinExtKey key)
		{
			if (key == null)
				return null;
			return key.ExtKey;
		}
	}

	/// <summary>
	/// Base58 representation of an ExtPubKey, within a particular network.
	/// </summary>
	public class BitcoinExtPubKey : BitcoinExtKeyBase, IHDKey
	{
		/// <summary>
		/// Constructor. Creates an extended public key from the Base58 representation, checking the expected network.
		/// </summary>
		public BitcoinExtPubKey(string base58, Network expectedNetwork = null)
		{
			Init<BitcoinExtPubKey>(base58, expectedNetwork);
		}

		/// <summary>
		/// Constructor. Creates a representation of an extended public key, within the specified network.
		/// </summary>
		public BitcoinExtPubKey(ExtPubKey key, Network network)
			: base(key, network)
		{
		}

		ExtPubKey _PubKey;

		/// <summary>
		/// Gets the extended public key, converting from the Base58 representation.
		/// </summary>
		public ExtPubKey ExtPubKey
		{
			get
			{
				if (_PubKey == null)
				{
					_PubKey = new ExtPubKey(new BitcoinStream(vchData));
				}
				return _PubKey;
			}
		}

		protected override bool IsValid
		{
			get
			{
				var baseSize = 1 + 4 + 4 + 32;
				if (vchData.Length != baseSize + 33 && vchData.Length != baseSize + 65)
					return false;
				try
				{
					_PubKey = new ExtPubKey(new BitcoinStream(vchData));
					_PubKey.ReadWrite(new BitcoinStream(vchData));
					return true;
				}
				catch { return false; }
			}
		}

		/// <summary>
		/// Gets the type of item represented by this Base58 data.
		/// </summary>
		public override Base58Type Type
		{
			get
			{
				return Base58Type.EXT_PUBLIC_KEY;
			}
		}

		/// <summary>
		/// Gets the script of the hash of the public key of the extended key of this Base58 item.
		/// </summary>
		public override Script ScriptPubKey
		{
			get
			{
				return ExtPubKey.ScriptPubKey;
			}
		}

		/// <summary>
		/// Implicit cast from BitcoinExtPubKey to ExtPubKey.
		/// </summary>
		public static implicit operator ExtPubKey(BitcoinExtPubKey key)
		{
			if (key == null)
				return null;
			return key.ExtPubKey;
		}

		IHDKey IHDKey.Derive(KeyPath keyPath)
		{
			return Derive(keyPath);
		}

		public BitcoinExtPubKey Derive(uint index)
		{
			return ExtPubKey.Derive(index).GetWif(Network);
		}

		public BitcoinExtPubKey Derive(KeyPath keyPath)
		{
			if (keyPath == null)
				throw new ArgumentNullException(nameof(keyPath));
			return new BitcoinExtPubKey(ExtPubKey.Derive(keyPath), Network);
		}

		public PubKey GetPublicKey()
		{
			return ExtPubKey.pubkey;
		}

		bool IHDKey.CanDeriveHardenedPath()
		{
			return false;
		}
	}
}
