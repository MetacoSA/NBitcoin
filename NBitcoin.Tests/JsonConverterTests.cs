using NBitcoin.JsonConverters;
using NBitcoin.OpenAsset;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class JsonConverterTests
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanSerializeInJson()
		{
			Key k = new Key();
			CanSerializeInJsonCore(DateTimeOffset.UtcNow);
			CanSerializeInJsonCore(new byte[] { 1, 2, 3 });
			CanSerializeInJsonCore(k);
			CanSerializeInJsonCore(Money.Coins(5.0m));
			CanSerializeInJsonCore(k.PubKey.GetAddress(Network.Main));
			CanSerializeInJsonCore(new KeyPath("1/2"));
			CanSerializeInJsonCore(Network.Main);
			CanSerializeInJsonCore(new uint256(RandomUtils.GetBytes(32)));
			CanSerializeInJsonCore(new uint160(RandomUtils.GetBytes(20)));
			CanSerializeInJsonCore(new AssetId(k.PubKey));
			CanSerializeInJsonCore(k.PubKey.ScriptPubKey);
			CanSerializeInJsonCore(new Key().PubKey.WitHash.GetAddress(Network.Main));
			CanSerializeInJsonCore(new Key().PubKey.WitHash.ScriptPubKey.GetWitScriptAddress(Network.Main));
			var sig = k.Sign(new uint256(RandomUtils.GetBytes(32)));
			CanSerializeInJsonCore(sig);
			CanSerializeInJsonCore(new TransactionSignature(sig, SigHash.All));
			CanSerializeInJsonCore(k.PubKey.Hash);
			CanSerializeInJsonCore(k.PubKey.ScriptPubKey.Hash);
			CanSerializeInJsonCore(k.PubKey.WitHash);
			CanSerializeInJsonCore(k);
			CanSerializeInJsonCore(k.PubKey);
			CanSerializeInJsonCore(new WitScript(new Script(Op.GetPushOp(sig.ToDER()), Op.GetPushOp(sig.ToDER()))));
			CanSerializeInJsonCore(new LockTime(1));
			CanSerializeInJsonCore(new LockTime(DateTime.UtcNow));
			CanSerializeInJsonCore(new FeeRate(Money.Satoshis(1), 1000));
			CanSerializeInJsonCore(new FeeRate(Money.Satoshis(1000), 1000));
		}

		private T CanSerializeInJsonCore<T>(T value)
		{
			var str = Serializer.ToString(value);
			var obj2 = Serializer.ToObject<T>(str);
			Assert.Equal(str, Serializer.ToString(obj2));
			return obj2;
		}
	}
}
