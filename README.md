# NBitcoin

[![Join the chat at https://gitter.im/MetacoSA/NBitcoin](https://badges.gitter.im/MetacoSA/NBitcoin.svg)](https://gitter.im/MetacoSA/NBitcoin?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![Build status](https://ci.appveyor.com/api/projects/status/6xq0yg942jatra0x?svg=true)](https://ci.appveyor.com/project/NicolasDorier/nbitcoin)
<img src="http://segwit.co/static/public/images/logo.png" width="100"> [![NuGet](https://img.shields.io/nuget/v/NBitcoin.svg)](https://www.nuget.org/packages/NBitcoin)

NBitcoin is the most complete Bitcoin library for the .NET platform. It implements all most relevant Bitcoin Improvement Proposals (BIPs). It also provides low level access to Bitcoin primitives so you can easily build your application on top of it. Join us in our [gitter chat room](https://gitter.im/MetacoSA/NBitcoin).
It works on Windows, Mac and Linux with Xamarin, Unity, .NET Core or CLR. (Porting to Unity should not be that hard if you need it)

The best documentation available is our [eBook](https://programmingblockchain.gitbooks.io/programmingblockchain/content/), and the excellent unit tests. There are also some more resources below.

# How to use ?
With NuGet :
>**Install-Package NBitcoin** 

Go on the [NuGet website](https://www.nuget.org/packages/NBitcoin/) for more information.

The packages supports:

* With full features: Windows Desktop applications, Mono Desktop applications and platforms supported by [.NET Standard 1.3](https://docs.microsoft.com/en-us/dotnet/articles/standard/library) (.NET Core, Xamarin IOS, Xamarin Android, UWP and more).
* With limited features: Platforms supported by [.NET Standard 1.1](https://docs.microsoft.com/en-us/dotnet/articles/standard/library) (Windows Phone, Windows 8.0 apps).

To compile it by yourself, you can git clone, open the project and hit the compile button in Visual Studio.
How to get started ? Check out this article [on CodeProject](http://www.codeproject.com/Articles/768412/NBitcoin-The-most-complete-Bitcoin-port-Part-Crypt) for some basic Bitcoin operations, or [this Introduction to NBitcoin video](https://www.youtube.com/watch?v=X4ZwRWIF49w).

# How to use with Litecoin ?

>**Install-Package NBitcoin.Litecoin** 

This is a light weight package with corresponding `Network` instances for Litecoin mainnet and testnet. [Sources](https://github.com/MetacoSA/NBitcoin.Litecoin)

# How to use with my own blockchain?

 If your cryptocurrency is similar enough to Bitcoin, please check out how we integrated [litecoin to NBitcoin](https://github.com/MetacoSA/NBitcoin.Litecoin).

# How to use in Unity 3.5

In command prompt:

```
git clone https://github.com/MetacoSA/NBitcoin/
cd NBitcoin
git checkout unity35
build-unity.bat
```

Then put the two libraries NBitcoin.dll and System.Threading.Tasks.Net35.dll found in "NBitcoin\NBitcoin\bin\Release" into your asset folder.

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
* A SPV Wallet implementation [with sample](https://github.com/NicolasDorier/NBitcoin.SPVSample)
* The parsing of standard scripts and creation of custom ones
* The serialization of blocks, transactions and scripts
* The signing and verification with private keys (with support for compact signatures) for proving ownership
* Bloom filters and partial merkle trees
* Segregated Witness ([BIP 141](https://github.com/bitcoin/bips/blob/master/bip-0141.mediawiki), [BIP 143](https://github.com/bitcoin/bips/blob/master/bip-0143.mediawiki), [BIP 144](https://github.com/bitcoin/bips/blob/master/bip-0144.mediawiki))
* Bech32 segwit address implementation with error detection [BIP 173](https://github.com/bitcoin/bips/blob/master/bip-0173.mediawiki)
* Mnemonic code for generating deterministic keys ([BIP 39](https://github.com/bitcoin/bips/blob/master/bip-0039.mediawiki)), credits to [Thasshiznets](https://github.com/Thashiznets/BIP39.NET)
* Hierarchical Deterministic Wallets ([BIP 32](https://github.com/bitcoin/bips/blob/master/bip-0032.mediawiki))
* Payment Protocol ([BIP 70](https://github.com/bitcoin/bips/blob/master/bip-0070.mediawiki))
* Payment URLs ([BIP 21](https://github.com/bitcoin/bips/blob/master/bip-0021.mediawiki),[BIP 72](https://github.com/bitcoin/bips/blob/master/bip-0072.mediawiki))
* Two-Factor keys ([BIP 38](http://www.codeproject.com/Articles/775226/NBitcoin-Cryptography-Part))
* Stealth Addresses ([Also on codeproject](http://www.codeproject.com/Articles/775226/NBitcoin-Cryptography-Part))

NBitcoin is inspired by Bitcoin Core code but provides a simpler object oriented API (e.g., new Key().PubKey.Address.ToString() to generate a key and get the associated address). It relies on the BouncyCastle cryptography library instead of OpenSSL, yet replicates OpenSSL bugs to guarantee compatibility. NBitcoin also ports the integrality of Bitcoin Core unit tests with their original data in order to validate the compatibility of the two implementations.

NBitcoin is licensed under the [MIT License](https://opensource.org/licenses/MIT) and we encourage you to use it to explore, learn, debug, play, share and create software for Bitcoin and with other Metaco services.

## Useful doc :

* **Ebook** [Programming The Blockchain in C#](https://www.gitbook.com/book/programmingblockchain/programmingblockchain/details)

* **NBitcoin Github** : [https://github.com/NicolasDorier/NBitcoin](https://github.com/NicolasDorier/NBitcoin "https://github.com/NicolasDorier/NBitcoin")

* **NBitcoin Nuget** : [https://www.nuget.org/packages/NBitcoin/](https://www.nuget.org/packages/NBitcoin/ "https://www.nuget.org/packages/NBitcoin/")

* **Intro**: [http://www.codeproject.com/Articles/768412/NBitcoin-The-most-complete-Bitcoin-port-Part-Crypt](http://www.codeproject.com/Articles/768412/NBitcoin-The-most-complete-Bitcoin-port-Part-Crypt)

* **Stealth Payment**, and **BIP38** : [http://www.codeproject.com/Articles/775226/NBitcoin-Cryptography-Part](http://www.codeproject.com/Articles/775226/NBitcoin-Cryptography-Part)

* **How to build transaction** : [http://www.codeproject.com/Articles/835098/NBitcoin-Build-Them-All](http://www.codeproject.com/Articles/835098/NBitcoin-Build-Them-All "http://www.codeproject.com/Articles/835098/NBitcoin-Build-Them-All")

* **Using the NBitcoin Indexer** : [http://www.codeproject.com/Articles/819567/NBitcoin-Indexer-A-scalable-and-fault-tolerant-blo](http://www.codeproject.com/Articles/819567/NBitcoin-Indexer-A-scalable-and-fault-tolerant-blo "http://www.codeproject.com/Articles/819567/NBitcoin-Indexer-A-scalable-and-fault-tolerant-blo")

* **How to Scan the blockchain** : [http://www.codeproject.com/Articles/784519/NBitcoin-How-to-scan-the-Blockchain](http://www.codeproject.com/Articles/784519/NBitcoin-How-to-scan-the-Blockchain "http://www.codeproject.com/Articles/784519/NBitcoin-How-to-scan-the-Blockchain") (You can dismissthe ScanState for that, now I concentrate on the indexer)

Please, use github issues for questions or feedback. For confidential requests or specific demands, contact us on [Metaco support](mailto:support@metaco.com "support@metaco.com").


## Useful link for a free IDE :
Visual Studio Community Edition : [https://www.visualstudio.com/products/visual-studio-community-vs](https://www.visualstudio.com/products/visual-studio-community-vs "https://www.visualstudio.com/products/visual-studio-community-vs")
