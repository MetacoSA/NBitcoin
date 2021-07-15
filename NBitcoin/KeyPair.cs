#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class KeyPair
	{
		public KeyPair(Key key, IPubKey pubKey)
		{
			if (key is null)
				throw new ArgumentNullException(nameof(key));
			if (pubKey is null)
				throw new ArgumentNullException(nameof(pubKey));
			PubKey = pubKey;
			Key = key;
		}

		public IPubKey PubKey { get; }
		public Key Key { get; }

#if HAS_SPAN
		public static TaprootKeyPair CreateTaprootPair(Key key)
		{
			return CreateTaprootPair(key, null);
		}
		public static TaprootKeyPair CreateTaprootPair(Key key, uint256? merkleRoot)
		{
			if (key is null)
				throw new ArgumentNullException(nameof(key));
			return new TaprootKeyPair(key, key.PubKey.GetTaprootFullPubKey(merkleRoot));
		}
#endif
		public static KeyPair CreateECDSAPair(Key key)
		{
			if (key is null)
				throw new ArgumentNullException(nameof(key));
			return new KeyPair(key, key.PubKey);
		}
	}
#if HAS_SPAN
	public class TaprootKeyPair : KeyPair
	{
		public TaprootKeyPair(Key key, TaprootFullPubKey pubKey) : base(key, pubKey)
		{
			Key = key;
			PubKey = pubKey;
		}
		public new TaprootFullPubKey PubKey { get; }
		public new Key Key { get; }

		public TaprootSignature SignTaprootKeySpend(uint256 hash, TaprootSigHash sigHash)
		{
			if (hash == null)
				throw new ArgumentNullException(nameof(hash));
			return Key.SignTaprootKeySpend(hash, PubKey.MerkleRoot, sigHash);
		}
	}
#endif
}
