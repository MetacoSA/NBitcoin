using System;
using System.Collections.Generic;
using System.Diagnostics;
using NBitcoin.Scripting.Parser;
using static NBitcoin.Scripting.PubKeyProvider;
using static NBitcoin.Scripting.ParserUtil;
using System.Linq;
using NBitcoin.DataEncoders;
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
#if HAS_SPAN
					if (r.Value is BitcoinSecret sc)
					{
						repo.SetSecret(sc.PubKey.TaprootPubKey, sc);
					}

					if (r.Value is Origin origin)
					{
						if (origin.Inner is HD hdPkPV)
						{
							repo.SetKeyOrigin(hdPkPV.Extkey.GetPublicKey().TaprootPubKey, origin.KeyOriginInfo);
						}
						if (origin.Inner is Const constPkPV)
						{
							repo.SetKeyOrigin(constPkPV.Pk.TaprootPubKey, origin.KeyOriginInfo);
						}
					}
#endif
				}
				return r;
			};
		}
		#endregion

		#region pubkey
		private static Parser<char, PubKey> PPubKeyCompressed(ISigningRepository? repo) =>
			(from x in Parse.Hex.Repeat(66).Text().Then(s => Parse.TryConvert(s, c => new PubKey(c)))
			select x).InjectRepository(repo);

		private static HexEncoder Hex = new HexEncoder();

		/// <summary>
		/// The handling of pubkey depends on where in tr() we are using it.
		/// e.g. for InternalKey (P) in taproot, it must always be x-only.
		/// But for Tapscript, it supports pubkey versioning, thus it is not always x-only in the future.
		/// This enum is used to distinguish those context.
		/// </summary>
		internal enum PubKeyContext
		{
			NonSegwit,
			SegwitV0,
			TaprootInternalKey,
			TapScript,
		}

#if HAS_SPAN
		private static Parser<char, TaprootPubKey> PPubkeyXOnly(ISigningRepository? repo) =>
			(
				from x in Parse.Hex.Repeat(64).Text().Then<char, string, TaprootPubKey>(s =>
				{
					return (i, n) =>
					{
						var bytes = Hex.DecodeData(s);
						return
							TaprootPubKey.TryCreate(bytes, out var result)
							? ParserResult<char, TaprootPubKey>.Success(i, result)
							: ParserResult<char, TaprootPubKey>.Failure(i, $"Failed to parse x-only pubkey {s}");
					};
				})
				select x
			).InjectRepository(repo);
#endif

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

		private static Parser<char, PubKeyProvider> PConstPubKeyProvider(ISigningRepository? repo, Network n, PubKeyContext ctx)
		{
			var onlyCompressed = ctx != PubKeyContext.NonSegwit;

			Func<bool, Parser<char, PubKeyProvider>> compressedOrWifParser = xOnly =>
				from pk in PPubKeyCompressed(repo).Or(PWIF(repo, n, onlyCompressed))
				select PubKeyProvider.NewConst(pk, xOnly);
			Parser<char, PubKeyProvider>? xonlyParser =
#if HAS_SPAN
				from pk in PPubkeyXOnly(repo)
				select PubKeyProvider.NewConst(pk);
#else
				null;
#endif

			return ctx switch
			{
				PubKeyContext.NonSegwit =>
					from pk in PPubKey(repo).Or(PWIF(repo, n, false))
					select PubKeyProvider.NewConst(pk),
				PubKeyContext.SegwitV0 =>
					from pk in PPubKey(repo).Or(PWIF(repo, n, true))
					select PubKeyProvider.NewConst(pk),
				PubKeyContext.TaprootInternalKey =>
					compressedOrWifParser(true).Or(xonlyParser),
				PubKeyContext.TapScript =>
					compressedOrWifParser(true).Or(xonlyParser),
				_  => throw new Exception("Unreachable"),
			};
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
		private static Parser<char, PubKeyProvider> POriginPubkeyProvider(ISigningRepository? repo, Network n, PubKeyContext ctx) =>
			from rootedKeyPath in PRootedKeyPath(repo)
			from inner in PConstPubKeyProvider(repo, n, ctx).Or(PHDPubKeyProvider(repo, n))
			select PubKeyProvider.NewOrigin(rootedKeyPath, inner);

		internal static Parser<char, PubKeyProvider> PPubKeyProvider(ISigningRepository? repo, Network n, PubKeyContext ctx) =>
			PConstPubKeyProvider(repo, n, ctx)
				.Or(PHDPubKeyProvider(repo, n))
				.Or(POriginPubkeyProvider(repo, n, ctx));
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
					from _name in Parse.String("raw")
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

		private static P PPKHelper(string name, Func<PubKeyProvider, Network, OutputDescriptor> constructor, ISigningRepository? repo, Network n, PubKeyContext ctx) =>
			PExprHelper(Parse.String(name).Text(), PPubKeyProvider(repo, n, ctx), constructor, n);

		internal static P PPK(ISigningRepository? repo, Network n, PubKeyContext ctx) =>
			PPKHelper("pk", OutputDescriptor.NewPK, repo, n, ctx);

		internal static P PPKH(ISigningRepository? repo, Network n, PubKeyContext ctx) =>
			PPKHelper("pkh", OutputDescriptor.NewPKH, repo, n, ctx);

		internal static P PWPKH(ISigningRepository? repo, Network n) =>
			PPKHelper("wpkh", OutputDescriptor.NewWPKH, repo, n, PubKeyContext.SegwitV0);

		internal static P PCombo(ISigningRepository? repo, Network n) =>
			PPKHelper("combo", OutputDescriptor.NewCombo, repo, n, PubKeyContext.NonSegwit);

		internal static P PMulti(ISigningRepository? repo, Network n, uint? maxMultisigN, PubKeyContext ctx) =>
			from name in Parse.String("sortedmulti").XOr(Parse.String("multi")).Text()
			let isSorted = name.StartsWith("sorted")
			from _l in Parse.Char('(')
			from m in Parse.Digit.XMany().Text().Then(d => Parse.TryConvert(d.Trim(), UInt32.Parse))
			where m != 0
			from _c in Parse.Char(',')
			from pkProviders in PPubKeyProvider(repo, n, ctx).DelimitedBy(Parse.Char(','))
			where m <= pkProviders.Count()
			from _r in Parse.Char(')')
			where !maxMultisigN.HasValue ||  pkProviders.Count() <= maxMultisigN.Value
			select OutputDescriptor.NewMulti(m, pkProviders, isSorted, n);

#if HAS_SPAN

		// uncompressed public key is not allowed in Taproot context, thus we use different pubkey provider parser here.
		private static Parser<char, PubKeyProvider> PPubKeyProviderForTaproot(ISigningRepository? repo, Network n, PubKeyContext ctx) =>
			PConstPubKeyProvider(repo, n: n, ctx)
				.Or(PHDPubKeyProvider(repo, n))
				.Or(POriginPubkeyProvider(repo, n, ctx));
		private static P PMultiA(ISigningRepository? repo, Network n) =>
			from name in Parse.String("sortedmulti_a").XOr(Parse.String("multi_a")).Text()
			let isSorted = name.StartsWith("sorted")
			from _l in Parse.Char('(')
			from m in Parse.Digit.XMany().Text().Then(d => Parse.TryConvert(d.Trim(), UInt32.Parse))
			where m != 0
			from _c in Parse.Char(',')
			from pkProviders in PPubKeyProviderForTaproot(repo, n, PubKeyContext.TapScript).DelimitedBy(Parse.Char(','))
			where m <= pkProviders.Count()
			from _r in Parse.Char(')')
			select OutputDescriptor.NewMulti(m, pkProviders, isSorted, n, true);

		// inside of `{}` of TapScript, happens to be the same of those of `wsh()`, but the supported pubkey types
		// are bit different.
		private static P PTapLeaf(ISigningRepository? repo, Network n) =>
			PPK(repo, n, PubKeyContext.TapScript)
				.Or(PPKH(repo, n, PubKeyContext.TapScript))
				.Or(PMultiA(repo, n));
		private static Parser<char, OutputDescriptor.TapTree> PTapScript(ISigningRepository? repo, Network n)
		{
			var pTree =
				from _l in Parse.Char('{')
				from l in PTapScript(repo, n)
				from _ in Parse.Char(',')
				from r in PTapScript(repo, n)
				from _r in Parse.Char('}')
				select OutputDescriptor.TapTree.NewTree(l, r);
			var pLeaf =
				from inner in PTapLeaf(repo, n)
				select OutputDescriptor.TapTree.NewLeaf(inner);
			return
				pTree.Or(pLeaf);
		}

		private static P PTRInner(ISigningRepository? repo, Network n) =>
			from internalPk in PPubKeyProviderForTaproot(repo, n, PubKeyContext.TaprootInternalKey)
			from _ in Parse.Char(',')
			from tapTree in PTapScript(repo, n)
			select OutputDescriptor.NewTr(internalPk, n, tapTree);

		private static P PTapRootNoScriptInner(ISigningRepository repo, Network n) =>
			from internalPk in PPubKeyProviderForTaproot(repo, n, PubKeyContext.TaprootInternalKey)
			select OutputDescriptor.NewTr(internalPk, n);

		private static P PRawTr(ISigningRepository? repo, Network n)
			=> PExprHelper(Parse.String("rawtr").Text(), PPubKeyProviderForTaproot(repo, n, PubKeyContext.TaprootInternalKey), OutputDescriptor.NewRawTr, n);
#endif

		private static P PWSHInner(ISigningRepository? repo, Network n, PubKeyContext ctx, uint? maxMultisigN = null) =>
			PPK(repo, n, ctx)
				.Or(PPKH(repo, n, ctx))
				.Or(PMulti(repo, n, maxMultisigN, ctx));

		private static P PInner(ISigningRepository? repo, Network n, PubKeyContext ctx, uint? maxMultisigN = null) =>
			PWSHInner(repo, n, ctx, maxMultisigN)
				.Or(PWPKH(repo, n));

		private static P PWSH(ISigningRepository? repo, Network n) =>
			PExprHelper(Parse.String("wsh").Text(), PWSHInner(repo, n, PubKeyContext.SegwitV0), OutputDescriptor.NewWSH, n);

		private static P PSH(ISigningRepository? repo, Network n) =>
			PExprHelper(
				(Parse.String("sh").Text()).Except(Parse.String("wsh")),
				PInner(repo, n, PubKeyContext.NonSegwit, 15).Or(PWSH(repo, n)),
				OutputDescriptor.NewSH,
				n);

#if HAS_SPAN
		internal static P PTR(ISigningRepository? repo, Network n) =>
			from _n in Parse.String("tr")
			from _l in Parse.Char('(')
			from item in PTRInner(repo, n).Or(PTapRootNoScriptInner(repo, n))
			from _r in Parse.Char(')')
			select item;
#endif
		private static P POutputDescriptor(ISigningRepository? repo, Network n) =>
			PAddr(n)
				.Or(PRaw(repo, n))
				.Or(PCombo(repo, n))
				.Or(PInner(repo, n, PubKeyContext.NonSegwit))
				.Or(PWSH(repo, n))
				.Or(PSH(repo, n))
#if HAS_SPAN
				.Or(PTR(repo, n))
				.Or(PRawTr(repo, n))
#endif
				.End();

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
				whyFailure = $"Multiple '#'s Symbols {str}";
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

#if HAS_SPAN
		if (result is OutputDescriptor.Tr tr)
			{
				//if (!tr.IsKeyPathSpendOnly)
				//	throw new NotSupportedException($"We currently do not support tr() descriptor with tapscript");
				if (!tr.IsRange() && repo is not null)
				{
					if (tr.TryGetSpendInfo(repo, out var spendInfo))
					{
						repo.SetTaprootSpendInfo(spendInfo.OutputPubKey, spendInfo);
					}
				}
			}
#endif
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
