using NBitcoin.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class MnemonicReference_tests
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanCreateBrainAddress()
		{
			var repo = new NoSqlBlockRepository();
			var chain = new ConcurrentChain();

			Block b = new Block();
			b.Transactions.Add(new Transaction());
			b.Transactions.Add(new Transaction()
			{
				Outputs =
				{
					new TxOut(),
					new TxOut(Money.Zero,BitcoinAddress.Create("15sYbVpRh6dyWycZMwPdxJWD4xbfxReeHe"))
				}
			});
			b.UpdateMerkleRoot();
			repo.PutAsync(b).Wait();
			chain.SetTip(b.Header);


			MnemonicReference address = MnemonicReference.CreateAsync(chain, repo, 0, 1, 1).Result;
			MnemonicReference address2 = MnemonicReference.ParseAsync(chain, repo, Wordlist.English, address.ToString(Wordlist.English)).Result;
			Assert.Equal(address.ToString(), address2.ToString());


			chain = new ConcurrentChain(Network.Main);
			var block = Network.Main.GetGenesis();
			var mnemo = MnemonicReference.Create(chain, block.Transactions[0], block, 0);

		}

#if !NOSOCKET
		[Fact]
		[Trait("MainNet", "MainNet")]
		public void CanCreateBrainAddressFromNetwork()
		{
			using(var node = Node.ConnectToLocal(Network.Main))
			{
				node.VersionHandshake();
				using(var listener = node.CreateListener())
				{
					node.SendMessage(new GetDataPayload(new InventoryVector(InventoryType.MSG_BLOCK, new uint256(" 00000000000000001790ced14940f5dc4a61cec2547b78f6dbfc1ebcbf1192d9 "))));
					var payload = listener.ReceivePayload<BlockPayload>();
					var block = payload.Object;
					var tx = block.Transactions.First(t => t.GetHash() == new uint256("4a85f6cc29aca334c1a78c5db74b492b741e67958aee59ff827c4c0862f4fbc1"));
				//http://www.xbt.hk/cgi-bin/ma1.pl?txid=4a85f6cc29aca334c1a78c5db74b492b741e67958aee59ff827c4c0862f4fbc1&txo=2&mincs=20
					var chain = node.GetChain();
					var result = MnemonicReference.Create(chain, tx, block, 1);
				}
			}
		}
#endif
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanCreateBrainAddress2()
		{
			Test(1782, 123, 1000, 1, 2);
		}

		class MockChain : ChainBase
		{
			public void Return(BlockHeader header, int height)
			{
				_Return = new ChainedBlock(header, height);
			}
			ChainedBlock _Return;
			public override ChainedBlock GetBlock(uint256 id)
			{
				return _Return;
			}

			public override ChainedBlock GetBlock(int height)
			{
				return _Return;
			}

			public override ChainedBlock Tip
			{
				get
				{
					throw new NotImplementedException();
				}
			}

			public override int Height
			{
				get
				{
					throw new NotImplementedException();
				}
			}

			protected override IEnumerable<ChainedBlock> EnumerateFromStart()
			{
				throw new NotImplementedException();
			}

			public override ChainedBlock SetTip(ChainedBlock pindex)
			{
				throw new NotImplementedException();
			}
		}
		private void Test(int blockHeight, int txIndex, int txCount, int txOutIndex, int txOutCount)
		{
			var repo = new NoSqlBlockRepository();
			var chain = new MockChain();
			var block = new Block();
			Transaction relevantTx = null;

			for(int i = 0 ; i < txCount ; i++)
			{
				var tx = block.AddTransaction(new Transaction());
				if(i == txIndex)
				{
					relevantTx = tx;
					for(int ii = 0 ; ii < txOutCount ; ii++)
					{
						var txout = tx.AddOutput(new TxOut());
						if(ii == txOutIndex)
							txout.Value = Money.Coins(1.0m);
					}
				}
			}
			block.UpdateMerkleRoot();
			chain.Return(block.Header, blockHeight);

			repo.PutAsync(block).Wait();


			var address = MnemonicReference.CreateAsync(chain, repo, blockHeight, txIndex, txOutIndex).Result;
			var address2 = MnemonicReference.ParseAsync(chain, repo, Wordlist.English, address.ToString()).Result;
			Assert.Equal(address.ToString(), address2.ToString());
			Assert.Equal(Money.Coins(1.0m), address.Output.Value);
			Assert.Equal(Money.Coins(1.0m), address2.Output.Value);

			var merkleBlock = block.Filter(relevantTx.GetHash());
			var address3 = MnemonicReference.Parse(chain, Wordlist.English, address.ToString(), relevantTx, merkleBlock);
			Assert.Equal(address.ToString(), address3.ToString());
		}
	}
}
