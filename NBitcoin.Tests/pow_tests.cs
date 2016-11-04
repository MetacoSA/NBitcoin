using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	using NBitcoin.BitcoinCore;

	public class pow_tests
	{

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public static void CanCalculatePowCorrectly()
		{
			var chain = new ConcurrentChain(File.ReadAllBytes(TestDataLocations.BlockHeadersLocation));

			foreach (var block in chain.EnumerateAfter(chain.Genesis))
			{
				Assert.True(block.CheckPowPosAndTarget(Network.Main));
			}
		}
	}
}
