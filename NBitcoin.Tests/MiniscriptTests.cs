#if !NO_RECORDS
using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using NBitcoin.WalletPolicies;
using static NBitcoin.WalletPolicies.MiniscriptNode;
using static NBitcoin.WalletPolicies.Miniscript;
using NBitcoin.Scripting;
using Xunit.Abstractions;
using System.Net;
using NBitcoin.Secp256k1;
using NBitcoin.WalletPolicies.Visitors;

namespace NBitcoin.Tests
{
	[Trait("UnitTest", "UnitTest")]
	public class MiniscriptTests
	{
		public ITestOutputHelper Log { get; }

		public MiniscriptTests(ITestOutputHelper helper)
		{
			Log = helper;
		}
		[Theory]
		[InlineData("and_v(v:pk(A),pk(B))")]
		[InlineData("and_v(v:pk(A),pk(A))")]
		[InlineData("and_v(or_c(pk(B),or_c(pk(C),v:older(1000))),pk(A))")]
		[InlineData("and_v(or_c(pk(B),or_c(pk(C),v:older(D))),pk(A))")]
		[InlineData("or_d(pk([d4ab66f1/48'/1'/0'/2']tpubDEXYN145WM4rVKtcWpySBYiVQ229pmrnyAGJT14BBh2QJr7ABJswchDicZfFaauLyXhDad1nCoCZQEwAW87JPotP93ykC9WJvoASnBjYBxW/**),and_v(v:pkh([33fa0ffc/48'/1'/0'/2']tpubDEqzYAym2MnGqKdqu2ZtGQkDTSrvDWCrcoamspjRJR78nr8w5tAgu371r8LtcyWWWXGemenTMxmoLhQM3ww8gUfobBXUWxLEkfR7kGjD6jC/**),older(65535)))")]
		[InlineData("or_c(pk(alice),and_v(pk(bob),older(timelock)))")]
		[InlineData("andor(pk(key_remote),or_i(and_v(v:pkh(key_local),hash160(H)),older(1008)),pk(key_revocation))")]
		[InlineData("0")]
		[InlineData("1")]
		[InlineData("pk_k(A)")]
		[InlineData("pk_k(@0/**)")]
		[InlineData("pk_h(A)")]
		[InlineData("pk(A)")]
		[InlineData("pkh(A)")]
		[InlineData("older(A)")]
		[InlineData("after(A)")]
		[InlineData("sha256(A)")]
		[InlineData("ripemd160(A)")]
		[InlineData("hash256(A)")]
		[InlineData("hash160(A)")]
		[InlineData("andor(A,B,C)")]
		[InlineData("and_v(A,B)")]
		[InlineData("and_b(A,B)")]
		[InlineData("and_n(A,B)")]
		[InlineData("or_b(A,B)")]
		[InlineData("or_c(A,B)")]
		[InlineData("or_d(A,B)")]
		[InlineData("or_i(A,B)")]
		[InlineData("thresh(2,A,B)")]
		[InlineData("multi(1,A)")]
		[InlineData("multi_a(3,A,B,C)")]
		[InlineData("A")]
		[InlineData("a:A")]
		[InlineData("s:A")]
		[InlineData("c:A")]
		[InlineData("t:A")]
		[InlineData("d:A")]
		[InlineData("v:A")]
		[InlineData("j:A")]
		[InlineData("n:A")]
		[InlineData("l:A")]
		[InlineData("u:A")]
		[InlineData("dv:older(144)")]
		public void CanRoundtripMiniscript(string miniscript)
		{
			var parsed = Miniscript.Parse(miniscript, new MiniscriptParsingSettings(Network.RegTest, KeyType.Classic) {  AllowedParameters = ParameterTypeFlags.All });
			var actual = parsed.ToString();
			Assert.Equal(miniscript, actual);
		}

		[Theory]
		[InlineData(ParameterTypeFlags.All, "pkh(A)", true)]
		[InlineData(ParameterTypeFlags.All, "pkh(@0/**)", true)]
		[InlineData(ParameterTypeFlags.NamedParameter, "pkh(A)", true)]
		[InlineData(ParameterTypeFlags.NamedParameter, "older(A)", true)]
		[InlineData(ParameterTypeFlags.NamedParameter, "sha256(A)", true)]
		[InlineData(ParameterTypeFlags.NamedParameter, "pkh(@0/**)", false)]
		[InlineData(ParameterTypeFlags.KeyPlaceholder, "pkh(A)", false)]
		[InlineData(ParameterTypeFlags.KeyPlaceholder, "pkh(@0/**)", true)]
		[InlineData(ParameterTypeFlags.KeyPlaceholder, "older(A)", false)]
		[InlineData(ParameterTypeFlags.KeyPlaceholder, "sha256(A)", false)]
		[InlineData(ParameterTypeFlags.None, "pkh(A)", false)]
		[InlineData(ParameterTypeFlags.None, "pkh(@0/**)", false)]
		public void CanToggleParameterParsing(ParameterTypeFlags flag, string miniscript, bool expected)
		{
			var settings = new MiniscriptParsingSettings(Network.Main, KeyType.Classic) { AllowedParameters = flag };
			Assert.Equal(expected, Miniscript.TryParse(miniscript, settings, out _));
		}

		[Fact]
		public void CanParseTr()
		{
			var settings = new MiniscriptParsingSettings(Network.RegTest) { Dialect = MiniscriptDialect.BIP388, AllowedParameters = ParameterTypeFlags.NamedParameter };
			var miniscript = "tr(A,{BL,{{EL,FR},DR}})";
			var parsed = Miniscript.Parse(miniscript, settings);
			Assert.True(parsed.RootNode is TaprootNode
			{
				InternalKeyNode: Parameter { Name: "A" },
				ScriptTreeRootNode: TaprootBranchNode
				{
					Left: Parameter { Name: "BL" },
					Right: TaprootBranchNode
					{
						Left: TaprootBranchNode
						{
							Left: Parameter { Name: "EL" },
							Right: Parameter { Name: "FR" }
						},
						Right: Parameter { Name: "DR" }
					}
				}
			});
		}
		[Fact]
		public void CanGenerateTrScript()
		{
			var settings = new MiniscriptParsingSettings(Network.RegTest) { Dialect = MiniscriptDialect.BIP388 };
			var parsed = Miniscript.Parse("tr(@0/**,{pkh(@1/**),{{pkh(@2/**),pkh(@3/**)},pkh(@4/**)}})", settings);
			var keys = GenerateKeys(5);
			parsed = parsed.ReplaceKeyPlaceholdersByHDKeys(keys);
			parsed = parsed.Derive(AddressIntent.Deposit, 0).Miniscript;
			var scripts = parsed.ToScripts();
			var taprootInfo = parsed.GetTaprootInfo();
			Assert.NotNull(taprootInfo);
			Assert.NotNull(taprootInfo.MerkleRoot);

			// Example from https://github.com/bitcoin/bips/blob/master/bip-0386.mediawiki
			// We can't take literally the example, because the format of output descriptors is more relaxed than BIP388
			// so we need to derive everything separately.
			var privKey = new BitcoinSecret("L4rK1yDtCWekvXuE6oXD9jCYfFNV2cWRpVuPLBcCU2z8TrisoyY1", Network.Main);
			var internalKey = new TaprootPubKey(Encoders.Hex.DecodeData("a34b99f22c790c4e36b2b3c2c35a36db06226e41c692fc82b8b56ac1c540c5bd"));
			var A = new BitcoinExtKey("xprvA2JDeKCSNNZky6uBCviVfJSKyQ1mDYahRjijr5idH2WwLsEd4Hsb2Tyh8RfQMuPh7f7RtyzTtdrbdqqsunu5Mm3wDvUAKRHSC34sJ7in334", Network.Main).Neuter().Derive(KeyPath.Parse("0")).GetPublicKey().TaprootPubKey;
			var B = new BitcoinExtPubKey("xpub6ERApfZwUNrhLCkDtcHTcxd75RbzS1ed54G1LkBUHQVHQKqhMkhgbmJbZRkrgZw4koxb5JaHWkY4ALHY2grBGRjaDMzQLcgJvLJuZZvRcEL", Network.Main).GetPublicKey().TaprootPubKey;
			var C = new PubKey("02df12b7035bdac8e3bab862a3a83d06ea6b17b6753d52edecba9be46f5d09e076").TaprootPubKey;
			var D = privKey.PubKey.TaprootPubKey;
			settings.AllowedParameters = ParameterTypeFlags.NamedParameter;
			parsed = Miniscript.Parse("tr(InternalKey,{pk(A),{{pk(B),pk(C)},pk(D)}})", settings);
			parsed = parsed.ReplaceParameters(new()
			{
				{ "A", MiniscriptNode.Create(A) },
				{ "B", MiniscriptNode.Create(B) },
				{ "C", MiniscriptNode.Create(C) },
				{ "D", MiniscriptNode.Create(D) },
				{ "InternalKey", MiniscriptNode.Parameter.Create(internalKey) },
			});
			Assert.Equal("512071fff39599a7b78bc02623cbe814efebf1a404f5d8ad34ea80f213bd8943f574", parsed.ToScripts().ScriptPubKey.ToHex());

			var original = parsed.ToString();
			for (int i = 0; i < original.Length; i++)
			{
				var ms = original[0..i];
				Assert.False(Miniscript.TryParse(ms, settings, out var err, out _));
				Assert.IsAssignableFrom<MiniscriptError>(err);
				ms += "&";
				Assert.False(Miniscript.TryParse(ms, settings, out err, out _));
				Assert.IsAssignableFrom<MiniscriptError>(err);
			}
		}

		[Fact]
		public void CanRoundtripMiniscriptBIP388()
		{
			var settings = new MiniscriptParsingSettings(Network.RegTest) { Dialect = MiniscriptDialect.BIP388 };
			var miniscript = "wsh(or_i(and_v(v:thresh(1,pkh([f25bdff6/48'/1'/0'/2']tpubDF8cqMgmJ6BMJwMoEhwAfgVDdXs29y6w2qG1i1ciaYVmqQ6cRTjNoqWJZD2kAR6vJrGcpVBVyYEgYm5GE88F3Z2SVbQxqwdbRZeyUeGwTnk/<4;5>/*),a:pkh([6abb52a9/48'/1'/0'/2']tpubDFZTCVU1Sa9nJXCxx97UFvGausHQPFjJyaiDbdr8GNqjCLKwYc8ihegK7yJdcizs9HMbiGA7ke1HiCENVHaERvNANHW7U2Wo2qnRsuqB52r/<4;5>/*),a:pkh([d4ab66f1/48'/1'/0'/2']tpubDEXYN145WM4rVKtcWpySBYiVQ229pmrnyAGJT14BBh2QJr7ABJswchDicZfFaauLyXhDad1nCoCZQEwAW87JPotP93ykC9WJvoASnBjYBxW/<4;5>/*)),older(10)),or_d(multi(3,[f25bdff6/48'/1'/0'/2']tpubDF8cqMgmJ6BMJwMoEhwAfgVDdXs29y6w2qG1i1ciaYVmqQ6cRTjNoqWJZD2kAR6vJrGcpVBVyYEgYm5GE88F3Z2SVbQxqwdbRZeyUeGwTnk/<0;1>/*,[6abb52a9/48'/1'/0'/2']tpubDFZTCVU1Sa9nJXCxx97UFvGausHQPFjJyaiDbdr8GNqjCLKwYc8ihegK7yJdcizs9HMbiGA7ke1HiCENVHaERvNANHW7U2Wo2qnRsuqB52r/<0;1>/*,[d4ab66f1/48'/1'/0'/2']tpubDEXYN145WM4rVKtcWpySBYiVQ229pmrnyAGJT14BBh2QJr7ABJswchDicZfFaauLyXhDad1nCoCZQEwAW87JPotP93ykC9WJvoASnBjYBxW/<0;1>/*),and_v(v:thresh(2,pkh([f25bdff6/48'/1'/0'/2']tpubDF8cqMgmJ6BMJwMoEhwAfgVDdXs29y6w2qG1i1ciaYVmqQ6cRTjNoqWJZD2kAR6vJrGcpVBVyYEgYm5GE88F3Z2SVbQxqwdbRZeyUeGwTnk/<2;3>/*),a:pkh([6abb52a9/48'/1'/0'/2']tpubDFZTCVU1Sa9nJXCxx97UFvGausHQPFjJyaiDbdr8GNqjCLKwYc8ihegK7yJdcizs9HMbiGA7ke1HiCENVHaERvNANHW7U2Wo2qnRsuqB52r/<2;3>/*),a:pkh([d4ab66f1/48'/1'/0'/2']tpubDEXYN145WM4rVKtcWpySBYiVQ229pmrnyAGJT14BBh2QJr7ABJswchDicZfFaauLyXhDad1nCoCZQEwAW87JPotP93ykC9WJvoASnBjYBxW/<2;3>/*)),older(5)))))#757lxvur";
			var noChecksumMiniscript = miniscript.Replace("#757lxvur", "");
			var wrongChecksumMiniscript = miniscript.Replace("#757lxvur", "#756lxvur");
			var cutChecksumMiniscript = miniscript.Replace("#757lxvur", "#756lx");
			var miniscriptWithGarbage = miniscript + "and";

			var parsed = Miniscript.Parse(noChecksumMiniscript, settings);
			var actual = parsed.ToString();
			Assert.Equal(noChecksumMiniscript, actual);

			parsed = Miniscript.Parse(miniscript, settings);
			actual = parsed.ToString();
			Assert.Equal(noChecksumMiniscript, actual);

			var ex = Assert.Throws<MiniscriptFormatException>(() => Miniscript.Parse(wrongChecksumMiniscript, settings));
			Assert.IsType<MiniscriptError.InvalidChecksum>(ex.Error);

			ex = Assert.Throws<MiniscriptFormatException>(() => Miniscript.Parse(cutChecksumMiniscript, settings));
			Assert.IsType<MiniscriptError.InvalidChecksum>(ex.Error);

			ex = Assert.Throws<MiniscriptFormatException>(() => Miniscript.Parse(miniscriptWithGarbage, settings));
			Assert.IsType<MiniscriptError.UnexpectedToken>(ex.Error);

			var policy = WalletPolicy.Parse(miniscript, Network.RegTest);
			Assert.Equal(miniscript, policy.ToString(true));

			var address = policy.FullDescriptor.Derive(AddressIntent.Deposit, 1).Miniscript.ToScripts().ScriptPubKey.GetDestinationAddress(Network.TestNet);
			Assert.Equal("tb1qkhfzpjc3llj953rdfyzdzy0re88xvlvy4s3z5t0ur49dtlyzh34qhag7m5", address.ToString());
		}

		[Theory]
		[InlineData("tr(@0/**)", ScriptPubKeyType.TaprootBIP86)]
		[InlineData("wpkh(@0/**)", ScriptPubKeyType.Segwit)]
		[InlineData("sh(wpkh(@0/**))", ScriptPubKeyType.SegwitP2SH)]
		[InlineData("pkh(@0/**)", ScriptPubKeyType.Legacy)]
		public void CanGenerateSegwitAndTaprootFromMiniscript(string str, ScriptPubKeyType scriptPubKeyType)
		{
			var root = new ExtKey().GetWif(Network.TestNet);
			var account = root.Derive(new KeyPath("48'/1'/0'/2'"));
			var derivedPubKey = account.Derive(new KeyPath("1/123")).GetPublicKey();
			var keyNode = new HDKeyNode(new KeyPath("48'/1'/0'/2'").ToRootedKeyPath(root), account.Neuter());
			var miniscript = Miniscript.Parse(str, new MiniscriptParsingSettings(root.Network) { Dialect = MiniscriptDialect.BIP388 });
			miniscript = miniscript.ReplaceKeyPlaceholdersByHDKeys([keyNode]);
			var miniscriptWithHDKeys = miniscript;
			miniscript = miniscript.Derive(AddressIntent.Change, 123).Miniscript;
			var scripts = miniscript.ToScripts();
			var expected = derivedPubKey.GetScriptPubKey(scriptPubKeyType);
			Assert.Equal(expected, scripts.ScriptPubKey);

			var policy = WalletPolicy.Parse(miniscriptWithHDKeys.ToString(), root.Network);
			var k = Assert.Single(policy.KeyInformationVector);
			Assert.Equal(k, keyNode);
			Assert.Equal(str, policy.DescriptorTemplate.ToString());
		}

		[Fact]
		public void CanGenerateScripts()
		{
			var settings = new MiniscriptParsingSettings(Network.RegTest)
			{
				Dialect = MiniscriptDialect.BIP388,
				AllowedParameters = ParameterTypeFlags.All
			};
			var script = Miniscript.Parse("pkh([aaaaaaaa/44h/1h/0h]tpubDDV486pBqkML6Ywhznz8DS3VS95h3q4A2pUMCc6yy739QpKMg3gA8EXGrjraDBDxrhLsezepjCEfBtak5wngDH4vMh6aXKV8hPN7JsMtdEf/<0;1>/*)", settings);
			var expectedScripts = script.Derive(AddressIntent.Deposit, 0).Miniscript.ToScripts();
			Assert.Equal("76a914bc1bf603880f28ffaf9c888b5660e9adf8dcadac88ac", expectedScripts.ScriptPubKey.ToHex());
			Assert.Equal("76a914bc1bf603880f28ffaf9c888b5660e9adf8dcadac88ac", expectedScripts.ScriptCode.ToHex());
			Assert.Null(expectedScripts.RedeemScript);

			script = Miniscript.Parse("sh(pkh([aaaaaaaa/44h/1h/0h]tpubDDV486pBqkML6Ywhznz8DS3VS95h3q4A2pUMCc6yy739QpKMg3gA8EXGrjraDBDxrhLsezepjCEfBtak5wngDH4vMh6aXKV8hPN7JsMtdEf/<0;1>/*))", settings);
			expectedScripts = script.Derive(AddressIntent.Deposit, 0).Miniscript.ToScripts();
			Assert.Equal("a91498062e6879a7b6643735a626ac129db470087a2f87", expectedScripts.ScriptPubKey.ToHex());
			Assert.Equal("76a914bc1bf603880f28ffaf9c888b5660e9adf8dcadac88ac", expectedScripts.ScriptCode.ToHex());
			Assert.Equal("76a914bc1bf603880f28ffaf9c888b5660e9adf8dcadac88ac", expectedScripts.RedeemScript.ToHex());

			script = Miniscript.Parse("wsh(pkh([aaaaaaaa/44h/1h/0h]tpubDDV486pBqkML6Ywhznz8DS3VS95h3q4A2pUMCc6yy739QpKMg3gA8EXGrjraDBDxrhLsezepjCEfBtak5wngDH4vMh6aXKV8hPN7JsMtdEf/<0;1>/*))", settings);
			expectedScripts = script.Derive(AddressIntent.Deposit, 0).Miniscript.ToScripts();
			Assert.Equal("0020df3d2f624d5744aa959d0e87cfdfb33dec486a329aee2aeee8da17c98f7e999f", expectedScripts.ScriptPubKey.ToHex());
			Assert.Equal("76a914bc1bf603880f28ffaf9c888b5660e9adf8dcadac88ac", expectedScripts.ScriptCode.ToHex());
			Assert.Equal("76a914bc1bf603880f28ffaf9c888b5660e9adf8dcadac88ac", expectedScripts.RedeemScript.ToHex());

			script = Miniscript.Parse("sh(wsh(pkh([aaaaaaaa/44h/1h/0h]tpubDDV486pBqkML6Ywhznz8DS3VS95h3q4A2pUMCc6yy739QpKMg3gA8EXGrjraDBDxrhLsezepjCEfBtak5wngDH4vMh6aXKV8hPN7JsMtdEf/<0;1>/*)))", settings);
			expectedScripts = script.Derive(AddressIntent.Deposit, 0).Miniscript.ToScripts();
			Assert.Equal("a914151fd2c5490f9a6488a72fad3d8fa7bd1e3fab3387", expectedScripts.ScriptPubKey.ToHex());
			Assert.Equal("76a914bc1bf603880f28ffaf9c888b5660e9adf8dcadac88ac", expectedScripts.ScriptCode.ToHex());
			Assert.Equal("76a914bc1bf603880f28ffaf9c888b5660e9adf8dcadac88ac", expectedScripts.RedeemScript.ToHex());
		}

		[Fact]
		public void CanGenerateSH()
		{
			var root = new ExtKey().GetWif(Network.TestNet);
			var parsingSettings = new MiniscriptParsingSettings(root.Network) { Dialect = MiniscriptDialect.BIP388 };
			var account = root.Derive(new KeyPath("48'/1'/0'/2'"));
			var derivedPubKey = account.Derive(new KeyPath("1/123")).GetPublicKey();
			var keyNode = new HDKeyNode(new KeyPath("48'/1'/0'/2'").ToRootedKeyPath(root), account.Neuter());
			var pkh = PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(derivedPubKey);
			var miniscript = Miniscript.Parse("wsh(pkh(@0/**))", parsingSettings);
			miniscript = miniscript.ReplaceKeyPlaceholdersByHDKeys([keyNode]);
			miniscript = miniscript.Derive(AddressIntent.Change, 123).Miniscript;
			var scripts = miniscript.ToScripts();
			Assert.Equal(pkh.WitHash.ScriptPubKey, scripts.ScriptPubKey);
			Assert.Equal(pkh, scripts.RedeemScript);

			miniscript = Miniscript.Parse("sh(pkh(@0/**))", parsingSettings);
			miniscript = miniscript.ReplaceKeyPlaceholdersByHDKeys([keyNode]);
			miniscript = miniscript.Derive(AddressIntent.Change, 123).Miniscript;
			scripts = miniscript.ToScripts();
			Assert.Equal(pkh.Hash.ScriptPubKey, scripts.ScriptPubKey);
			Assert.Equal(pkh, scripts.RedeemScript);

			miniscript = Miniscript.Parse("sh(wsh(pkh(@0/**)))", parsingSettings);
			miniscript = miniscript.ReplaceKeyPlaceholdersByHDKeys([keyNode]);
			miniscript = miniscript.Derive(AddressIntent.Change, 123).Miniscript;
			scripts = miniscript.ToScripts();
			Assert.Equal(pkh.WitHash.ScriptPubKey.Hash.ScriptPubKey, scripts.ScriptPubKey);
			Assert.Equal(pkh, scripts.RedeemScript);
		}

		[Theory]
		[InlineData("hash256(03a195e87b81956f63837927446ffb42ace1675778264597b9aa0aa0d461b892)", "OP_SIZE 20 OP_EQUALVERIFY OP_HASH256 03a195e87b81956f63837927446ffb42ace1675778264597b9aa0aa0d461b892 OP_EQUAL")]
		[InlineData("sha256(03a195e87b81956f63837927446ffb42ace1675778264597b9aa0aa0d461b892)", "OP_SIZE 20 OP_EQUALVERIFY OP_SHA256 03a195e87b81956f63837927446ffb42ace1675778264597b9aa0aa0d461b892 OP_EQUAL")]
		[InlineData("ripemd160(03a195e87b81956f63837927446ffb42ace16757)", "OP_SIZE 20 OP_EQUALVERIFY OP_RIPEMD160 03a195e87b81956f63837927446ffb42ace16757 OP_EQUAL")]
		[InlineData("hash160(03a195e87b81956f63837927446ffb42ace16757)", "OP_SIZE 20 OP_EQUALVERIFY OP_HASH160 03a195e87b81956f63837927446ffb42ace16757 OP_EQUAL")]
		[InlineData("pk(Alice)", "<Alice> OP_CHECKSIG")]
		[InlineData("pk(L4ufc8BCfbWH73SNfJpYBR8nVZmdRqfZu8wCwVXy1nWC3Ac5uMz2)", "029c6d96193c911a05fd9d0583cbb30fe2844f26a312aa58384e0b5d8b9bbfae23 OP_CHECKSIG")]
		[InlineData("v:pk(Alice)", "<Alice> OP_CHECKSIGVERIFY")]
		[InlineData("dv:older(144)", "OP_DUP OP_IF 9000 OP_CSV OP_VERIFY OP_ENDIF")]
		[InlineData("or_b(pk(key_1),s:pk(key_2))", "<key_1> OP_CHECKSIG OP_SWAP <key_2> OP_CHECKSIG OP_BOOLOR")]
		[InlineData("or_d(pk(key_likely),pkh(key_unlikely))", "<key_likely> OP_CHECKSIG OP_IFDUP OP_NOTIF OP_DUP OP_HASH160 <HASH160(key_unlikely)> OP_EQUALVERIFY OP_CHECKSIG OP_ENDIF")]
		[InlineData("and_v(v:pk(key_user),or_d(pk(key_service),older(12960)))", "<key_user> OP_CHECKSIGVERIFY <key_service> OP_CHECKSIG OP_IFDUP OP_NOTIF a032 OP_CSV OP_ENDIF")]
		[InlineData("thresh(3,pk(key_1),s:pk(key_2),s:pk(key_3),sln:older(12960))", "<key_1> OP_CHECKSIG OP_SWAP <key_2> OP_CHECKSIG OP_ADD OP_SWAP <key_3> OP_CHECKSIG OP_ADD OP_SWAP OP_IF 0 OP_ELSE a032 OP_CSV OP_0NOTEQUAL OP_ENDIF OP_ADD 3 OP_EQUAL")]
		[InlineData("andor(pk(key_local),older(1008),pk(key_revocation))", "<key_local> OP_CHECKSIG OP_NOTIF <key_revocation> OP_CHECKSIG OP_ELSE f003 OP_CSV OP_ENDIF")]
		[InlineData("t:or_c(pk(key_revocation),and_v(v:pk(key_remote),or_c(pk(key_local),v:hash160(hash_value))))", "<key_revocation> OP_CHECKSIG OP_NOTIF <key_remote> OP_CHECKSIGVERIFY <key_local> OP_CHECKSIG OP_NOTIF OP_SIZE 20 OP_EQUALVERIFY OP_HASH160 <hash_value> OP_EQUALVERIFY OP_ENDIF OP_ENDIF 1")]
		[InlineData("andor(pk(key_remote),or_i(and_v(v:pkh(key_local),hash160(hash_value)),older(1008)),pk(key_revocation))", "<key_remote> OP_CHECKSIG OP_NOTIF <key_revocation> OP_CHECKSIG OP_ELSE OP_IF OP_DUP OP_HASH160 <HASH160(key_local)> OP_EQUALVERIFY OP_CHECKSIGVERIFY OP_SIZE 20 OP_EQUALVERIFY OP_HASH160 <hash_value> OP_EQUAL OP_ELSE f003 OP_CSV OP_ENDIF OP_ENDIF")]
		[InlineData("multi(2,A_key,B_key,C_key)", "2 <A_key> <B_key> <C_key> 3 OP_CHECKMULTISIG")]
		[InlineData("sortedmulti(2,021668c18319ca953898f6346e42d16679ed721506814082d3851a6c26e104d233,03bbcb66e7c5ed0da2d72e6bf3dea4aa5bfc40e33ef9a0f878310a5bfddab97969,027305bfd28e0baa3c18121c9500bed3084dc9074001e9de341122a412af5a0eac)", "2 021668c18319ca953898f6346e42d16679ed721506814082d3851a6c26e104d233 027305bfd28e0baa3c18121c9500bed3084dc9074001e9de341122a412af5a0eac 03bbcb66e7c5ed0da2d72e6bf3dea4aa5bfc40e33ef9a0f878310a5bfddab97969 3 OP_CHECKMULTISIG")]
		[InlineData("sortedmulti(1,04a34b99f22c790c4e36b2b3c2c35a36db06226e41c692fc82b8b56ac1c540c5bd5b8dec5235a0fa8722476c7709c02559e3aa73aa03918ba2d492eea75abea235,03a34b99f22c790c4e36b2b3c2c35a36db06226e41c692fc82b8b56ac1c540c5bd)", "1 03a34b99f22c790c4e36b2b3c2c35a36db06226e41c692fc82b8b56ac1c540c5bd 04a34b99f22c790c4e36b2b3c2c35a36db06226e41c692fc82b8b56ac1c540c5bd5b8dec5235a0fa8722476c7709c02559e3aa73aa03918ba2d492eea75abea235 2 OP_CHECKMULTISIG")]
		[InlineData("multi(1,03a34b99f22c790c4e36b2b3c2c35a36db06226e41c692fc82b8b56ac1c540c5bd,04a34b99f22c790c4e36b2b3c2c35a36db06226e41c692fc82b8b56ac1c540c5bd5b8dec5235a0fa8722476c7709c02559e3aa73aa03918ba2d492eea75abea235)", "1 03a34b99f22c790c4e36b2b3c2c35a36db06226e41c692fc82b8b56ac1c540c5bd 04a34b99f22c790c4e36b2b3c2c35a36db06226e41c692fc82b8b56ac1c540c5bd5b8dec5235a0fa8722476c7709c02559e3aa73aa03918ba2d492eea75abea235 2 OP_CHECKMULTISIG")]
		[InlineData("multi_a(2,A_key,B_key,C_key)", "<A_key> OP_CHECKSIG <B_key> OP_CHECKSIGADD <C_key> OP_CHECKSIGADD 2 OP_NUMEQUAL")]
		[InlineData("older(A)", "<A> OP_CSV")]
		[InlineData("l:A", "OP_IF 0 OP_ELSE <A> OP_ENDIF")]
		[InlineData("u:A", "OP_IF <A> OP_ELSE 0 OP_ENDIF")]
		[InlineData("j:A", "OP_SIZE OP_0NOTEQUAL OP_IF <A> OP_ENDIF")]
		[InlineData("d:A", "OP_DUP OP_IF <A> OP_ENDIF")]
		public void CanGenerateScript(string miniscript, string expected)
		{
			var parsed = Miniscript.Parse(miniscript, new MiniscriptParsingSettings(Network.Main, KeyType.Classic) { AllowedParameters = ParameterTypeFlags.All });
			Assert.Equal(miniscript, parsed.ToString()); // Sanity check
			Assert.Equal(expected, parsed.ToScriptCodeString());
		}

		[Fact]
		public void CanParseMusigExpression()
		{
			var settings = new MiniscriptParsingSettings(Network.Main) { Dialect = MiniscriptDialect.BIP388 };
			var miniscript = "tr(musig(02f9308a019258c31049344f85f89d5229b531c845836f99b08601f113bce036f9,03dff1d77f2a671c5f36183726db2341be58feae1da2deced843240f7b502ba659,023590a94e768f8e1815c2f24b4d80a8e3149316c3518ce7b7ad338368d038ca66))";
			var script = Miniscript.Parse(miniscript, settings);
			Assert.Equal("512079e6c3e628c9bfbce91de6b7fb28e2aec7713d377cf260ab599dcbc40e542312", script.ToScripts().ScriptPubKey.ToHex());

			miniscript = "tr(f9308a019258c31049344f85f89d5229b531c845836f99b08601f113bce036f9,pk(musig(xpub6ERApfZwUNrhLCkDtcHTcxd75RbzS1ed54G1LkBUHQVHQKqhMkhgbmJbZRkrgZw4koxb5JaHWkY4ALHY2grBGRjaDMzQLcgJvLJuZZvRcEL,xpub68NZiKmJWnxxS6aaHmn81bvJeTESw724CRDs6HbuccFQN9Ku14VQrADWgqbhhTHBaohPX4CjNLf9fq9MYo6oDaPPLPxSb7gwQN3ih19Zm4Y)/**))";

			string[] expected = ["512068983d461174afc90c26f3b2821d8a9ced9534586a756763b68371a404635cc8",
			"5120368e2d864115181bdc8bb5dc8684be8d0760d5c33315570d71a21afce4afd43e",
			"512097a1e6270b33ad85744677418bae5f59ea9136027223bc6e282c47c167b471d5"];
			for (int i = 0; i < expected.Length; i++)
			{
				script = Miniscript.Parse(miniscript, new MiniscriptParsingSettings(Network.Main) { Dialect = MiniscriptDialect.BIP388 });
				script = script.Derive(AddressIntent.Deposit, i).Miniscript;
				Assert.Equal(expected[i], script.ToScripts().ScriptPubKey.ToHex());
			}
			settings.AllowedParameters = ParameterTypeFlags.All;
			Assert.True(Miniscript.TryParse("tr(musig(A,B,C))", settings, out _));
			// Support nested
			Assert.True(Miniscript.TryParse("tr(musig(musig(A,B),C))", settings, out _));
			Assert.True(Miniscript.TryParse("tr(musig(musig(@0/**,@1/**),@2/**))", settings, out _));
			Assert.True(Miniscript.TryParse("tr(musig(A,B)/**)", settings, out _));
			// Support nested multipath
			Assert.True(Miniscript.TryParse("tr(musig(@0/**,B)/**)", settings, out _));

			miniscript = "tr(musig(@0/**,@1/**,musig(@2/**,@3/**))/**)";
			var m = Miniscript.Parse(miniscript, settings);
			Assert.Equal(miniscript, m.ToString());
			var keys = GenerateKeys(4);
			m = m.ReplaceKeyPlaceholdersByHDKeys(keys);
			m = m.Derive(AddressIntent.Deposit, 1).Miniscript;

			var dKeys = keys.Select(k => k.Key.Derive(0).Derive(1).GetPublicKey().ECKey).ToArray();
			var musigNested = ECPubKey.MusigAggregate([dKeys[2], dKeys[3]], true);
			var musig = ECPubKey.MusigAggregate([dKeys[0], dKeys[1], musigNested], true);
			var expectedPubKey = new ExtPubKey(new PubKey(musig, true), DeriveVisitor.BIP0328CC).Derive(0).Derive(1).GetPublicKey();
			var expectedScript = expectedPubKey.GetScriptPubKey(ScriptPubKeyType.TaprootBIP86);
			Assert.Equal(expectedScript, m.ToScripts().ScriptPubKey);

			var pks1 =
				Enumerable.Range(0, 3)
				.Select(i => ExtPubKey.Parse("xpub6ERApfZwUNrhLCkDtcHTcxd75RbzS1ed54G1LkBUHQVHQKqhMkhgbmJbZRkrgZw4koxb5JaHWkY4ALHY2grBGRjaDMzQLcgJvLJuZZvRcEL", Network.Main).Derive((uint)i))
				.ToArray();
			var pks2 =
				Enumerable.Range(0, 3)
				.Select(i => ExtPubKey.Parse("xpub68NZiKmJWnxxS6aaHmn81bvJeTESw724CRDs6HbuccFQN9Ku14VQrADWgqbhhTHBaohPX4CjNLf9fq9MYo6oDaPPLPxSb7gwQN3ih19Zm4Y", Network.Main).Derive(new KeyPath("0/0")).Derive((uint)i))
				.ToArray();
			string[] expectedScripts =
				[
				"5120abd47468515223f58a1a18edfde709a7a2aab2b696d59ecf8c34f0ba274ef772",
				"5120fe62e7ed20705bd1d3678e072bc999acb014f07795fa02cb8f25a7aa787e8cbd",
				"51201311093750f459039adaa2a5ed23b0f7a8ae2c2ffb07c5390ea37e2fb1050b41"
				];
			miniscript = "tr(50929b74c1a04954b78b4b6035e97a5e078a5a0f28ec96d547bfee9ace803ac0,sortedmulti_a(2,A,B))";
			m = Miniscript.Parse(miniscript, settings);
			Assert.Equal(miniscript, m.ToString());
			for (int i = 0; i < pks1.Length; i++)
			{
				var pm = m.ReplaceParameters(new()
				{
					["A"] = MiniscriptNode.Create(pks1[i].GetPublicKey().TaprootPubKey),
					["B"] = MiniscriptNode.Create(pks2[i].GetPublicKey().TaprootPubKey)
				});
				var generated = Encoders.Hex.EncodeData(pm.ToScripts().ScriptPubKey.ToBytes());
				Assert.Equal(expectedScripts[i], generated);
			}
		}

		[Fact]
		public void CanHandleSpaces()
		{
			var script = Miniscript.Parse("and_v(v:pk ( A ),pk (B) )", new MiniscriptParsingSettings(Network.Main, KeyType.Classic) { AllowedParameters = ParameterTypeFlags.NamedParameter });
			Assert.Equal("and_v(v:pk(A),pk(B))", script.ToString());
		}

		[Fact]
		public void CanReplaceParameters()
		{
			var parsed = Miniscript.Parse("and_v(or_c(pk(A),or_c(pk(B),v:older(C))),pk(A))", new MiniscriptParsingSettings(Network.Main, KeyType.Classic) { AllowedParameters = ParameterTypeFlags.NamedParameter });
			var a = new Key().PubKey;
			var b = new Key().PubKey;
			new MiniscriptParsingSettings(Network.Main, KeyType.Classic);
			var exception = Assert.Throws<MiniscriptReplacementException>(() => parsed.ReplaceParameters(new()
			{
				{ "A", new MiniscriptNode.Value.LockTimeValue(1) }
			}));
			Assert.Equal("A", exception.ParameterName);
			Assert.IsType<MiniscriptNode.ParameterRequirement.Key>(exception.Requirement);

			parsed = parsed.ReplaceParameters(new()
			{
				{ "A", new MiniscriptNode.Value.PubKeyValue(a) },
				{ "B", new MiniscriptNode.Value.PubKeyValue(b) }
			});
			Assert.Equal($"and_v(or_c(pk({a}),or_c(pk({b}),v:older(C))),pk({a}))", parsed.ToString());
			Assert.Single(parsed.Parameters);
			parsed = parsed.ReplaceParameters(new()
			{
				{ "C", new MiniscriptNode.Value.LockTimeValue(new LockTime(10)) },
			});
			Assert.Equal($"and_v(or_c(pk({a}),or_c(pk({b}),v:older(10))),pk({a}))", parsed.ToString());
			Assert.Empty(parsed.Parameters);
		}

		[Theory]
		[InlineData("", typeof(MiniscriptError.IncompleteExpression))]
		[InlineData("and_v(v:pk(A),older(A))", typeof(MiniscriptError.MixedParameterType))]
		[InlineData("and_v(v:pk(A,B),older(A))", typeof(MiniscriptError.TooManyParameters))]
		[InlineData("and_v(older(A))", typeof(MiniscriptError.TooFewParameters))]
		[InlineData("and_v(older(A)", typeof(MiniscriptError.IncompleteExpression))]
		[InlineData("and_v", typeof(MiniscriptError.IncompleteExpression))]
		[InlineData("v:pk(A", typeof(MiniscriptError.IncompleteExpression))]
		[InlineData("ando(", typeof(MiniscriptError.UnknownFragmentName))]
		[InlineData("ripemd160(03a195e87b81956f63837927446ffb42ace1675)", typeof(MiniscriptError.HashExpected))]
		[InlineData("hash160(03a195e87b81956f63837927446ffb42ace167)", typeof(MiniscriptError.HashExpected))]
		[InlineData("hash160(03a195e87b81956f63837927446ffb42ace1670000)", typeof(MiniscriptError.HashExpected))]
		[InlineData("hash256(03a195e87b81956f63837927446ffb42ace1675778264597b9aa0aa0d461b89)", typeof(MiniscriptError.HashExpected))]
		[InlineData("sha256(03a195e87b81956f63837927446ffb42ace1675778264597b9aa0aa0d461b8)", typeof(MiniscriptError.HashExpected))]
		[InlineData("multi(2,A)", typeof(MiniscriptError.TooFewParameters))]
		[InlineData("thresh(2,A)", typeof(MiniscriptError.TooFewParameters))]
		[InlineData("sh(A)", typeof(MiniscriptError.UnknownFragmentName))]
		[InlineData("wpkh(A)", typeof(MiniscriptError.UnknownFragmentName))]
		[InlineData("tr(A)", typeof(MiniscriptError.UnknownFragmentName))]
		[InlineData("pkh(A)B", typeof(MiniscriptError.UnexpectedToken))]
		[InlineData("multi(2,545a50b9996bf8573999af86cab671204b7c6453cf953ef00ec5ad613b6c5689)", typeof(MiniscriptError.KeyExpected))]
		[InlineData("multi_a(2,02545a50b9996bf8573999af86cab671204b7c6453cf953ef00ec5ad613b6c5689)", typeof(MiniscriptError.KeyExpected))]
		public void CheckMiniscriptErrors(string miniscript, Type expectedError)
		{
			Assert.False(Miniscript.TryParse(miniscript, new MiniscriptParsingSettings(Network.Main, KeyType.Classic) { AllowedParameters = ParameterTypeFlags.All }, out var error, out _));
			Assert.NotNull(error);
			Assert.IsType(expectedError, error);
		}
		[Theory]
		[InlineData("multi(2,545a50b9996bf8573999af86cab671204b7c6453cf953ef00ec5ad613b6c5689)", typeof(MiniscriptError.InvalidTopFragment))]
		[InlineData("and_v(v:pk(A,B),older(A))", typeof(MiniscriptError.InvalidTopFragment))]
		[InlineData("and_v(older(A))", typeof(MiniscriptError.InvalidTopFragment))]
		[InlineData("sh(wsh(sh(pkh(A))))", typeof(MiniscriptError.UnknownFragmentName))]
		public void CheckMiniscriptBIP388Errors(string miniscript, Type expectedError)
		{
			Assert.False(Miniscript.TryParse(miniscript, new MiniscriptParsingSettings(Network.Main) { Dialect = MiniscriptDialect.BIP388 }, out var error, out _));
			Assert.NotNull(error);
			Assert.IsType(expectedError, error);
		}

		[Theory]
		[InlineData("@0/**", "@0", 0, 1)]
		[InlineData("@1/**", "@1", 0, 1)]
		[InlineData("@1/<0;1>/*", "@1", 0, 1)]
		[InlineData("@3/<2;3>/*", "@3", 2, 3)]
		public void CanParseMultiPathParameter(string str, string name, int deposit, int change)
		{
			Assert.True(MultipathNode.TryParse(str, Network.TestNet, out var key) && key.Target is Parameter);
			var p = (Parameter)key.Target;
			Assert.Equal((name, deposit, change), (p.Name, key.DepositIndex, key.ChangeIndex));
			var shortForm = str.Contains("/**");
			if (shortForm || !key.CanUseShortForm)
				Assert.Equal(str, key.ToString());
		}
		[Theory]
		[InlineData(AddressIntent.Deposit)]
		[InlineData(AddressIntent.Change)]
		public void CanMassDeriveMiniscripts(AddressIntent intent)
		{
			HDKeyNode[] keys = GenerateKeys(3);
			var miniscript = Miniscript.Parse("multi(2,@0/<0;1>/*,@1/<3;4>/*,@2/**)", Network.Main);
			miniscript = miniscript.ReplaceParameters(new()
			{
				{ "@0", keys[0]},
				{ "@1", keys[1]},
				{ "@2", keys[2]},
			});
			var indexes = Enumerable.Range(15, 5).ToArray();
			var allMiniscripts = miniscript.Derive(new DeriveParameters(intent, indexes));
			Assert.Equal(5, allMiniscripts.Length);

			var typeIdx = intent is AddressIntent.Deposit ? new[] { 0, 3, 0 } : new[] { 1, 4, 1 };
			var a = keys[0].Key.Derive((uint)typeIdx[0]).Derive(15 + 3).GetPublicKey();
			var b = keys[1].Key.Derive((uint)typeIdx[1]).Derive(15 + 3).GetPublicKey();
			var c = keys[2].Key.Derive((uint)typeIdx[2]).Derive(15 + 3).GetPublicKey();
			var derived = allMiniscripts[3].Miniscript;
			var expected = $"multi(2,{a},{b},{c})";
			Assert.Equal(expected, derived.ToString());

			var derivedKey = allMiniscripts[3].DerivedKeys[keys[0]];
			Assert.Equal(new KeyPath($"{typeIdx[0]}/{15 + 3}"), derivedKey.KeyPath);
			Assert.Equal(a, ((Value.PubKeyValue)derivedKey.Pubkey).PubKey);
		}

		private static HDKeyNode[] GenerateKeys(int count)
		{
			return Enumerable.Range(0, count).Select(_ =>
			{
				var root = new ExtKey().GetWif(Network.RegTest);
				return new HDKeyNode(new KeyPath("48'/1'/0'").ToRootedKeyPath(root.ExtKey), root.Neuter());
			}).ToArray();
		}

		[Fact]
		public void CanManipulateKeyExpression()
		{
			var parsing = new MiniscriptParsingSettings(Network.RegTest) { Dialect = MiniscriptDialect.BIP388 };
			var keyExpr = HDKeyNode.Parse("[d4ab66f1/48'/1'/0'/2']tpubDEXYN145WM4rVKtcWpySBYiVQ229pmrnyAGJT14BBh2QJr7ABJswchDicZfFaauLyXhDad1nCoCZQEwAW87JPotP93ykC9WJvoASnBjYBxW", Network.RegTest);
			var miniscript = Miniscript.Parse("pkh(@0/<0;1>/*)", parsing);
			miniscript = miniscript.ReplaceParameters(new()
			{
				{ "@0", keyExpr},
			});
			Assert.Equal("pkh([d4ab66f1/48'/1'/0'/2']tpubDEXYN145WM4rVKtcWpySBYiVQ229pmrnyAGJT14BBh2QJr7ABJswchDicZfFaauLyXhDad1nCoCZQEwAW87JPotP93ykC9WJvoASnBjYBxW/<0;1>/*)", miniscript.ToString());
			Assert.Equal(Network.RegTest, miniscript.Network);

			// Make it readable again
			var noKeys = miniscript.ReplaceHDKeysByKeyPlaceholders(out var hdKeys);
			Assert.Equal("pkh(@0/<0;1>/*)", noKeys.ToString());
			Assert.Equal("OP_DUP OP_HASH160 <HASH160(@0/<0;1>/*)> OP_EQUALVERIFY OP_CHECKSIG", noKeys.ToScriptString());

			// Can We reverse operation?
			miniscript = noKeys.ReplaceKeyPlaceholdersByHDKeys(hdKeys);
			Assert.Equal("pkh([d4ab66f1/48'/1'/0'/2']tpubDEXYN145WM4rVKtcWpySBYiVQ229pmrnyAGJT14BBh2QJr7ABJswchDicZfFaauLyXhDad1nCoCZQEwAW87JPotP93ykC9WJvoASnBjYBxW/<0;1>/*)", miniscript.ToString());
			Assert.Equal(Network.RegTest, miniscript.Network);

			// Can we parse the multi path key information?
			miniscript = Miniscript.Parse(miniscript.ToString(), parsing);

			miniscript = miniscript.Derive(AddressIntent.Deposit, 50).Miniscript;
			Assert.Equal("pkh(035061f24ab15de479008738557f7120a0c6299ceb1033669303473837b4314342)", miniscript.ToString());

			Assert.Equal("pkh(035061f24ab15de479008738557f7120a0c6299ceb1033669303473837b4314342)", $"pkh({keyExpr.Key.Derive(new KeyPath("0/50")).GetPublicKey()})");

			// Let's check how it works with two parameters
			var multi = Miniscript.Parse("multi(2,a,b)", new MiniscriptParsingSettings(miniscript.Network) { AllowedParameters = ParameterTypeFlags.NamedParameter });
			var a = CreateMultiPathKeyInformation();
			var b = CreateMultiPathKeyInformation();
			multi = multi.ReplaceParameters(new()
			{
				{ "a",  a },
				{ "b",  b },
			}).ReplaceHDKeysByKeyPlaceholders(out _);
			Assert.Equal("multi(2,@0/<1;2>/*,@1/<1;2>/*)", multi.ToString());
		}

		private MultipathNode CreateMultiPathKeyInformation()
		{
			var k = new ExtKey().GetWif(Network.RegTest);
			var root = k;
			var accountKeyPath = new KeyPath("44'/1'");
			var account = k.Derive(accountKeyPath).Neuter();
			var hdKey = new HDKeyNode(new RootedKeyPath(root.GetPublicKey().GetHDFingerPrint(), accountKeyPath), account);
			return new MultipathNode(1, 2, hdKey, true);
		}

		[Fact]
		public void CanParseMultiPathHDKey()
		{
			string i = "[d4ab66f1/48'/1'/0'/2']tpubDEXYN145WM4rVKtcWpySBYiVQ229pmrnyAGJT14BBh2QJr7ABJswchDicZfFaauLyXhDad1nCoCZQEwAW87JPotP93ykC9WJvoASnBjYBxW/<0;1>/*";
			var k = MultipathNode.Parse(i, Network.RegTest);
			var pk = (HDKeyNode)k.Target;
			Assert.Equal(new HDFingerprint(0xf166abd4), pk.RootedKeyPath.MasterFingerprint);
			Assert.Equal(new KeyPath("48'/1'/0'/2'"), pk.RootedKeyPath.KeyPath);
			Assert.Equal("d4ab66f1/48'/1'/0'/2'", pk.RootedKeyPath.ToString());
			Assert.Equal(new BitcoinExtPubKey("tpubDEXYN145WM4rVKtcWpySBYiVQ229pmrnyAGJT14BBh2QJr7ABJswchDicZfFaauLyXhDad1nCoCZQEwAW87JPotP93ykC9WJvoASnBjYBxW", Network.RegTest), pk.Key);
			Assert.Equal(0, k.DepositIndex);
			Assert.Equal(1, k.ChangeIndex);
		}
		[Fact]
		public void CanParseKeyInformation()
		{
			string i = "[d4ab66f1/48'/1'/0'/2']tpubDEXYN145WM4rVKtcWpySBYiVQ229pmrnyAGJT14BBh2QJr7ABJswchDicZfFaauLyXhDad1nCoCZQEwAW87JPotP93ykC9WJvoASnBjYBxW";
			var k = HDKeyNode.Parse(i, Network.RegTest);
			Assert.Equal(i, k.ToString());
			Assert.Equal(new HDFingerprint(0xf166abd4), k.RootedKeyPath.MasterFingerprint);
			Assert.Equal(new KeyPath("48'/1'/0'/2'"), k.RootedKeyPath.KeyPath);
			Assert.Equal("d4ab66f1/48'/1'/0'/2'", k.RootedKeyPath.ToString());
			Assert.Equal(new BitcoinExtPubKey("tpubDEXYN145WM4rVKtcWpySBYiVQ229pmrnyAGJT14BBh2QJr7ABJswchDicZfFaauLyXhDad1nCoCZQEwAW87JPotP93ykC9WJvoASnBjYBxW", Network.RegTest), k.Key);

			var xpriv = new ExtKey().GetWif(Network.RegTest);
			i = $"[d4ab66f1/48'/1'/0'/2']{xpriv}";
			k = HDKeyNode.Parse(i, Network.RegTest);
			Assert.Equal(i, k.ToString());
			Assert.Equal(new HDFingerprint(0xf166abd4), k.RootedKeyPath.MasterFingerprint);
			Assert.Equal(new KeyPath("48'/1'/0'/2'"), k.RootedKeyPath.KeyPath);
			Assert.Equal("d4ab66f1/48'/1'/0'/2'", k.RootedKeyPath.ToString());
			Assert.Equal(xpriv, k.Key);

			// Garbage at the end
			Assert.False(HDKeyNode.TryParse(i + "/", Network.RegTest, out _));
		}

		[Theory]
		[InlineData("@1")]
		[InlineData("@0/0/**")]
		[InlineData("@0/**/*")]
		[InlineData("@0/<2147483648,0>/*")]
		[InlineData("@0/<,-2>/*")]
		public void InvalidMultiPath(string str)
		{
			Assert.False(MultipathNode.TryParse(str, Network.Main, out _));
		}
	}
}
#endif
