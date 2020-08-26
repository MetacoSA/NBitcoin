using System;
using System.Collections.Generic;
using System.Diagnostics;
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
					throw new ArgumentNullException(nameof(extkey));

				if (path == null)
					throw new ArgumentNullException(nameof(path));

				Extkey = extkey;
				Path = path;
				Derive = derive;
			}

			public BitcoinExtPubKey Extkey { get; }
			public KeyPath Path { get; }
			public DeriveType Derive { get; }

			internal string GetPathString()
			{
				var path = Path == KeyPath.Empty ? "" : $"/{Path}";
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

			internal bool TryGetExtKey(Func<KeyId, Key> privateKeyProvider, out BitcoinExtKey extKey)
			{
				extKey = null;
				if (privateKeyProvider == null)
					return false;
				var privKey = privateKeyProvider(this.Extkey.ExtPubKey.PubKey.Hash);
				if (privKey == null)
					return false;
				extKey = new BitcoinExtKey(Extkey, privKey);
				return true;
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

		public PubKey GetPubKey(uint pos, Func<KeyId, Key> privateKeyProvider)
			=> GetPubKey(pos, privateKeyProvider, out var _);
		public PubKey GetPubKey(uint pos, Func<KeyId, Key> privateKeyProvider, out RootedKeyPath keyOriginInfo)
		{
			if (!this.TryGetPubKey(pos, privateKeyProvider, out keyOriginInfo, out var result))
				throw new InvalidOperationException($"Failed to get pubkey for {this} .Position: {pos}");
			return result;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="privateKeyProvider">In case of the hardend derivation.
		/// You must give private key by this to derive child</param>
		/// <param name="keyOriginInfo"></param>
		/// <param name="pubkey"></param>
		/// <returns></returns>
		public bool TryGetPubKey(uint pos, Func<KeyId, Key> privateKeyProvider, out RootedKeyPath keyOriginInfo, out PubKey pubkey)
		{
			pubkey = null;
			keyOriginInfo = null;
			switch (this)
			{
				case OriginPubKeyProvider self:
					if (!self.Inner.TryGetPubKey(pos, privateKeyProvider, out var subKeyOriginInfo, out pubkey))
						return false;
					if (subKeyOriginInfo != null)
					{
						keyOriginInfo = new RootedKeyPath(
							self.KeyOriginInfo.MasterFingerprint,
							new KeyPath(self.KeyOriginInfo.KeyPath.Indexes.Concat(subKeyOriginInfo.KeyPath.Indexes).ToArray())
							);
					}
					else
					{
						keyOriginInfo = self.KeyOriginInfo;
					}
					return true;
				case ConstPubKeyProvider self:
					pubkey = self.Pk;
					return true;
				case HDPubKeyProvider self:
					// 1. Derive PublicKey
					if (self.IsHardened())
					{
						if (!self.TryGetExtKey(privateKeyProvider, out var extkey))
							return false;
						extkey = extkey.Derive(self.Path);
						if (self.Derive == DeriveType.UNHARDENED)
							extkey = extkey.Derive(pos);
						if (self.Derive == DeriveType.HARDENED)
							extkey = extkey.Derive(pos | 0x80000000);
						pubkey = extkey.Neuter().ExtPubKey.PubKey;
					} else
					{
						var extkey = new BitcoinExtPubKey(self.Extkey.ToString());
						extkey = extkey.Derive(self.Path);
						if (self.Derive == DeriveType.UNHARDENED)
							extkey = extkey.Derive(pos);
						Debug.Assert(self.Derive != DeriveType.HARDENED);
						pubkey = extkey.ExtPubKey.PubKey;
					}
					// 2. get a relative keypath. assuming masterFingerPrint is not of real "master key"
					// but of xpub which this provider holds.
					var keyId = self.Extkey.ExtPubKey.PubKey.Hash;
					var index = new List<uint>(self.Path.Indexes);
					if (self.Derive == DeriveType.HARDENED)
						index.Add(pos | 0x80000000);
					if (self.Derive == DeriveType.UNHARDENED)
						index.Add(pos);

					keyOriginInfo = new RootedKeyPath(
						HDFingerprint.FromKeyId(keyId),
						new KeyPath(index.ToArray())
						);
					return true;

			}
			throw new Exception("Unreachable!");
		}
		public bool IsRange() => (this) switch
			{
				OriginPubKeyProvider self => self.Inner.IsRange(),
				ConstPubKeyProvider _ => false,
				HDPubKeyProvider self => self.Derive != DeriveType.NO,
				_ => throw new Exception("Unreachable!"),
			};
		public bool IsCompressed() => (this) switch
			{
				OriginPubKeyProvider self =>
					self.Inner.IsCompressed(),
				ConstPubKeyProvider self =>
					self.Pk.IsCompressed,
				HDPubKeyProvider _ =>
					false,
				_ => throw new Exception("Unreachable!"),
			};

		public override string ToString() => (this) switch
			{
				OriginPubKeyProvider self =>
					$"[{self.KeyOriginInfo}]{self.Inner}",
				ConstPubKeyProvider self =>
					self.Pk.ToHex(),
				HDPubKeyProvider self =>
					$"{self.Extkey.ToWif()}{self.GetPathString()}",
				_ =>
					throw new Exception("Unreachable!"),
			};

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
					if (!self.Inner.TryGetPrivateString(secretProvider, out ret))
						return false;
					ret = $"[{self.KeyOriginInfo}]{ret}";
					return true;
				case ConstPubKeyProvider self:
					if (!secretProvider.TryGetSecret(self.Pk.Hash, out var secretConst))
						return false;
					ret = secretConst.ToString();
					return true;
				case HDPubKeyProvider self:
					if (!secretProvider.TryGetSecret(self.Extkey.ExtPubKey.PubKey.Hash, out var secretHD))
						return false;
					ret = $"{secretHD}{self.GetPathString()}";
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
