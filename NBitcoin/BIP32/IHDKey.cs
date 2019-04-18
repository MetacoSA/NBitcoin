using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin
{
	public interface IHDKey
	{
		IHDKey Derive(uint index);
		PubKey GetPublicKey();
	}
}
