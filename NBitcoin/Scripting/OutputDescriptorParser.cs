using System;
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
		#region pubkey
		private static Parser<char, PubKey> PPubKeyCompressed() =>
			(from x in Parse.Hex.Repeat(66).Text().Then(s => Parse.TryConvert(s, c => new PubKey(c)))
			select x);

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
		private static Parser<char, TaprootPubKey> PPubkeyXOnly() =>
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
			);
#endif

		private static readonly Parser<char, PubKey> PPubKeyUncompressed =
			from x in Parse.Hex.Repeat(130).Text().Then(s => Parse.TryConvert(s, c => new PubKey(c)))
			select x;

		private static Parser<char, PubKey> PPubKey() =>
			(PPubKeyCompressed().Or(PPubKeyUncompressed));

		private static Parser<char, BitcoinExtPubKey> PRawXPub(Network n) =>
			(from base58Str in Parse.Base58.XMany().Text()
			 from x in Parse.TryConvert(base58Str, c => new BitcoinExtPubKey(c, n))
			 select x);
		private static Parser<char, RootedKeyPath> PRootedKeyPath() =>
			(from _l in Parse.Char('[')
			 from x in (Parse.CharExcept(']').XMany().Text())
				 .Then(inner => Parse.TryConvert(inner, RootedKeyPath.Parse))
			 from _r in Parse.Char(']')
			 select x);

		private static Parser<char, KeyPath> PKeyPath() =>
			(from x in (Parse.Digit.Or(Parse.Chars("/\'h")).XMany()).Text()
				.Then(s => Parse.TryConvert(s, KeyPath.Parse))
			 select x);

		#endregion

		#region Private keys

		private static Parser<char, BitcoinSecret> PTryConvertSecret(string secretStr, Network n) =>
			Parse.TryConvert(secretStr, i => new BitcoinSecret(i, n));
		private static Parser<char, PubKey> PWIF(Network n, bool onlyCompressed) =>
			(from xStr in Parse.Base58.XMany().Text()
			 from x in PTryConvertSecret(xStr, n)
			 where !onlyCompressed || x.PubKey.IsCompressed
			 select x)
			 	.Select(secret => secret.PubKey);
		private static Parser<char, BitcoinExtPubKey> PExtKey(Network n) =>
			// why not just read base58 string first? A. Because failing fast improves speed.
			(from base58Str in Parse.Base58.XMany().Text()
			 from x in Parse.TryConvert(base58Str, c => new BitcoinExtKey(c, n))
			 select x).Select(extKey => extKey.Neuter());

		#endregion

		#region PubKeyProvider

		private static Parser<char, PubKeyProvider> PConstPubKeyProvider(Network n, PubKeyContext ctx)
		{
			var onlyCompressed = ctx != PubKeyContext.NonSegwit;

			Func<bool, Parser<char, PubKeyProvider>> compressedOrWifParser = xOnly =>
				from pk in PPubKeyCompressed().Or(PWIF(n, onlyCompressed))
				select PubKeyProvider.NewConst(pk, xOnly);
			Parser<char, PubKeyProvider>? xonlyParser =
#if HAS_SPAN
				from pk in PPubkeyXOnly()
				select PubKeyProvider.NewConst(pk);
#else
				null;
#endif

			return ctx switch
			{
				PubKeyContext.NonSegwit =>
					from pk in PPubKey().Or(PWIF(n, false))
					select PubKeyProvider.NewConst(pk),
				PubKeyContext.SegwitV0 =>
					from pk in PPubKey().Or(PWIF(n, true))
					select PubKeyProvider.NewConst(pk),
				PubKeyContext.TaprootInternalKey =>
					compressedOrWifParser(true).Or(xonlyParser),
				PubKeyContext.TapScript =>
					compressedOrWifParser(true).Or(xonlyParser),
				_  => throw new Exception("Unreachable"),
			};
		}

		private static Parser<char, Tuple<BitcoinExtPubKey, KeyPath>> PHDPkProviderNoPath(Network n) =>
			from extKey in PRawXPub(n).Or(PExtKey(n))
			select Tuple.Create(extKey, KeyPath.Empty);
		private static Parser<char, Tuple<BitcoinExtPubKey, KeyPath>> PHDPkProviderWithpath(Network n) =>
			from extKey in PRawXPub(n).Or(PExtKey(n))
			from keyPath in PKeyPath()
			select Tuple.Create(extKey, keyPath);
		private static Parser<char, Tuple<BitcoinExtPubKey, KeyPath>> PHDPkProviderCommon(Network n) =>
			PHDPkProviderWithpath(n).Or(PHDPkProviderNoPath(n));
		private static Parser<char, PubKeyProvider> PHardendedHDPubKeyProvider(Network n) =>
			from items in PHDPkProviderCommon(n)
			from prefix in Parse.Char('*')
			from isHardened in Parse.Char('\'').Or(Parse.Char('h'))
			select PubKeyProvider.NewHD(items.Item1, items.Item2, DeriveType.HARDENED);

		private static Parser<char, PubKeyProvider> PUnHardendedHDPubKeyProvider(Network n) =>
			from items in PHDPkProviderCommon(n)
			from prefix in Parse.Char('*')
			select PubKeyProvider.NewHD(items.Item1, items.Item2, DeriveType.UNHARDENED);

		private static Parser<char, PubKeyProvider> PStaticHDPubKeyProvider(Network n) =>
			from items in PHDPkProviderCommon(n)
			select PubKeyProvider.NewHD(items.Item1, items.Item2, DeriveType.NO);

		private static Parser<char, PubKeyProvider> PHDPubKeyProvider(Network n) =>
			// The order is important here.
			PHardendedHDPubKeyProvider(n)
				.Or(PUnHardendedHDPubKeyProvider(n))
				.Or(PStaticHDPubKeyProvider(n));
		private static Parser<char, PubKeyProvider> POriginPubkeyProvider(Network n, PubKeyContext ctx) =>
			from rootedKeyPath in PRootedKeyPath()
			from inner in PConstPubKeyProvider(n, ctx).Or(PHDPubKeyProvider(n))
			select PubKeyProvider.NewOrigin(rootedKeyPath, inner);

		internal static Parser<char, PubKeyProvider> PPubKeyProvider(Network n, PubKeyContext ctx) =>
			PConstPubKeyProvider(n, ctx)
				.Or(PHDPubKeyProvider(n))
				.Or(POriginPubkeyProvider(n, ctx));
		#endregion

		private static Parser<char, BitcoinAddress> PTryConvertAddr(string addrStr, Network n) =>
			Parse.TryConvert(addrStr, i => BitcoinAddress.Create(i, n));
		internal static P PAddr(Network n) =>
			from _n in Parse.String("addr")
			from inner in SurroundedByBrackets
			from addr in PTryConvertAddr(inner, n)
			select OutputDescriptor.NewAddr(addr, n);

		internal static P PRaw(Network n)
		{
			var PScript =
				(
					from _name in Parse.String("raw")
					from inner in SurroundedByBrackets
					from sc in Parse.TryConvert(inner, str => Script.FromHex(str))
					select sc
				);
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

		private static P PPKHelper(string name, Func<PubKeyProvider, Network, OutputDescriptor> constructor, Network n, PubKeyContext ctx) =>
			PExprHelper(Parse.String(name).Text(), PPubKeyProvider(n, ctx), constructor, n);

		internal static P PPK(Network n, PubKeyContext ctx) =>
			PPKHelper("pk", OutputDescriptor.NewPK, n, ctx);

		internal static P PPKH(Network n, PubKeyContext ctx) =>
			PPKHelper("pkh", OutputDescriptor.NewPKH, n, ctx);

		internal static P PWPKH(Network n) =>
			PPKHelper("wpkh", OutputDescriptor.NewWPKH, n, PubKeyContext.SegwitV0);

		internal static P PCombo(Network n) =>
			PPKHelper("combo", OutputDescriptor.NewCombo, n, PubKeyContext.NonSegwit);

		internal static P PMulti(Network n, uint? maxMultisigN, PubKeyContext ctx) =>
			from name in Parse.String("sortedmulti").XOr(Parse.String("multi")).Text()
			let isSorted = name.StartsWith("sorted")
			from _l in Parse.Char('(')
			from m in Parse.Digit.XMany().Text().Then(d => Parse.TryConvert(d.Trim(), UInt32.Parse))
			where m != 0
			from _c in Parse.Char(',')
			from pkProviders in PPubKeyProvider(n, ctx).DelimitedBy(Parse.Char(','))
			where m <= pkProviders.Count()
			from _r in Parse.Char(')')
			where !maxMultisigN.HasValue ||  pkProviders.Count() <= maxMultisigN.Value
			select OutputDescriptor.NewMulti(m, pkProviders, isSorted, n);

#if HAS_SPAN

		// uncompressed public key is not allowed in Taproot context, thus we use different pubkey provider parser here.
		private static Parser<char, PubKeyProvider> PPubKeyProviderForTaproot(Network n, PubKeyContext ctx) =>
			PConstPubKeyProvider(n, ctx)
				.Or(PHDPubKeyProvider(n))
				.Or(POriginPubkeyProvider(n, ctx));
		private static P PMultiA(Network n) =>
			from name in Parse.String("sortedmulti_a").XOr(Parse.String("multi_a")).Text()
			let isSorted = name.StartsWith("sorted")
			from _l in Parse.Char('(')
			from m in Parse.Digit.XMany().Text().Then(d => Parse.TryConvert(d.Trim(), UInt32.Parse))
			where m != 0
			from _c in Parse.Char(',')
			from pkProviders in PPubKeyProviderForTaproot(n, PubKeyContext.TapScript).DelimitedBy(Parse.Char(','))
			where m <= pkProviders.Count()
			from _r in Parse.Char(')')
			select OutputDescriptor.NewMulti(m, pkProviders, isSorted, n, true);

		// inside of `{}` of TapScript, happens to be the same of those of `wsh()`, but the supported pubkey types
		// are bit different.
		private static P PTapLeaf(Network n) =>
			PPK(n, PubKeyContext.TapScript)
				.Or(PPKH(n, PubKeyContext.TapScript))
				.Or(PMultiA(n));
		private static Parser<char, OutputDescriptor.TapTree> PTapScript(Network n)
		{
			var pTree =
				from _l in Parse.Char('{')
				from l in PTapScript(n)
				from _ in Parse.Char(',')
				from r in PTapScript(n)
				from _r in Parse.Char('}')
				select OutputDescriptor.TapTree.NewTree(l, r);
			var pLeaf =
				from inner in PTapLeaf(n)
				select OutputDescriptor.TapTree.NewLeaf(inner);
			return
				pTree.Or(pLeaf);
		}

		private static P PTRInner(Network n) =>
			from internalPk in PPubKeyProviderForTaproot(n, PubKeyContext.TaprootInternalKey)
			from _ in Parse.Char(',')
			from tapTree in PTapScript(n)
			select OutputDescriptor.NewTr(internalPk, n, tapTree);

		private static P PTapRootNoScriptInner(Network n) =>
			from internalPk in PPubKeyProviderForTaproot(n, PubKeyContext.TaprootInternalKey)
			select OutputDescriptor.NewTr(internalPk, n);

		private static P PRawTr(Network n)
			=> PExprHelper(Parse.String("rawtr").Text(), PPubKeyProviderForTaproot(n, PubKeyContext.TaprootInternalKey), OutputDescriptor.NewRawTr, n);
#endif

		private static P PWSHInner(Network n, PubKeyContext ctx, uint? maxMultisigN = null) =>
			PPK(n, ctx)
				.Or(PPKH(n, ctx))
				.Or(PMulti(n, maxMultisigN, ctx));

		private static P PInner(Network n, PubKeyContext ctx, uint? maxMultisigN = null) =>
			PWSHInner(n, ctx, maxMultisigN)
				.Or(PWPKH(n));

		private static P PWSH(Network n) =>
			PExprHelper(Parse.String("wsh").Text(), PWSHInner(n, PubKeyContext.SegwitV0), OutputDescriptor.NewWSH, n);

		private static P PSH(Network n) =>
			PExprHelper(
				(Parse.String("sh").Text()).Except(Parse.String("wsh")),
				PInner(n, PubKeyContext.NonSegwit, 15).Or(PWSH(n)),
				OutputDescriptor.NewSH,
				n);

#if HAS_SPAN
		internal static P PTR(Network n) =>
			from _n in Parse.String("tr")
			from _l in Parse.Char('(')
			from item in PTRInner(n).Or(PTapRootNoScriptInner(n))
			from _r in Parse.Char(')')
			select item;
#endif
		private static P POutputDescriptor(Network n) =>
			PAddr(n)
				.Or(PRaw(n))
				.Or(PCombo(n))
				.Or(PInner(n, PubKeyContext.NonSegwit))
				.Or(PWSH(n))
				.Or(PSH(n))
#if HAS_SPAN
				.Or(PTR(n))
				.Or(PRawTr(n))
#endif
				.End();

		internal static bool TryParseOD(string str, Network network, out OutputDescriptor? result)
			=> TryParseOD(str, network, out _, out result,false);
		private static bool TryParseOD(string str, Network network, out string? whyFailure, out OutputDescriptor? result, bool requireCheckSum = false)
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

			var res = POutputDescriptor(network).TryParse(str, network);
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
			}
#endif
			return true;
		}

		internal static bool TryParseOD(string str, Network network, out OutputDescriptor? result, bool requireCheckSum)
			=> TryParseOD(str, network, out _, out result, requireCheckSum);

		internal static OutputDescriptor ParseOD(string str, Network network)
		=> ParseOD(str, network, false);

		internal static OutputDescriptor ParseOD(string str, Network network, bool requireCheckSum)
		{
			if (!TryParseOD(str, network, out var whyFailure, out var result, requireCheckSum) || result is null)
				throw new ParsingException(whyFailure);
			return result;
		}

	}
}
#nullable disable
