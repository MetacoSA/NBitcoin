#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using UnKnownKVMap = System.Collections.Generic.SortedDictionary<byte[], byte[]>;
using HDKeyPathKVMap = System.Collections.Generic.SortedDictionary<NBitcoin.PubKey, NBitcoin.RootedKeyPath>;
using HDTaprootKeyPathKVMap = System.Collections.Generic.SortedDictionary<NBitcoin.TaprootPubKey, NBitcoin.TaprootKeyPath>;
using NBitcoin.Crypto;
using System.Collections;

namespace NBitcoin
{
	public abstract class PSBTCoin
	{
		protected HDKeyPathKVMap hd_keypaths = new HDKeyPathKVMap(PubKeyComparer.Instance);
		protected HDTaprootKeyPathKVMap hd_taprootkeypaths = new HDTaprootKeyPathKVMap(Comparer<NBitcoin.TaprootPubKey>.Default);
		protected UnKnownKVMap unknown = new SortedDictionary<byte[], byte[]>(BytesComparer.Instance);
		protected Script? redeem_script;
		protected Script? witness_script;
		protected readonly PSBT Parent;
		public PSBT PSBT => Parent;
		public PSBTCoin(PSBT parent)
		{
			hd_keypaths = new HDKeyPathKVMap(PubKeyComparer.Instance);
			unknown = new UnKnownKVMap(BytesComparer.Instance);
			Parent = parent;
		}

		public SortedDictionary<byte[], byte[]> Unknown => unknown;

		public HDKeyPathKVMap HDKeyPaths
		{
			get
			{
				return hd_keypaths;
			}
		}
		public HDTaprootKeyPathKVMap HDTaprootKeyPaths
		{
			get
			{
				return hd_taprootkeypaths;
			}
		}

		public IEnumerable<KeyValuePair<IPubKey, RootedKeyPath>> EnumerateKeyPaths()
		{
			foreach (var v in hd_keypaths)
			{
				yield return new KeyValuePair<IPubKey, RootedKeyPath>(v.Key, v.Value);
			}
			foreach (var v in hd_taprootkeypaths)
			{
				yield return new KeyValuePair<IPubKey, RootedKeyPath>(v.Key, v.Value.RootedKeyPath);
			}
		}

		public TaprootInternalPubKey? TaprootInternalKey { get; set; }

		public Script? RedeemScript
		{
			get
			{
				return redeem_script;
			}
			set
			{
				redeem_script = value;
			}
		}

		public Script? WitnessScript
		{
			get
			{
				return witness_script;
			}
			set
			{
				witness_script = value;
			}
		}

		public virtual void AddKeyPath(PubKey pubKey, RootedKeyPath rootedKeyPath)
		{
			if (rootedKeyPath == null)
				throw new ArgumentNullException(nameof(rootedKeyPath));
			if (pubKey is null)
				throw new ArgumentNullException(nameof(pubKey));
			hd_keypaths.AddOrReplace(pubKey, rootedKeyPath);

			// Let's try to be smart, if the added key match the scriptPubKey then we are in p2psh p2wpkh
			if (Parent.Settings.IsSmart && redeem_script == null)
			{
				var output = GetTxOut();
				if (output != null)
				{
					if (pubKey.WitHash.ScriptPubKey.Hash.ScriptPubKey == output.ScriptPubKey)
					{
						redeem_script = pubKey.WitHash.ScriptPubKey;
					}
				}
			}
		}

		/// <summary>
		/// Filter the keys which contains the <paramref name="accountKey"/> and <paramref name="accountKeyPath"/> in the HDKeys and whose input/output
		/// the same scriptPubKeys as <paramref name="accountHDScriptPubKey"/>.
		/// </summary>
		/// <param name="accountHDScriptPubKey">The accountHDScriptPubKey used to generate addresses</param>
		/// <param name="accountKey">The account key that will be used to sign (ie. 49'/0'/0')</param>
		/// <param name="accountKeyPath">The account key path</param>
		/// <returns>HD Keys matching master root key</returns>
		public IEnumerable<PSBTHDKeyMatch> HDKeysFor(IHDScriptPubKey? accountHDScriptPubKey, IHDKey accountKey, RootedKeyPath? accountKeyPath = null)
		{
			if (accountKey == null)
				throw new ArgumentNullException(nameof(accountKey));
			return HDKeysFor(accountHDScriptPubKey, accountKey, accountKeyPath, accountKey.GetPublicKey().GetHDFingerPrint());
		}
		internal IEnumerable<PSBTHDKeyMatch> HDKeysFor(IHDScriptPubKey? accountHDScriptPubKey, IHDKey accountKey, RootedKeyPath? accountKeyPath, HDFingerprint accountFingerprint)
		{
			var coinScriptPubKey = this.GetTxOut()?.ScriptPubKey;
			bool Match(KeyValuePair<IPubKey, RootedKeyPath> hdKey, HDFingerprint? expectedMasterFp, IHDKey accountKey, KeyPath addressPath)
			{
				if (expectedMasterFp is not null &&
					hdKey.Value.MasterFingerprint != expectedMasterFp.Value)
					return false;
				if (accountHDScriptPubKey is not null)
				{
					var derivedScript = accountHDScriptPubKey.Derive(addressPath);
					if (derivedScript is null)
						return false;
					if (derivedScript.ScriptPubKey != coinScriptPubKey)
						return false;
				}
				var derived = accountKey.Derive(addressPath);
				if (derived is null)
					return false;
				return hdKey.Key switch
				{
					PubKey pk => derived.GetPublicKey().Equals(pk),
#if HAS_SPAN
					TaprootPubKey tpk => derived.GetPublicKey().GetTaprootFullPubKey() is var tfp && (tfp.OutputKey.Equals(tpk) || tfp.InternalKey.AsTaprootPubKey().Equals(tpk)),
#endif
					_ => false
				};
			}
			accountKey = accountKey.AsHDKeyCache();
			accountHDScriptPubKey = accountHDScriptPubKey?.AsHDKeyCache();
			foreach (var hdKey in EnumerateKeyPaths())
			{
				bool matched = false;
				// The case where the fingerprint of the hdkey is exactly equal to the accountKey
				if (Match(hdKey, accountFingerprint, accountKey, hdKey.Value.KeyPath))
				{
					yield return CreateHDKeyMatch(accountKey, hdKey.Value.KeyPath, hdKey);
					matched = true;
				}

				// The typical case where accountkey is based on an hardened derivation (eg. 49'/0'/0')
				if (!matched && accountKeyPath?.MasterFingerprint is HDFingerprint mp)
				{
					var addressPath = hdKey.Value.KeyPath.GetAddressKeyPath();
					// The cases where addresses are generated on a non-hardened path below it (eg. 49'/0'/0'/0/1)
					if (addressPath.Indexes.Length != 0)
					{
						if (Match(hdKey, mp, accountKey, addressPath))
						{
							yield return CreateHDKeyMatch(accountKey, addressPath, hdKey);
							matched = true;
						}
					}
					// in some cases addresses are generated on a hardened path below the account key (eg. 49'/0'/0'/0'/1') in which case we
					// need to brute force what the address key path is
					else
					{
						int addressPathSize = 0;
						var hdKeyIndexes = hdKey.Value.KeyPath.Indexes;
						while (addressPathSize <= hdKey.Value.KeyPath.Indexes.Length)
						{
							var indexes = new uint[addressPathSize];
							Array.Copy(hdKeyIndexes, hdKey.Value.KeyPath.Length - addressPathSize, indexes, 0, addressPathSize);
							addressPath = new KeyPath(indexes);
							if (Match(hdKey, null, accountKey, addressPath))
							{
								yield return CreateHDKeyMatch(accountKey, addressPath, hdKey);
								matched = true;
								break;
							}
							addressPathSize++;
						}
					}
				}
			}
		}

		protected abstract PSBTHDKeyMatch CreateHDKeyMatch(IHDKey accountKey, KeyPath addressKeyPath, KeyValuePair<IPubKey, RootedKeyPath> kv);

		public abstract TxOut? GetTxOut();
	}
}
#nullable disable
