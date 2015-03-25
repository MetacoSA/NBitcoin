NBitcoin
=======

#How to use ?
With nuget :
>Install-Package NBitcoin 

Go on the [nuget website](https://www.nuget.org/packages/NBitcoin/) for more information.

The packages supports the following Portable profile :

* net45
* portable-net45+win+wpa81+Xamarin.iOS10+MonoAndroid10+MonoTouch10
* portable-net45+win+wpa81+wp80+Xamarin.iOS10+MonoAndroid10+MonoTouch10
* portable-net45+MonoAndroid1

To complile it by yourself, you just have to git clone, open the project and hit the compile button on visual studio.
How to get started ? Check out this article [on CodeProject](http://www.codeproject.com/Articles/768412/NBitcoin-The-most-complete-Bitcoin-port-Part-Crypt) to do some basic crypto operations.

##What is it about ?
This is the most complete and faithful porting I know of bitcoin.

##Description
Why is it a big deal ? Because you can run it and debug into it without any linux-voodoo-setup to make bitcoin running.
Visual studio express for free, XUnit and you are up to go.

* A [TransactionBuilder](http://www.codeproject.com/Articles/835098/NBitcoin-Build-Them-All) supporting Stealth, Open Asset, and all standard transactions
* Full port of the test suite of bitcoin core with their own data
* Full script evaluation and parsing
* RPC Client
* Mnemonic code for generating deterministic keys ([BIP 39](https://github.com/bitcoin/bips/blob/master/bip-0039.mediawiki)), credits to [Thasshiznets](https://github.com/Thashiznets/BIP39.NET)
* Payment Protocol ([BIP 70](https://github.com/bitcoin/bips/blob/master/bip-0070.mediawiki))
* Payment URL ([BIP 21](https://github.com/bitcoin/bips/blob/master/bip-0021.mediawiki),[BIP 72](https://github.com/bitcoin/bips/blob/master/bip-0072.mediawiki))
* Two Factor keys ([BIP 38](http://www.codeproject.com/Articles/775226/NBitcoin-Cryptography-Part))
* Stealth Address ([Also on codeproject](http://www.codeproject.com/Articles/775226/NBitcoin-Cryptography-Part))
* Recognize standard script and permit to create them
* Object model faithful to the C++ API but with C# goodness
* Simpler API (here is how to generate a key and get the address : new Key().PubKey.Address.ToString())
* Bloom filter, partial merkle tree
* Serialization of Blocks, Transactions, Script
* Signing/verification with private keys, support compact signature for prooving ownership
* Hierarchical Deterministic Wallets ([BIP 32](https://github.com/bitcoin/bips/blob/master/bip-0032.mediawiki))


I ported directly from the C++, except the OpenSSL part where I'm using BouncyCaslte instead. (BitcoinJ helped me a lot on the implementation)
I also ported OpenSSL bugs (you can't believe how much time it took me) ;)

Mono.NAT is used to open port if you intent to use host a node,
SqLite is a database used.

Please, use the code to explore/learn/debug/play/sharing/create the licence is MIT, so you should be good to go.
This is the simple way and most complete way to see the internal of bitcoin without going to C++ madness.

## Useful doc :

 Ebook [Blockchain Programming in C#](https://aois.blob.core.windows.net/public/Blockchain Programming in CSharp.pdf)

NBitcoin Github : https://github.com/NicolasDorier/NBitcoin

NBitcoin Nuget : https://www.nuget.org/packages/NBitcoin/

Intro : http://www.codeproject.com/Articles/768412/NBitcoin-The-most-complete-Bitcoin-port-Part-Crypt

Stealth Payment, and BIP38 : http://www.codeproject.com/Articles/775226/NBitcoin-Cryptography-Part

How to build transaction : http://www.codeproject.com/Articles/835098/NBitcoin-Build-Them-All

Using the NBitcoin Indexer : http://www.codeproject.com/Articles/819567/NBitcoin-Indexer-A-scalable-and-fault-tolerant-blo

How to Scan the blockchain : http://www.codeproject.com/Articles/784519/NBitcoin-How-to-scan-the-Blockchain (You can dismissthe ScanState for that, now I concentrate on the indexer)

Bitcoin address : 15sYbVpRh6dyWycZMwPdxJWD4xbfxReeHe

if you want to contact me : http://nicolas-dorier.com/Contact will do the rest by email :)


##Useful link :
Visual studio express : http://www.visualstudio.com/en-us/products/visual-studio-express-vs.aspx
