using System;
using NBitcoin.Scripting.Parser;
using static NBitcoin.Scripting.MiniscriptDSLParser;
using static NBitcoin.Scripting.PubKeyProvider;
using static NBitcoin.Scripting.ParserUtil;
using System.Linq;
using P = NBitcoin.Scripting.Parser.Parser<char, NBitcoin.Scripting.OutputDescriptor>;

namespace NBitcoin.Scripting
{
	internal static class OutputDescriptorParser
	{
		#region Utils
		private static Parser<char, T> InjectRepository<T>(this Parser<char, T> subParser, ISigningRepository repo)
		{
			return i =>
			{
				var r = subParser(i);
				if (r.IsSuccess && repo != null)
				{
					if (r.Value is BitcoinExtKey extkeyV)
					{
						repo.SetSecret(extkeyV.GetPublicKey().Hash, extkeyV);
					}
					if (r.Value is BitcoinExtPubKey extPubKeyV)
					{
						repo.SetPubKey(extPubKeyV.GetPublicKey().Hash, extPubKeyV.GetPublicKey());
					}
					if (r.Value is PubKey pkV)
					{
						repo.SetPubKey(pkV.Hash, pkV);
					}
					if (r.Value is BitcoinSecret secretV)
					{
						repo.SetSecret(secretV.PubKey.Hash, secretV);
					}
					if (r.Value is Script scV)
					{
						repo.SetScript(scV.Hash, scV);
					}
					if (r.Value is OriginPubKeyProvider pkpV)
					{
						if (pkpV.Inner is HDPubKeyProvider hdPkPV)
						{
							repo.SetKeyOrigin(hdPkPV.Extkey.GetPublicKey().Hash, pkpV.KeyOriginInfo);
						}
						if (pkpV.Inner is ConstPubKeyProvider constPkPV)
						{
							repo.SetKeyOrigin(constPkPV.Pk.Hash, pkpV.KeyOriginInfo);
						}
					}
				}
				return r;
			};
		}
		#endregion

		#region pubkey
		private static readonly Parser<char, PubKey> PPubKeyCompressed =
			from x in Parse.Hex.Repeat(66).Text().Then(s => Parse.TryConvert(s, c => new PubKey(c)))
			select x;

		private static readonly Parser<char, PubKey> PPubKeyUncompressed =
			from x in Parse.Hex.Repeat(130).Text().Then(s => Parse.TryConvert(s, c => new PubKey(c)))
			select x;

		private static Parser<char, PubKey> PPubKey(ISigningRepository repo) =>
			(PPubKeyCompressed.Or(PPubKeyUncompressed)).InjectRepository(repo);

		private static Parser<char, BitcoinExtPubKey> PRawXPub(ISigningRepository repo) =>
			(from magic in Parse.String("xpub").Or(Parse.String("tpub")).Text()
			 from base58Str in Parse.Base58.XMany().Text()

			 from x in Parse.TryConvert((magic + base58Str), c => new BitcoinExtPubKey(c))
			 select x).InjectRepository(repo);
		private static Parser<char, RootedKeyPath> PRootedKeyPath(ISigningRepository repo) =>
			(from _l in Parse.Char('[').Token()
			 from x in (Parse.CharExcept(']').XMany().Text().Token())
				 .Then(inner => Parse.TryConvert(inner, RootedKeyPath.Parse))
			 from _r in Parse.Char(']').Token()
			 select x).InjectRepository(repo);

		private static Parser<char, KeyPath> PKeyPath(ISigningRepository repo) =>
			(from x in (Parse.Digit.Or(Parse.Chars("/\'h")).XMany()).Text()
				.Then(s => Parse.TryConvert(s, KeyPath.Parse))
			 select x).InjectRepository(repo);

		#endregion

		#region Private keys

		private static Parser<char, PubKey> PWIF(ISigningRepository repo) =>
			(from x in Parse.Base58.XMany().Text().Then(s => Parse.TryConvert(s, c => new BitcoinSecret(s)))
			 select x).InjectRepository(repo).Select(secret => secret.PubKey);
		private static Parser<char, BitcoinExtPubKey> PExtKey(ISigningRepository repo) =>
			// why not just read base58 string first? A. Because failing fast improves speed.
			(from magic in Parse.String("xprv").Or(Parse.String("tprv")).Text()
			 from base58Str in Parse.Base58.XMany().Text()
			 from x in Parse.TryConvert(magic + base58Str, c => new BitcoinExtKey(c))
			 select x).InjectRepository(repo).Select(extKey => extKey.Neuter());

		#endregion

		#region PubKeyProvider

		private static Parser<char, PubKeyProvider> PConstPubKeyProvider(ISigningRepository repo) =>
			from pk in PPubKey(repo).Or(PWIF(repo))
			select PubKeyProvider.NewConst(pk);

		private static Parser<char, Tuple<BitcoinExtPubKey, KeyPath>> PHDPkProviderNoPath(ISigningRepository repo) =>
			from extKey in PRawXPub(repo).Or(PExtKey(repo))
			select Tuple.Create(extKey, KeyPath.Empty);
		private static Parser<char, Tuple<BitcoinExtPubKey, KeyPath>> PHDPkProviderWithpath(ISigningRepository repo) =>
			from extKey in PRawXPub(repo).Or(PExtKey(repo))
			from keyPath in PKeyPath(repo)
			select Tuple.Create(extKey, keyPath);
		private static Parser<char, Tuple<BitcoinExtPubKey, KeyPath>> PHDPkProviderCommon(ISigningRepository repo) =>
			PHDPkProviderWithpath(repo).Or(PHDPkProviderNoPath(repo));
		private static Parser<char, PubKeyProvider> PHardendedHDPubKeyProvider(ISigningRepository repo) =>
			from items in PHDPkProviderCommon(repo)
			from prefix in Parse.Char('*')
			from isHardened in Parse.Char('\'').Or(Parse.Char('h'))
			select PubKeyProvider.NewHD(items.Item1, items.Item2, DeriveType.HARDENED);

		private static Parser<char, PubKeyProvider> PUnHardendedHDPubKeyProvider(ISigningRepository repo) =>
			from items in PHDPkProviderCommon(repo)
			from prefix in Parse.Char('*')
			select PubKeyProvider.NewHD(items.Item1, items.Item2, DeriveType.UNHARDENED);

		private static Parser<char, PubKeyProvider> PStaticHDPubKeyProvider(ISigningRepository repo) =>
			from items in PHDPkProviderCommon(repo)
			select PubKeyProvider.NewHD(items.Item1, items.Item2, DeriveType.NO);

		private static Parser<char, PubKeyProvider> PHDPubKeyProvider(ISigningRepository repo) =>
			// The order is important here.
			PHardendedHDPubKeyProvider(repo)
				.Or(PUnHardendedHDPubKeyProvider(repo))
				.Or(PStaticHDPubKeyProvider(repo));
		private static Parser<char, PubKeyProvider> POriginPubkeyProvider(ISigningRepository repo) =>
			from rootedKeyPath in PRootedKeyPath(repo)
			from inner in PConstPubKeyProvider(repo).Or(PHDPubKeyProvider(repo))
			select PubKeyProvider.NewOrigin(rootedKeyPath, inner);

		internal static Parser<char, PubKeyProvider> PPubKeyProvider(ISigningRepository repo) =>
			PConstPubKeyProvider(repo).Or(PHDPubKeyProvider(repo)).Or(POriginPubkeyProvider(repo));
		#endregion


		private static Parser<char, BitcoinAddress> PTryConvertAddr(string addrStr) =>
			Parse.TryConvert(addrStr, i => BitcoinAddress.Create(i, Network.Main))
				.Or(Parse.TryConvert(addrStr, i => BitcoinAddress.Create(i, Network.TestNet)))
				.Or(Parse.TryConvert(addrStr, i => BitcoinAddress.Create(i, Network.RegTest)));
		internal static readonly P PAddr =
			from _n in Parse.String("addr")
			from inner in SurroundedByBrackets
			from addr in PTryConvertAddr(inner)
			select OutputDescriptor.NewAddr(addr);

		internal static P PRaw(ISigningRepository repo)
		{
			var PScript =
				(
					from _n in Parse.String("raw")
					from inner in SurroundedByBrackets
					from sc in Parse.TryConvert(inner, str => new Script(str))
					select sc
				).InjectRepository(repo);
			return PScript.Select(s => OutputDescriptor.NewRaw(s));
		}

	private static P PExprHelper<T>(
			string name,
			Parser<char, T> pInner,
			Func<T, OutputDescriptor> constructor
			) =>
			from _n in Parse.String(name)
			from _l in Parse.Char('(')
			from item in pInner
			from _r in Parse.Char(')')
			select constructor(item);

		private static P PPKHelper(string name, Func<PubKeyProvider, OutputDescriptor> constructor, ISigningRepository repo) =>
			PExprHelper<PubKeyProvider>(name, PPubKeyProvider(repo), constructor);

		internal static P PPK(ISigningRepository repo) =>
			PPKHelper("pk", OutputDescriptor.NewPK, repo);

		internal static P PPKH(ISigningRepository repo) =>
			PPKHelper("pkh", OutputDescriptor.NewPKH, repo);

		internal static P PWPKH(ISigningRepository repo) =>
			PPKHelper("wpkh", OutputDescriptor.NewWPKH, repo);

		internal static P PCombo(ISigningRepository repo) =>
			PPKHelper("combo", OutputDescriptor.NewCombo, repo);

		internal static P PMulti(ISigningRepository repo) =>
			from _name in Parse.String("multi")
			from _l in Parse.Char('(')
			from m in Parse.Digit.XMany().Text().Then(d => Parse.TryConvert(d, UInt32.Parse))
			from _c in Parse.Char(',')
			from pkProviders in PPubKeyProvider(repo).DelimitedBy(Parse.Char(',').Token())
			from _r in Parse.Char(')')
			select OutputDescriptor.NewMulti(m, pkProviders);


		internal static P PInner(ISigningRepository repo) =>
			PPK(repo).Or(PPKH(repo)).Or(PWPKH(repo)).Or(PMulti(repo));

		internal static P PWSH(ISigningRepository repo) =>
			PExprHelper("wsh", PInner(repo), OutputDescriptor.NewWSH);
		internal static P PSH(ISigningRepository repo) =>
			PExprHelper("sh", PInner(repo).Or(PWSH(repo)), OutputDescriptor.NewSH);

		internal static P POutputDescriptor(ISigningRepository repo) =>
			PAddr
				.Or(PRaw(repo))
				.Or(PCombo(repo))
				.Or(PInner(repo))
				.Or(PWSH(repo))
				.Or(PSH(repo)).End();

		internal static bool TryParseOD(string str, out OutputDescriptor result, bool requireCheckSum = false)
			=> TryParseOD(str, out var _, out result,requireCheckSum);
		private static bool TryParseOD(string str, out string whyFailure,  out OutputDescriptor result, bool requireCheckSum = false, ISigningRepository repo = null)
		{
			result = null;
			whyFailure = null;
			var checkSplit = str.Split('#');
			if (checkSplit.Length > 2)
			{
				whyFailure = "Multiple '#'s Symbols";
				return false;
			}
			if (checkSplit.Length == 1 && requireCheckSum)
			{
				whyFailure = "Missing checksum";
				return false;
			}
			if (checkSplit.Length == 2)
			{
				str = checkSplit[0];
				if (checkSplit[1].Length != 8)
				{
					whyFailure = "Invalid length of Checksum";
					return false;
				}
				var realCheckSum = OutputDescriptor.GetCheckSum(str);
				if (realCheckSum != checkSplit[1])
				{
					whyFailure = "CheckSum mismatch";
					return false;
				}
			}

			var res = POutputDescriptor(repo).TryParse(str);
			if (!res.IsSuccess)
			{
				whyFailure = res.Description;
				return false;
			}
			result = res.Value;
			return true;
		}
		internal static OutputDescriptor ParseOD(string str, bool requireCheckSum = false)
		{
			if (!TryParseOD(str, out var whyFailure, out var result, requireCheckSum))
				throw new ParseException(whyFailure);
			return result;
		}

	}
}