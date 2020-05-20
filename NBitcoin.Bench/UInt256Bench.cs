using BenchmarkDotNet.Attributes;
using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.Bench
{
	public class UInt256Bench
	{
		uint256 Value;
		byte[] Bytes;
		byte[] EmptyArray;
		string ValueStr;
		[GlobalSetup]
		public void Setup()
		{
			Value = RandomUtils.GetUInt256();
			ValueStr = Value.ToString();
			Bytes = Value.ToBytes();
			EmptyArray = new byte[32];
		}
		[Benchmark]
		public void WriteToString()
		{
			Value.ToString();
		}
		[Benchmark]
		public void Read()
		{
			new uint256(Bytes);
		}
		[Benchmark]
		public void IsValid()
		{
			((HexEncoder)Encoders.Hex).IsValid(ValueStr);
		}

		[Benchmark]
		public void ReadString()
		{
			new uint256(ValueStr);
		}
		[Benchmark]
		public void WriteToArray()
		{
			Value.ToBytes(EmptyArray);
		}
#if HAS_SPAN
		[Benchmark]
		public void WriteToSpan()
		{
			Value.ToBytes(EmptyArray.AsSpan());
		}
#endif
	}
}
