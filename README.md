NBitcoin
=======

How to use ?
With nuget https://www.nuget.org/packages/NBitcoin/

It is the most complete and faithful porting I know of bitcoin.

Why is it a big deal ? Because you can run it and debug into it without any linux-voodoo-setup to make it run.
Visual studio express for free, XUnit and you are up to go.

[list]
[li]Full port of the test suite of bitcoin core with their own data[/li]
[li]Full script evaluation and parsing[/li]
[li]Recognize standard script and permit to create them[/li]
[li]Object model faithful to the C++ API but with C# goodness[/li]
[li]Simpler API (here is how to generate a key and get the address : new Key().PubKey.Address.ToString())[/li]
[li]Bloom filter, partial merkle tree[/li]
[li]Serialization of Blocks, Transactions, Script[/li]
[li]Signing/verification with private keys, support compact signature for prooving ownership[/li]
[/list]

This week, I will implement BIP 32, which should permit a third party to generate public key without knowing the private key. (Useful for payment server since you would not care if it become hacked)
Then the RPC client part. Which I need your help because I don't have any satoshi myself, so I can't emit transactions for testing. If you like my work, submit the testing satoshis to 15sYbVpRh6dyWycZMwPdxJWD4xbfxReeHe ;)

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

Useful link :
Visual studio express : http://www.visualstudio.com/en-us/products/visual-studio-express-vs.aspx
XUnit runner (only if using vs express) : http://xunit.codeplex.com/releases