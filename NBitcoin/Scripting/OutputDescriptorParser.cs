using System;
using NBitcoin.Scripting.Parser;
using static NBitcoin.Scripting.PubKeyProvider;
using static NBitcoin.Scripting.ParserUtil;
using System.Linq;
using P = NBitcoin.Scripting.Parser.Parser<char, NBitcoin.Scripting.OutputDescriptor>;

#nullable enable

namespace NBitcoin.Scripting
{
	internal static class OutputDescriptorParser
	{
		#region Utils
		private static Parser<char, T> InjectRepository<T>(this Parser<char, T> subParser, ISigningRepository? repo)
		{
			return (i, n) =>
			{
				var r = subParser(i, n);
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
					if (r.Value is Origin pkpV)
					{
						if (pkpV.Inner is HD hdPkPV)
						{
							repo.SetKeyOrigin(hdPkPV.Extkey.GetPublicKey().Hash, pkpV.KeyOriginInfo);
						}
						if (pkpV.Inner is Const constPkPV)
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
		private static Parser<char, PubKey> PPubKeyCompressed(ISigningRepository? repo) =>
			(from x in Parse.Hex.Repeat(66).Text().Then(s => Parse.TryConvert(s, c => new PubKey(c)))
			select x).InjectRepository(repo);

		private static readonly Parser<char, PubKey> PPubKeyUncompressed =
			from x in Parse.Hex.Repeat(130).Text().Then(s => Parse.TryConvert(s, c => new PubKey(c)))
			select x;

		private static Parser<char, PubKey> PPubKey(ISigningRepository? repo) =>
			(PPubKeyCompressed(repo).Or(PPubKeyUncompressed)).InjectRepository(repo);

		private static Parser<char, BitcoinExtPubKey> PRawXPub(ISigningRepository? repo, Network n) =>
			(from base58Str in Parse.Base58.XMany().Text()
			 from x in Parse.TryConvert(base58Str, c => new BitcoinExtPubKey(c, n))
			 select x).InjectRepository(repo);
		private static Parser<char, RootedKeyPath> PRootedKeyPath(ISigningRepository? repo) =>
			(from _l in Parse.Char('[')
			 from x in (Parse.CharExcept(']').XMany().Text())
				 .Then(inner => Parse.TryConvert(inner, RootedKeyPath.Parse))
			 from _r in Parse.Char(']')
			 select x).InjectRepository(repo);

		private static Parser<char, KeyPath> PKeyPath(ISigningRepository? repo) =>
			(from x in (Parse.Digit.Or(Parse.Chars("/\'h")).XMany()).Text()
				.Then(s => Parse.TryConvert(s, KeyPath.Parse))
			 select x).InjectRepository(repo);

		#endregion

		#region Private keys

		private static Parser<char, BitcoinSecret> PTryConvertSecret(string secretStr, Network n) =>
			Parse.TryConvert(secretStr, i => new BitcoinSecret(i, n));
		private static Parser<char, PubKey> PWIF(ISigningRepository? repo, Network n, bool onlyCompressed) =>
			(from xStr in Parse.Base58.XMany().Text()
			 from x in PTryConvertSecret(xStr, n)
			 where !onlyCompressed || x.PubKey.IsCompressed
			 select x).InjectRepository(repo)
			 	.Select(secret => secret.PubKey);
		private static Parser<char, BitcoinExtPubKey> PExtKey(ISigningRepository? repo, Network n) =>
			// why not just read base58 string first? A. Because failing fast improves speed.
			(from base58Str in Parse.Base58.XMany().Text()
			 from x in Parse.TryConvert(base58Str, c => new BitcoinExtKey(c, n))
			 select x).InjectRepository(repo).Select(extKey => extKey.Neuter());

		#endregion

		#region PubKeyProvider

		private static Parser<char, PubKeyProvider> PConstPubKeyProvider(ISigningRepository? repo, bool onlyCompressed, Network n)
		{
			if (onlyCompressed)
			{
				return from pk in PPubKeyCompressed(repo).Or(PWIF(repo, n, onlyCompressed))
					   select PubKeyProvider.NewConst(pk);
			}
			return from pk in PPubKey(repo).Or(PWIF(repo, n, false))
				select PubKeyProvider.NewConst(pk);
		}

		private static Parser<char, Tuple<BitcoinExtPubKey, KeyPath>> PHDPkProviderNoPath(ISigningRepository? repo, Network n) =>
			from extKey in PRawXPub(repo, n).Or(PExtKey(repo, n))
			select Tuple.Create(extKey, KeyPath.Empty);
		private static Parser<char, Tuple<BitcoinExtPubKey, KeyPath>> PHDPkProviderWithpath(ISigningRepository? repo, Network n) =>
			from extKey in PRawXPub(repo, n).Or(PExtKey(repo, n))
			from keyPath in PKeyPath(repo)
			select Tuple.Create(extKey, keyPath);
		private static Parser<char, Tuple<BitcoinExtPubKey, KeyPath>> PHDPkProviderCommon(ISigningRepository? repo, Network n) =>
			PHDPkProviderWithpath(repo, n).Or(PHDPkProviderNoPath(repo, n));
		private static Parser<char, PubKeyProvider> PHardendedHDPubKeyProvider(ISigningRepository? repo, Network n) =>
			from items in PHDPkProviderCommon(repo, n)
			from prefix in Parse.Char('*')
			from isHardened in Parse.Char('\'').Or(Parse.Char('h'))
			select PubKeyProvider.NewHD(items.Item1, items.Item2, DeriveType.HARDENED);

		private static Parser<char, PubKeyProvider> PUnHardendedHDPubKeyProvider(ISigningRepository? repo, Network n) =>
			from items in PHDPkProviderCommon(repo, n)
			from prefix in Parse.Char('*')
			select PubKeyProvider.NewHD(items.Item1, items.Item2, DeriveType.UNHARDENED);

		private static Parser<char, PubKeyProvider> PStaticHDPubKeyProvider(ISigningRepository? repo, Network n) =>
			from items in PHDPkProviderCommon(repo, n)
			select PubKeyProvider.NewHD(items.Item1, items.Item2, DeriveType.NO);

		private static Parser<char, PubKeyProvider> PHDPubKeyProvider(ISigningRepository? repo, Network n) =>
			// The order is important here.
			PHardendedHDPubKeyProvider(repo, n)
				.Or(PUnHardendedHDPubKeyProvider(repo, n))
				.Or(PStaticHDPubKeyProvider(repo, n));
		private static Parser<char, PubKeyProvider> POriginPubkeyProvider(ISigningRepository? repo, Network n) =>
			from rootedKeyPath in PRootedKeyPath(repo)
			from inner in PConstPubKeyProvider(repo, false, n).Or(PHDPubKeyProvider(repo, n))
			select PubKeyProvider.NewOrigin(rootedKeyPath, inner);

		internal static Parser<char, PubKeyProvider> PPubKeyProvider(ISigningRepository? repo, bool onlyCompressed, Network n) =>
			PConstPubKeyProvider(repo, onlyCompressed, n).Or(PHDPubKeyProvider(repo, n)).Or(POriginPubkeyProvider(repo, n));
		#endregion


		private static Parser<char, BitcoinAddress> PTryConvertAddr(string addrStr, Network n) =>
			Parse.TryConvert(addrStr, i => BitcoinAddress.Create(i, n));
		internal static P PAddr(Network n) =>
			from _n in Parse.String("addr")
			from inner in SurroundedByBrackets
			from addr in PTryConvertAddr(inner, n)
			select OutputDescriptor.NewAddr(addr, n);

		internal static P PRaw(ISigningRepository? repo, Network n)
		{
			var PScript =
				(
					from _n in Parse.String("raw")
					from inner in SurroundedByBrackets
					from sc in Parse.TryConvert(inner, str => Script.FromHex(str))
					select sc
				).InjectRepository(repo);
			return PScript.Select(s => OutputDescriptor.NewRaw(s, n));
		}

	private static P PExprHelper<T>(
			Parser<char, string> PName,
			Parser<char, T> pInner,
			Func<T, Network, OutputDescriptor> constructor,
			Network n
			) =>
			from _n in PName
			from _l in Parse.Char('(')
			from item in pInner
			from _r in Parse.Char(')')
			select constructor(item, n);

		private static P PPKHelper(string name, Func<PubKeyProvider, Network, OutputDescriptor> constructor, ISigningRepository? repo, bool onlyCompressed, Network n) =>
			PExprHelper<PubKeyProvider>(Parse.String(name).Text(), PPubKeyProvider(repo, onlyCompressed, n), constructor, n);

		internal static P PPK(ISigningRepository? repo, Network n, bool onlyCompressed = false) =>
			PPKHelper("pk", OutputDescriptor.NewPK, repo, onlyCompressed, n);

		internal static P PPKH(ISigningRepository? repo, Network n, bool onlyCompressed = false) =>
			PPKHelper("pkh", OutputDescriptor.NewPKH, repo, onlyCompressed, n);

		internal static P PWPKH(ISigningRepository? repo, Network n) =>
			PPKHelper("wpkh", OutputDescriptor.NewWPKH, repo, true, n);

		internal static P PCombo(ISigningRepository? repo, Network n, bool onlyCompressed = false) =>
			PPKHelper("combo", OutputDescriptor.NewCombo, repo, onlyCompressed, n);

		internal static P PMulti(ISigningRepository? repo, bool onlyCompressed, Network n, uint? maxN = null) =>
			from name in Parse.String("sortedmulti").XOr(Parse.String("multi")).Text()
			let isSorted = name.StartsWith("sorted")
			from _l in Parse.Char('(')
			from m in Parse.Digit.XMany().Text().Then(d => Parse.TryConvert(d.Trim(), UInt32.Parse))
			where m != 0
			from _c in Parse.Char(',')
			from pkProviders in PPubKeyProvider(repo, onlyCompressed, n).DelimitedBy(Parse.Char(','))
			where m <= pkProviders.Count()
			from _r in Parse.Char(')')
			where !maxN.HasValue || pkProviders.Count() <= maxN
			select OutputDescriptor.NewMulti(m, pkProviders, isSorted, n);

		internal static P PWSHInner(ISigningRepository? repo, Network n, bool onlyCompressed = false, uint? maxMultisigKeyN = null) =>
			PPK(repo, n, onlyCompressed)
				.Or(PPKH(repo, n, onlyCompressed))
				.Or(PMulti(repo, onlyCompressed, n, maxMultisigKeyN));
		internal static P PInner(ISigningRepository? repo, Network n, bool onlyCompressed = false, uint? maxMultisigKeyN = null) =>
			PWSHInner(repo, n, onlyCompressed, maxMultisigKeyN)
				.Or(PWPKH(repo, n));

		internal static P PWSH(ISigningRepository? repo, Network n) =>
			PExprHelper(Parse.String("wsh").Text(), PWSHInner(repo, n, true), OutputDescriptor.NewWSH, n);
		internal static P PSH(ISigningRepository? repo, Network n) =>
			PExprHelper((Parse.String("sh").Text()).Except(Parse.String("wsh")), PInner(repo, n, false, 15).Or(PWSH(repo, n)), OutputDescriptor.NewSH, n);

		internal static P POutputDescriptor(ISigningRepository? repo, Network n) =>
			PAddr(n)
				.Or(PRaw(repo, n))
				.Or(PCombo(repo, n))
				.Or(PInner(repo, n))
				.Or(PWSH(repo, n))
				.Or(PSH(repo, n)).End();

		internal static bool TryParseOD(string str, Network network, out OutputDescriptor? result, bool requireCheckSum = false, ISigningRepository? repo = null)
			=> TryParseOD(str, network, out _, out result,requireCheckSum, repo);
		private static bool TryParseOD(string str, Network network, out string? whyFailure, out OutputDescriptor? result, bool requireCheckSum = false, ISigningRepository? repo = null)
		{
			if (network is null)
				throw new ArgumentNullException(nameof(network));
			if (str == null) throw new ArgumentNullException(nameof(str));
			str = str.Replace(" ", "");
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
					whyFailure = $"CheckSum mismatch. Expected: {checkSplit[1]}; Actual: {realCheckSum}";
					return false;
				}
			}

			var res = POutputDescriptor(repo, network).TryParse(str, network);
			if (!res.IsSuccess)
			{
				whyFailure = res.Description;
				return false;
			}
			result = res.Value;
			if (result is OutputDescriptor.Multi multi && multi.PkProviders.Count > 3)
			{
				whyFailure = "You can not have more than 3 pubkeys in top level multisig.";
				return false;
			}
			return true;
		}
		internal static OutputDescriptor ParseOD(string str, Network network, bool requireCheckSum = false, ISigningRepository? repo = null)
		{
			if (!TryParseOD(str, network, out var whyFailure, out var result, requireCheckSum, repo) || result is null)
				throw new ParsingException(whyFailure);
			return result;
		}

	}
}
#nullable disable
