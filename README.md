NBitcoin
=======

#How to use ?
With nuget :
>Install-Package NBitcoin 

Go on the [nuget website](https://www.nuget.org/packages/NBitcoin/) for more information.

To complile it by yourself, you just have to git clone, open the project and hit the compile button on visual studio.
How to get started ? Check out this article [on CodeProject](http://www.codeproject.com/Articles/768412/NBitcoin-The-most-complete-Bitcoin-port-Part-Crypt) to do some basic crypto operations.

##What is it about ?
This is the most complete and faithful porting I know of bitcoin.

##Description
Why is it a big deal ? Because you can run it and debug into it without any linux-voodoo-setup to make bitcoin running.
Visual studio express for free, XUnit and you are up to go.


* Full port of the test suite of bitcoin core with their own data
* Full script evaluation and parsing
* Two Factor keys ([BIP 38](http://www.codeproject.com/Articles/775226/NBitcoin-Cryptography-Part))
* Stealth Address ([Also on codeproject](http://www.codeproject.com/Articles/775226/NBitcoin-Cryptography-Part))
* Recognize standard script and permit to create them
* Object model faithful to the C++ API but with C# goodness
* Simpler API (here is how to generate a key and get the address : new Key().PubKey.Address.ToString())
* Bloom filter, partial merkle tree
* Serialization of Blocks, Transactions, Script
* Signing/verification with private keys, support compact signature for prooving ownership
* Hierarchical Deterministic Wallets ([BIP 32](https://github.com/bitcoin/bips/blob/master/bip-0032.mediawiki))


The RPC client is not yet done.
A basic implementation of a Node server and client using the RAW protocol is done. (NodeServer)

Public classes are clean and easy to use, but the implementation is a little messy due to the fact that I'm using C# convention and C++ and the same time. (I will clean that up after the RPC client implementation)

I ported directly from the C++, except the OpenSSL part where I'm using BouncyCaslte instead. (BitcoinJ helped me a lot on the implementation)
I also ported OpenSSL bugs (you can't believe how much time it took me) ;)

Mono.NAT is used to open port if you intent to use host a node,
SqLite is a database used.

Please, use the code to explore/learn/debug/play/sharing/create the licence is LGPL v3, so you should be good to go.
This is the simple way and most complete way to see the internal of bitcoin without going to C++ madness.

With no so much work, it should be Mono compliant. I don't have a lot of dependency on the Windows.


Info :
github : https://github.com/NicolasDorier/NBitcoin
bitcoin address : 15sYbVpRh6dyWycZMwPdxJWD4xbfxReeHe
if you want to contact me : http://nicolas-dorier.com/Contact will do the rest by email :)

Useful link :
Visual studio express : http://www.visualstudio.com/en-us/products/visual-studio-express-vs.aspx
XUnit runner (only if using vs express) : http://xunit.codeplex.com/releases
