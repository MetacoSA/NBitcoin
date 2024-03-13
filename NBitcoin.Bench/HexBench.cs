using BenchmarkDotNet.Attributes;
using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Bench
{
	public class HexBench
	{
		string str = "d54994ece1d11b19785c7248868696250ab195605b469632b7bd68130e880c9a";
		private byte[] bytes;

		[GlobalSetup]
		public void Setup()
		{
			bytes = Encoders.Hex.DecodeData(str);
		}
		[Benchmark]
		public void Decode()
		{
			Encoders.Hex.DecodeData(str);
		}
		[Benchmark]
		public void Encode()
		{
			Encoders.Hex.EncodeData(bytes);
		}
	}
}
