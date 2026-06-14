using BenchmarkDotNet.Attributes;
using NBitcoin.DataEncoders;
using System;

namespace NBitcoin.Bench;

[MemoryDiagnoser]
public class HexBench
{
	readonly string str = "d54994ece1d11b19785c7248868696250ab195605b469632b7bd68130e880c9a";
	readonly string uint256_str = "00000000ffffffffffffffffffffffffffffffffffffffffffffffffffffffff";
	private byte[] bytes;

	[GlobalSetup]
	public void Setup()
	{
		bytes = Encoders.Hex.DecodeData(str);
	}

	[Benchmark]
	public void DecodeData()
	{
		Encoders.Hex.DecodeData(str);
	}

	[Benchmark]
	public void DecodeDataSpan()
	{
		Span<byte> tmp = stackalloc byte[32];
		HexEncoder hex = (HexEncoder)Encoders.Hex;
		hex.DecodeData(uint256_str, tmp);
	}

	[Benchmark]
	public void EncodeData()
	{
		Encoders.Hex.EncodeData(bytes);
	}
}
