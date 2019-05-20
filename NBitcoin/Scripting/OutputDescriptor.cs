using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NBitcoin.Scripting.Parser;

namespace NBitcoin.Scripting
{

	public abstract class OutputDescriptor : IEquatable<OutputDescriptor>
	{
		# region subtypes
		public static class Tags
		{
			public const int AddressDescriptor = 0;
			public const int RawDescriptor = 1;
			public const int PKDescriptor = 2;
			public const int ExtPubKeyOutputDescriptor = 3;
			public const int PKHDescriptor = 4;
			public const int WPKHDescriptor = 5;
			public const int ComboDescriptor = 6;
			public const int MultisigDescriptor = 7;
			public const int SHDescriptor = 8;
			public const int WSHDescriptor = 9;
		}

		public class AddressDescriptor : OutputDescriptor
		{
			public BitcoinAddress Address { get; }
			public AddressDescriptor(BitcoinAddress address) : base(Tags.AddressDescriptor)
			{
				if (address == null)
					throw new ArgumentNullException(nameof(address));
				Address = address;
			}
		}

		public class RawDescriptor : OutputDescriptor
		{
			public Script Script;

			internal RawDescriptor(Script script) : base(Tags.RawDescriptor) => Script = script ?? throw new ArgumentNullException(nameof(script));
		}

		public class PKDescriptor : OutputDescriptor
		{
			public PubKeyProvider PkProvider;
			internal PKDescriptor(PubKeyProvider pkProvider) : base(Tags.PKDescriptor)
			{
				if (pkProvider == null)
					throw new ArgumentNullException(nameof(pkProvider));
				PkProvider = pkProvider;
			}
		}

		public class PKHDescriptor : OutputDescriptor
		{
			public PubKeyProvider PkProvider;
			internal PKHDescriptor(PubKeyProvider pkProvider) : base(Tags.PKHDescriptor)
			{
				if (pkProvider == null)
					throw new ArgumentNullException(nameof(pkProvider));
				PkProvider = pkProvider;
			}
		}

		public class WPKHDescriptor : OutputDescriptor
		{
			public PubKeyProvider PkProvider;
			internal WPKHDescriptor(PubKeyProvider pkProvider) : base(Tags.WPKHDescriptor)
			{
				if (pkProvider == null)
					throw new ArgumentNullException(nameof(pkProvider));
				PkProvider = pkProvider;
			}
		}
		public class ComboDescriptor : OutputDescriptor
		{
			public PubKeyProvider PkProvider;
			internal ComboDescriptor(PubKeyProvider pkProvider) : base(Tags.ComboDescriptor)
			{
				if (pkProvider == null)
					throw new ArgumentNullException(nameof(pkProvider));
				PkProvider = pkProvider;
			}
		}

		public class MultisigDescriptor : OutputDescriptor
		{
			public List<PubKeyProvider> PkProviders;
			internal MultisigDescriptor(uint threshold, IEnumerable<PubKeyProvider> pkProviders) : base(Tags.MultisigDescriptor)
			{
				if (pkProviders == null)
					throw new ArgumentNullException(nameof(pkProviders));
				PkProviders = pkProviders.ToList();
				if (PkProviders.Count == 0)
					throw new ArgumentException("Multisig Descriptor can not have empty pubkey providers");
				Threshold = threshold;
			}

			public uint Threshold { get; }
		}

		public class SHDescriptor : OutputDescriptor
		{
			public OutputDescriptor Inner;
			internal SHDescriptor(OutputDescriptor inner) : base(Tags.SHDescriptor)
			{
				if (inner == null)
					throw new ArgumentNullException(nameof(inner));
				if (inner.IsTopLevelOnly())
					throw new ArgumentException($"{inner} can not be inner element for SHDescriptor");
				Inner = inner;
			}
		}

		public class WSHDescriptor : OutputDescriptor
		{
			public OutputDescriptor Inner;
			internal WSHDescriptor(OutputDescriptor inner) : base(Tags.WSHDescriptor)
			{
				if (inner == null)
					throw new ArgumentNullException(nameof(inner));
				if (inner.IsTopLevelOnly() || inner.IsWSH())
					throw new ArgumentException($"{inner} can not be inner element for WSHDescriptor");
				Inner = inner;
			}
		}

		internal int Tag { get; }
		private OutputDescriptor(int tag)
		{
			Tag = tag;
		}

		public static OutputDescriptor NewAddr(BitcoinAddress addr) => new AddressDescriptor(addr);
		public static OutputDescriptor NewRaw(Script sc) => new RawDescriptor(sc);
		public static OutputDescriptor NewPK(PubKeyProvider pk) => new PKDescriptor(pk);
		public static OutputDescriptor NewPKH(PubKeyProvider pk) => new PKHDescriptor(pk);
		public static OutputDescriptor NewWPKH(PubKeyProvider pk) => new WPKHDescriptor(pk);
		public static OutputDescriptor NewCombo(PubKeyProvider pk) => new ComboDescriptor(pk);
		public static OutputDescriptor NewMulti(uint m, IEnumerable<PubKeyProvider> pks) => new MultisigDescriptor(m, pks);

		public static OutputDescriptor NewSH(OutputDescriptor inner) => new SHDescriptor(inner);
		public static OutputDescriptor NewWSH(OutputDescriptor inner) => new WSHDescriptor(inner);

		public bool IsAddr() => Tag == Tags.AddressDescriptor;
		public bool IsRaw() => Tag == Tags.RawDescriptor;
		public bool IsPK() => Tag == Tags.PKDescriptor;
		public bool IsPKH() => Tag == Tags.PKHDescriptor;
		public bool IsWPKH() => Tag == Tags.WPKHDescriptor;
		public bool IsCombo() => Tag == Tags.ComboDescriptor;
		public bool IsMulti() => Tag == Tags.MultisigDescriptor;
		public bool IsSH() => Tag == Tags.SHDescriptor;
		public bool IsWSH() => Tag == Tags.WSHDescriptor;

		public bool IsTopLevelOnly() =>
			IsAddr() || IsRaw() || IsCombo() || IsSH();

		#endregion

		#region Descriptor specific things
		public bool TryExpand(
			uint pos,
			Func<KeyId, ISecret> secretProvider,
			ISigningRepository signingProvider,
			out List<Script> outputScripts
			)
		{
			outputScripts = new List<Script>();
			return TryExpand(pos, secretProvider, signingProvider, outputScripts);
		}
		private bool TryExpand(
			uint pos,
			Func<KeyId, ISecret> secretProvider,
			ISigningRepository signingProvider,
			List<Script> outputScripts
			)
		{
			switch (this)
			{
				case AddressDescriptor self:
					break;
				case RawDescriptor self:
					break;
				case PKDescriptor self:
					if (!self.PkProvider.TryGetPubKey(pos, out var keyOrigin1, out var pubkey1))
						return false;
					outputScripts.AddRange(MakeScripts(pubkey1));
					return true;
				case PKHDescriptor self:
					if (!self.PkProvider.TryGetPubKey(pos, out var keyOrigin2, out var pubkey2))
						return false;
					outputScripts.AddRange(MakeScripts(pubkey2));
					return true;
				case WPKHDescriptor self:
					if (!self.PkProvider.TryGetPubKey(pos, out var keyOrigin3, out var pubkey3))
						return false;
					outputScripts.AddRange(MakeScripts(pubkey3));
					return true;
				case ComboDescriptor self:
					if (!self.PkProvider.TryGetPubKey(pos, out var keyOrigin4, out var pubkey4))
						return false;
					outputScripts.AddRange(MakeScripts(pubkey4));
					return true;
				case MultisigDescriptor self:
					var multiSigPKs = new List<PubKey>();
					foreach (var pkProvider in self.PkProviders)
					{
						if (!pkProvider.TryGetPubKey(pos, out var keyOrigin5, out var pubkey5))
							return false;
						multiSigPKs.Add(pubkey5);
					}
					outputScripts.AddRange(multiSigPKs.SelectMany(pk => MakeScripts(pk)));
					return true;
				case SHDescriptor self:
					if (!self.Inner.TryExpand(pos, secretProvider, signingProvider, out var shInnerResult))
						return false;
					outputScripts.AddRange(shInnerResult.Select(innerSc => innerSc.Hash.ScriptPubKey));
					return true;
				case WSHDescriptor self:
					if (!self.Inner.TryExpand(pos, secretProvider, signingProvider, out var wshInnerResult))
						return false;
					outputScripts.AddRange(wshInnerResult.Select(innerSc => innerSc.Hash.ScriptPubKey));
					return true;
			}
			throw new Exception("Unreachable");
		}

		private List<Script> MakeScripts(PubKey key)
		{
			switch (this)
			{
				case AddressDescriptor self:
					return new List<Script>() { self.Address.ScriptPubKey };
				case RawDescriptor self:
					return new List<Script>() { self.Script };
				case PKDescriptor self:
					return new List<Script>() { key.ScriptPubKey };
				case PKHDescriptor self:
					return new List<Script>() { key.Hash.ScriptPubKey };
				case WPKHDescriptor self:
					return new List<Script>() { key.WitHash.ScriptPubKey };
				case ComboDescriptor self:
					return new List<Script>()
					{
						key.ScriptPubKey,
						key.Hash.ScriptPubKey,
						key.WitHash.ScriptPubKey,
						key.WitHash.ScriptPubKey.Hash.ScriptPubKey
					};
			}

			throw new Exception("Unreachable");
		}

		public bool IsSolvable()
		{
			switch (this.Tag)
			{
				case Tags.AddressDescriptor:
				case Tags.RawDescriptor:
					return false;
				case Tags.SHDescriptor:
					return ((SHDescriptor)this).Inner.IsSolvable();
				case Tags.WSHDescriptor:
					return ((WSHDescriptor)this).Inner.IsSolvable();
				default:
					return true;
			}
		}

		public bool IsRange()
		{
			switch (this)
			{
				case AddressDescriptor _:
					return false;
				case RawDescriptor _:
					return false;
				case PKDescriptor self:
					return self.PkProvider.IsRange();
				case PKHDescriptor self:
					return self.PkProvider.IsRange();
				case WPKHDescriptor self:
					return self.PkProvider.IsRange();
				case ComboDescriptor self:
					return self.PkProvider.IsRange();
				case MultisigDescriptor self:
					return self.PkProviders.Any(pk => pk.IsRange());
				case SHDescriptor self:
					return self.Inner.IsRange();
				case WSHDescriptor self:
					return self.Inner.IsRange();
			}
			throw new Exception("Unreachable");
		}

		#endregion

		# region string (De)serializer

		public override string ToString()
		{
			var inner = ToStringHelper();
			return $"{inner}#{OutputDescriptor.GetCheckSum(inner)}";
		}

		public bool TryGetPrivateString(ISigningRepository secretProvider, out string result)
		{
			result = null;
			if (!TryGetPrivateStringHelper(secretProvider, out var inner))
				return false;
			result = $"{inner}#{OutputDescriptor.GetCheckSum(inner)}";
			return true;
		}

		private bool TryGetPrivateStringHelper(ISigningRepository secretProvider, out string result)
		{
			result = null;
			switch (this.Tag)
			{
				case Tags.AddressDescriptor:
				case Tags.RawDescriptor:
					result = this.ToStringHelper();
					return true;
				case Tags.PKDescriptor:
					if (!((PKDescriptor)this).PkProvider.TryGetPrivateString(secretProvider, out var privStr1))
						return false;
					result = $"pk({privStr1})";
					return true;
				case Tags.PKHDescriptor:
					if (!((PKHDescriptor)this).PkProvider.TryGetPrivateString(secretProvider, out var privStr2))
						return false;
					result = $"pkh({privStr2})";
					return true;
				case Tags.WPKHDescriptor:
					if (!((WPKHDescriptor)this).PkProvider.TryGetPrivateString(secretProvider, out var privStr3))
						return false;
					result = $"wpkh({privStr3})";
					return true;
				case Tags.ComboDescriptor:
					if (!((ComboDescriptor)this).PkProvider.TryGetPrivateString(secretProvider, out var privStr4))
						return false;
					result = $"combo({privStr4})";
					return true;
				case Tags.MultisigDescriptor:
					var subKeyList = new List<string>();
					var multi = (MultisigDescriptor)this;
					foreach (var prov in (multi.PkProviders))
					{
						if (!prov.TryGetPrivateString(secretProvider, out var tmp))
							return false;
						subKeyList.Add(tmp);
					}
					result = $"multi({multi.Threshold},{String.Join(",", subKeyList)})";
					return true;
				case Tags.SHDescriptor:
					if (!((SHDescriptor)this).Inner.TryGetPrivateStringHelper(secretProvider, out var shInner))
						return false;
					result = $"sh({shInner})";
					return true;
				case Tags.WSHDescriptor:
					if (!((WSHDescriptor)this).Inner.TryGetPrivateStringHelper(secretProvider, out var wshInner))
						return false;
					result = $"wsh({wshInner})";
					return true;
			}
			throw new Exception("Unreachable");
		}

		private string ToStringHelper()
		{
			switch (this)
			{
				case AddressDescriptor self:
					return $"addr({self.Address})";
				case RawDescriptor self:
					return $"raw({self.Script})";
				case PKDescriptor self:
					return $"pk({self.PkProvider})";
				case PKHDescriptor self:
					return $"pkh({self.PkProvider})";
				case WPKHDescriptor self:
					return $"wpkh({self.PkProvider})";
				case ComboDescriptor self:
					return $"combo({self.PkProvider})";
				case MultisigDescriptor self:
					var pksStr = String.Join(",", self.PkProviders);
					return $"multi({self.Threshold},{pksStr})";
				case SHDescriptor self:
					return $"sh({self.Inner.ToStringHelper()})";
				case WSHDescriptor self:
					return $"wsh({self.Inner.ToStringHelper()})";
			}
			throw new Exception("unreachable");
		}
		public static OutputDescriptor Parse(string desc, bool requireCheckSum = false, ISigningRepository repo = null)
			=> OutputDescriptorParser.ParseOD(desc, requireCheckSum, repo);

		public static bool TryParse(string desc, out OutputDescriptor result, bool requireCheckSum = false, ISigningRepository repo = null)
		{
			if (!OutputDescriptorParser.TryParseOD(desc, out result, requireCheckSum, repo))
				return false;
			return true;
		}

		#endregion

		#region Equatable

		public sealed override bool Equals(object obj)
			=> Equals(obj as OutputDescriptor);

		public bool Equals(OutputDescriptor other)
		{
			if (other == null || this.Tag != other.Tag)
				return false;

			switch (this)
			{
				case AddressDescriptor self:
					return self.Address.Equals(((AddressDescriptor)other).Address);
				case RawDescriptor self:
					return self.Script.Equals(((RawDescriptor)other).Script);
				case PKDescriptor self:
					return self.PkProvider.Equals(((PKDescriptor)other).PkProvider);
				case PKHDescriptor self:
					return self.PkProvider.Equals(((PKHDescriptor)other).PkProvider);
				case WPKHDescriptor self:
					return self.PkProvider.Equals(((WPKHDescriptor)other).PkProvider);
				case ComboDescriptor self:
					return self.PkProvider.Equals(((ComboDescriptor)other).PkProvider);
				case MultisigDescriptor self:
					var otherM = (MultisigDescriptor)other;
					return self.Threshold == otherM.Threshold && self.PkProviders.SequenceEqual(otherM.PkProviders);
				case SHDescriptor self:
					return self.Inner.Equals(((SHDescriptor)other).Inner);
				case WSHDescriptor self:
					return self.Inner.Equals(((WSHDescriptor)other).Inner);
			}
			throw new Exception("Unreachable!");
		}

		public override int GetHashCode()
		{
			if (this != null)
			{
				int num = 0;
				switch (this)
				{
					case AddressDescriptor self:
						{
							num = 0;
							return -1640531527 + self.Address.GetHashCode() + ((num << 6) + (num >> 2));
						}
					case RawDescriptor self:
						{
							num = 1;
							return -1640531527 + self.Script.GetHashCode() + ((num << 6) + (num >> 2));
						}
					case PKDescriptor self:
						{
							num = 2;
							return -1640531527 + self.PkProvider.GetHashCode() + ((num << 6) + (num >> 2));
						}
					case PKHDescriptor self:
						{
							num = 3;
							return -1640531527 + self.PkProvider.GetHashCode() + ((num << 6) + (num >> 2));
						}
					case WPKHDescriptor self:
						{
							num = 4;
							return -1640531527 + self.PkProvider.GetHashCode() + ((num << 6) + (num >> 2));
						}
					case ComboDescriptor self:
						{
							num = 5;
							return -1640531527 + self.PkProvider.GetHashCode() + ((num << 6) + (num >> 2));
						}
					case MultisigDescriptor self:
						{
							num = 6;
							return -1640531527 + self.PkProviders.GetHashCode() + ((num << 6) + (num >> 2));
						}
					case SHDescriptor self:
						{
							num = 7;
							return -1640531527 + self.Inner.GetHashCode() + ((num << 6) + (num >> 2));
						}
					case WSHDescriptor self:
						{
							num = 8;
							return -1640531527 + self.Inner.GetHashCode() + ((num << 6) + (num >> 2));
						}
				}
			}
			return 0;
		}

		#endregion

		#region checksum
		/** The character set for the checksum itself (same as bech32). */
		static readonly char[] CHECKSUM_CHARSET = "qpzry9x8gf2tvdw0s3jn54khce6mua7l".ToArray();
		static readonly string INPUT_CHARSET_STRING =
		"0123456789()[],'/*abcdefgh@:$%{}" +
        "IJKLMNOPQRSTUVWXYZ&+-.;<=>?!^_|~" +
        "ijklmnopqrstuvwxyzABCDEFGH`#\"\\ ";

		static readonly char[] INPUT_CHARSET = INPUT_CHARSET_STRING.ToArray();

		internal static string GetCheckSum(string desc)
		{
			ulong c = 1;
			int cls = 0;
			int clscount = 0;
			foreach(var ch in desc.ToCharArray())
			{
				var pos = INPUT_CHARSET_STRING.IndexOf(ch);
				if (pos == -1)
					return "";
				c = PolyMod(c, pos & 31);
				cls = cls * 3 + (pos >> 5);
				if (clscount++ == 3)
				{
					c = PolyMod(c, cls);
					cls = 0;
					clscount = 0;
				}
			}
			if (clscount > 0) c = PolyMod(c, cls);
			for (int j = 0; j < 8; j++) c = PolyMod(c, 0);
			c ^= 1;
			var result = new char[8];
			for (int j = 0; j < 8; j++)
			{
				result[j] = CHECKSUM_CHARSET[(c >> (5 * (7 - j))) & 31];
			}
			return new String(result);
		}
		static ulong PolyMod(ulong c, int val)
		{
			ulong c0 = c >> 35;
			c = ((c & 0x7ffffffffUL) << 5) ^ (ulong)val;
			if ((c0 & 1UL) != 0) c ^= 0xf5dee51989;
			if ((c0 & 2UL) != 0) c ^= 0xa9fdca3312;
			if ((c0 & 4UL) != 0) c ^= 0x1bab10e32d;
			if ((c0 & 8) != 0) c ^= 0x3706b1677a;
			if ((c0 & 16) != 0) c ^= 0x644d626ffd;
			return c;
		}

		#endregion
	}
}