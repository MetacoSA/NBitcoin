using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NBitcoin.Altcoins.SmartCashInternals
{
	public class SmartCashKeccak
	{
		public byte[] ComputeHash(byte[] input)
		{
			byte[] hashResult = input;
			var keccak256 = HashX11.HashFactory.Crypto.SHA3.CreateKeccak256();
			hashResult = keccak256.ComputeBytes(hashResult).GetBytes();
			return hashResult.Take(32).ToArray();
		}
	}
}
