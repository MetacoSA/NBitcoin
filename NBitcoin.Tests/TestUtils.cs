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
		public static void Eventually(Func<bool> act)
		{
			var cancel = new CancellationTokenSource(20000);
			while(!act())
			{
				cancel.Token.ThrowIfCancellationRequested();
				Thread.Sleep(50);
			}
		}

		public static byte[] ToBytes(string str)
		{
			byte[] result = new byte[str.Length];
			for(int i = 0; i < str.Length; i++)
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
			var block = new Block();
			block.AddTransaction(tx);
			block.UpdateMerkleRoot();
			return block;
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
