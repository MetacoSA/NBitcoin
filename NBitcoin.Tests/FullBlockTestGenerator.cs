using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Tests
{
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
		String ruleName;
		Rule(String ruleName)
		{
			this.ruleName = ruleName;
		}
	}
	class TransactionOutPointWithValue
	{
		public OutPoint outpoint;
		public BigInteger value;
		Script scriptPubKey;
		public TransactionOutPointWithValue(OutPoint outpoint, BigInteger value, Script scriptPubKey)
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

		internal RuleList GetBlocksToTest(bool addSigExpensiveBlocks, bool runLargeReorgs, BlockStore blockStore)
		{
			List<Rule> blocks = new List<Rule>();
			RuleList ret = new RuleList(blocks, hashHeaderMap, 10);

			Queue<TransactionOutPointWithValue> spendableOutputs = new Queue<TransactionOutPointWithValue>();

			int chainHeadHeight = 1;
			Block chainHead = Network.GetGenesis().CreateNextBlockWithCoinbase(coinbaseOutKeyPubKey.GetAddress(Network), chainHeadHeight);

			return ret;
		}
	}
}
