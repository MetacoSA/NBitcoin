#NBitcoin

NBitcoin is the most complete Bitcoin library for the .NET platform. It is compatible with Open Assets and implements most relevant Bitcoin Improvement Proposals (BIPs). It provides also low level access to Bitcoin primitives so you can easily create your own smart contracts.


#How to use ?
With nuget :
>**Install-Package NBitcoin** 

Or if you are running on Mono (using mono develop) :
>**Install-Package NBitcoin.Mono**

Go on the [nuget website](https://www.nuget.org/packages/NBitcoin/) for more information.

The packages supports the following Portable profile :

* net45
* portable-net45+win+wpa81+Xamarin.iOS10+MonoAndroid10+MonoTouch10
* portable-net45+win+wpa81+wp80+Xamarin.iOS10+MonoAndroid10+MonoTouch10
* portable-net45+MonoAndroid1

To compile it by yourself, you can git clone, open the project and hit the compile button on visual studio.
How to get started ? Check out this article [on CodeProject](http://www.codeproject.com/Articles/768412/NBitcoin-The-most-complete-Bitcoin-port-Part-Crypt) for some basic Bitcoin operations.


##Description
NBitcoin notably includes:

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

NBitcoin is inspired by Bitcoin Core code but provides a simpler object oriented API (e.g., new Key().PubKey.Address.ToString() to generate a key and get the associated address). It relies on BountyCastle cryptography library instead of OpenSSL, yet replicates OpenSSL bugs to guarantee compatibility. NBitcoin also ports the integrality of Bitcoin Core unit tests with their original data in order to validate the compatibility of the two implementations.

NBitcoin license is MIT and we encourage you to use it to explore, learn, debug, play, share and create software for Bitcoin and with other Metaco services.

## Useful doc :

* **Ebook** [Blockchain Programming in C#](https://aois.blob.core.windows.net/public/Blockchain%20Programming%20in%20CSharp.pdf)

* **NBitcoin Github** : [https://github.com/NicolasDorier/NBitcoin](https://github.com/NicolasDorier/NBitcoin "https://github.com/NicolasDorier/NBitcoin")

* **NBitcoin Nuget** : [https://www.nuget.org/packages/NBitcoin/](https://www.nuget.org/packages/NBitcoin/ "https://www.nuget.org/packages/NBitcoin/")

* **Intro**: [http://www.codeproject.com/Articles/768412/NBitcoin-The-most-complete-Bitcoin-port-Part-Crypt](http://www.codeproject.com/Articles/768412/NBitcoin-The-most-complete-Bitcoin-port-Part-Crypt)

* **Stealth Payment**, and **BIP38** : [http://www.codeproject.com/Articles/775226/NBitcoin-Cryptography-Part](http://www.codeproject.com/Articles/775226/NBitcoin-Cryptography-Part)

* **How to build transaction** : [http://www.codeproject.com/Articles/835098/NBitcoin-Build-Them-All](http://www.codeproject.com/Articles/835098/NBitcoin-Build-Them-All "http://www.codeproject.com/Articles/835098/NBitcoin-Build-Them-All")

* **Using the NBitcoin Indexer** : [http://www.codeproject.com/Articles/819567/NBitcoin-Indexer-A-scalable-and-fault-tolerant-blo](http://www.codeproject.com/Articles/819567/NBitcoin-Indexer-A-scalable-and-fault-tolerant-blo "http://www.codeproject.com/Articles/819567/NBitcoin-Indexer-A-scalable-and-fault-tolerant-blo")

* **How to Scan the blockchain** : [http://www.codeproject.com/Articles/784519/NBitcoin-How-to-scan-the-Blockchain](http://www.codeproject.com/Articles/784519/NBitcoin-How-to-scan-the-Blockchain "http://www.codeproject.com/Articles/784519/NBitcoin-How-to-scan-the-Blockchain") (You can dismissthe ScanState for that, now I concentrate on the indexer)

Please, use github issues for questions or feedback. For confidential requests or specific demands, contact us on [Metaco support](mailto:support@metaco.com "support@metaco.com").


##Useful link :
Visual studio express : [http://www.visualstudio.com/en-us/products/visual-studio-express-vs.aspx](http://www.visualstudio.com/en-us/products/visual-studio-express-vs.aspx "http://www.visualstudio.com/en-us/products/visual-studio-express-vs.aspx")
