using NBitcoin.Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class cmpctblock_tests
	{
		[Fact]
		[Trait("CoreBeta", "CoreBeta")]
		public void CanRoundtripCmpctBlock()
		{
			Block block = new Block();
			block.Transactions.Add(new Transaction());
			var cmpct = new CmpctBlockPayload(block);
			cmpct.Clone();
		}

		// todo: revisit this test when fixing the node tests
		//[Fact]
		//[Trait("CoreBeta", "CoreBeta")]
		public void CanAskCmpctBlock()
		{
			var alice = new BitcoinSecret("KypycJyxP5yA4gSedEBRse5q5f8RwYKG8xi8z4SRe2rdaioL3YNc").PrivateKey;
			var satoshi = new BitcoinSecret("KypycJyxP5yA4gSedEBRse5q5f8RwYKG8xi8z4SRe2rdaioL3YNc").ToNetwork(Network.RegTest);
			using(var builder = NodeBuilder.Create(version: "C:\\Bitcoin\\bitcoind.exe"))
			{
				var now = new DateTimeOffset(2015, 07, 18, 0, 0, 0, TimeSpan.Zero);
				builder.ConfigParameters.Add("mocktime", Utils.DateTimeToUnixTime(now).ToString());
				var bitcoind = builder.CreateNode(false);
				var bitcoind2 = builder.CreateNode(false);
				builder.StartAll();
				
				bitcoind.SetMinerSecret(satoshi);
				bitcoind.MockTime = now;
				
				bitcoind2.SetMinerSecret(satoshi);
				bitcoind2.MockTime = now;

				var rpc = bitcoind.CreateRPCClient();
				rpc.AddNode(bitcoind2.Endpoint, true);

				var client1 = bitcoind.CreateNodeClient(new NodeConnectionParameters()
				{
					Version = ProtocolVersion.SHORT_IDS_BLOCKS_VERSION
				});
				using(var listener = client1.CreateListener())
				{
					client1.VersionHandshake();
					var sendCmpct = listener.ReceivePayload<SendCmpctPayload>();
					Assert.Equal(1U, sendCmpct.Version);
					Assert.Equal(false, sendCmpct.PreferHeaderAndIDs);

					//Announcement
					client1.SendMessage(new SendCmpctPayload(false));
					bitcoind.Generate(1);
					var inv = listener.ReceivePayload<InvPayload>();
					Assert.True(inv.First().Type == InventoryType.MSG_BLOCK);

					//Request block
					inv.First().Type = InventoryType.MSG_CMPCT_BLOCK;
					client1.SendMessage(new GetDataPayload(inv.First()));
					var blk = listener.ReceivePayload<CmpctBlockPayload>();

					//Request transaction
					var getTxn = new GetBlockTxnPayload();
					getTxn.BlockId = blk.Header.GetHash();
					getTxn.Indices.Add(0);
					client1.SendMessage(getTxn);
					var blockTxn = listener.ReceivePayload<BlockTxnPayload>();
					Assert.True(blockTxn.BlockId == blk.Header.GetHash());
					Assert.True(blockTxn.Transactions[0].GetHash() == blk.PrefilledTransactions[0].Transaction.GetHash());

					bitcoind.Generate(100);
					var tx = bitcoind.GiveMoney(alice.ScriptPubKey, Money.Coins(1), false);
					var lastBlk = bitcoind.Generate(1)[0];

					while(true)
					{
						var invv = listener.ReceivePayload<InvPayload>().First();
						if(invv.Hash == lastBlk.GetHash())
						{
							invv.Type = InventoryType.MSG_CMPCT_BLOCK;
							client1.SendMessage(new GetDataPayload(invv));
							break;
						}
					}

					blk = listener.ReceivePayload<CmpctBlockPayload>();
					Assert.Equal(1, blk.ShortIds.Count);
					Assert.Equal(blk.ShortIds[0], blk.GetShortID(tx.GetHash()));

					//Let the node know which is the last block that we know
					client1.SendMessage(new InvPayload(new InventoryVector(InventoryType.MSG_BLOCK, blk.Header.GetHash())));
					bitcoind.Generate(1);
					inv = listener.ReceivePayload<InvPayload>();
					inv.First().Type = InventoryType.MSG_CMPCT_BLOCK;
					client1.SendMessage(new GetDataPayload(inv.First()));
					blk = listener.ReceivePayload<CmpctBlockPayload>();

					//Prefer being notified with cmpctblock
					client1.SendMessage(new SendCmpctPayload(true));
					//Let the node know which is the last block that we know
					client1.SendMessage(new InvPayload(new InventoryVector(InventoryType.MSG_BLOCK, blk.Header.GetHash())));
					bitcoind.Generate(1);
					blk = listener.ReceivePayload<CmpctBlockPayload>();

					//The node ask to connect to use in high bandwidth mode
					var blocks = bitcoind.Generate(1, broadcast: false);
					client1.SendMessage(new HeadersPayload(blocks[0].Header));		
					var cmpct = listener.ReceivePayload<SendCmpctPayload>(); //Should become one of the three high bandwidth node
					Assert.True(cmpct.PreferHeaderAndIDs);
					var getdata = listener.ReceivePayload<GetDataPayload>();					
					Assert.True(getdata.Inventory[0].Type == InventoryType.MSG_CMPCT_BLOCK);
					client1.SendMessage(new CmpctBlockPayload(blocks[0]));					

					//Should be able to get a compact block with Inv
					blocks = bitcoind.Generate(1, broadcast: false);
					client1.SendMessage(new InvPayload(blocks[0]));
					getdata = listener.ReceivePayload<GetDataPayload>();
					Assert.True(getdata.Inventory[0].Type == InventoryType.MSG_CMPCT_BLOCK);
					client1.SendMessage(new CmpctBlockPayload(blocks[0]));


					//Send as prefilled transaction 0 and 2
					var tx1 = bitcoind.GiveMoney(satoshi.ScriptPubKey, Money.Coins(1.0m), broadcast: false);
					var tx2 = bitcoind.GiveMoney(satoshi.ScriptPubKey, Money.Coins(2.0m), broadcast: false);
					var tx3 = bitcoind.GiveMoney(satoshi.ScriptPubKey, Money.Coins(3.0m), broadcast: false);
					blocks = bitcoind.Generate(1, broadcast: false);
					Assert.True(blocks[0].Transactions.Count == 4);
					var cmpctBlk = new CmpctBlockPayload();
					cmpctBlk.Nonce = RandomUtils.GetUInt64();
					cmpctBlk.Header = blocks[0].Header;
					cmpctBlk.PrefilledTransactions.Add(new PrefilledTransaction() { Index = 0, Transaction = blocks[0].Transactions[0] });
					cmpctBlk.PrefilledTransactions.Add(new PrefilledTransaction() { Index = 2, Transaction = blocks[0].Transactions[2] });
					cmpctBlk.AddTransactionShortId(blocks[0].Transactions[1]);
					cmpctBlk.AddTransactionShortId(blocks[0].Transactions[3]);
					client1.SendMessage(cmpctBlk);

					//Check that node ask for 1 and 3
					var gettxn = listener.ReceivePayload<GetBlockTxnPayload>();
					Assert.Equal(2, gettxn.Indices.Count);
					Assert.Equal(1, gettxn.Indices[0]);
					Assert.Equal(3, gettxn.Indices[1]);

					client1.SendMessage(new BlockTxnPayload()
					{
						BlockId = blocks[0].GetHash(),
						Transactions =
						{
							blocks[0].Transactions[1],
							blocks[0].Transactions[3],
						}
					});

					//Both nodes updated ?
					var chain1 = client1.GetChain();
					Assert.Equal(blocks[0].GetHash(), chain1.Tip.HashBlock);
					using(var client2 = bitcoind2.CreateNodeClient())
					{
						client2.VersionHandshake();
						var chain2 = client2.GetChain();
						Assert.Equal(chain1.Tip.HashBlock, chain2.Tip.HashBlock);
					}

					//Block with coinbase only
					blocks = bitcoind.Generate(1, broadcast: false);
					client1.SendMessage(new CmpctBlockPayload(blocks[0]));
					client1.SynchronizeChain(chain1);
					Assert.Equal(chain1.Tip.HashBlock, blocks[0].GetHash());
				}
			}
		}
	}
}
