using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace NBitcoin.Bench
{
	public class BlockSample
	{
		public byte[] BigBlockBytes;
		public Block BigBlock;
		public void Download()
		{
			HttpClient client = new HttpClient();
			BigBlockBytes = client.GetAsync("http://api.qbit.ninja/blocks/0000000000000000000490c3088acf1277355aa7ab49038ad40947067ff7afa1?format=raw")
				.GetAwaiter().GetResult()
				.Content.ReadAsByteArrayAsync()
				.GetAwaiter().GetResult();
			BigBlock = Block.Load(BigBlockBytes, Network.Main);
		}
	}
}
