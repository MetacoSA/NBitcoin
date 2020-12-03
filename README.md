# NBitcoin

[![Join the chat at https://gitter.im/MetacoSA/NBitcoin](https://badges.gitter.im/MetacoSA/NBitcoin.svg)](https://gitter.im/MetacoSA/NBitcoin?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![Build status](https://github.com/MetacoSA/NBitcoin/workflows/CI/badge.svg)](https://github.com/MetacoSA/NBitcoin/actions?query=workflow%3ACI)

NBitcoin is the most complete Bitcoin library for the .NET platform. It implements all most relevant Bitcoin Improvement Proposals (BIPs). It also provides low level access to Bitcoin primitives so you can easily build your application on top of it. Join us in our [gitter chat room](https://gitter.im/MetacoSA/NBitcoin).
It works on Windows, Mac and Linux with Xamarin, Unity, .NET Core or CLR. (Porting to Unity should not be that hard if you need it)

The best documentation available is our [eBook](https://programmingblockchain.gitbooks.io/programmingblockchain/content/), and the excellent unit tests. There are also some more resources below.

You can also browse the API easily through [the API reference](https://metacosa.github.io/NBitcoin/api/index.html).

# How to use ?

In .NET Core:
```bash
dotnet add package NBitcoin
```
If using legacy .NET Framework in Visual Studio
```bash
Install-Package NBitcoin
```
You can also just use the `Manage NuGet Package` window on your project in Visual Studio.

Go on the [NuGet website](https://www.nuget.org/packages/NBitcoin/) for more information.

The packages support:

* With full features: Windows Desktop applications, Mono Desktop applications and platforms supported by [.NET Standard 1.3](https://docs.microsoft.com/en-us/dotnet/articles/standard/library) (.NET Core, Xamarin IOS, Xamarin Android, UWP and more).
* With limited features: platforms supported by [.NET Standard 1.1](https://docs.microsoft.com/en-us/dotnet/articles/standard/library) (Windows Phone, Windows 8.0 apps).

To compile it by yourself, you can git clone, open the project and hit the compile button in Visual Studio.


 # How to get started? 
 
 First, you need to understand Bitcoin, for this read:
 * [Programming The Blockchain in C#](https://programmingblockchain.gitbooks.io/programmingblockchain/content/)

 Once you get familiar with Bitcoin terminology with this book, follow up by reading:

 * [NBitcoin documentation](https://github.com/NicolasDorier/NBitcoin.Docs/blob/master/README.md)

 This will teach you how to use NBitcoin in a practical way.

# How to use with Altcoins ?

> **Install-Package NBitcoin.Altcoins** 

Find more information [here](NBitcoin.Altcoins).

# How to debug in NBitcoin source code?

When a new version of `NBitcoin`, `NBitcoin.Altcoins` or `NBitcoin.TestFramework` is released on Nuget, we also upload a separate symbol package (`snupkg`) with SourceLink enabled. This is enabled from version `4.1.1.73`.

This means that it is possible to debug into NBitcoin code, and the source will be fetched transparently from github.

This works on both Visual Studio Code and Visual Studio for Windows.

## Debug inside source with Visual Studio

You need to run at least Visual Studio 15.9.
Then, you need to:

* Go in `Tools / Options / Debugging / General` and turn off `Enable Just My Code`.
* Go in `Tools / Options / Debugging / Symbols` and add `https://symbols.nuget.org/download/symbols` to the `Symbol file (.pdb) locations`, make sure it is checked.

You should also check `Microsoft Symbol Server` or your debugging experience in visual studio will be slowed down.

Now you can Debug your project and step inside any call to NBitcoin.

## Debug inside source with Visual Studio Code

Inside your `launch.json`, add the following to `.NET Core Launch (console)` configuration:

```json
"justMyCode": false,
"symbolOptions": {
    "searchPaths": [ "https://symbols.nuget.org/download/symbols" ],
    "searchMicrosoftSymbolServer": true
},
```

Now you can Debug your project and step inside any call to NBitcoin.

# How to use with my own blockchain?

 Find more information [here](NBitcoin.Altcoins).

# How to use in Unity?

You should use at least `Unity 2018.2` using `Script Runtime Version` `.NET 4.x Equivalent` and `Api Compatibility Level` `.NET Standard 2.0`.
You can see more on [this post](https://blogs.unity3d.com/2018/07/11/scripting-runtime-improvements-in-unity-2018-2/).

Then you need to compile NBitcoin:

```powershell
git clone https://github.com/MetacoSA/NBitcoin/
cd NBitcoin/NBitcoin
dotnet publish -c Release -f netstandard2.0
Remove-Item -Force -Recurse .\bin\Release\netstandard2.0\publish\runtimes\
```

Then put the libraries of `.\bin\Release\netstandard2.0` into your asset folder.

If you need altcoins support, use the same step but with `cd NBitcoin/NBitcoin.Altcoins` instead.

# How to use in .NET Core

If you want to use .NET Core, first install .NET Core [as documented here](https://www.microsoft.com/net/core#windowsvs2017).

Then:
```
mkdir MyProject
cd MyProject
dotnet new console
dotnet add package NBitcoin
dotnet restore
```
Then edit your Program.cs:
```
using System;
using NBitcoin;

namespace _125350929
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World! " + new Key().GetWif(Network.Main));
        }
    }
}
```
You can then run with
```
dotnet run
```

We advise you to use [Visual Studio Code](https://code.visualstudio.com/) as the editor for your project.

## Description
NBitcoin notably includes:

* A [TransactionBuilder](http://www.codeproject.com/Articles/835098/NBitcoin-Build-Them-All) supporting Stealth, Open Asset, and all standard transactions
* Full script evaluation and parsing
* A RPC Client
* A Rest Client
* The parsing of standard scripts and creation of custom ones
* The serialization of blocks, transactions and scripts
* The signing and verification with private keys (with support for compact signatures) for proving ownership
* Bloom filters and partial merkle trees
* Segregated Witness ([BIP 141](https://github.com/bitcoin/bips/blob/master/bip-0141.mediawiki), [BIP 143](https://github.com/bitcoin/bips/blob/master/bip-0143.mediawiki), [BIP 144](https://github.com/bitcoin/bips/blob/master/bip-0144.mediawiki))
* Bech32 segwit address implementation with error detection [BIP 173](https://github.com/bitcoin/bips/blob/master/bip-0173.mediawiki)
* Mnemonic code for generating deterministic keys ([BIP 39](https://github.com/bitcoin/bips/blob/master/bip-0039.mediawiki)), credits to [Thasshiznets](https://github.com/Thashiznets/BIP39.NET)
* Hierarchical Deterministic Wallets ([BIP 32](https://github.com/bitcoin/bips/blob/master/bip-0032.mediawiki))
* Payment URLs ([BIP 21](https://github.com/bitcoin/bips/blob/master/bip-0021.mediawiki))
* Full Bitcoin P2P implementation with SOCKS5 support for connecting through Tor
* [A C# implementation of secp256k1](NBitcoin.Secp256k1/README.md)

Please read our [ebook](https://programmingblockchain.gitbooks.io/programmingblockchain/content/) to understand the capabilities.

NBitcoin is inspired by Bitcoin Core code but provides a simpler object oriented API (e.g., new Key().PubKey.Address.ToString() to generate a key and get the associated address). It relies on the BouncyCastle cryptography library instead of OpenSSL, yet replicates OpenSSL bugs to guarantee compatibility. NBitcoin also ports the integrality of Bitcoin Core unit tests with their original data in order to validate the compatibility of the two implementations.

NBitcoin is licensed under the [MIT License](https://opensource.org/licenses/MIT) and we encourage you to use it to explore, learn, debug, play, share and create software for Bitcoin and with other Metaco services.

## How to connect use a SOCKS5 proxy to connect to a Bitcoin node?

Here an example which assume you run Tor with SOCKS5 proxy on port 9050.
```csharp
var connectionParameters = new NodeConnectionParameters();
connectionParameters.TemplateBehaviors.Add(new SocksSettingsBehavior(Utils.ParseEndpoint("localhost", 9050)));
Node node = await Node.ConnectAsync(Network.Main, "7xnmrhmkvptbcvpl.onion:8333", connectionParameters);
node.VersionHandshake();
```

## Some OSS projects using NBitcoin

* [Wasabi](http://github.com/zkSNACKs/WalletWasabi): Privacy focused, ZeroLink compliant Bitcoin wallet.

* [StratisBitcoinFullNode](http://github.com/stratisproject/StratisBitcoinFullNode): Bitcoin full node in C# https://stratisplatform.com

* [Breeze](http://github.com/stratisproject/Breeze): Breeze Wallet, the first full-block SPV bitcoin wallet 

* [BlockExplorer](http://github.com/stratisproject/BlockExplorer): A set of projects that can index and query stratis blockchains on the fullnode. 

* [BTCPay Server](http://github.com/btcpayserver/btcpayserver): A cross platform, self-hosted server compatible with Bitpay API 

* [NTumbleBit](http://github.com/NTumbleBit/NTumbleBit): TumbleBit Implementation in .NET Core 

* [BitPoker](http://github.com/bitcoinbrisbane/BitPoker): Decentralised peer to peer poker, using bitcoin http://www.bitpoker.io

* [Zen-Wallet](http://github.com/zenprotocol/zen-wallet): Node and GUI for the Zen Protocol. https://www.zenprotocol.com

* [Metaco-Trader](http://github.com/MetacoSA/Metaco-Trader): Bitcoin Wallet for advanced user based on a NBitcoin.Server 

* [Swarmops](http://github.com/Swarmops/Swarmops): Admin backend for any bitcoin-native or swarm organization http://sandbox.swarmops.com/

* [Nako](http://github.com/CoinVault/Nako): A Bitcoin and Altcoin server api that indexes blockchain transactions and addresses 

* [NBXplorer](http://github.com/dgarage/NBXplorer): A minimalist UTXO tracker for HD Wallets with bitcoin based altcoin support 

* [UnitCurrency](http://github.com/unitcurrency/unitcurrency): UnitCoin - a hybrid scrypt PoW + PoS based cryptocurrency.

* [Openchain](http://github.com/openchain/openchain): Openchain node reference implementation. https://www.openchain.org/

* [BreezeProject](http://github.com/BreezeHub/BreezeProject): Breeze Masternode and Wallet with Breeze Privacy Protocol 

* [geewallet](https://gitlab.com/nblockchain/geewallet/): Non-custodial, minimalistic & pragmatist opensource crossplatform lightweight brainwallet to hold the most important cryptoassets in the same application with ease & peace of mind

* [blockcore](https://github.com/block-core/blockcore): Modular Bitcoin fullnode implementation. https://www.blockcore.net/

## Useful doc :

* **Ebook** [Programming The Blockchain in C#](https://www.gitbook.com/book/programmingblockchain/programmingblockchain/details)

* **NBitcoin Github** : [https://github.com/NicolasDorier/NBitcoin](https://github.com/NicolasDorier/NBitcoin "https://github.com/NicolasDorier/NBitcoin")

* **NBitcoin Nuget** : [https://www.nuget.org/packages/NBitcoin/](https://www.nuget.org/packages/NBitcoin/ "https://www.nuget.org/packages/NBitcoin/")

* **Intro**: [http://www.codeproject.com/Articles/768412/NBitcoin-The-most-complete-Bitcoin-port-Part-Crypt](http://www.codeproject.com/Articles/768412/NBitcoin-The-most-complete-Bitcoin-port-Part-Crypt)

* **Stealth Payment**, and **BIP38** : [http://www.codeproject.com/Articles/775226/NBitcoin-Cryptography-Part](http://www.codeproject.com/Articles/775226/NBitcoin-Cryptography-Part)

* **How to build transaction** : [http://www.codeproject.com/Articles/835098/NBitcoin-Build-Them-All](http://www.codeproject.com/Articles/835098/NBitcoin-Build-Them-All "http://www.codeproject.com/Articles/835098/NBitcoin-Build-Them-All")

* **Using the NBitcoin Indexer** : [http://www.codeproject.com/Articles/819567/NBitcoin-Indexer-A-scalable-and-fault-tolerant-blo](http://www.codeproject.com/Articles/819567/NBitcoin-Indexer-A-scalable-and-fault-tolerant-blo "http://www.codeproject.com/Articles/819567/NBitcoin-Indexer-A-scalable-and-fault-tolerant-blo")

* **How to Scan the blockchain** : [http://www.codeproject.com/Articles/784519/NBitcoin-How-to-scan-the-Blockchain](http://www.codeproject.com/Articles/784519/NBitcoin-How-to-scan-the-Blockchain "http://www.codeproject.com/Articles/784519/NBitcoin-How-to-scan-the-Blockchain") (You can dismiss the ScanState for that, now I concentrate on the indexer)

Please, use github issues for questions or feedback. For confidential requests or specific demands, contact us on [Metaco support](mailto:support@metaco.com "support@metaco.com").


## Useful link for a free IDE :
Visual Studio Community Edition : [https://www.visualstudio.com/products/visual-studio-community-vs](https://www.visualstudio.com/products/visual-studio-community-vs "https://www.visualstudio.com/products/visual-studio-community-vs")
