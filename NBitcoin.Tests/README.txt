To run test tests, download and install xUnit test runner for Visual studio on the following link :
http://visualstudiogallery.msdn.microsoft.com/463c5987-f82b-46c8-a97e-b1cde42b9099

You can then group the unit tests per "Trait" in the Test Explorer.
Here are the trait NBitcoin is using :

"Core" means I ported it directly from the bitcoind source, they have no dependency.
"UnitTest" means the test is self contained without dependency, but not ported from Bitcoin Core.
"Benchmark" are simple method you can use or modify to measure performance of NBitcoin, they have some file system dependencies.

The two following tests depends on a running local node by bitcoin-qt or bitcoind
"RPCClient" means the test call RPC server of bitcoind.
"NodeServer" means the test use the bitcoin protocol to interact with the local node.
Please run bitcoind or qt with the following command line:
"C:\Program Files\Bitcoin\daemon\bitcoind.exe" -server -testnet
Then read the error message that tells you the location where you need to create a Bitcoin.conf file with the following lines :
rpcuser=bitcoinrpc
rpcpassword=passwordyouwant

The test network dns seed does not always work, if so download
https://aois.blob.core.windows.net/public/peers.dat

And replace your BITCOIN_FOLDER\testnet3\peers.dat
Then wait for the sync to finish completely.
