using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NBitcoin
{
	/// <summary>
	/// Interface for injecting security sensitive stuffs to other objects.
	/// This is equivalent to `SigningProvider` class in bitcoin core.
	/// Currently used in OutputDescriptor to safely inject private key information
	/// </summary>
	public abstract class ISigningRepository
	{
		/// <summary>
		/// In case of Witness Script, use HashForLookup Property as a key.
		/// </summary>
		/// <param name="scriptId"></param>
		/// <param name="script"></param>
		/// <returns></returns>
		public abstract bool TryGetScript(ScriptId scriptId, out Script script);
		public abstract bool TryGetPubKey(KeyId keyId, out PubKey pubkey);
		public abstract bool TryGetKeyOrigin(KeyId keyId, out RootedKeyPath keyorigin);
		public abstract bool TryGetSecret(KeyId keyId, out ISecret secret);

		public virtual bool IsSolvable(Script scriptPubKey)
		{
			var temp = scriptPubKey.FindTemplate();
			if (temp is PayToPubkeyTemplate p2pkT)
			{
				var pk = p2pkT.ExtractScriptPubKeyParameters(scriptPubKey);
				if (TryGetPubKey(pk.Hash, out var _))
					return true;
			}

			if (temp is PayToPubkeyHashTemplate p2pkhT)
			{
				var keyId = p2pkhT.ExtractScriptPubKeyParameters(scriptPubKey);
				if (TryGetPubKey(keyId, out var _))
					return true;
			}
			PubKey[] pks = null;
			if (temp is PayToMultiSigTemplate p2multiT)
			{
				pks = p2multiT.ExtractScriptPubKeyParameters(scriptPubKey).PubKeys;
			}

			if (temp is PayToScriptHashTemplate p2shT)
			{
				var scriptId = p2shT.ExtractScriptPubKeyParameters(scriptPubKey);
				// This will give us witness script directly in case of p2sh-p2wsh
				if (TryGetScript(scriptId, out var sc))
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
					if (TryGetScript(witId.HashForLookUp, out var sc))
						pks = sc.GetAllPubKeys();
				}
				var wpkh = PayToWitPubKeyHashTemplate.Instance.ExtractScriptPubKeyParameters(scriptPubKey);
				if (wpkh != null)
				{
					var witKeyId = wpkh.AsKeyId();
					if (TryGetPubKey(witKeyId, out var _))
						return true;
				}
			}

			if (pks != null)
			{
				foreach (var pk in pks)
				{
					if (!TryGetPubKey(pk.Hash, out var _))
						return false;
				}
				return true;
			}
			return false;
		}


		/// <summary>
		/// In case of Witness Script, use HashForLookup property as a key.
		/// </summary>
		/// <param name="scriptId"></param>
		/// <param name="script"></param>
		/// <returns></returns>
		public abstract void SetScript(ScriptId scriptId, Script script);
		public abstract void SetPubKey(KeyId keyId, PubKey pubkey);
		public abstract void SetSecret(KeyId keyId, ISecret secret);
		public abstract void SetKeyOrigin(KeyId keyId, RootedKeyPath keyOrigin);
		public abstract ISigningRepository Merge(ISigningRepository other);
	}
	public class FlatSigningRepository : ISigningRepository
	{
		public ConcurrentDictionary<KeyId, ISecret> Secrets {get;}
		public ConcurrentDictionary<KeyId, PubKey> Pubkeys { get; }
		public ConcurrentDictionary<KeyId, RootedKeyPath> KeyOrigins { get; }
		public ConcurrentDictionary<ScriptId, Script> Scripts { get; }

		public FlatSigningRepository()
		{
			Secrets = new ConcurrentDictionary<KeyId, ISecret>();
			Pubkeys = new ConcurrentDictionary<KeyId, PubKey>();
			KeyOrigins = new ConcurrentDictionary<KeyId, RootedKeyPath>();
			Scripts = new ConcurrentDictionary<ScriptId, Script>();
		}

		public override bool TryGetScript(ScriptId scriptId, out Script script)
			=> Scripts.TryGetValue(scriptId, out script);

		public override bool TryGetPubKey(KeyId keyId, out PubKey pubkey)
			=> Pubkeys.TryGetValue(keyId, out pubkey);

		public override bool TryGetKeyOrigin(KeyId keyId, out RootedKeyPath keyOrigin)
			=> KeyOrigins.TryGetValue(keyId, out keyOrigin);

		public override bool TryGetSecret(KeyId keyId, out ISecret key)
			=> Secrets.TryGetValue(keyId, out key);

		public Key GetPrivateKey(KeyId id)
		{
			if (!TryGetSecret(id, out var res))
				return null;
			return res.PrivateKey;
		}
		public override void SetScript(ScriptId scriptId, Script script)
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

		public override void SetPubKey(KeyId keyId, PubKey pubkey)
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

		public override void SetSecret(KeyId keyId, ISecret secret)
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

		public override void SetKeyOrigin(KeyId keyId, RootedKeyPath keyOrigin)
		{
			if (keyId == null)
			{
				throw new ArgumentNullException(nameof(keyId));
			}

			if (keyOrigin == null)
			{
				throw new ArgumentNullException(nameof(keyOrigin));
			}

			KeyOrigins.AddOrReplace(keyId, keyOrigin);
		}

		public override ISigningRepository Merge(ISigningRepository other)
		{
			if (!(other is FlatSigningRepository))
			{
				throw new NotSupportedException($"{nameof(FlatSigningRepository)} can be merged only with the same type");
			}

			var otherRepo = (FlatSigningRepository) other;
			MergeDict(Secrets, otherRepo.Secrets);
			MergeDict(Pubkeys, otherRepo.Pubkeys);
			MergeDict(Scripts, otherRepo.Scripts);
			MergeDict(KeyOrigins, otherRepo.KeyOrigins);
			return this;
		}

		private void MergeDict<U, T>(ConcurrentDictionary<U, T> a, ConcurrentDictionary<U, T> b)
		{
			foreach (var bItem in b)
			{
				a.AddOrReplace(bItem.Key, bItem.Value);
			}
		}
	}
}
