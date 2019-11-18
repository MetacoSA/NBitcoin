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
			CanSerializeInJsonCore(k.PubKey.GetAddress(ScriptPubKeyType.Legacy, Network.Main), Network.Main);
			CanSerializeInJsonCore(new KeyPath("1/2"));
			CanSerializeInJsonCore(RootedKeyPath.Parse("7b09d780/0'/0'/2'"));
			CanSerializeInJsonCore(Network.Main);
			CanSerializeInJsonCore(new uint256(RandomUtils.GetBytes(32)));
			CanSerializeInJsonCore(new uint160(RandomUtils.GetBytes(20)));
			CanSerializeInJsonCore(new AssetId(k.PubKey));
			CanSerializeInJsonCore(k.PubKey.ScriptPubKey);
			CanSerializeInJsonCore(new Key().PubKey.WitHash.GetAddress(Network.Main), Network.Main);
			CanSerializeInJsonCore(new Key().PubKey.WitHash.ScriptPubKey.GetWitScriptAddress(Network.Main), Network.Main);
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
			CanSerializeInJsonCore(new LockTime(130), out var str);
			Assert.Equal("130", str);
			CanSerializeInJsonCore(new LockTime(DateTime.UtcNow));
			CanSerializeInJsonCore(new Sequence(1));
			CanSerializeInJsonCore(new Sequence?(1));
			CanSerializeInJsonCore(new Sequence?());
			CanSerializeInJsonCore(new FeeRate(Money.Satoshis(1), 1000));
			CanSerializeInJsonCore(new FeeRate(Money.Satoshis(1000), 1000));
			CanSerializeInJsonCore(new FeeRate(0.5m));
			CanSerializeInJsonCore(new HDFingerprint(0x0a), out str);
			Assert.Equal("\"0a000000\"", str);
			var print = Serializer.ToObject<HDFingerprint>("\"0a000000\"");
			var print2 = Serializer.ToObject<HDFingerprint>("10");
			Assert.Equal(print, print2);

			var printn = Serializer.ToObject<HDFingerprint?>("\"0a000000\"");
			var print2n = Serializer.ToObject<HDFingerprint?>("10");
			Assert.Equal(printn, print2n);
			Assert.Null(Serializer.ToObject<HDFingerprint?>(""));

			var psbt = PSBT.Parse("70736274ff01009a020000000258e87a21b56daf0c23be8e7070456c336f7cbaa5c8757924f545887bb2abdd750000000000ffffffff838d0427d0ec650a68aa46bb0b098aea4422c071b2ca78352a077959d07cea1d0100000000ffffffff0270aaf00800000000160014d85c2b71d0060b09c9886aeb815e50991dda124d00e1f5050000000016001400aea9a2e5f0f876a588df5546e8742d1d87008f00000000000100bb0200000001aad73931018bd25f84ae400b68848be09db706eac2ac18298babee71ab656f8b0000000048473044022058f6fc7c6a33e1b31548d481c826c015bd30135aad42cd67790dab66d2ad243b02204a1ced2604c6735b6393e5b41691dd78b00f0c5942fb9f751856faa938157dba01feffffff0280f0fa020000000017a9140fb9463421696b82c833af241c78c17ddbde493487d0f20a270100000017a91429ca74f8a08f81999428185c97b5d852e4063f6187650000002202029583bf39ae0a609747ad199addd634fa6108559d6c5cd39b4c2183f1ab96e07f473044022074018ad4180097b873323c0015720b3684cc8123891048e7dbcd9b55ad679c99022073d369b740e3eb53dcefa33823c8070514ca55a7dd9544f157c167913261118c01010304010000000104475221029583bf39ae0a609747ad199addd634fa6108559d6c5cd39b4c2183f1ab96e07f2102dab61ff49a14db6a7d02b0cd1fbb78fc4b18312b5b4e54dae4dba2fbfef536d752ae2206029583bf39ae0a609747ad199addd634fa6108559d6c5cd39b4c2183f1ab96e07f10d90c6a4f000000800000008000000080220602dab61ff49a14db6a7d02b0cd1fbb78fc4b18312b5b4e54dae4dba2fbfef536d710d90c6a4f0000008000000080010000800001012000c2eb0b0000000017a914b7f5faf40e3d40a5a459b1db3535f2b72fa921e887220203089dc10c7ac6db54f91329af617333db388cead0c231f723379d1b99030b02dc473044022062eb7a556107a7c73f45ac4ab5a1dddf6f7075fb1275969a7f383efff784bcb202200c05dbb7470dbf2f08557dd356c7325c1ed30913e996cd3840945db12228da5f010103040100000001042200208c2353173743b595dfb4a07b72ba8e42e3797da74e87fe7d9d7497e3b2028903010547522103089dc10c7ac6db54f91329af617333db388cead0c231f723379d1b99030b02dc21023add904f3d6dcf59ddb906b0dee23529b7ffb9ed50e5e86151926860221f0e7352ae2206023add904f3d6dcf59ddb906b0dee23529b7ffb9ed50e5e86151926860221f0e7310d90c6a4f000000800000008003000080220603089dc10c7ac6db54f91329af617333db388cead0c231f723379d1b99030b02dc10d90c6a4f00000080000000800200008000220203a9a4c37f5996d3aa25dbac6b570af0650394492942460b354753ed9eeca5877110d90c6a4f000000800000008004000080002202027f6399757d2eff55a136ad02c684b1838b6556e5f1b6b34282a94b6b5005109610d90c6a4f00000080000000800500008000", Network.Main);
			var psbtJson = Serializer.ToString(psbt, Network.Main);
			var psbt2 = Serializer.ToObject<PSBT>(psbtJson, Network.Main);
			Assert.Equal(psbt, psbt2);


			var expectedOutpoint = OutPoint.Parse("44f69ca74088d6d88e30156da85aae54543a87f67cdfdabbe9b53a92d6d7027c01000000");
			var actualOutpoint = Serializer.ToObject<OutPoint>("\"44f69ca74088d6d88e30156da85aae54543a87f67cdfdabbe9b53a92d6d7027c01000000\"", Network.Main);
			Assert.Equal(expectedOutpoint, actualOutpoint);
			actualOutpoint = Serializer.ToObject<OutPoint>("\"7c02d7d6923ab5e9bbdadf7cf6873a5454ae5aa86d15308ed8d68840a79cf644-1\"", Network.Main);
			Assert.Equal(expectedOutpoint, actualOutpoint);

			CanSerializeInJsonCore(expectedOutpoint, out str);
			Assert.Equal("\"44f69ca74088d6d88e30156da85aae54543a87f67cdfdabbe9b53a92d6d7027c01000000\"", str);

			Assert.Throws<JsonObjectException>(() =>
			{
				Serializer.ToObject<OutPoint>("1");
			});
		}

		private T CanSerializeInJsonCore<T>(T value, Network network = null)
		{
			var str = Serializer.ToString(value, network);
			var obj2 = Serializer.ToObject<T>(str, network);
			Assert.Equal(str, Serializer.ToString(obj2, network));
			return obj2;
		}
		private T CanSerializeInJsonCore<T>(T value, out string str)
		{
			str = Serializer.ToString(value);
			var obj2 = Serializer.ToObject<T>(str);
			Assert.Equal(str, Serializer.ToString(obj2));
			return obj2;
		}
	}
}
