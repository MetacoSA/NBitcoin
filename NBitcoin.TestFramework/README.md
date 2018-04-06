# NBitcoin.TestFramework
A test framework for easily create bitcoind instances in integration tests.

Example:
```
using(var builder = NodeBuilder.Create())
{
	var node1 = builder.CreateNode();
	var node2 = builder.CreateNode();
	builder.StartAll();
	node2.Generate(50);
	node2.Sync(node1);

	var bestblock1 = node1.CreateRPCClient().GetBestBlockHash();
	var bestblock2 = node2.CreateRPCClient().GetBestBlockHash();
	Assert.Equal(bestblock1, bestblock2);
}
```

Available on [Nuget](https://www.nuget.org/packages/NBitcoin.TestFramework/).

```
Install-Package NBitcoin.TestFramework
```

First run might take times as Bitcoin Core is downloaded automatically.
