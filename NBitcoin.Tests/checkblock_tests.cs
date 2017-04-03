using NBitcoin.DataEncoders;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class checkblock_tests
	{

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanCalculateMerkleRoot()
		{
			Block block = new Block();
			block.ReadWrite(Encoders.Hex.DecodeData(File.ReadAllText(@"data\block169482.txt")));
			Assert.Equal(block.Header.HashMerkleRoot, block.GetMerkleRoot().Hash);
		}

		private Block read_block(string blockName)
		{
			var file = "Data/" + blockName;
			if(File.Exists(file))
			{
				Block b = new Block();
				b.ReadWrite(File.ReadAllBytes(file)); // skip msgheader/size
				return b;
			}
			else
			{
				WebClient client = new WebClient();
				client.DownloadFile("http://webbtc.com/block/0000000000000024b58eeb1134432f00497a6a860412996e7a260f47126eed07.bin", file);
				return read_block(blockName);
			}
		}
	}
}
