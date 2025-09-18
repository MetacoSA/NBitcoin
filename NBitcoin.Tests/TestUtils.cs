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
	public static class Extensions
	{
		/// <summary>
		/// Returns this value after a brief delay. Decred nodes and wallets may
		/// require a brief delay after certain operations to allow the
		/// node/wallet complete processing the operation. Without this delay,
		/// the next step in a test may fail.
		/// </summary>
		/// <param name="value">The value to be returned after the
		/// delay.</param>
		/// <param name="delay">How many milliseconds to delay before returning
		/// the value.</param>
		public static async Task<T> WithDelay<T>(this T value, int delay)
		{
			if (delay > 0) await Task.Delay(delay);
			return value;
		}
	}

	class TestUtils
	{
		public static void Eventually(Func<bool> act)
		{
			var cancel = new CancellationTokenSource(20000);
			while (!act())
			{
				cancel.Token.ThrowIfCancellationRequested();
				Thread.Sleep(50);
			}
		}

		public static byte[] ToBytes(string str)
		{
			byte[] result = new byte[str.Length];
			for (int i = 0; i < str.Length; i++)
			{
				result[i] = (byte)str[i];
			}
			return result;
		}

		internal static byte[] ParseHex(string data)
		{
			return Encoders.Hex.DecodeData(data);
		}

		public static Block CreateFakeBlock(Transaction tx)
		{
			Block block = Network.Main.Consensus.ConsensusFactory.CreateBlock();
			block.AddTransaction(tx);
			block.UpdateMerkleRoot();
			return block;
		}

		public static Block CreateFakeBlock()
		{
			var tx = Network.Main.CreateTransaction();
			tx.Inputs.Add();
			var block = TestUtils.CreateFakeBlock(tx);
			block.Header.HashPrevBlock = new uint256(RandomUtils.GetBytes(32));
			block.Header.Nonce = RandomUtils.GetUInt32();
			return block;
		}

		internal static void EnsureNew(string folderName)
		{
			if (Directory.Exists(folderName))
				Directory.Delete(folderName, true);
			while (true)
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
