﻿#if !NOFILEIO
using NBitcoin.BitcoinCore;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.OpenAsset;
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;
using NBitcoin.RPC;
using NBitcoin.SPV;
using NBitcoin.Stealth;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class RepositoryTests
	{
		public class RawData : IBitcoinSerializable
		{
			public RawData()
			{

			}
			public RawData(byte[] data)
			{
				_Data = data;
			}
			#region IBitcoinSerializable Members

			public void ReadWrite(BitcoinStream stream)
			{
				stream.ReadWriteAsVarString(ref _Data);
			}

			private byte[] _Data = new byte[0];
			public byte[] Data
			{
				get
				{
					return _Data;
				}
			}

			#endregion
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		//The last block is off by 1 byte + lots of padding zero at the end
		public void CanEnumerateIncompleteBlk()
		{
			Assert.Equal(301, StoredBlock.EnumerateFile(@"data/blocks/incompleteblk.dat").Count());
		}
		
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public static void CanRequestTransactionOnQBit()
		{
			var repo = new QBitNinjaTransactionRepository(Network.Main);
			var result = repo.Get("c3462373f1a722c66cbb1b93712df94aa7b3731f4142cd8413f10c9e872927de");
			Assert.NotNull(result);
			Assert.Equal("c3462373f1a722c66cbb1b93712df94aa7b3731f4142cd8413f10c9e872927de", result.GetHash().ToString());

			result = repo.Get("c3462373f1a722c66cbb1b93712df94aa7b3731f4142cd8413f10c9e872927df");
			Assert.Null(result);

			repo = new QBitNinjaTransactionRepository(Network.TestNet);
			result = repo.Get("7d4c5d69a85c70ff70daff789114b9b76fb6d2613ac18764bd96f0a2b9358782");
			Assert.NotNull(result);
		}

		enum CoinType : int
		{
			Segwit = 0,
			SegwitP2SH = 1,
			P2SH = 2,
			Normal = 3,
			P2WPKH = 4
		}
		private static Coin RandomCoin(Key[] bobs, Money amount, CoinType type)
		{
			if(bobs.Length == 1)
			{
				var bob = bobs[0];
				if(type == CoinType.Normal)
					return new Coin(new uint256(RandomUtils.GetBytes(32)), 0, amount, bob.PubKey.Hash.ScriptPubKey);
				if(type == CoinType.P2WPKH)
					return new Coin(new uint256(RandomUtils.GetBytes(32)), 0, amount, bob.PubKey.WitHash.ScriptPubKey);
				if(type == CoinType.P2SH)
					return new Coin(new uint256(RandomUtils.GetBytes(32)), 0, amount, bob.PubKey.ScriptPubKey.Hash.ScriptPubKey).ToScriptCoin(bob.PubKey.ScriptPubKey);
				if(type == CoinType.SegwitP2SH)
					return new Coin(new uint256(RandomUtils.GetBytes(32)), 0, amount, bob.PubKey.ScriptPubKey.WitHash.ScriptPubKey.Hash.ScriptPubKey).ToScriptCoin(bob.PubKey.ScriptPubKey);
				if(type == CoinType.Segwit)
					return new Coin(new uint256(RandomUtils.GetBytes(32)), 0, amount, bob.PubKey.ScriptPubKey.WitHash.ScriptPubKey).ToScriptCoin(bob.PubKey.ScriptPubKey);
				throw new NotSupportedException();
			}
			else
			{
				while(type == CoinType.Normal || type == CoinType.P2WPKH)
				{
					type = (CoinType)(RandomUtils.GetUInt32() % 5);
				}
				var script = PayToMultiSigTemplate.Instance.GenerateScriptPubKey((int)(1 + (RandomUtils.GetUInt32() % bobs.Length)), bobs.Select(b => b.PubKey).ToArray());
				if(type == CoinType.P2SH)
					return new Coin(new uint256(RandomUtils.GetBytes(32)), 0, amount, script.Hash.ScriptPubKey).ToScriptCoin(script);
				if(type == CoinType.SegwitP2SH)
					return new Coin(new uint256(RandomUtils.GetBytes(32)), 0, amount, script.WitHash.ScriptPubKey.Hash.ScriptPubKey).ToScriptCoin(script);
				if(type == CoinType.Segwit)
					return new Coin(new uint256(RandomUtils.GetBytes(32)), 0, amount, script.WitHash.ScriptPubKey).ToScriptCoin(script);
				throw new NotSupportedException();
			}
		}

		private static Coin RandomCoin(Key bob, Money amount, bool p2pkh = false)
		{
			return new Coin(new uint256(RandomUtils.GetBytes(32)), 0, amount, p2pkh ? bob.PubKey.Hash.ScriptPubKey : bob.PubKey.WitHash.ScriptPubKey);
		}
		private static Coin RandomCoin2(Key bob, Money amount, bool p2pkh = false)
		{
			return new Coin(new uint256(RandomUtils.GetBytes(32)), 0, amount, p2pkh ? bob.PubKey.Hash.ScriptPubKey : bob.PubKey.WitHash.ScriptPubKey);
		}

		[Fact]
		public void Play()
		{
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanBuildTransactionWithSubstractFeeAndSendEstimatedFees()
		{
			var signer = new Key();
			var builder = new TransactionBuilder();
			builder.AddKeys(signer);
			builder.AddCoins(RandomCoin(signer, Money.Coins(1)));
			builder.Send(new Key().ScriptPubKey, Money.Coins(1));
			builder.SubtractFees();
			builder.SendEstimatedFees(new FeeRate(Money.Satoshis(100), 1));
			var v = VerifyFees(builder, new FeeRate(Money.Satoshis(100), 1));
			Assert.Equal(v.expectedBaseSize, v.baseSize); // No signature here, should be fix
			Assert.True(v.witSize - v.expectedWitsize < 2); // the signature size might vary of 1 or 2 bytes

			for(int i = 0; i < 100; i++)
			{
				builder = new TransactionBuilder();
				for(int ii = 0; ii < 1 + RandomUtils.GetUInt32() % 10; ii++)
				{
					var signersCount = 1 + (int)(RandomUtils.GetUInt32() % 6);
					var signers = Enumerable.Range(0, signersCount).Select(_ => new Key()).ToArray();
					builder.AddCoins(RandomCoin(signers, Money.Coins(1), (CoinType)(RandomUtils.GetUInt32() % 5)));
					builder.AddKeys(signers);
					builder.Send(new Key().ScriptPubKey, Money.Coins(0.9m));

				}
				builder.SubtractFees();
				builder.SetChange(new Key().ScriptPubKey);
				builder.SendEstimatedFees(builder.StandardTransactionPolicy.MinRelayTxFee);
				VerifyFees(builder);
			}
		}

		private static (int expectedBaseSize, int expectedWitsize, int baseSize, int witSize) VerifyFees(TransactionBuilder builder, FeeRate feeRate = null)
		{
			feeRate = feeRate ?? builder.StandardTransactionPolicy.MinRelayTxFee;
			var result = builder.BuildTransaction(true);
			builder.EstimateSizes(result, out int witSize, out int baseSize);
			var expectedWitsize = result.ToBytes().Length - result.WithOptions(TransactionOptions.None).ToBytes().Length;
			var expectedBaseSize = result.WithOptions(TransactionOptions.None).ToBytes().Length;
			Assert.True(expectedBaseSize <= baseSize);
			Assert.True(expectedWitsize <= witSize);
			Assert.True(feeRate.FeePerK.Almost(result.GetFeeRate(builder.FindSpentCoins(result)).FeePerK, 0.01m));
			Assert.True(feeRate.FeePerK <= result.GetFeeRate(builder.FindSpentCoins(result)).FeePerK);

			return (expectedBaseSize, expectedWitsize, baseSize, witSize);
		}


		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void TwoGroupsCanSendToSameDestination()
		{
			var alice = new Key();
			var carol = new Key();
			var bob = new Key();

			var builder = new TransactionBuilder();
			builder.StandardTransactionPolicy.CheckFee = false;
			Transaction tx = builder
				.AddCoins(RandomCoin2(alice, Money.Coins(1.0m)))
				.AddKeys(alice)
				.Send(bob, Money.Coins(0.3m))
				.SetChange(alice)
				.Then()
				.AddCoins(RandomCoin2(carol, Money.Coins(1.1m)))
				.AddKeys(carol)
				.Send(bob, Money.Coins(0.1m))
				.SetChange(carol)
				.BuildTransaction(sign: true);

			Assert.Equal(2, tx.Inputs.Count);
			Assert.Equal(3, tx.Outputs.Count);
			Assert.Equal(1, tx.Outputs
								.Where(o => o.ScriptPubKey == bob.ScriptPubKey)
								.Where(o => o.Value == Money.Coins(0.3m) + Money.Coins(0.1m))
								.Count());
			Assert.Equal(1, tx.Outputs
							  .Where(o => o.ScriptPubKey == alice.ScriptPubKey)
							  .Where(o => o.Value == Money.Coins(0.7m))
							  .Count());
			Assert.Equal(1, tx.Outputs
								.Where(o => o.ScriptPubKey == carol.ScriptPubKey)
								.Where(o => o.Value == Money.Coins(1.0m))
								.Count());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanCacheNoSqlRepository()
		{
			var cached = new CachedNoSqlRepository(new InMemoryNoSqlRepository());
			byte[] data1 = new byte[] { 1, 2, 3, 4, 5, 6 };
			byte[] data2 = new byte[] { 11, 22, 33, 4, 5, 66 };
			cached.InnerRepository.Put("data1", new RawData(data1));
			Assert.NotNull(cached.Get<RawData>("data1"));
			cached.InnerRepository.Put("data1", new RawData(data2));
			cached.Flush();
			var data1Actual = cached.InnerRepository.Get<RawData>("data1");
			AssertEx.CollectionEquals(data1Actual.Data, data2);
			cached.Put("data1", new RawData(data1));

			data1Actual = cached.InnerRepository.Get<RawData>("data1");
			AssertEx.CollectionEquals(data1Actual.Data, data2);

			cached.Flush();

			data1Actual = cached.InnerRepository.Get<RawData>("data1");
			AssertEx.CollectionEquals(data1Actual.Data, data1);

			cached.Put("data1", null);
			cached.Flush();
			Assert.Null(cached.InnerRepository.Get<RawData>("data1"));

			cached.Put("data1", new RawData(data1));
			cached.Put("data1", null);
			cached.Flush();
			Assert.Null(cached.InnerRepository.Get<RawData>("data1"));

			cached.Put("data1", null);
			cached.Put("data1", new RawData(data1));
			cached.Flush();
			Assert.NotNull(cached.InnerRepository.Get<RawData>("data1"));
		}
	}
}
#endif