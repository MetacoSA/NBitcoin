using System.Collections.Generic;
using FsCheck;
using NBitcoin;

namespace NBitcoin.Tests.Generators
{
	public class ChainParamsGenerator
	{

		public static Arbitrary<Network> NetworkArb() =>
			Arb.From(NetworkGen());
		public static Gen<Network> NetworkGen() =>
			Gen.OneOf(new List<Gen<Network>> {
					Gen.Constant(Network.Main),
					Gen.Constant(Network.TestNet),
					Gen.Constant(Network.RegTest)
			});

		public static Gen<ChainName> NetworkType =>
			NetworkGen().Select(n => n.ChainName);
	}
}
