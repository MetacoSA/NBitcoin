#nStratis

nStratis is a stratis blockchain library for the .NET platform. It is compatible with Open Assets and implements most relevant Bitcoin Improvement Proposals (BIPs). It provides also low level access to Stratis primitives so you can easily create your own smart contracts.


#How to use ?
With nuget :
>**Install-Package nStratis** 

Go on the [nuget website](https://www.nuget.org/packages/nStratis/) for more information.

The packages supports:

* With full features, Windows Desktop applications, Mono Desktop applications, and plateform supported at [.NET Standard 1.3](https://docs.microsoft.com/en-us/dotnet/articles/standard/library) (.NET Core, Xamarin IOS, Xamarin Android, UWP).
* With limited features, plateform supported at [.NET Standard 1.1](https://docs.microsoft.com/en-us/dotnet/articles/standard/library) (Windows Phone, Windows 8.0 apps).

##Description
nStratis notably includes:

* A [TransactionBuilder](http://www.codeproject.com/Articles/835098/NBitcoin-Build-Them-All) supporting Stealth, Open Asset, and all standard transactions
* Full script evaluation and parsing
* A RPC Client
* A SPV Wallet implementation [with sample](https://github.com/NicolasDorier/NBitcoin.SPVSample)
* The parsing of standard scripts and creation of custom ones
* The serialization of blocks, transactions and script
* The signing and verification with private keys (with support for compact signatures) for proving ownership
* Bloom filters and partial merkle trees
* Mnemonic code for generating deterministic keys ([BIP 39](https://github.com/bitcoin/bips/blob/master/bip-0039.mediawiki)), credits to [Thasshiznets](https://github.com/Thashiznets/BIP39.NET)
* Hierarchical Deterministic Wallets ([BIP 32](https://github.com/bitcoin/bips/blob/master/bip-0032.mediawiki))
* Payment Protocol ([BIP 70](https://github.com/bitcoin/bips/blob/master/bip-0070.mediawiki))
* Payment URLs ([BIP 21](https://github.com/bitcoin/bips/blob/master/bip-0021.mediawiki),[BIP 72](https://github.com/bitcoin/bips/blob/master/bip-0072.mediawiki))
* Two-Factor keys ([BIP 38](http://www.codeproject.com/Articles/775226/NBitcoin-Cryptography-Part))
* Stealth Addresses ([Also on codeproject](http://www.codeproject.com/Articles/775226/NBitcoin-Cryptography-Part))

nStratis is a port of NBitcoin https://github.com/MetacoSA/NBitcoin

nStratis license is MIT and we encourage you to use it to explore, learn, debug, play, share and create software for Stratis/Bitcoin.

##Useful link for a free IDE :
Visual Studio Community Edition : [https://www.visualstudio.com/products/visual-studio-community-vs](https://www.visualstudio.com/products/visual-studio-community-vs "https://www.visualstudio.com/products/visual-studio-community-vs")
