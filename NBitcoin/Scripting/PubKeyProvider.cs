using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NBitcoin.Scripting
{
	/// <summary>
	/// Public key objects in descriptors.
	/// </summary>
	public abstract class PubKeyProvider : IEquatable<PubKeyProvider>
	{

		# region subtypes and constructors

		private PubKeyProvider() {}

		/// <summary>
		/// Wrapper for other pubkey provider which contains (parent key finger print + relative derivation path to inner Pubkey provider)
		/// </summary>
		public class Origin : PubKeyProvider
		{
			internal Origin(RootedKeyPath keyOriginInfo, PubKeyProvider inner)
			{
				if (keyOriginInfo == null)
					throw new ArgumentNullException(nameof(keyOriginInfo));
				if (inner == null)
					throw new ArgumentNullException(nameof(inner));
				if (inner is Origin)
					throw new ArgumentException($"Origin can not have {inner} as inner value");
				KeyOriginInfo = keyOriginInfo;
				Inner = inner;
			}

			public RootedKeyPath KeyOriginInfo { get; }
			public PubKeyProvider Inner { get; }

		}


		public class Const : PubKeyProvider
		{
			internal Const(PubKey pk)
			{
				if (pk == null)
					throw new ArgumentNullException(nameof(pk));
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
		public class HD : PubKeyProvider
		{
			public HD(BitcoinExtPubKey extkey, KeyPath path, DeriveType derive)
			{
				if (extkey == null)
					throw new ArgumentNullException(nameof(extkey));

				if (path == null)
					throw new ArgumentNullException(nameof(path));

				if (!Enum.IsDefined(typeof(DeriveType), derive))
					throw new ArgumentException($"Invalid value for DeriveType {derive}", nameof(derive));

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

			internal bool IsHardened() => Derive == DeriveType.HARDENED || Path.IsHardenedPath;

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
			new Origin(keyOrigin, inner);
		public static PubKeyProvider NewConst(PubKey pk) =>
			new Const(pk);

		public static PubKeyProvider NewHD(BitcoinExtPubKey extPubKey, KeyPath kp, DeriveType t) =>
			new HD(extPubKey, kp, t);

		#endregion

		public PubKey GetPubKey(uint pos, Func<KeyId, Key> privateKeyProvider)
			=> GetPubKey(pos, privateKeyProvider, out _);
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
			if (privateKeyProvider == null) throw new ArgumentNullException(nameof(privateKeyProvider));
			pubkey = null;
			keyOriginInfo = null;
			switch (this)
			{
				case Origin self:
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
				case Const self:
					pubkey = self.Pk;
					return true;
				case HD self:
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
						var extkey = new BitcoinExtPubKey(self.Extkey.ToString(), self.Extkey.Network);
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
			Origin self => self.Inner.IsRange(),
			Const _ => false,
			HD self => self.Derive != DeriveType.NO,
			_ => throw new Exception("Unreachable!"),
		};
		public bool IsCompressed() => (this) switch
		{
			Origin self =>
				self.Inner.IsCompressed(),
			Const self =>
				self.Pk.IsCompressed,
			HD _ =>
				false,
			_ => throw new Exception("Unreachable!"),
		};

		public override string ToString() => (this) switch
		{
			Origin self =>
				$"[{self.KeyOriginInfo.ToStringWithEmptyKeyPathAware()}]{self.Inner}",
			Const self =>
				self.Pk.ToHex(),
			HD self =>
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
				case Origin self:
					if (!self.Inner.TryGetPrivateString(secretProvider, out ret))
						return false;
					ret = $"[{self.KeyOriginInfo.ToStringWithEmptyKeyPathAware()}]{ret}";
					return true;
				case Const self:
					if (!secretProvider.TryGetSecret(self.Pk.Hash, out var secretConst))
						return false;
					ret = secretConst.ToString();
					return true;
				case HD self:
					if (!secretProvider.TryGetSecret(self.Extkey.ExtPubKey.PubKey.Hash, out var secretHD))
						return false;
					ret = $"{secretHD}{self.GetPathString()}";
					return true;
			}
			throw new Exception("Unreachable!");
		}

		public sealed override bool Equals(object obj)
			=> Equals(obj as PubKeyProvider);


		public bool Equals(PubKeyProvider other) => other != null && (this) switch
		{
			Const self =>
				other is Const o &&
				self.Pk.Equals(o.Pk),
			HD self =>
				other is HD o &&
				self.Derive == o.Derive &&
				self.Path.Equals(o.Path) &&
				self.Extkey.Equals(o.Extkey),
			Origin self =>
				other is Origin o &&
				self.KeyOriginInfo.Equals(o.KeyOriginInfo) &&
				self.Inner.Equals(o.Inner),
			_ =>
				throw new Exception("Unreachable"),
		};

		public override int GetHashCode()
		{
			int num;
			switch (this)
			{
				case Const self:
					{
						num = 0;
						return -1640531527 + self.Pk.GetHashCode() + ((num << 6) + (num >> 2));
					}
				case HD self:
					{
						num = 1;
						num = -1640531527 + self.Extkey.GetHashCode() + ((num << 6) + (num >> 2));
						num = -1640531527 + self.Path.GetHashCode() + ((num << 6) + (num >> 2));
						return -1640531527 + self.Derive.GetHashCode() + ((num << 6) + (num >> 2));
					}
				case Origin self:
					{
						num = 2;
						num = -1640531527 + self.Inner.GetHashCode() + ((num << 6) + (num >> 2));
						return -1640531527 + self.KeyOriginInfo.GetHashCode() + ((num << 6) + (num >> 2));
					}
			}
			return 0;
		}
	}
}
