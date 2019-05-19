using System;
using System.Linq;
using NBitcoin.BuilderExtensions;

namespace NBitcoin.Scripting
{
	/// <summary>
	/// Public key objects in descriptors.
	/// </summary>
	public abstract class PubKeyProvider
	{

		# region subtypes and constructors
		internal static class Tags
		{
			public const int OriginPubKeyProvider = 0;
			public const int ConstPubKeyProvider = 1;
			public const int HDPubKeyProvider = 2;
		}
		internal int Tag { get; private set; }

		private PubKeyProvider(int tag) => Tag = tag;
		public class OriginPubKeyProvider : PubKeyProvider
		{
			internal OriginPubKeyProvider(RootedKeyPath keyOriginInfo, PubKeyProvider inner) : base(Tags.OriginPubKeyProvider)
			{
				if (keyOriginInfo == null)
					throw new ArgumentNullException(nameof(keyOriginInfo));
				if (inner == null)
					throw new ArgumentNullException(nameof(inner));
				if (inner.IsOrigin())
					throw new ArgumentException($"OriginPubKeyProvider can not have {inner} as inner value");
				KeyOriginInfo = keyOriginInfo;
				Inner = inner;
			}

			public RootedKeyPath KeyOriginInfo { get; }
			public PubKeyProvider Inner { get; }

			internal string OriginString()
				=> KeyOriginInfo.MasterFingerprint.ToString() + KeyOriginInfo.KeyPath.ToString();
		}

		public class ConstPubKeyProvider : PubKeyProvider
		{
			internal ConstPubKeyProvider(PubKey pk) : base(Tags.ConstPubKeyProvider)
			{
				if (pk == null)
					throw new System.ArgumentNullException(nameof(pk));
				Pk = pk;
			}

			public PubKey Pk { get; }
		}

		public enum DeriveType
		{
			NO,
			UNHARDENED,
			HARDENED
		}
		public class HDPubKeyProvider : PubKeyProvider
		{
			public HDPubKeyProvider(BitcoinExtPubKey extkey, KeyPath path, DeriveType derive) : base(Tags.HDPubKeyProvider)
			{
				if (extkey == null)
					throw new System.ArgumentNullException(nameof(extkey));

				if (path == null)
					throw new System.ArgumentNullException(nameof(path));

				Extkey = extkey;
				Path = path;
				Derive = derive;
			}

			public BitcoinExtPubKey Extkey { get; }
			public KeyPath Path { get; }
			public DeriveType Derive { get; }

			internal string GetPathString()
			{
				var path = $"/{Path.ToString()}";
				if (IsRange())
					path += "/*";
				if (Derive == DeriveType.HARDENED)
					path += "\'";
				return path;
			}

			internal bool IsHardened()
			{
				if (Derive == DeriveType.HARDENED) return true;
				return Path.IsHardenedPath;
			}
		}

		public static PubKeyProvider NewOrigin(RootedKeyPath keyOrigin, PubKeyProvider inner) =>
			new OriginPubKeyProvider(keyOrigin, inner);
		public static PubKeyProvider NewConst(PubKey pk) =>
			new ConstPubKeyProvider(pk);

		public static PubKeyProvider NewHD(BitcoinExtPubKey extPubKey, KeyPath kp, DeriveType t) =>
			new HDPubKeyProvider(extPubKey, kp, t);

		public bool IsOrigin() => Tag == Tags.OriginPubKeyProvider;
		public bool IsConst() => Tag == Tags.ConstPubKeyProvider;
		public bool IsHD() => Tag == Tags.HDPubKeyProvider;

		#endregion

		public PubKey GetPubKey(uint pos)
			=> GetPubKey(pos, out var _);
		public PubKey GetPubKey(uint pos, out RootedKeyPath keyOriginInfo)
		{
			if (!this.TryGetPubKey(pos, out keyOriginInfo, out var result))
				throw new InvalidOperationException($"Failed to get pubkey for {this} .Position: {pos}");
			return result;
		}
		public bool TryGetPubKey(uint pos, out RootedKeyPath keyOriginInfo, out PubKey pubkey)
		{
			pubkey = null;
			keyOriginInfo = null;
			switch (this)
			{
				case OriginPubKeyProvider self:
					if (!self.Inner.TryGetPubKey(pos, out keyOriginInfo, out pubkey))
						return false;
					keyOriginInfo = self.KeyOriginInfo;
					return true;
				case ConstPubKeyProvider self:
					pubkey = self.Pk;
					return true;
				case HDPubKeyProvider self:
					var keyid = self.Extkey.ExtPubKey.PubKey.Hash;
					if (self.Derive == DeriveType.HARDENED)
						pos = pos | 0x80000000;
					var newPath = new KeyPath(self.Path.Indexes.Concat(new []{pos}).ToArray());
					keyOriginInfo = new RootedKeyPath(
						HDFingerprint.FromKeyId(keyid),
						newPath
						);
					return true;

			}
			throw new Exception("Unreachable!");
		}
		public bool IsRange()
		{
			switch (this)
			{
				case OriginPubKeyProvider self:
					return self.Inner.IsRange();
				case ConstPubKeyProvider self:
					return false;
				case HDPubKeyProvider self:
					return self.Derive == DeriveType.NO;

			}
			throw new Exception("Unreachable!");
		}
		public bool IsCompressed()
		{
			switch (this)
			{
				case OriginPubKeyProvider self:
					return self.Inner.IsCompressed();
				case ConstPubKeyProvider self:
					return self.Pk.IsCompressed;
				case HDPubKeyProvider self:
					return false;
			}
			throw new Exception("Unreachable!");
		}

		public override string ToString()
		{
			switch (this)
			{
				case OriginPubKeyProvider self:
					return $"[{self.OriginString()}]{self.Inner.ToString()}";
				case ConstPubKeyProvider self:
					return self.Pk.ToHex();
				case HDPubKeyProvider self:
					return $"{self.Extkey.ToWif()}{self.GetPathString()}";
			}
			throw new Exception("Unreachable!");
		}

		/// <summary>
		/// Get the descriptor string form including the private data (If available in arg).
		/// </summary>
		/// <param name=extKeyProvider>Should return null if it could not find any corresponding ExtKey</param>
		/// <returns></returns>
		internal bool TryGetPrivateString(
			Func<KeyId, ISecret> secretProvider,
			out string ret)
		{
			ret = null;
			switch (this)
			{
				case OriginPubKeyProvider self:
					if (!self.TryGetPrivateString(secretProvider, out ret))
						return false;
					ret = $"[{self.OriginString()}]{ret}";
					return true;
				case ConstPubKeyProvider self:
					if (secretProvider != null)
					{
						var wif = secretProvider(self.Pk.Hash);
						if (wif == null)
							return false;
						ret = wif.ToString();
						return true;
					}
					return false;
				case HDPubKeyProvider self:
					if (secretProvider != null)
					{
						var secret = secretProvider(self.Extkey.ExtPubKey.PubKey.Hash);
						if (secret == null)
							return false;
						ret = $"{secret.ToString()}{self.GetPathString()}";
						return true;
					}
					return false;
			}
			throw new Exception("Unreachable!");
		}
	}
}