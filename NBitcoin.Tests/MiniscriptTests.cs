#if !NO_RECORDS
using NBitcoin.DataEncoders;
using NBitcoin.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace NBitcoin.Tests
{
	[Trait("UnitTest", "UnitTest")]
	public class MiniscriptTests
	{
		[Theory]
		[InlineData("and_v(v:pk(A),pk(B))")]
		[InlineData("and_v(v:pk(A),pk(A))")]
		[InlineData("and_v(or_c(pk(B),or_c(pk(C),v:older(1000))),pk(A))")]
		[InlineData("and_v(or_c(pk(B),or_c(pk(C),v:older(D))),pk(A))")]
		//[InlineData("or_d(pk([d4ab66f1/48'/1'/0'/2']tpubDEXYN145WM4rVKtcWpySBYiVQ229pmrnyAGJT14BBh2QJr7ABJswchDicZfFaauLyXhDad1nCoCZQEwAW87JPotP93ykC9WJvoASnBjYBxW/<0;1>/*),and_v(v:pkh([33fa0ffc/48'/1'/0'/2']tpubDEqzYAym2MnGqKdqu2ZtGQkDTSrvDWCrcoamspjRJR78nr8w5tAgu371r8LtcyWWWXGemenTMxmoLhQM3ww8gUfobBXUWxLEkfR7kGjD6jC/<0;1>/*),older(65535)))")]
		[InlineData("or_c(pk(alice),and_v(pk(bob),older(timelock)))")]
		[InlineData("andor(pk(key_remote),or_i(and_v(v:pkh(key_local),hash160(H)),older(1008)),pk(key_revocation))")]
		[InlineData("0")]
		[InlineData("1")]
		[InlineData("pk_k(A)")]
		[InlineData("pk_k(@0/**)")]
		[InlineData("pk_k(@0/<0;1>/*)")]
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
			var parsed = Miniscript.Parse(miniscript);
			var actual = parsed.ToString();
			Assert.Equal(miniscript, actual);
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
			var parsed = Miniscript.Parse(miniscript);
			Assert.Equal(miniscript, parsed.ToString()); // Sanity check
			Assert.Equal(expected, parsed.ToScriptString());
		}

		[Fact]
		public void CanBuildSatisfactionPath()
		{
			var miniscript = Miniscript.Parse(
				"thresh(2, " +
				"pk(A)," +
				"multi_a(1,B,C)," +
				"hash160(D)," +
				"v:and_b(v:older(19),or_b(hash256(E),multi_a(2,H,I,J)))," +
				"andor(pk(K),pk(F),or_b(pk(G),pk(Z))))");

			var builder = miniscript.BuildSatisfactionPath();

			Assert.False(builder.Select(out _));
			// First choice to take, should be thresh
			Assert.Equal(miniscript.ToString(), builder.CurrentChoice.Fragment.ToString());
			Assert.Equal("pk(A)", builder.CurrentChoice.Parameters[0].ToString());
			builder.Select(out var choice, 3, 4);

			Assert.Equal("(3,4)", builder.GetPath().ToString());
			// I chose the v:and_b branch (3), so now it should ask me about or_b
			Assert.Equal("or_b(hash256(E),multi_a(2,H,I,J))", choice.Fragment.ToString());

			builder.Select(out choice, 1);
			Assert.Equal("multi_a(2,H,I,J)", choice.Fragment.ToString());

			builder.Select(out choice, 2, 0);
			// Then it should go back to andor(pk(K),pk(F),pk(G)) (4)
			Assert.Equal("andor(pk(K),pk(F),or_b(pk(G),pk(Z)))", choice.Fragment.ToString());

			Assert.False(builder.Select(out choice, choice.Parameters[1]));

			Assert.Equal("or_b(pk(G),pk(Z))", choice.Fragment.ToString());
			Assert.True(builder.Select(out choice, choice.Parameters[1]));
			Assert.Null(choice);

			var satisfactionPath = builder.GetPath();
			Assert.Equal("(3,4)/1/(0,2)/1/1", satisfactionPath.ToString());

			var satisfactions = miniscript.GetRequiredSatisfactions(satisfactionPath);
			
		}

		[Fact]
		public void CanHandleSpaces()
		{
			var script = Miniscript.Parse(" and_v(v:pk ( A ),pk (B) ) ");
			Assert.Equal("and_v(v:pk(A),pk(B))", script.ToString());
		}

		[Fact]
		public void CanReplaceParameters()
		{
			var parsed = Miniscript.Parse("and_v(or_c(pk(A),or_c(pk(B),v:older(C))),pk(A))");
			var a = new Key().PubKey;
			var b = new Key().PubKey;

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
		public void CheckMiniscriptErrors(string miniscript, Type expectedError)
		{
			Assert.False(Miniscript.TryParse(miniscript, out var error, out _));
			Assert.NotNull(error);
			Assert.IsType(expectedError, error);
		}

		[Theory]
		[InlineData("@0/**", 0, 0, 1)]
		[InlineData("@1/**", 1, 0, 1)]
		[InlineData("@1/<0;1>/*", 1, 0, 1)]
		[InlineData("@3/<2;3>/*", 3, 2, 3)]
		public void CanParseKeyPlaceHolder(string str, int idx, int deposit, int change)
		{
			Assert.True(KeyPlaceholder.TryParse(str, out var key));
			Assert.Equal((idx, deposit, change), (key.KeyIndex, key.Deposit, key.Change));
			Assert.Equal(str, key.ToString());
		}

		[Fact]
		public void CanManipulateKeyInformationInMiniscript()
		{
			var keyInfo = KeyInformation.Parse("[d4ab66f1/48'/1'/0'/2']tpubDEXYN145WM4rVKtcWpySBYiVQ229pmrnyAGJT14BBh2QJr7ABJswchDicZfFaauLyXhDad1nCoCZQEwAW87JPotP93ykC9WJvoASnBjYBxW", Network.RegTest);
			var miniscript = Miniscript.Parse("pk(@0/<0;1>/*)");
			miniscript = miniscript.ReplaceParameters(new()
			{
				{ "@0", MiniscriptNode.Create(keyInfo)},
			});
			Assert.Equal("pk([d4ab66f1/48'/1'/0'/2']tpubDEXYN145WM4rVKtcWpySBYiVQ229pmrnyAGJT14BBh2QJr7ABJswchDicZfFaauLyXhDad1nCoCZQEwAW87JPotP93ykC9WJvoASnBjYBxW/<0;1>/*)", miniscript.ToString());
			Assert.Equal(Network.RegTest, miniscript.Settings.Network);

			// Make it readable again
			var noKeys = miniscript.ReplaceKeyInformationByPlaceholder(false);
			Assert.Equal("pk(@0/<0;1>/*)", noKeys.ToString());
			noKeys = miniscript.ReplaceKeyInformationByPlaceholder(true);
			Assert.Equal("pk(@0/**)", noKeys.ToString());
			Assert.Equal("<@0/**> OP_CHECKSIG", noKeys.ToScriptString());

			// Can we parse the multi path key information?
			miniscript = Miniscript.Parse(miniscript.ToString(), miniscript.Settings);

			var beforeDeriv = miniscript;
			miniscript = miniscript.Derive(AddressIntent.Deposit, 50, KeyType.Taproot);
			Assert.Equal("pk(5061f24ab15de479008738557f7120a0c6299ceb1033669303473837b4314342)", miniscript.ToString());

			miniscript = beforeDeriv;
			miniscript = miniscript.Derive(AddressIntent.Deposit, 50, KeyType.Classic);
			Assert.Equal("pk(035061f24ab15de479008738557f7120a0c6299ceb1033669303473837b4314342)", miniscript.ToString());

			Assert.Equal("pk(035061f24ab15de479008738557f7120a0c6299ceb1033669303473837b4314342)", $"pk({keyInfo.PubKey.Derive(new KeyPath("0/50")).GetPublicKey()})");

			// Let's check how it works with two parameters
			var multi = Miniscript.Parse("multi(2,a,b)");
			var a = CreateMultiPathKeyInformation();
			var b = CreateMultiPathKeyInformation();
			multi = multi.ReplaceParameters(new()
			{
				{ "a",  MiniscriptNode.Create(a) },
				{ "b",  MiniscriptNode.Create(b) },
			}).ReplaceKeyInformationByPlaceholder();
			Assert.Equal("multi(2,@0/<1;2>/*,@1/<1;2>/*)", multi.ToString());
			// Classic should be guessed, because multi is not a taproot script
			Assert.Equal(KeyType.Classic, multi.Settings.KeyType);
		}

		private MultiPathKeyInformation CreateMultiPathKeyInformation()
		{
			var k = new ExtKey().GetWif(Network.RegTest);
			var root = k;
			var accountKeyPath = new KeyPath("44'/1'");
			var account = k.Derive(accountKeyPath).Neuter();
			return new MultiPathKeyInformation(new RootedKeyPath(root.GetPublicKey().GetHDFingerPrint(), accountKeyPath), account, 1, 2);
		}

		[Fact]
		public void CanParseMultiPathKeyInformation()
		{
			string i = "[d4ab66f1/48'/1'/0'/2']tpubDEXYN145WM4rVKtcWpySBYiVQ229pmrnyAGJT14BBh2QJr7ABJswchDicZfFaauLyXhDad1nCoCZQEwAW87JPotP93ykC9WJvoASnBjYBxW/<0;1>/*";
			var k = MultiPathKeyInformation.Parse(i, Network.RegTest);
			Assert.Equal(i, k.ToString());
			Assert.Equal(new HDFingerprint(0xf166abd4), k.RootedKeyPath.MasterFingerprint);
			Assert.Equal(new KeyPath("48'/1'/0'/2'"), k.RootedKeyPath.KeyPath);
			Assert.Equal("d4ab66f1/48'/1'/0'/2'", k.RootedKeyPath.ToString());
			Assert.Equal(new BitcoinExtPubKey("tpubDEXYN145WM4rVKtcWpySBYiVQ229pmrnyAGJT14BBh2QJr7ABJswchDicZfFaauLyXhDad1nCoCZQEwAW87JPotP93ykC9WJvoASnBjYBxW", Network.RegTest), k.PubKey);
			Assert.Equal(0, k.DepositIndex);
			Assert.Equal(1, k.ChangeIndex);
		}
		[Fact]
		public void CanParseKeyInformation()
		{
			string i = "[d4ab66f1/48'/1'/0'/2']tpubDEXYN145WM4rVKtcWpySBYiVQ229pmrnyAGJT14BBh2QJr7ABJswchDicZfFaauLyXhDad1nCoCZQEwAW87JPotP93ykC9WJvoASnBjYBxW";
			var k = KeyInformation.Parse(i, Network.RegTest);
			Assert.Equal(i, k.ToString());
			Assert.Equal(new HDFingerprint(0xf166abd4), k.RootedKeyPath.MasterFingerprint);
			Assert.Equal(new KeyPath("48'/1'/0'/2'"), k.RootedKeyPath.KeyPath);
			Assert.Equal("d4ab66f1/48'/1'/0'/2'", k.RootedKeyPath.ToString());
			Assert.Equal(new BitcoinExtPubKey("tpubDEXYN145WM4rVKtcWpySBYiVQ229pmrnyAGJT14BBh2QJr7ABJswchDicZfFaauLyXhDad1nCoCZQEwAW87JPotP93ykC9WJvoASnBjYBxW", Network.RegTest), k.PubKey);
		}

		[Fact]
		public void KeyPlaceHolderEqualityCheck()
		{
			Assert.Equal(KeyPlaceholder.Parse("@0/**"), KeyPlaceholder.Parse("@0/<0;1>/*"));
			Assert.NotEqual(KeyPlaceholder.Parse("@0/**"), KeyPlaceholder.Parse("@0/<0;2>/*"));
		}

		[Theory]
		[InlineData("@1")]
		[InlineData("@0/0/**")]
		[InlineData("@0/**/*")]
		[InlineData("@0/<2147483648,0>/*")]
		[InlineData("@0/<,-2>/*")]
		public void InvalidKeyPlaceHolder(string str)
		{
			Assert.False(KeyPlaceholder.TryParse(str, out _));
		}
	}
}
#endif
