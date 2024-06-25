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
			var parsed = Miniscript.Parse(miniscript, new MiniscriptParsingSettings(Network.RegTest, KeyType.Classic));
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
			var settings = new MiniscriptParsingSettings(Network.RegTest) { Dialect = MiniscriptDialect.BIP388 };
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
			parsed = parsed.Derive(AddressIntent.Deposit, 0);
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
				Log.WriteLine(ms);
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

			var address = policy.FullDescriptor.Derive(AddressIntent.Deposit, 1).ToScripts().ScriptPubKey.GetDestinationAddress(Network.TestNet);
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
			miniscript = miniscript.Derive(AddressIntent.Change, 123);
			var scripts = miniscript.ToScripts();
			var expected = derivedPubKey.GetScriptPubKey(scriptPubKeyType);
			Assert.Equal(expected, scripts.ScriptPubKey);

			var policy = WalletPolicy.Parse(miniscriptWithHDKeys.ToString(), root.Network);
			var k = Assert.Single(policy.KeyInformationVector);
			Assert.Equal(k, keyNode);
			Assert.Equal(str, policy.DescriptorTemplate.ToString());
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
			miniscript = miniscript.Derive(AddressIntent.Change, 123);
			var scripts = miniscript.ToScripts();
			Assert.Equal(pkh.WitHash.ScriptPubKey, scripts.ScriptPubKey);
			Assert.Equal(pkh, scripts.RedeemScript);

			miniscript = Miniscript.Parse("sh(pkh(@0/**))", parsingSettings);
			miniscript = miniscript.ReplaceKeyPlaceholdersByHDKeys([keyNode]);
			miniscript = miniscript.Derive(AddressIntent.Change, 123);
			scripts = miniscript.ToScripts();
			Assert.Equal(pkh.Hash.ScriptPubKey, scripts.ScriptPubKey);
			Assert.Equal(pkh, scripts.RedeemScript);

			miniscript = Miniscript.Parse("sh(wsh(pkh(@0/**)))", parsingSettings);
			miniscript = miniscript.ReplaceKeyPlaceholdersByHDKeys([keyNode]);
			miniscript = miniscript.Derive(AddressIntent.Change, 123);
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
		[InlineData("multi_a(2,A_key,B_key,C_key)", "<A_key> OP_CHECKSIG <B_key> OP_CHECKSIGADD <C_key> OP_CHECKSIGADD 2 OP_NUMEQUAL")]
		[InlineData("older(A)", "<A> OP_CSV")]
		[InlineData("l:A", "OP_IF 0 OP_ELSE <A> OP_ENDIF")]
		[InlineData("u:A", "OP_IF <A> OP_ELSE 0 OP_ENDIF")]
		[InlineData("j:A", "OP_SIZE OP_0NOTEQUAL OP_IF <A> OP_ENDIF")]
		[InlineData("d:A", "OP_DUP OP_IF <A> OP_ENDIF")]
		public void CanGenerateScript(string miniscript, string expected)
		{
			var parsed = Miniscript.Parse(miniscript, new MiniscriptParsingSettings(Network.Main, KeyType.Classic));
			Assert.Equal(miniscript, parsed.ToString()); // Sanity check
			Assert.Equal(expected, parsed.ToScriptCodeString());
		}

		[Fact]
		public void CanHandleSpaces()
		{
			var script = Miniscript.Parse("and_v(v:pk ( A ),pk (B) )", new MiniscriptParsingSettings(Network.Main, KeyType.Classic));
			Assert.Equal("and_v(v:pk(A),pk(B))", script.ToString());
		}

		[Fact]
		public void CanReplaceParameters()
		{
			var parsed = Miniscript.Parse("and_v(or_c(pk(A),or_c(pk(B),v:older(C))),pk(A))", new MiniscriptParsingSettings(Network.Main, KeyType.Classic));
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
			Assert.False(Miniscript.TryParse(miniscript, new MiniscriptParsingSettings(Network.Main, KeyType.Classic), out var error, out _));
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
			var a = keys[0].PubKey.Derive((uint)typeIdx[0]).Derive(15 + 3).GetPublicKey();
			var b = keys[1].PubKey.Derive((uint)typeIdx[1]).Derive(15 + 3).GetPublicKey();
			var c = keys[2].PubKey.Derive((uint)typeIdx[2]).Derive(15 + 3).GetPublicKey();
			var derived = allMiniscripts[3];
			var expected = $"multi(2,{a},{b},{c})";
			Assert.Equal(expected, derived.ToString());
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

			miniscript = miniscript.Derive(AddressIntent.Deposit, 50);
			Assert.Equal("pkh(035061f24ab15de479008738557f7120a0c6299ceb1033669303473837b4314342)", miniscript.ToString());

			Assert.Equal("pkh(035061f24ab15de479008738557f7120a0c6299ceb1033669303473837b4314342)", $"pkh({keyExpr.PubKey.Derive(new KeyPath("0/50")).GetPublicKey()})");

			// Let's check how it works with two parameters
			var multi = Miniscript.Parse("multi(2,a,b)", miniscript.Network);
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
			Assert.Equal(new BitcoinExtPubKey("tpubDEXYN145WM4rVKtcWpySBYiVQ229pmrnyAGJT14BBh2QJr7ABJswchDicZfFaauLyXhDad1nCoCZQEwAW87JPotP93ykC9WJvoASnBjYBxW", Network.RegTest), pk.PubKey);
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
			Assert.Equal(new BitcoinExtPubKey("tpubDEXYN145WM4rVKtcWpySBYiVQ229pmrnyAGJT14BBh2QJr7ABJswchDicZfFaauLyXhDad1nCoCZQEwAW87JPotP93ykC9WJvoASnBjYBxW", Network.RegTest), k.PubKey);
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
