﻿using BenchmarkDotNet.Attributes;
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
		[GlobalSetup]
		public void Setup()
		{
			Value = RandomUtils.GetUInt256();
			Bytes = Value.ToBytes();
			EmptyArray = new byte[32];
		}
		[Benchmark]
		public void Read()
		{
			new uint256(Bytes);
		}
		[Benchmark]
		public void WriteToArray()
		{
			Value.ToBytes(EmptyArray);
		}
		[Benchmark]
		public void WriteToSpan()
		{
			Value.ToBytes(EmptyArray.AsSpan());
		}
	}
}
