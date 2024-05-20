using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
#if HAS_SPAN
using NBitcoin.Secp256k1;
#endif

#nullable enable

namespace NBitcoin
{
	/// <summary>
	/// Interface for injecting security sensitive stuffs to other objects.
	/// This is equivalent to `SigningProvider` class in bitcoin core.
	/// Currently used in OutputDescriptor to safely inject private key information
	/// </summary>
	public interface ISigningRepository
	{
		/// <summary>
		/// In case of Witness Script, use HashForLookup Property as a key.
		/// </summary>
		/// <param name="scriptId"></param>
		/// <param name="script"></param>
		/// <returns></returns>
		bool TryGetScript(ScriptId scriptId, [MaybeNullWhen(false)] out Script script);
		bool TryGetPubKey(KeyId keyId, [MaybeNullWhen(false)] out PubKey pubkey);


		/// <summary>
		///  Get KeyOrigin info for the public key id.
		/// </summary>
		/// <param name="keyId"></param>
		/// <param name="keyorigin"></param>
		/// <returns></returns>
		bool TryGetKeyOrigin(KeyId keyId, [MaybeNullWhen(false)] out RootedKeyPath keyorigin);

		bool TryGetSecret(KeyId keyId, [MaybeNullWhen(false)] out ISecret secret);


		/// <summary>
		/// In case of Witness Script, use HashForLookup property as a key.
		/// </summary>
		/// <param name="scriptId"></param>
		/// <param name="script"></param>
		/// <returns></returns>
		void SetScript(ScriptId scriptId, Script script);
		void SetPubKey(KeyId keyId, PubKey pubkey);
		void SetSecret(KeyId keyId, ISecret secret);
		void SetKeyOrigin(KeyId keyId, RootedKeyPath keyOrigin);

#if HAS_SPAN
		bool TryGetKeyOrigin(TaprootPubKey taprootPubKey, [MaybeNullWhen(false)] out RootedKeyPath keyorigin);
		bool TryGetTaprootSpendInfo(TaprootPubKey taprootOutput,  [MaybeNullWhen(false)] out TaprootSpendInfo spendInfo);
		bool TryGetSecret(TaprootPubKey key, [MaybeNullWhen(false)] out ISecret secret);
		void SetTaprootSpendInfo(TaprootPubKey key, TaprootSpendInfo value);
		void SetSecret(TaprootPubKey key, BitcoinSecret secret);
		void SetKeyOrigin(TaprootPubKey taprootPubKey, RootedKeyPath keyOrigin);
#endif

		/// <summary>
		/// Consume the argument and take everything it holds.
		/// This method should at least support consuming `FlatSigningRepository`.
		/// </summary>
		/// <param name="other"></param>
		void Merge(ISigningRepository other);
	}

	public static class ISigningRepositoryExtensions
	{

		[Obsolete("Use OutputDescriptor.IsSolvable")]
		public static bool IsSolvable(this ISigningRepository repo, Script scriptPubKey)
		{
			var temp = scriptPubKey.FindTemplate();

#if HAS_SPAN
			if (temp is PayToTaprootTemplate p2trT)
			{
				if (p2trT.ExtractScriptPubKeyParameters(scriptPubKey) is TaprootPubKey pk)
				{
					if (repo.TryGetTaprootSpendInfo(pk, out var spendInfo))
					{
						if (spendInfo.OutputPubKey.OutputKey.Equals(pk))
							return true;
					}
				}
			}
#endif

			if (temp is PayToPubkeyTemplate p2pkT)
			{
				var pk = p2pkT.ExtractScriptPubKeyParameters(scriptPubKey)!;
				if (repo.TryGetPubKey(pk.Hash, out var _))
					return true;
			}

			if (temp is PayToPubkeyHashTemplate p2pkhT)
			{
				var keyId = p2pkhT.ExtractScriptPubKeyParameters(scriptPubKey)!;
				if (repo.TryGetPubKey(keyId, out var _))
					return true;
			}
			PubKey[]? pks = null;
			if (temp is PayToMultiSigTemplate p2multiT)
			{
				pks = p2multiT.ExtractScriptPubKeyParameters(scriptPubKey)!.PubKeys;
			}

			if (temp is PayToScriptHashTemplate p2shT)
			{
				var scriptId = p2shT.ExtractScriptPubKeyParameters(scriptPubKey)!;
				// This will give us witness script directly in case of p2sh-p2wsh
				if (repo.TryGetScript(scriptId, out var sc))
				{
					scriptPubKey = sc;
					pks = sc.GetAllPubKeys();
				}
			}

			if (temp is PayToWitTemplate)
			{
				var witId = PayToWitScriptHashTemplate.Instance.ExtractScriptPubKeyParameters(scriptPubKey);
				if (witId != null)
				{
					if (repo.TryGetScript(witId.HashForLookUp, out var sc))
						pks = sc.GetAllPubKeys();
				}
				var wpkh = PayToWitPubKeyHashTemplate.Instance.ExtractScriptPubKeyParameters(scriptPubKey);
				if (wpkh != null)
				{
					var witKeyId = wpkh.AsKeyId();
					if (repo.TryGetPubKey(witKeyId, out var _))
						return true;
				}
			}

			if (pks != null)
			{
				foreach (var pk in pks)
				{
					if (!repo.TryGetPubKey(pk.Hash, out var _))
						return false;
				}
				return true;
			}
			return false;
		}

		public static Key? GetPrivateKey(this ISigningRepository repo, KeyId id)
		{
			if (!repo.TryGetSecret(id, out var res))
				return null;
			return res.PrivateKey;
		}

	}

	public class FlatSigningRepository : ISigningRepository
	{
		public ConcurrentDictionary<KeyId, ISecret> Secrets {get;}
		public ConcurrentDictionary<KeyId, PubKey> Pubkeys { get; }
		public ConcurrentDictionary<KeyId, RootedKeyPath> KeyIdToKeyOrigins { get; }
		public ConcurrentDictionary<ScriptId, Script> Scripts { get; }
#if HAS_SPAN
		public ConcurrentDictionary<TaprootPubKey, TaprootSpendInfo> SpendInfos { get; }
		public ConcurrentDictionary<TaprootPubKey, RootedKeyPath> TaprootKeyOrigins { get; }
		public ConcurrentDictionary<TaprootPubKey, ISecret> TaprootKeysToSecret { get;  }
#endif

		public FlatSigningRepository()
		{
			Secrets = new ConcurrentDictionary<KeyId, ISecret>();
			Pubkeys = new ConcurrentDictionary<KeyId, PubKey>();
			KeyIdToKeyOrigins = new ConcurrentDictionary<KeyId, RootedKeyPath>();
			Scripts = new ConcurrentDictionary<ScriptId, Script>();
#if HAS_SPAN
			SpendInfos = new ConcurrentDictionary<TaprootPubKey, TaprootSpendInfo>();
			TaprootKeyOrigins = new ConcurrentDictionary<TaprootPubKey, RootedKeyPath>();
			TaprootKeysToSecret = new ConcurrentDictionary<TaprootPubKey, ISecret>();
#endif
		}

		public bool TryGetScript(ScriptId scriptId, [MaybeNullWhen(false)] out Script script)
			=> Scripts.TryGetValue(scriptId, out script);

		public bool TryGetPubKey(KeyId keyId, [MaybeNullWhen(false)] out PubKey pubkey)
			=> Pubkeys.TryGetValue(keyId, out pubkey);

		public bool TryGetKeyOrigin(KeyId keyId, [MaybeNullWhen(false)] out RootedKeyPath keyOrigin)
			=> KeyIdToKeyOrigins.TryGetValue(keyId, out keyOrigin);


		public bool TryGetSecret(KeyId keyId, [MaybeNullWhen(false)] out ISecret key)
			=> Secrets.TryGetValue(keyId, out key);


		public void SetScript(ScriptId scriptId, Script script)
		{
			if (scriptId == null)
			{
				throw new ArgumentNullException(nameof(scriptId));
			}

			if (script == null)
			{
				throw new ArgumentNullException(nameof(script));
			}

			Scripts.AddOrReplace(scriptId, script);
		}

		public void SetPubKey(KeyId keyId, PubKey pubkey)
		{
			if (keyId == null)
			{
				throw new ArgumentNullException(nameof(keyId));
			}

			if (pubkey == null)
			{
				throw new ArgumentNullException(nameof(pubkey));
			}

			Pubkeys.AddOrReplace(keyId, pubkey);
		}

		public void SetSecret(KeyId keyId, ISecret secret)
		{
			if (keyId == null)
			{
				throw new ArgumentNullException(nameof(keyId));
			}

			if (secret == null)
			{
				throw new ArgumentNullException(nameof(secret));
			}

			Secrets.AddOrReplace(keyId, secret);
		}

		public void SetKeyOrigin(KeyId keyId, RootedKeyPath keyOrigin)
		{
			if (keyId == null)
			{
				throw new ArgumentNullException(nameof(keyId));
			}

			if (keyOrigin == null)
			{
				throw new ArgumentNullException(nameof(keyOrigin));
			}

			KeyIdToKeyOrigins.AddOrReplace(keyId, keyOrigin);
		}


#if HAS_SPAN
		public bool TryGetKeyOrigin(TaprootPubKey taprootPubKey, [MaybeNullWhen(false)]out RootedKeyPath keyorigin)
			=> TaprootKeyOrigins.TryGetValue(taprootPubKey, out keyorigin);

		public bool TryGetTaprootSpendInfo(TaprootPubKey taprootOutput, [MaybeNullWhen(false)] out TaprootSpendInfo spendInfo)
			=> SpendInfos.TryGetValue(taprootOutput, out spendInfo);

		public bool TryGetSecret(TaprootPubKey key, [MaybeNullWhen(false)]out ISecret secret)
			=> TaprootKeysToSecret.TryGetValue(key, out secret);

		public void SetTaprootSpendInfo(TaprootPubKey key, TaprootSpendInfo value)
		{
			if (key == null) throw new ArgumentNullException(nameof(key));
			if (value == null) throw new ArgumentNullException(nameof(value));

			SpendInfos.AddOrReplace(key, value);
		}

		public void SetSecret(TaprootPubKey key, BitcoinSecret secret)
		{
			if (key == null) throw new ArgumentNullException(nameof(key));
			if (secret == null) throw new ArgumentNullException(nameof(secret));

			TaprootKeysToSecret.AddOrReplace(key, secret);
		}

		public void SetKeyOrigin(TaprootPubKey taprootPubKey, RootedKeyPath keyOrigin)
		{
			if (taprootPubKey == null) throw new ArgumentNullException(nameof(taprootPubKey));
			if (keyOrigin == null) throw new ArgumentNullException(nameof(keyOrigin));
			TaprootKeyOrigins.AddOrReplace(taprootPubKey, keyOrigin);
		}
#endif

		public void Merge(ISigningRepository other)
		{
			if (!(other is FlatSigningRepository))
			{
				throw new NotSupportedException($"{nameof(FlatSigningRepository)} can be merged only with the same type");
			}

			var otherRepo = (FlatSigningRepository) other;
			MergeDict(Secrets, otherRepo.Secrets);
			MergeDict(Pubkeys, otherRepo.Pubkeys);
			MergeDict(Scripts, otherRepo.Scripts);
			MergeDict(KeyIdToKeyOrigins, otherRepo.KeyIdToKeyOrigins);
#if HAS_SPAN
			MergeDict(SpendInfos, otherRepo.SpendInfos);
			MergeDict(TaprootKeyOrigins, otherRepo.TaprootKeyOrigins);
			MergeDict(TaprootKeysToSecret, otherRepo.TaprootKeysToSecret);
#endif
		}

		private void MergeDict<U, T>(ConcurrentDictionary<U, T> a, ConcurrentDictionary<U, T> b) where U : notnull
		{
			foreach (var bItem in b)
			{
				a.AddOrReplace(bItem.Key, bItem.Value);
			}
		}
	}
}
#nullable disable
