using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.RPC
{
	public class RPCCapabilities
	{
		public int Version { get; set; }
		public bool SupportSignRawTransactionWith { get; set; }
		public bool SupportSegwit { get; set; }
		public bool SupportTaproot { get; set; }
		public bool SupportScanUTXOSet { get; set; }
		public bool SupportGetNetworkInfo { get; set; }
		public bool SupportEstimateSmartFee { get; set; }
		public bool SupportGenerateToAddress { get; set; }
		public bool SupportTestMempoolAccept { get; set; }

		public RPCCapabilities Clone(int newVersion)
		{
			return new RPCCapabilities()
			{
				Version = newVersion,
				SupportScanUTXOSet = SupportScanUTXOSet,
				SupportSegwit = SupportSegwit,
				SupportTaproot = SupportTaproot,
				SupportSignRawTransactionWith = SupportSignRawTransactionWith,
				SupportGetNetworkInfo = SupportGetNetworkInfo,
				SupportEstimateSmartFee = SupportEstimateSmartFee,
				SupportGenerateToAddress = SupportGenerateToAddress
			};
		}

		public override string ToString()
		{
			return $"Version: {Version}{Environment.NewLine}" +
				$"SupportScanUTXOSet: {SupportScanUTXOSet}{Environment.NewLine}" +
				$"SupportSegwit: {SupportSegwit}{Environment.NewLine}" +
				$"SupportTaproot: {SupportTaproot}{Environment.NewLine}" +
				$"SupportSignRawTransactionWith: {SupportSignRawTransactionWith}{Environment.NewLine}" +
				$"SupportGetNetworkInfo: {SupportGetNetworkInfo}{Environment.NewLine}" +
				$"SupportEstimateSmartFee: {SupportEstimateSmartFee}{Environment.NewLine}" +
				$"SupportGenerateToAddress: {SupportGenerateToAddress}{Environment.NewLine}";
		}
	}
}
