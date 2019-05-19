using System;
using NBitcoin.Scripting.Parser;
using static NBitcoin.Scripting.MiniscriptDSLParser;
using static NBitcoin.Scripting.PubKeyProvider;
using P = NBitcoin.Scripting.Parser.Parser<char, NBitcoin.Scripting.OutputDescriptor>;

namespace NBitcoin.Scripting
{
	public static class OutputDescriptorParser
	{
		#region pubkey
		private static readonly Parser<char, PubKey> PPubKeyCompressed =
			from x in Parse.Hex.Repeat(66).Text().Then(s => Parse.TryConvert(s, c => new PubKey(c)))
			select x;

		private static readonly Parser<char, PubKey> PPubKeyUncompressed =
			from x in Parse.Hex.Repeat(130).Text().Then(s => Parse.TryConvert(s, c => new PubKey(c)))
			select x;

		private static readonly Parser<char, PubKey> PPubKey =
			PPubKeyCompressed.Or(PPubKeyUncompressed);

		private static readonly Parser<char, BitcoinExtPubKey> PRawXPub =
			from x in ((Parse.String("tpub").Or(Parse.String("xpub")))
				.Then(s => Parse.Base58.XMany().Text()))
				.Then(s => Parse.TryConvert(s, c => new BitcoinExtPubKey(c)))
			select x;
		private static readonly Parser<char, RootedKeyPath> PRootedKeyPath =
			from _l in Parse.Char('[').Token()
			from x in (Parse.CharExcept(']').XMany().Text().Token())
				.Then(inner => Parse.TryConvert(inner, RootedKeyPath.Parse))
			from _r in Parse.Char(']').Token()
			select x;

		private static readonly Parser<char, KeyPath> PKeyPath =
			from x in (Parse.Digit.Or(Parse.Chars("/\'h")).XMany()).Text()
				.Then(s => Parse.TryConvert(s, KeyPath.Parse))
			select x;

		#endregion

		#region PubKeyProvider

		private static readonly Parser<char, PubKeyProvider> PConstPubKeyProvider =
			from pk in PPubKey
			select PubKeyProvider.NewConst(pk);
		private static readonly Parser<char, Tuple<BitcoinExtPubKey, KeyPath>> PPkProviderCommon =
			from extKey in PRawXPub
			from keyPath in PKeyPath
			from prefix in Parse.Char('/')
			select Tuple.Create(extKey, keyPath);
		private static readonly Parser<char, PubKeyProvider> PHardendedHDPubKeyProvider =
			from items in PPkProviderCommon
			from isRange in Parse.Char('*')
			from isHardened in Parse.Char('\'')
			select PubKeyProvider.NewHD(items.Item1, items.Item2, DeriveType.HARDENED);

		private static readonly Parser<char, PubKeyProvider> PUnHardendedHDPubKeyProvider =
			from items in PPkProviderCommon
			from isRange in Parse.Char('*')
			select PubKeyProvider.NewHD(items.Item1, items.Item2, DeriveType.UNHARDENED);

		private static readonly Parser<char, PubKeyProvider> PStaticHDPubKeyProvider =
			from items in PPkProviderCommon
			select PubKeyProvider.NewHD(items.Item1, items.Item2, DeriveType.NO);

		private static readonly Parser<char, PubKeyProvider> PHDPubKeyProvider =
			// The order is important here.
			PHardendedHDPubKeyProvider
				.Or(PUnHardendedHDPubKeyProvider)
				.Or(PStaticHDPubKeyProvider);
		private static readonly Parser<char, PubKeyProvider> POriginPubkeyProvider =
			from rootedKeyPath in PRootedKeyPath
			from inner in PConstPubKeyProvider.Or(PHDPubKeyProvider)
			select PubKeyProvider.NewOrigin(rootedKeyPath, inner);

		#endregion

		private static Parser<char, BitcoinAddress> PTryConvertAddr(string addrStr) =>
			Parse.TryConvert(addrStr, i => BitcoinAddress.Create(i, Network.Main))
				.Or(Parse.TryConvert(addrStr, i => BitcoinAddress.Create(i, Network.TestNet)))
				.Or(Parse.TryConvert(addrStr, i => BitcoinAddress.Create(i, Network.RegTest)));
		private static readonly Parser<char, OutputDescriptor> PAddrOutputDescriptor =
			from _n in Parse.String("addr")
			from inner in SurroundedByBrackets
			from addr in PTryConvertAddr(inner)
			select OutputDescriptor.NewAddr(addr);

		internal static readonly Parser<char, OutputDescriptor> POutputDescriptor =
			PAddrOutputDescriptor;
	}
}