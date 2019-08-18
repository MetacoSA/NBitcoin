using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin
{
	public interface IHasForkId
	{
		uint ForkId
		{
			get;
		}
	}
}
