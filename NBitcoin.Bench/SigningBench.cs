using BenchmarkDotNet.Attributes;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.Bench
{
	[MemoryDiagnoser]
	public class SigningBench
	{
		Key key;
		PubKey pubkey;
		uint256 hash;
		ECDSASignature sig;
		[GlobalSetup]
		public void Setup()
		{
			key = new Key(Encoders.Hex.DecodeData("f9327ee0af0ec64a358c2a5435a474ee00c7ddae285b71157b453c63c8effbd7"));
			pubkey = key.PubKey;
			hash = new uint256(Encoders.Hex.DecodeData("f50b59252ed8ff895b769b07892d21733acffb5b3a2486957e6027459dd950a4"), false);
			sig = new ECDSASignature(Encoders.Hex.DecodeData("30440220022ba405180aee65f35e5276165b9b828d0737aadc394b59b33c7a28b86e04da02200b309a4782b085c66b39cf7f5341f2e88f20de05a1a9367c323517a1a3a37211"));
		}
		[Benchmark]
		public void Sign()
		{
			key.Sign(hash);
		}
		[Benchmark]
		public void Verify()
		{
			pubkey.Verify(hash, sig);
		}
	}
}
