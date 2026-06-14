using BenchmarkDotNet.Attributes;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;

namespace NBitcoin.Bench;

[MemoryDiagnoser]
public class HashBench
{
	private byte[] bytes;

	[GlobalSetup]
	public void Setup()
	{
		bytes = Encoders.Hex.DecodeData("d54994ece1d11b19785c7248868696250ab195605b469632b7bd68130e880c9a");
	}

	[Benchmark]
	public void SHA1()
	{
		Hashes.SHA1(bytes, 0, bytes.Length);
	}

	[Benchmark]
	public void SHA256()
	{
		Hashes.SHA256(bytes);
	}

	[Benchmark]
	public void SHA512()
	{
		Hashes.SHA512(bytes);
	}
}
