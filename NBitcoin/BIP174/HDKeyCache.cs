using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin
{
	class HDKeyCache : IHDKey
	{
		private readonly IHDKey hdKey;
		private readonly KeyPath _PathFromRoot;
		private readonly ConcurrentDictionary<KeyPath, IHDKey> derivationCache;
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
			derivationCache = new ConcurrentDictionary<KeyPath, IHDKey>();
		}
		HDKeyCache(IHDKey hdKey, KeyPath childPath, ConcurrentDictionary<KeyPath, IHDKey> cache)
		{
			this.derivationCache = cache;
			_PathFromRoot = childPath;
			this.hdKey = hdKey;
		}

		public IHDKey Derive(uint index)
		{
			var childPath = _PathFromRoot.Derive(index);
			var key = derivationCache.GetOrAdd(childPath, _ => hdKey.Derive(index));
			return new HDKeyCache(key, childPath, derivationCache);
		}

		internal int Cached => derivationCache.Count;

		public PubKey GetPublicKey()
		{
			return this.hdKey.GetPublicKey();
		}
	}
}
