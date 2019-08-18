using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.RPC
{
	public class ScanTxoutPubkey
	{
		/// <summary>
		///  Describe public key of this private key
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public static ScanTxoutPubkey PrivateKey(BitcoinSecret key)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));
			return new ScanTxoutPubkey($"{key}");
		}

		/// <summary>
		///  Describe public key of this private key
		/// </summary>
		/// <param name="key"></param>
		/// <param name="network"></param>
		/// <returns></returns>
		public static ScanTxoutPubkey PrivateKey(Key key, Network network)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));
			if (network == null)
				throw new ArgumentNullException(nameof(network));
			return new ScanTxoutPubkey($"{key.GetBitcoinSecret(network)}");
		}

		/// <summary>
		///  Describe public key
		/// </summary>
		/// <param name="publicKey"></param>
		/// <returns></returns>
		public static ScanTxoutPubkey PublicKey(PubKey publicKey)
		{
			if (publicKey == null)
				throw new ArgumentNullException(nameof(publicKey));
			return new ScanTxoutPubkey($"{publicKey.ToHex()}");
		}

		/// <summary>
		///  Describe ext public key
		/// </summary>
		/// <param name="extPubKey"></param>
		/// <param name="keyPath">The root of the keypath to follow</param>
		/// <returns></returns>
		public static ScanTxoutPubkey ExtPubKey(BitcoinExtPubKey extPubKey, KeyPath keyPath)
		{
			if (extPubKey == null)
				throw new ArgumentNullException(nameof(extPubKey));

			StringBuilder builder = new StringBuilder();
			builder.Append(extPubKey.ToString());
			if (keyPath != null && keyPath.Indexes.Length != 0)
			{
				builder.Append(keyPath.ToString().Replace("m/", String.Empty));
			}
			return new ScanTxoutPubkey($"{builder.ToString()}");
		}

		/// <summary>
		///  Describe ext public key
		/// </summary>
		/// <param name="extPubKey"></param>
		/// <param name="network"></param>
		/// <param name="keyPath">The root of the keypath to follow</param>
		/// <returns></returns>
		public static ScanTxoutPubkey ExtPubKey(ExtPubKey extPubKey, Network network, KeyPath keyPath)
		{
			if (extPubKey == null)
				throw new ArgumentNullException(nameof(extPubKey));
			if (network == null)
				throw new ArgumentNullException(nameof(network));

			return ExtPubKey(extPubKey.GetWif(network), keyPath);
		}


		public ScanTxoutPubkey(string value)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			Value = value;
		}

		public string Value { get; }

		public override string ToString()
		{
			return Value;
		}
	}
	public class ScanTxoutDescriptor
	{
		/// <summary>
		///  Pay-to-pubkey (P2PK) output for public key P
		/// </summary>
		/// <param name="pubkey"></param>
		/// <returns></returns>
		public static ScanTxoutDescriptor PK(ScanTxoutPubkey pubkey)
		{
			if (pubkey == null)
				throw new ArgumentNullException(nameof(pubkey));
			return new ScanTxoutDescriptor($"pk({pubkey.ToString()})");
		}
		/// <summary>
		/// Pay-to-pubkey-hash (P2PKH) output for public key P.
		/// </summary>
		/// <param name="pubkey"></param>
		/// <returns></returns>
		public static ScanTxoutDescriptor PKH(ScanTxoutPubkey pubkey)
		{
			if (pubkey == null)
				throw new ArgumentNullException(nameof(pubkey));
			return new ScanTxoutDescriptor($"pkh({pubkey.ToString()})");
		}

		/// <summary>
		/// Pay-to-witness-pubkey-hash (P2WPKH) output for public key P.
		/// </summary>
		/// <param name="pubkey"></param>
		/// <returns></returns>
		public static ScanTxoutDescriptor WPKH(ScanTxoutPubkey pubkey)
		{
			if (pubkey == null)
				throw new ArgumentNullException(nameof(pubkey));
			return new ScanTxoutDescriptor($"wpkh({pubkey.ToString()})");
		}

		/// <summary>
		/// Pay-to-script-hash (P2SH) output for script S
		/// </summary>
		/// <param name="descriptor"></param>
		/// <returns></returns>
		public static ScanTxoutDescriptor SH(ScanTxoutDescriptor descriptor)
		{
			if (descriptor == null)
				throw new ArgumentNullException(nameof(descriptor));
			return new ScanTxoutDescriptor($"sh({descriptor.ToString()})");
		}

		/// <summary>
		/// Pay-to-witness-script-hash (P2WSH) output for script S
		/// </summary>
		/// <param name="descriptor"></param>
		/// <returns></returns>
		public static ScanTxoutDescriptor WSH(ScanTxoutDescriptor descriptor)
		{
			if (descriptor == null)
				throw new ArgumentNullException(nameof(descriptor));
			return new ScanTxoutDescriptor($"wsh({descriptor.ToString()})");
		}

		/// <summary>
		/// Combination of P2PK, P2PKH, P2WPKH, and P2SH-P2WPKH for public key P.
		/// </summary>
		/// <param name="descriptor"></param>
		/// <returns></returns>
		public static ScanTxoutDescriptor Combo(ScanTxoutPubkey pubkey)
		{
			if (pubkey == null)
				throw new ArgumentNullException(nameof(pubkey));

			return new ScanTxoutDescriptor($"combo({pubkey})");
		}

		/// <summary>
		/// k-of-n multisig for given public keys
		/// </summary>
		/// <param name="k">k from the "k-of-n multisig" for given public keys</param>
		/// <param name="pubkeys"></param>
		/// <returns></returns>
		public static ScanTxoutDescriptor Multi(int k, params ScanTxoutPubkey[] pubkeys)
		{
			if (pubkeys == null)
				throw new ArgumentNullException(nameof(pubkeys));

			return new ScanTxoutDescriptor($"multi({k},{String.Join(",", pubkeys.Select(p => p.ToString()).ToArray())})");
		}

		/// <summary>
		/// Output to address
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		public static ScanTxoutDescriptor Addr(BitcoinAddress address)
		{
			if (address == null)
				throw new ArgumentNullException(nameof(address));

			return new ScanTxoutDescriptor($"addr({address})");
		}

		/// <summary>
		/// scriptPubKey with raw bytes
		/// </summary>
		/// <param name="scriptPubKey"></param>
		/// <returns></returns>
		public static ScanTxoutDescriptor Raw(Script scriptPubKey)
		{
			if (scriptPubKey == null)
				throw new ArgumentNullException(nameof(scriptPubKey));

			return new ScanTxoutDescriptor($"raw({scriptPubKey.ToHex()})");
		}

		public ScanTxoutDescriptor(string value)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			Value = value;
		}
		public string Value { get; }

		public override string ToString()
		{
			return Value;
		}
	}
	public class ScanTxoutSetObject
	{
		public ScanTxoutSetObject(ScanTxoutDescriptor descriptor, int? range = null)
		{
			if (descriptor == null)
				throw new ArgumentNullException(nameof(descriptor));
			Descriptor = descriptor;
			Range = range;
		}
		public ScanTxoutDescriptor Descriptor { get; }
		public int? Range { get; }
	}

	public class ScanTxoutSetResponse
	{
		public int SearchedItems { get; internal set; }
		public bool Success { get; internal set; }
		public ScanTxoutOutput[] Outputs { get; set; }
		public Money TotalAmount { get; set; }
	}

	public class ScanTxoutOutput
	{
		public Coin Coin { get; set; }
		public int Height { get; set; }
	}
}
