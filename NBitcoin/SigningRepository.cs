using System;
using System.Collections.Concurrent;

namespace NBitcoin
{
	/// <summary>
	/// Interface for injecting security sensitive stuffs to other objects.
	/// This is equivalent to `SigningProvider` class in bitcoin core.
	/// Currently used in OutputDescriptor to safely inject private key information
	/// </summary>
	public interface ISigningRepository
	{
		bool TryGetScript(ScriptId scriptId, out Script script);
		bool TryGetPubKey(KeyId keyId, out PubKey pubkey);
		bool TryGetKeyOrigin(KeyId keyId, out RootedKeyPath keyorigin);
		bool TryGetSecret(KeyId keyId, out ISecret secret);

		void SetScript(ScriptId scriptId, Script script);
		void SetPubKey(KeyId keyId, PubKey pubkey);
		void SetSecret(KeyId keyId, ISecret secret);
		void SetKeyOrigin(KeyId keyId, RootedKeyPath keyOrigin);
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

		public bool TryGetScript(ScriptId scriptId, out Script script)
			=> Scripts.TryGetValue(scriptId, out script);

		public bool TryGetPubKey(KeyId keyId, out PubKey pubkey)
			=> Pubkeys.TryGetValue(keyId, out pubkey);

		public bool TryGetKeyOrigin(KeyId keyId, out RootedKeyPath keyOrigin)
			=> KeyOrigins.TryGetValue(keyId, out keyOrigin);

		public bool TryGetSecret(KeyId keyId, out ISecret key)
			=> Secrets.TryGetValue(keyId, out key);

		public void SetScript(ScriptId scriptId, Script script)
			=> Scripts.AddOrReplace(scriptId, script);

		public void SetPubKey(KeyId keyId, PubKey pubkey)
			=> Pubkeys.AddOrReplace(keyId, pubkey);

		public void SetSecret(KeyId keyId, ISecret secret)
			=> Secrets.AddOrReplace(keyId, secret);

		public void SetKeyOrigin(KeyId keyId, RootedKeyPath keyOrigin)
			=> KeyOrigins.AddOrReplace(keyId, keyOrigin);


		public FlatSigningRepository Merge(FlatSigningRepository other)
		{
			MergeDict(Secrets, other.Secrets);
			MergeDict(Pubkeys, other.Pubkeys);
			MergeDict(Scripts, other.Scripts);
			MergeDict(KeyOrigins, other.KeyOrigins);
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