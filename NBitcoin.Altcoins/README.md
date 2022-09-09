# NBitcoin.Altcoins

Currently supported altcoins are:

* Argoneum
* BCash
* BGold
* BitCore
* Dash
* Dogecoin
* Verge
* Dystem
* Feathercoin
* Groestlcoin
* LBRYCredits
* Liquid
* Litecoin
* Monacoin
* MonetaryUnit
* Polis
* Terracoin
* UFO
* Viacoin
* Zclassic
* Koto
* Bitcoinplus
* Chaincoin
* ZCoin
* DogeCash
* Qtum
* XDS
* Althash
* Neblio
* Optical Bitcoin

## How to use?

This package expose altcoin's `Network` class.
For example if you want to use Litecoin testnet:

```
Network network = NBitcoin.Altcoins.Litecoin.Instance.Testnet;
```

You can then use this fork generating a Litecoin address for example:

```
Console.WriteLine(new Key().PubKey.GetAddress(network));
```

## How to support my own altcoin?

Follow Litecoin example and make a pull request.

NBitcoin developers do not test those PRs, so you are responsible to keep it working.

## How to test?

If you want to test your newly created `Network`, update [WellknownNodeDownloadData](../NBitcoin.TestFramework/WellknownNodeDownloadData.cs) so the test environment can download binaries and run for your blockchain on regtest.

Then, change [NodeBuilderEx](../NBitcoin.Tests/NodeBuilderEx.cs) like the following example.

```
public static NodeBuilder Create([CallerMemberName] string caller = null)
{
	return NodeBuilder.Create(NodeDownloadData.Litecoin.v0_15_1, Altcoins.AltNetworkSets.Litecoin.Regtest, caller);
}
```

You can then run the tests for your altcoin in command line from the NBitcoin.Tests project:

Note that the first time can take a while because the test environment download the node binaries.

```
dotnet test NBitcoin.Tests.csproj --filter "Altcoins=Altcoins" -p:ParallelizeTestCollections=false --framework netcoreapp2.1
```

You can also manually execute any test with Visual Studio.

Note that the tests with the trait `Altcoins=Altcoins` are only doing some sanity check. You might want to run additional tests.

You can take a look at [this commit as an example](https://github.com/MetacoSA/NBitcoin/commit/e075d1549ddd356f112cb3322c240490382c757e).
