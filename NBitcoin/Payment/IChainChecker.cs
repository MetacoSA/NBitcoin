﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Payment
{
	public interface IChainChecker
	{
		bool VerifyChain(byte[] certificate, byte[][] additionalCertificates);
	}
}
