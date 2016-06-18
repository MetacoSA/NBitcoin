using NBitcoin.Protocol;
using System;
using System.Collections.Generic;
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

		[Fact]
		[Trait("CoreBeta", "CoreBeta")]
		public void CanAskCmpctBlock()
		{
			var alice = new BitcoinSecret("KypycJyxP5yA4gSedEBRse5q5f8RwYKG8xi8z4SRe2rdaioL3YNc").PrivateKey;
			var satoshi = new BitcoinSecret("KypycJyxP5yA4gSedEBRse5q5f8RwYKG8xi8z4SRe2rdaioL3YNc").ToNetwork(Network.RegTest);
			using(var builder = NodeBuilder.Create(version: "C:\\Bitcoin\\bitcoind.exe"))
			{
				var now = new DateTimeOffset(2015, 07, 18, 0, 0, 0, TimeSpan.Zero);
				builder.ConfigParameters.Add("mocktime", Utils.DateTimeToUnixTime(now).ToString());
				var bitcoind = builder.CreateNode(true);
				bitcoind.SetMinerSecret(satoshi);
				bitcoind.MockTime = now;
				var rpc = bitcoind.CreateRPCClient();

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
					var getdata = listener.ReceivePayload<GetDataPayload>();
					
					Assert.True(getdata.Inventory[0].Type == InventoryType.MSG_CMPCT_BLOCK);
					client1.SendMessage(new CmpctBlockPayload(blocks[0]));
					var cmpct = listener.ReceivePayload<SendCmpctPayload>();
					Assert.True(cmpct.PreferHeaderAndIDs);
				}
			}
		}
	}
}
