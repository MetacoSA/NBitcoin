using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin
{
	class HDKeyCache : IHDKey
	{
		private readonly IHDKey hdKey;
		private readonly KeyPath _PathFromRoot;
		readonly Dictionary<KeyPath, IHDKey> derivationCache;
		public IHDKey Inner
		{
			get
			{
				return hdKey;
			}
		}
		internal HDKeyCache(IHDKey masterKey)
		{
			this.hdKey = masterKey;
			_PathFromRoot = new KeyPath();
			derivationCache = new Dictionary<KeyPath, IHDKey>();
		}
		HDKeyCache(IHDKey hdKey, KeyPath childPath, Dictionary<KeyPath, IHDKey> cache)
		{
			this.derivationCache = cache;
			_PathFromRoot = childPath;
			this.hdKey = hdKey;
		}

		public IHDKey Derive(uint index)
		{
			var key = hdKey;
			var childPath = _PathFromRoot.Derive(index);
			System.Threading.Monitor.Enter(derivationCache);
			if (derivationCache.TryGetValue(childPath, out var cachedKey))
			{
				System.Threading.Monitor.Exit(derivationCache);
				key = cachedKey;
			}
			else
			{
				System.Threading.Monitor.Exit(derivationCache);
				key = key.Derive(index);
				lock (derivationCache)
				{
					if (derivationCache.Count < 256)
						derivationCache.Add(childPath, key);
				}
			}
			return new HDKeyCache(key, childPath, derivationCache);
		}

		public PubKey GetPublicKey()
		{
			return this.hdKey.GetPublicKey();
		}
	}
}
