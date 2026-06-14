using BenchmarkDotNet.Attributes;
using System;
using System.IO;

namespace NBitcoin.Bench;

public class ConcurrentChainBench
{
	byte[] Bytes; 

	[GlobalSetup]
	public void Setup()
	{
		var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WalletWasabi", "Client", "BitcoinP2pNetwork", "BlockHeadersMain.dat");
		Bytes = File.ReadAllBytes(appDataPath);
	}

	[Benchmark]
	public void WriteToString()
	{
		var chain = new ConcurrentChain(Bytes, Network.Main.Consensus.ConsensusFactory);
	}
}
