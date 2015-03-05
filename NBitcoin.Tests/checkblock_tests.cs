using NBitcoin.DataEncoders;
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
		[Trait("Core", "Core")]
		public void May15()
		{
			// Putting a 1MB binary file in the git repository is not a great
			// idea, so this test is only run if you manually download
			// test/data/Mar12Fork.dat from
			// http://sourceforge.net/projects/bitcoin/files/Bitcoin/blockchain/Mar12Fork.dat/download
			var tMay15 = Utils.UnixTimeToDateTime(1368576000);
			ValidationState state = Network.Main.CreateValidationState();
			state.CheckProofOfWork = false;
			state.Now = tMay15; // Test as if it was right at May 15

			Block forkingBlock = read_block("Mar12Fork.dat");

			// After May 15'th, big blocks are OK:
			forkingBlock.Header.BlockTime = tMay15; // Invalidates PoW
			Assert.True(state.CheckBlock(forkingBlock));

		}

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
				b.ReadWrite(File.ReadAllBytes(file), 8); // skip msgheader/size
				return b;
			}
			else
			{
				WebClient client = new WebClient();
				client.DownloadFile("http://heanet.dl.sourceforge.net/project/bitcoin/Bitcoin/blockchain/" + blockName, file);
				return read_block(blockName);
			}
		}
	}
}
