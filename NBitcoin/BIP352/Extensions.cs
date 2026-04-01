#if HAS_SPAN
using System;
using NBitcoin.DataEncoders;

namespace NBitcoin.BIP352
{

	public static class Extensions
	{
		public static SilentPaymentBech32Encoder GetSilentPaymentBech32Encoder(this Network network) =>
			new(Encoders.ASCII.DecodeData(GetHrpForNetwork(network)));

		private static string GetHrpForNetwork(Network network)
		{
			if (network == Network.Main)
			{
				return "sp";
			}

			if (network == Network.TestNet)
			{
				return "tsp";
			}

			if (network == Network.RegTest)
			{
				return "tprt";
			}

			throw new ArgumentException($"Network {network.Name} is not supported");
		}
	}
}
#endif
