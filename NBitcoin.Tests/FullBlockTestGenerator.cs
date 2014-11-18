using NBitcoin.Scanning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class FullBlockTest
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void RunFullBlockTest()
		{
			var generator = new FullBlockTestGenerator(Network.Main);

			ValidationState validation = new ValidationState(Network.Main);
			validation.CheckMerkleRoot = false;
			validation.CheckProofOfWork = false;

			var scan =
				new ScanState(new PubKeyHashScanner(generator.CoinbaseKey.PubKey.ID),
						new Chain(),
						new Account(),
						0);
			scan.CheckDoubleSpend = true;

			var mainChain = new Chain(Network.Main);
			var indexed = new IndexedBlockStore(new InMemoryNoSqlRepository(), CreateBlockStore());
			indexed.Put(Network.Main.GetGenesis());
			foreach(var test in generator.GetBlocksToTest(true, true).list.OfType<BlockAndValidity>())
			{
				indexed.Put(test.block);
				mainChain.TrySetTip(test.block.Header);
				Assert.True(scan.Process(mainChain, indexed) == test.connects);
				//if(!)
				//{
				//	Assert.True(test.throwsException);
				//}
				Assert.Equal(test.heightAfterBlock, scan.Chain.Height);
				Assert.Equal(test.hashChainTipAfterBlock, scan.Chain.Tip.HashBlock);
				mainChain.SetTip(scan.Chain.Tip);
			}
		}

		private BlockStore CreateBlockStore([CallerMemberName]string folder = null)
		{
			TestUtils.EnsureNew(folder);
			return new BlockStore(folder, Network.Main);
		}
	}
	/**
* Represents a block which is sent to the tested application and which the application must either reject or accept,
* depending on the flags in the rule
*/
	class BlockAndValidity : Rule
	{
		public Block block;
		public uint256 blockHash;
		public bool connects;
		public bool throwsException;
		public bool sendOnce; // We can throw away the memory for this block once we send it the first time (if bitcoind asks again, its broken)
		public uint256 hashChainTipAfterBlock;
		public int heightAfterBlock;

		public BlockAndValidity(Dictionary<uint256, int> blockToHeightMap, Dictionary<uint256, Block> hashHeaderMap, Block block,
								bool connects, bool throwsException, uint256 hashChainTipAfterBlock, int heightAfterBlock, String blockName)
			: base(blockName)
		{
			if(connects && throwsException)
				throw new InvalidOperationException("A block cannot connect if an exception was thrown while adding it.");
			this.block = block;
			this.blockHash = block.GetHash();
			this.connects = connects;
			this.throwsException = throwsException;
			this.hashChainTipAfterBlock = hashChainTipAfterBlock;
			this.heightAfterBlock = heightAfterBlock;

			// Keep track of the set of blocks indexed by hash
			hashHeaderMap.AddOrReplace(block.GetHash(), block.Clone());

			// Double-check that we are always marking any given block at the same height
			int height = 0;
			;
			if(blockToHeightMap.TryGetValue(hashChainTipAfterBlock, out height))
				Assert.True(height == heightAfterBlock);
			else
				blockToHeightMap.Add(hashChainTipAfterBlock, heightAfterBlock);
		}

		public BlockAndValidity setSendOnce(bool sendOnce)
		{
			this.sendOnce = sendOnce;
			return this;
		}
	}

	class RuleList
	{
		public List<Rule> list;
		public int maximumReorgBlockCount;
		Dictionary<uint256, Block> hashHeaderMap;
		public RuleList(List<Rule> list, Dictionary<uint256, Block> hashHeaderMap, int maximumReorgBlockCount)
		{
			this.list = list;
			this.hashHeaderMap = hashHeaderMap;
			this.maximumReorgBlockCount = maximumReorgBlockCount;
		}
	}
	/** An arbitrary rule which the testing client must match */
	class Rule
	{
		public String ruleName;
		internal Rule(String ruleName)
		{
			this.ruleName = ruleName;
		}
		public override string ToString()
		{
			return ruleName;
		}
	}
	class TransactionOutPointWithValue
	{
		public OutPoint outpoint;
		public Money value;
		public Script scriptPubKey;
		public TransactionOutPointWithValue(OutPoint outpoint, Money value, Script scriptPubKey)
		{
			this.outpoint = outpoint;
			this.value = value;
			this.scriptPubKey = scriptPubKey;
		}
	}

	//https://github.com/TheBlueMatt/test-scripts/blob/master/FullBlockTestGenerator.java
	public class FullBlockTestGenerator
	{
		private readonly Network _Network;
		private Key coinbaseOutKey;
		private PubKey coinbaseOutKeyPubKey;

		public Key CoinbaseKey
		{
			get
			{
				return coinbaseOutKey;
			}
		}

		// Used to double-check that we are always using the right next-height
		private Dictionary<uint256, int> blockToHeightMap = new Dictionary<uint256, int>();

		private Dictionary<uint256, Block> hashHeaderMap = new Dictionary<uint256, Block>();

		public Network Network
		{
			get
			{
				return _Network;
			}
		}
		public FullBlockTestGenerator(Network network)
		{
			_Network = network;
			coinbaseOutKey = new Key();
			coinbaseOutKeyPubKey = coinbaseOutKey.PubKey;
			//Utils.rollMockClock(0); // Set a mock clock for timestamp tests
		}

		internal RuleList GetBlocksToTest(bool addSigExpensiveBlocks, bool runLargeReorgs)
		{
			List<Rule> blocks = new List<Rule>();
			RuleList ret = new RuleList(blocks, hashHeaderMap, 10);

			Queue<TransactionOutPointWithValue> spendableOutputs = new Queue<TransactionOutPointWithValue>();

			int chainHeadHeight = 1;
			Block chainHead = Network.GetGenesis().CreateNextBlockWithCoinbase(coinbaseOutKeyPubKey, Money.Parse("50"));
			blocks.Add(new BlockAndValidity(blockToHeightMap, hashHeaderMap, chainHead, true, false, chainHead.GetHash(), 1, "Initial Block"));

			spendableOutputs.Enqueue(new TransactionOutPointWithValue(
				new OutPoint(chainHead.Transactions[0].GetHash(), 0),
				Money.Parse("50"), chainHead.Transactions[0].Outputs[0].ScriptPubKey));

			for(int i = 1 ; i < Network.SpendableCoinbaseDepth ; i++)
			{
				chainHead = chainHead.CreateNextBlockWithCoinbase(coinbaseOutKeyPubKey, Money.Parse("50"));
				chainHeadHeight++;
				blocks.Add(new BlockAndValidity(blockToHeightMap, hashHeaderMap, chainHead, true, false, chainHead.GetHash(), i + 1, "Initial Block chain output generation"));
				spendableOutputs.Enqueue(new TransactionOutPointWithValue(
						new OutPoint(chainHead.Transactions[0].GetHash(), 0),
						Money.Parse("50"), chainHead.Transactions.First().Outputs.First().ScriptPubKey));
			}

			// Start by building a couple of blocks on top of the genesis block.
			Block b1 = createNextBlock(chainHead, chainHeadHeight + 1, spendableOutputs.Dequeue(), null);
			blocks.Add(new BlockAndValidity(blockToHeightMap, hashHeaderMap, b1, true, false, b1.GetHash(), chainHeadHeight + 1, "b1"));
			spendableOutputs.Enqueue(new TransactionOutPointWithValue(
					new OutPoint(b1.Transactions[0].GetHash(), 0),
					b1.Transactions[0].Outputs[0].Value,
					b1.Transactions[0].Outputs[0].ScriptPubKey));

			TransactionOutPointWithValue out1 = spendableOutputs.Dequeue();
			Assert.True(out1 != null);
			Block b2 = createNextBlock(b1, chainHeadHeight + 2, out1, null);
			blocks.Add(new BlockAndValidity(blockToHeightMap, hashHeaderMap, b2, true, false, b2.GetHash(), chainHeadHeight + 2, "b2"));
			// Make sure nothing funky happens if we try to re-add b2
			blocks.Add(new BlockAndValidity(blockToHeightMap, hashHeaderMap, b2, true, false, b2.GetHash(), chainHeadHeight + 2, "b2"));
			spendableOutputs.Enqueue(new TransactionOutPointWithValue(
					new OutPoint(b2.Transactions[0].GetHash(), 0),
					b2.Transactions[0].Outputs[0].Value,
					b2.Transactions[0].Outputs[0].ScriptPubKey));
			// We now have the following chain (which output is spent is in parentheses):
			// genesis -> b1 (0) -> b2 (1)
			//
			// so fork like this:
			//
			// genesis -> b1 (0) -> b2 (1)
			// \-> b3 (1)
			//
			// Nothing should happen at this point. We saw b2 first so it takes priority.
			Block b3 = createNextBlock(b1, chainHeadHeight + 2, out1, null);
			blocks.Add(new BlockAndValidity(blockToHeightMap, hashHeaderMap, b3, true, false, b2.GetHash(), chainHeadHeight + 2, "b3"));
			// Make sure nothing breaks if we add b3 twice
			blocks.Add(new BlockAndValidity(blockToHeightMap, hashHeaderMap, b3, true, false, b2.GetHash(), chainHeadHeight + 2, "b3"));
			// Now we add another block to make the alternative chain longer.
			TransactionOutPointWithValue out2 = spendableOutputs.Dequeue();
			Assert.True(out2 != null);

			Block b4 = createNextBlock(b3, chainHeadHeight + 3, out2, null);
			blocks.Add(new BlockAndValidity(blockToHeightMap, hashHeaderMap, b4, true, false, b4.GetHash(), chainHeadHeight + 3, "b4"));


			//
			// genesis -> b1 (0) -> b2 (1)
			// \-> b3 (1) -> b4 (2)
			//
			// ... and back to the first chain.
			Block b5 = createNextBlock(b2, chainHeadHeight + 3, out2, null);
			blocks.Add(new BlockAndValidity(blockToHeightMap, hashHeaderMap, b5, true, false, b4.GetHash(), chainHeadHeight + 3, "b5"));
			spendableOutputs.Enqueue(new TransactionOutPointWithValue(
					new OutPoint(b5.Transactions[0].GetHash(), 0),
					b5.Transactions[0].Outputs[0].Value,
					b5.Transactions[0].Outputs[0].ScriptPubKey));

			TransactionOutPointWithValue out3 = spendableOutputs.Dequeue();

			Block b6 = createNextBlock(b5, chainHeadHeight + 4, out3, null);
			blocks.Add(new BlockAndValidity(blockToHeightMap, hashHeaderMap, b6, true, false, b6.GetHash(), chainHeadHeight + 4, "b6"));


			//
			// genesis -> b1 (0) -> b2 (1) -> b5 (2) -> b6 (3)
			// \-> b3 (1) -> b4 (2)
			//

			// Try to create a fork that double-spends
			// genesis -> b1 (0) -> b2 (1) -> b5 (2) -> b6 (3)
			// \-> b7 (3) -> b8 (4)
			// \-> b3 (1) -> b4 (2)
			//
			Block b7 = createNextBlock(b5, chainHeadHeight + 4, out2, null);
			blocks.Add(new BlockAndValidity(blockToHeightMap, hashHeaderMap, b7, true, false, b6.GetHash(), chainHeadHeight + 4, "b7"));

			TransactionOutPointWithValue out4 = spendableOutputs.Dequeue();

			Block b8 = createNextBlock(b7, chainHeadHeight + 5, out4, null);
			blocks.Add(new BlockAndValidity(blockToHeightMap, hashHeaderMap, b8, false, true, b6.GetHash(), chainHeadHeight + 4, "b8"));

			return ret;
		}



		private Block createNextBlock(Block baseBlock, int nextBlockHeight, TransactionOutPointWithValue prevOut,
			Money additionalCoinbaseValue)
		{
			int height = 0;
			if(blockToHeightMap.TryGetValue(baseBlock.GetHash(), out height))
				Assert.True(height == nextBlockHeight - 1);
			var coinbaseValue = Network.GetReward(nextBlockHeight) + (additionalCoinbaseValue ?? Money.Zero);
			Block block = baseBlock.CreateNextBlockWithCoinbase(coinbaseOutKeyPubKey, coinbaseValue);
			if(prevOut != null)
			{
				Transaction t = new Transaction();
				// Entirely invalid scriptPubKey to ensure we aren't pre-verifying too much
				t.AddOutput(new TxOut(new Money(0), new Script(Op.GetPushOp(1))));
				t.AddOutput(new TxOut(Money.Parse("1"),
						PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(coinbaseOutKeyPubKey)));
				// Spendable output
				t.AddOutput(new TxOut(Money.Zero, new Script(Op.GetPushOp(1))));
				addOnlyInputToTransaction(t, prevOut);
				block.AddTransaction(t);
				block.ComputeMerkleRoot();
			}
			return block;
		}

		private void addOnlyInputToTransaction(Transaction t, TransactionOutPointWithValue prevOut)
		{
			addOnlyInputToTransaction(t, prevOut, TxIn.NO_SEQUENCE);
		}

		private void addOnlyInputToTransaction(Transaction t, TransactionOutPointWithValue prevOut, uint sequence)
		{
			TxIn input = new TxIn(prevOut.outpoint)
			{
				ScriptSig = new Script(Op.GetPushOp(0))
			};
			input.Sequence = sequence;
			t.AddInput(input);

			var hash = prevOut.scriptPubKey.SignatureHash(t, 0, SigHash.All);
			input.ScriptSig = PayToPubkeyHashTemplate.Instance.GenerateScriptSig(
				new TransactionSignature(coinbaseOutKey.Sign(hash), SigHash.All),coinbaseOutKeyPubKey);
		}
	}
}
