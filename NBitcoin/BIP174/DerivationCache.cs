using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin
{
	class DerivationCache
	{
		private readonly IHDKey masterKey;
		Dictionary<KeyPath, IHDKey> derivationCache = new Dictionary<KeyPath, IHDKey>();
		public DerivationCache(IHDKey masterKey)
		{
			this.masterKey = masterKey;
		}

		public IHDKey Derive(KeyPath keyPath)
		{
			var key = masterKey;
			var childPath = new KeyPath();
			foreach (var index in keyPath.Indexes)
			{
				childPath = childPath.Derive(index);
				if (derivationCache.TryGetValue(childPath, out var cachedKey))
				{
					key = cachedKey;
					continue;
				}
				key = key.Derive(index);
				if (derivationCache.Count < 256)
					derivationCache.Add(childPath, key);
			}
			return key;
		}
	}
}
