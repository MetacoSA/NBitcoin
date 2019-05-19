using System;
using System.Linq;
using NBitcoin.BuilderExtensions;

namespace NBitcoin.Scripting
{
	/// <summary>
	/// Public key objects in descriptors.
	/// </summary>
	public abstract class PubKeyProvider : IEquatable<PubKeyProvider>
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

		/// <summary>
		/// Wrapper for other pubkey provider which contains (parent key finger print + relative derivation path to inner Pubkey provider)
		/// </summary>
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
					return self.Derive != DeriveType.NO;

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
					return $"[{self.KeyOriginInfo.ToString()}]{self.Inner.ToString()}";
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
			ISigningRepository secretProvider,
			out string ret)
		{
			if (secretProvider == null)
				throw new ArgumentNullException(nameof(secretProvider));

			ret = null;
			switch (this)
			{
				case OriginPubKeyProvider self:
					if (!self.TryGetPrivateString(secretProvider, out ret))
						return false;
					ret = $"[{self.KeyOriginInfo.ToString()}]{ret}";
					return true;
				case ConstPubKeyProvider self:
					if (!secretProvider.TryGetSecret(self.Pk.Hash, out var secretConst))
						return false;
					ret = secretConst.ToString();
					return true;
				case HDPubKeyProvider self:
					if (!secretProvider.TryGetSecret(self.Extkey.ExtPubKey.PubKey.Hash, out var secretHD))
						return false;
					ret = $"{secretHD.ToString()}{self.GetPathString()}";
					return true;
			}
			throw new Exception("Unreachable!");
		}

		public sealed override bool Equals(object obj)
			=> Equals(obj as PubKeyProvider);


		public bool Equals(PubKeyProvider other)
		{
			if (other is null || Tag != other.Tag)
				return false;

			switch (this.Tag)
			{
				case Tags.ConstPubKeyProvider:
					var s1 = (ConstPubKeyProvider)this;
					return s1.Pk.Equals(((ConstPubKeyProvider)other).Pk);
				case Tags.HDPubKeyProvider:
					var s2 = (HDPubKeyProvider)this;
					var o2 = (HDPubKeyProvider)other;
					return s2.Derive == o2.Derive &&
						s2.Path.Equals(o2.Path) &&
						s2.Extkey.Equals(o2.Extkey);
				case Tags.OriginPubKeyProvider:
					var s3 = (OriginPubKeyProvider)this;
					var o3 = (OriginPubKeyProvider)other;
					return s3.KeyOriginInfo.Equals(o3.KeyOriginInfo) &&
						s3.Inner.Equals(o3.Inner);
			}
			throw new Exception("Unreachable");
		}

		public override int GetHashCode()
		{
			if (this != null)
			{
				int num = 0;
				switch (this)
				{
					case ConstPubKeyProvider self:
						{
							num = 0;
							return -1640531527 + self.Pk.GetHashCode() + ((num << 6) + (num >> 2));
						}
					case HDPubKeyProvider self:
						{
							num = 1;
							num = -1640531527 + self.Extkey.GetHashCode() + ((num << 6) + (num >> 2));
							num = -1640531527 + self.Path.GetHashCode() + ((num << 6) + (num >> 2));
							return -1640531527 + self.Derive.GetHashCode() + ((num << 6) + (num >> 2));
						}
					case OriginPubKeyProvider self:
						{
							num = 2;
							num = -1640531527 + self.Inner.GetHashCode() + ((num << 6) + (num >> 2));
							return -1640531527 + self.KeyOriginInfo.GetHashCode() + ((num << 6) + (num >> 2));
						}
				}
			}
			return 0;
		}
	}
}