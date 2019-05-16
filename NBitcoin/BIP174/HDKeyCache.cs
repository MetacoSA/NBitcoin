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

		public IHDKey Derive(KeyPath keyPath)
		{
			if (keyPath == null)
				throw new ArgumentNullException(nameof(keyPath));
			var key = Inner;
			var childPath = _PathFromRoot;
			foreach (var index in keyPath.Indexes)
			{
				childPath = childPath.Derive(index);
				key = derivationCache.GetOrAdd(childPath, _ => key.Derive(new KeyPath(index)));
			}
			return new HDKeyCache(key, childPath, derivationCache);
		}

		internal int Cached => derivationCache.Count;

		public PubKey GetPublicKey()
		{
			return this.hdKey.GetPublicKey();
		}

		public bool CanDeriveHardenedPath()
		{
			return Inner.CanDeriveHardenedPath();
		}
	}
	class HDScriptPubKeyCache : IHDScriptPubKey
	{
		private readonly IHDScriptPubKey hdKey;
		private readonly KeyPath _PathFromRoot;
		private readonly ConcurrentDictionary<KeyPath, IHDScriptPubKey> derivationCache;
		public IHDScriptPubKey Inner
		{
			get
			{
				return hdKey;
			}
		}
		internal HDScriptPubKeyCache(IHDScriptPubKey masterKey)
		{
			this.hdKey = masterKey;
			_PathFromRoot = new KeyPath();
			derivationCache = new ConcurrentDictionary<KeyPath, IHDScriptPubKey>();
		}
		HDScriptPubKeyCache(IHDScriptPubKey hdKey, KeyPath childPath, ConcurrentDictionary<KeyPath, IHDScriptPubKey> cache)
		{
			this.derivationCache = cache;
			_PathFromRoot = childPath;
			this.hdKey = hdKey;
		}

		public IHDScriptPubKey Derive(KeyPath keyPath)
		{
			if (keyPath == null)
				throw new ArgumentNullException(nameof(keyPath));
			var key = Inner;
			var childPath = _PathFromRoot;
			foreach (var index in keyPath.Indexes)
			{
				childPath = childPath.Derive(index);
				key = derivationCache.GetOrAdd(childPath, _ => key.Derive(new KeyPath(index)));
			}
			return new HDScriptPubKeyCache(key, childPath, derivationCache);
		}

		internal int Cached => derivationCache.Count;

		public Script ScriptPubKey => Inner.ScriptPubKey;

		public bool CanDeriveHardenedPath()
		{
			return Inner.CanDeriveHardenedPath();
		}
	}
}
