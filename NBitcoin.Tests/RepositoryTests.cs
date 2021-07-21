#if !NOFILEIO
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.OpenAsset;
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;
using NBitcoin.RPC;
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
			if (bobs.Length == 1)
			{
				var bob = bobs[0];
				if (type == CoinType.Normal)
					return new Coin(new uint256(RandomUtils.GetBytes(32)), 0, amount, bob.PubKey.Hash.ScriptPubKey);
				if (type == CoinType.P2WPKH)
					return new Coin(new uint256(RandomUtils.GetBytes(32)), 0, amount, bob.PubKey.WitHash.ScriptPubKey);
				if (type == CoinType.P2SH)
					return new Coin(new uint256(RandomUtils.GetBytes(32)), 0, amount, bob.PubKey.ScriptPubKey.Hash.ScriptPubKey).ToScriptCoin(bob.PubKey.ScriptPubKey);
				if (type == CoinType.SegwitP2SH)
					return new Coin(new uint256(RandomUtils.GetBytes(32)), 0, amount, bob.PubKey.ScriptPubKey.WitHash.ScriptPubKey.Hash.ScriptPubKey).ToScriptCoin(bob.PubKey.ScriptPubKey);
				if (type == CoinType.Segwit)
					return new Coin(new uint256(RandomUtils.GetBytes(32)), 0, amount, bob.PubKey.ScriptPubKey.WitHash.ScriptPubKey).ToScriptCoin(bob.PubKey.ScriptPubKey);
				throw new NotSupportedException();
			}
			else
			{
				while (type == CoinType.Normal || type == CoinType.P2WPKH)
				{
					type = (CoinType)(RandomUtils.GetUInt32() % 5);
				}
				var script = PayToMultiSigTemplate.Instance.GenerateScriptPubKey((int)(1 + (RandomUtils.GetUInt32() % bobs.Length)), bobs.Select(b => b.PubKey).ToArray());
				if (type == CoinType.P2SH)
					return new Coin(new uint256(RandomUtils.GetBytes(32)), 0, amount, script.Hash.ScriptPubKey).ToScriptCoin(script);
				if (type == CoinType.SegwitP2SH)
					return new Coin(new uint256(RandomUtils.GetBytes(32)), 0, amount, script.WitHash.ScriptPubKey.Hash.ScriptPubKey).ToScriptCoin(script);
				if (type == CoinType.Segwit)
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
		Network Network => Network.Main;

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanBuildTransactionWithSubstractFeeAndSendEstimatedFees()
		{
			var signer = new Key();
			var builder = Network.CreateTransactionBuilder();
			builder.AddKeys(signer);
			builder.AddCoins(RandomCoin(signer, Money.Coins(1)));
			builder.Send(new Key(), Money.Coins(1));
			builder.SubtractFees();
			builder.SendEstimatedFees(new FeeRate(Money.Satoshis(100), 1));
			var v = VerifyFees(builder, new FeeRate(Money.Satoshis(100), 1));
			Assert.Equal(v.expectedBaseSize, v.baseSize); // No signature here, should be fix
			Assert.True(v.witSize - v.expectedWitsize < 4); // the signature size might vary

			for (int i = 0; i < 100; i++)
			{
				builder = Network.CreateTransactionBuilder();
				for (int ii = 0; ii < 1 + RandomUtils.GetUInt32() % 10; ii++)
				{
					var signersCount = 1 + (int)(RandomUtils.GetUInt32() % 6);
					var signers = Enumerable.Range(0, signersCount).Select(_ => new Key()).ToArray();
					builder.AddCoins(RandomCoin(signers, Money.Coins(1), (CoinType)(RandomUtils.GetUInt32() % 5)));
					builder.AddKeys(signers);
					builder.Send(new Key(), Money.Coins(0.9m));

				}
				builder.SubtractFees();
				builder.SetChange(new Key());
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
			Assert.True(feeRate.FeePerK.Almost(result.GetFeeRate(builder.FindSpentCoins(result)).FeePerK, 0.02m));
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

			var builder = Network.CreateTransactionBuilder();
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
			Assert.Single(tx.Outputs
								.Where(o => o.ScriptPubKey == bob.GetScriptPubKey(ScriptPubKeyType.Legacy))
								.Where(o => o.Value == Money.Coins(0.3m) + Money.Coins(0.1m))
);
			Assert.Single(tx.Outputs
							  .Where(o => o.ScriptPubKey == alice.GetScriptPubKey(ScriptPubKeyType.Legacy))
							  .Where(o => o.Value == Money.Coins(0.7m))
);
			Assert.Single(tx.Outputs
								.Where(o => o.ScriptPubKey == carol.GetScriptPubKey(ScriptPubKeyType.Legacy))
								.Where(o => o.Value == Money.Coins(1.0m))
);
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
