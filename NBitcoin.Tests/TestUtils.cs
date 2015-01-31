using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.Tests
{
	class TestUtils
	{
		public static Transaction CreateFakeTx(Money coin, KeyId to)
		{
			// Create a fake TX of sufficient realism to exercise the unit tests. Two outputs, one to us, one to somewhere
			// else to simulate change.
			Transaction t = new Transaction();
			TxOut outputToMe = new TxOut(coin, to);
			t.AddOutput(outputToMe);

			//TxOut change = new TxOut(Money.Parse("1.11"),
			//		new Key().PubKey.GetAddress(to.Network));
			//t.AddOutput(change);

			// Make a previous tx simply to send us sufficient coins. This prev tx is not really valid but it doesn't
			// matter for our purposes.
			Transaction prevTx = new Transaction();
			TxOut prevOut = new TxOut(coin, to);
			prevTx.AddOutput(prevOut);
			// Connect it.
			t.AddInput(prevTx, 0);

			// roundtrip tx
			return t.Clone();
		}
		public static Transaction CreateFakeTx(Money coin, BitcoinAddress to)
		{
			return CreateFakeTx(coin, (KeyId)to.Hash);
		}

		public static byte[] ToBytes(string str)
		{
			byte[] result = new byte[str.Length];
			for(int i = 0 ; i < str.Length ; i++)
			{
				result[i] = (byte)str[i];
			}
			return result;
		}

		internal static bool TupleEquals<T1, T2>(Tuple<T1, T2> a, Tuple<T1, T2> b)
		{
			return a.Item1.Equals(b.Item1) && a.Item2.Equals(b.Item2);
		}

		internal static byte[] ParseHex(string data)
		{
			return Encoders.Hex.DecodeData(data);
		}

		public static Block CreateFakeBlock(Transaction tx)
		{
			var block = new Block();
			block.AddTransaction(tx);
			block.UpdateMerkleRoot();
			return block;
		}

		public static PersistantChain CreateBlockChain(List<Block> blocks)
		{
			PersistantChain chain = new PersistantChain(Network.Main.GetGenesis().Header);
			foreach(var b in blocks)
			{
				b.Header.HashPrevBlock = chain.Tip.Header.GetHash();
				chain.TrySetTip(b.Header);
			}
			return chain;
		}

		public static Block CreateFakeBlock()
		{
			var block = TestUtils.CreateFakeBlock(new Transaction());
			block.Header.HashPrevBlock = new uint256(RandomUtils.GetBytes(32));
			block.Header.Nonce = RandomUtils.GetUInt32();
			return block;
		}

		internal static void EnsureNew(string folderName)
		{
			if(Directory.Exists(folderName))
				Directory.Delete(folderName, true);
			while(true)
			{
				try
				{
					Directory.CreateDirectory(folderName);
					break;
				}
				catch
				{
				}
			}

		}
	}
}
