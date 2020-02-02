using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class pow_tests
	{

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public static void CanCalculatePowCorrectly()
		{
			ConcurrentChain chain = new ConcurrentChain(Network.Main);
			EnsureDownloaded("MainChain.dat", "https://aois.blob.core.windows.net/public/MainChain.dat");
			chain.Load(File.ReadAllBytes("MainChain.dat"), Network.Main);
			foreach (var block in chain.EnumerateAfter(chain.Genesis))
			{
				var thisWork = block.GetWorkRequired(Network.Main);
				var thisWork2 = block.Previous.GetNextWorkRequired(Network.Main);
				Assert.Equal(thisWork, thisWork2);
				Assert.True(block.CheckProofOfWorkAndTarget(Network.Main));
			}
		}

		private static void EnsureDownloaded(string file, string url)
		{
			if (File.Exists(file))
				return;
			HttpClient client = new HttpClient();
			client.Timeout = TimeSpan.FromMinutes(5);
			var data = client.GetByteArrayAsync(url).GetAwaiter().GetResult();
			File.WriteAllBytes(file, data);
		}
	}
}
