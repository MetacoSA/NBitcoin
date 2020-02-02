using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.RPC
{
	public enum AddressType
	{
		Legacy,
		P2SHSegwit,
		Bech32
	}
	public class GetNewAddressRequest
	{
		public string Label
		{
			get; set;
		}

		public AddressType? AddressType
		{
			get; set;
		}
	}
}
