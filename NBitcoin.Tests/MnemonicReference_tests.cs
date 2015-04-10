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
					node.SendMessage(new GetDataPayload(new InventoryVector(InventoryType.MSG_BLOCK, new uint256(" 000000000000000001d6ec8218c6fdb1a757855238543e05def13a363b8ff95e"))));
					var payload = listener.ReceivePayload<BlockPayload>();
					var block = payload.Object;
					var tx = block.Transactions.First(t => t.GetHash() == new uint256("d1bc46420e21e0f7b059c04a851f3558669c67ea0dd1441836abc37413e1857d"));
					//http://www.xbt.hk/cgi-bin/ma1.pl?txid=4a85f6cc29aca334c1a78c5db74b492b741e67958aee59ff827c4c0862f4fbc1&txo=2&mincs=20
					//http://www.xbt.hk/cgi-bin/ma1.pl?txid=e05e5f4c81fd63eb92b3a4ee963c06176a0db3da092ee357be668e4f0ae68333&txo=5&mincs=20
					//http://www.xbt.hk/cgi-bin/ma1.pl?txid=d1bc46420e21e0f7b059c04a851f3558669c67ea0dd1441836abc37413e1857d&txo=1&mincs=20
					//http://www.xbt.hk/cgi-bin/ma1.pl?txid=0e3e2357e806b6cdb1f70b54c3a3a17b6714ee1f0e68bebb44a74b1efd512098&txo=1&mincs=20
					var chain = node.GetChain();
					var result = MnemonicReference.Create(chain, tx, block, 0);
					var result2 = MnemonicReference.Parse(chain, Wordlist.English, result.ToString(), tx, block);
					Assert.Equal(result.ToString(), result2.ToString());
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
