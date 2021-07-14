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
		public static KeyPair CreateTaprootBIP38Pair(Key key)
		{
			if (key is null)
				throw new ArgumentNullException(nameof(key));
			return new KeyPair(key, key.PubKey.GetTaprootPubKey());
		}
#endif
		public static KeyPair CreateECDSAPair(Key key)
		{
			if (key is null)
				throw new ArgumentNullException(nameof(key));
			return new KeyPair(key, key.PubKey);
		}
	}
}
