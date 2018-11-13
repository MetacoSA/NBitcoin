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
       Arb.From<BitcoinAddress>(randomAddress());

    public static Arbitrary<Tuple<BitcoinAddress, Network>> BitcoinAddressWithNetworkArb()
    {
      var tupleGen = from n in ChainParamsGenerator.NetworkGen()
                     from addr in randomAddress(n)
                     select Tuple.Create(addr, n);
      return Arb.From<Tuple<BitcoinAddress, Network>>(tupleGen);
    }

    public static Gen<BitcoinAddress> randomAddress() =>
      Gen.OneOf(p2pkhAddress(), p2shAddress(), bech32Address());

    public static Gen<BitcoinAddress> randomAddress(Network network) =>
      Gen.OneOf(p2pkhAddress(network), p2shAddress(network), bech32Address(network));
    public static Gen<BitcoinAddress> p2pkhAddress() =>
      from network in NetworkGen()
      from addr in p2pkhAddress(network)
      select addr;

    public static Gen<BitcoinAddress> p2pkhAddress(Network network) =>
      from pk in publicKey()
      select (BitcoinAddress)pk.GetAddress(network);

    public static Gen<BitcoinAddress> p2shAddress() =>
      from network in NetworkGen()
      from addr in p2shAddress()
      select addr;

    public static Gen<BitcoinAddress> p2shAddress(Network network) =>
      from pk in publicKey()
      select (BitcoinAddress)pk.GetScriptAddress(network);

    public static Gen<BitcoinAddress> bech32Address() =>
      from n in ChainParamsGenerator.NetworkGen()
      from addr in bech32Address(n)
      select addr;
    public static Gen<BitcoinAddress> bech32Address(Network network) =>
      Gen.OneOf(p2wpkhAddress(network), p2wshAddress(network));

    private static Gen<BitcoinAddress> p2wpkhAddress(Network network) =>
      from pk in publicKey()
      select (BitcoinAddress)pk.GetSegwitAddress(network);
    private static Gen<BitcoinAddress> p2wshAddress(Network network) =>
      from script in multiSignatureWitScript()
      select (BitcoinAddress)new BitcoinWitScriptAddress(script.ToScript().WitHash, network);
  }
}