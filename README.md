NBitcoin
=======

#How to use ?
With nuget :
>Install-Package NBitcoin 

Go on the [nuget website](https://www.nuget.org/packages/NBitcoin/) for more information.

To complile it by yourself, you just have to git clone, open the project and hit the compile button on visual studio.

##What is it about ?
This is the most complete and faithful porting I know of bitcoin.

##Description
Why is it a big deal ? Because you can run it and debug into it without any linux-voodoo-setup to make bitcoin running.
Visual studio express for free, XUnit and you are up to go.


* Full port of the test suite of bitcoin core with their own data
* Full script evaluation and parsing
* Recognize standard script and permit to create them
* Object model faithful to the C++ API but with C# goodness
* Simpler API (here is how to generate a key and get the address : new Key().PubKey.Address.ToString())
* Bloom filter, partial merkle tree
* Serialization of Blocks, Transactions, Script
* Signing/verification with private keys, support compact signature for prooving ownership
* Hierarchical Deterministic Wallets ([BIP 32](https://github.com/bitcoin/bips/blob/master/bip-0032.mediawiki))

The RPC client is not yet done. I need your help because I don't have any satoshi myself, so I can't emit transactions for testing. If you like my work, submit the testing satoshis to 15sYbVpRh6dyWycZMwPdxJWD4xbfxReeHe I will use any left over for buying pizza and getting ride of my fiat currency. ;)

Public classes are clean and easy to use, but the implementation is a little messy due to the fact that I'm using C# convention and C++ and the same time. (I will clean that up after the RPC client implementation)

I ported directly from the C++, except the OpenSSL part where I'm using BouncyCaslte instead. (BitcoinJ helped me a lot on the implementation)
I also ported OpenSSL bugs (you can't believe how much time it took me) ;)

Please, use the code to explore/learn/debug/play/sharing/create the licence is LGPL v3, so you should be good to go.
This is the simple way and most complete way to see the internal of bitcoin without going to C++ madness.

With no so much work, it should be Mono compliant. I don't have a lot of dependency on the Windows.

If you like my work, send some satoshi I can crucify for the testing of the RPC client. ;)

Info :
github : https://github.com/NicolasDorier/NBitcoin
bitcoin address : 15sYbVpRh6dyWycZMwPdxJWD4xbfxReeHe
if you want to contact me : http://nicolas-dorier.com/Contact will do the rest by email :)

Useful link :
Visual studio express : http://www.visualstudio.com/en-us/products/visual-studio-express-vs.aspx
XUnit runner (only if using vs express) : http://xunit.codeplex.com/releases
