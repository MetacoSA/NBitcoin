using FsCheck;
using NBitcoin;
using static NBitcoin.Tests.Generators.CryptoGenerator;
using static NBitcoin.Tests.Generators.ChainParamsGenerator;
using static NBitcoin.Tests.Generators.ScriptGenerator;
using System.Linq;
using System;

namespace NBitcoin.Tests.Generators
{
	public class AddressGenerator
	{
		public static Arbitrary<BitcoinAddress> BitcoinAddressArb() =>
			 Arb.From<BitcoinAddress>(RandomAddress());

		public static Arbitrary<Tuple<BitcoinAddress, Network>> BitcoinAddressWithNetworkArb()
		{
			var tupleGen = from n in ChainParamsGenerator.NetworkGen()
										 from addr in RandomAddress(n)
										 select Tuple.Create(addr, n);
			return Arb.From<Tuple<BitcoinAddress, Network>>(tupleGen);
		}

		public static Gen<BitcoinAddress> RandomAddress() =>
			Gen.OneOf(P2PKHAddress(), P2SHAddress(), Bech32Address());

		public static Gen<BitcoinAddress> RandomAddress(Network network) =>
			Gen.OneOf(P2PKHAddress(network), P2SHAddress(network), Bech32Address(network));
		public static Gen<BitcoinAddress> P2PKHAddress() =>
			from network in NetworkGen()
			from addr in P2PKHAddress(network)
			select addr;

		public static Gen<BitcoinAddress> P2PKHAddress(Network network) =>
			from pk in PublicKey()
			select (BitcoinAddress) pk.GetAddress(network);

		public static Gen<BitcoinAddress> P2SHAddress() =>
			from network in NetworkGen()
			from addr in P2SHAddress(network)
			select addr;

		public static Gen<BitcoinAddress> P2SHAddress(Network network) =>
			from pk in PublicKey()
			select (BitcoinAddress) pk.GetScriptAddress(network);

		public static Gen<BitcoinAddress> Bech32Address() =>
			from n in ChainParamsGenerator.NetworkGen()
			from addr in Bech32Address(n)
			select addr;
		public static Gen<BitcoinAddress> Bech32Address(Network network) =>
			Gen.OneOf(P2WPKHAddress(network), P2WSHAddress(network));

		private static Gen<BitcoinAddress> P2WPKHAddress(Network network) =>
			from pk in PublicKey()
			select (BitcoinAddress) pk.GetSegwitAddress(network);
		private static Gen<BitcoinAddress> P2WSHAddress(Network network) =>
			from script in MultiSignatureWitScript()
			select (BitcoinAddress) new BitcoinWitScriptAddress(script.ToScript().WitHash, network);
	}
}