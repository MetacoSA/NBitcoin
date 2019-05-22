using Xunit;
using NBitcoin.Scripting;
using NBitcoin.Scripting.Parser;
using FsCheck.Xunit;
using FsCheck;
using NBitcoin.Tests.Generators;
using System;
using static NBitcoin.Tests.Helpers.PrimitiveUtils;
using NBitcoin.Crypto;
using System.Collections.Generic;

namespace NBitcoin.Tests
{
	public class MiniscriptTests
	{
		public Network Network { get; }
		public Key[] Keys { get; }

		public MiniscriptTests()
		{
			Arb.Register<AbstractPolicyGenerator>();
			Arb.Register<CryptoGenerator>();
			Arb.Register<ScriptGenerator>();
			Network = Network.Main;
			Keys = new Key[] { new Key(), new Key(), new Key() };
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void DSLParserTests()
		{
			var pk = Keys[0].PubKey;
			var pk2 = Keys[1].PubKey;
			var pk3 = Keys[2].PubKey;
			DSLParserTestCore("time(100)", AbstractPolicy.NewTime(100));
			DSLParserTestCore($"pk({pk})", AbstractPolicy.NewCheckSig(pk));
			DSLParserTestCore($"multi(2,{pk2},{pk3})", AbstractPolicy.NewMulti(2, new PubKey[] { pk2, pk3 }));
			DSLParserTestCore(
				$"and(time(10),pk({pk}))",
				AbstractPolicy.NewAnd(
					AbstractPolicy.NewTime(10),
					AbstractPolicy.NewCheckSig(pk)
				)
			);
			DSLParserTestCore(
				$"and(time(10),and(pk({pk}),multi(2,{pk2},{pk3})))",
				AbstractPolicy.NewAnd(
					AbstractPolicy.NewTime(10),
					AbstractPolicy.NewAnd(
						AbstractPolicy.NewCheckSig(pk),
						AbstractPolicy.NewMulti(2, new PubKey[] { pk2, pk3 })
					)
				)
			);

			DSLParserTestCore(
				$"thres(2,time(100),multi(2,{pk2},{pk3}))",
				AbstractPolicy.NewThreshold(
					2,
					new AbstractPolicy[] {
						AbstractPolicy.NewTime(100),
						AbstractPolicy.NewMulti(2, new [] {pk2, pk3})
					}
				)
			);

			// Bigger than max possible blocknumber of OP_CSV
			Assert.Throws<ParseException>(() => MiniscriptDSLParser.DSLParser.Parse($"time(65536)"));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void DSLSubParserTest()
		{
			var pk = new Key().PubKey;
			var pk2 = new Key().PubKey;
			var pk3 = new Key().PubKey;
			var res = MiniscriptDSLParser.PThresholdExpr.Parse($"thres(2,time(100),multi(2,{pk2},{pk3}))");
			Assert.Equal(
				res,
				AbstractPolicy.NewThreshold(
					2,
					new AbstractPolicy[] {
						AbstractPolicy.NewTime(100),
						AbstractPolicy.NewMulti(2, new [] {pk2, pk3})
					}
				)
			);
		}

		private void DSLParserTestCore(string expr, AbstractPolicy expected)
		{
			var res = MiniscriptDSLParser.ParseDSL(expr);
			Assert.Equal(expected, res);
		}

		[Property]
		[Trait("PropertyTest", "BidrectionalConversion")]
		public void PolicyShouldConvertToDSLBidirectionally(AbstractPolicy policy)
			=> Assert.Equal(policy, MiniscriptDSLParser.ParseDSL(policy.ToString()));

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void PolicyToAstConversionTest()
		{
			var p = AbstractPolicy.NewHash(new uint256(0xdeadbeef));
			var p2 = CompiledNode.FromPolicy(p).BestT(0.0, 0.0).Ast.ToPolicy();
			Assert.Equal(p, p2);
		}

		[Property]
		[Trait("PropertyTest", "Verification")]
		public void PolicyShouldCompileToASTAndGetsBack(AbstractPolicy policy)
			// Bidirectional conversion is impossible since there are no way to distinguish `or` and `aor`
			=> CompiledNode.FromPolicy(policy).BestT(0.0, 0.0).Ast.ToPolicy();

		[Property]
		[Trait("PropertyTest", "Verification")]
		public void PolicyShouldCompileToScript(AbstractPolicy policy)
			=> CompiledNode.FromPolicy(policy).BestT(0.0, 0.0).Ast.ToScript();

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void ScriptDeserializationTest1()
		{
			var pk1 = new Key().PubKey;
			var pk2 = new Key().PubKey;
			var pk3 = new Key().PubKey;
			var hash1 = new uint256(0xdeadbeef);
			var sc = new Script("027be8d8ffe2f50ab5afcebf29ec2c9c75b50334905c9d15046e051c81a4ddbc68 OP_CHECKSIG");
			MiniscriptScriptParser.PPk.Parse(sc);
			MiniscriptScriptParser.PAstElemCore.Parse(sc);
			DeserializationTestCore(MiniscriptDSLParser.ParseDSL($"pk({pk1})"));
			var case1 = $"thres(1,aor(pk({pk1}),hash({hash1})),multi(1,{pk2},{pk3}))";
			DeserializationTestCore(MiniscriptDSLParser.ParseDSL(case1));
			DeserializationTestCore(MiniscriptDSLParser.ParseDSL($"and({case1},pk({pk1}))"));

			var htlcDSL = $"aor(and(hash({hash1}),pk({Keys[0].PubKey})),and(pk({Keys[1].PubKey}),time(10000)))";
			DeserializationTestCore(htlcDSL);

			var dsl = "thres(1, pk(02130c1c9a68369f14e4ce5c58acaa9d592ef8c5dcaf0a9d0fe92321c4bbc64eb3), aor(hash(bcf07a5893c7512fb9f4280690cbffdd6745d6b43e1c578b15f32e62ecca5439), time(0)))";
			DeserializationTestCore(dsl);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void ScriptDeserializationTest2()
		{
			// and_cat(v.time(0), t.time(0))
			var sc1 = new Script("0 OP_CSV OP_DROP 0 OP_CSV");
			var ast1 = AstElem.NewAndCat(AstElem.NewTimeV(0), AstElem.NewTimeT(0));
			DeserializationTestCore(sc1, ast1);

			var sc2 = new Script("0 OP_CSV");
			var ast2 = AstElem.NewTimeT(0);
			DeserializationTestCore(sc2, ast2);
			Assert.True(ast2.IsT());

			// or_if(t.time(0), t.time(0))
			var sc3 = new Script("OP_IF 0 OP_CSV OP_ELSE 0 OP_CSV OP_ENDIF");
			var ast3 = AstElem.NewOrIf(AstElem.NewTimeT(0), AstElem.NewTimeT(0));
			MiniscriptScriptParser.POrIfOfT.Parse(sc3);
			DeserializationTestCore(sc3, ast3);

			var sc4 = new Script("OP_DUP OP_IF 0 OP_CSV OP_DROP OP_ENDIF OP_SWAP 0209518deb4a2e7e0db86c611e4bbe4f8a6236478e8af5ac0e10cbc543dab2cfaf OP_CHECKSIG OP_ADD 1 OP_EQUAL");
			var ast4 = AstElem.NewThresh(
					1,
					new[] {
						AstElem.NewTime(0),
						AstElem.NewPkW(new PubKey("0209518deb4a2e7e0db86c611e4bbe4f8a6236478e8af5ac0e10cbc543dab2cfaf"))
					}
				);
			DeserializationTestCore(sc4, ast4);

			// multi(2, 2)
			var sc5 = new Script("2 02e38a30edddfb98c5973427a84f8e04376bd26f9ffaf60924e983f6056e2f020d 02d5b294505603232507635867f07bb498d8021db5b46a8276b6dc2823460b6684 2 OP_CHECKMULTISIG");
			Assert.True(MiniscriptScriptParser.PMulti.Parse(sc5).IsE());
			var ast5 = AstElem.NewMulti(2, new[] { new PubKey("02e38a30edddfb98c5973427a84f8e04376bd26f9ffaf60924e983f6056e2f020d"), new PubKey("02d5b294505603232507635867f07bb498d8021db5b46a8276b6dc2823460b6684") });
			DeserializationTestCore(sc5, ast5);

			// wrap(multi(2, 2))
			var sc6 = new Script("OP_TOALTSTACK 2 02e38a30edddfb98c5973427a84f8e04376bd26f9ffaf60924e983f6056e2f020d 02d5b294505603232507635867f07bb498d8021db5b46a8276b6dc2823460b6684 2 OP_CHECKMULTISIG OP_FROMALTSTACK");
			MiniscriptScriptParser.PWrap.Parse(sc6);
			var ast6 = AstElem.NewWrap(ast5);
			DeserializationTestCore(sc6, ast6);

			// thresh(1, time(0), wrap(multi(2, 2)))
			var sc7 = new Script("OP_DUP OP_IF 0 OP_CSV OP_DROP OP_ENDIF OP_TOALTSTACK 2 02e38a30edddfb98c5973427a84f8e04376bd26f9ffaf60924e983f6056e2f020d 02d5b294505603232507635867f07bb498d8021db5b46a8276b6dc2823460b6684 2 OP_CHECKMULTISIG OP_FROMALTSTACK OP_ADD 1 OP_EQUAL");
			MiniscriptScriptParser.PThresh.Parse(sc7);
			var ast7 = AstElem.NewThresh(
				1,
				new[]
					{
						AstElem.NewTime(0),
						ast6
					}
				);
			DeserializationTestCore(sc7, ast7);

			// and_casc(pk(02468ee57f149cbafe408a4c04cd7e76c03d23f6b1a6d1670a5c416f089dff61d8),time_f(0))
			var sc8 = new Script("02468ee57f149cbafe408a4c04cd7e76c03d23f6b1a6d1670a5c416f089dff61d8 OP_CHECKSIG OP_NOTIF 0 OP_ELSE 0 OP_CSV OP_0NOTEQUAL OP_ENDIF");
			var ast8 = AstElem.NewAndCasc(
					AstElem.NewPk(new PubKey("02468ee57f149cbafe408a4c04cd7e76c03d23f6b1a6d1670a5c416f089dff61d8")),
					AstElem.NewTimeF(0)
				);
			DeserializationTestCore(sc8, ast8);

			// wrap(and_casc(pk(02468ee57f149cbafe408a4c04cd7e76c03d23f6b1a6d1670a5c416f089dff61d8),time_f(0)))
			var sc9 = new Script("OP_TOALTSTACK 02468ee57f149cbafe408a4c04cd7e76c03d23f6b1a6d1670a5c416f089dff61d8 OP_CHECKSIG OP_NOTIF 0 OP_ELSE 0 OP_CSV OP_0NOTEQUAL OP_ENDIF OP_FROMALTSTACK ");
			var ast9 = AstElem.NewWrap(ast8);
			DeserializationTestCore(sc9, ast9);

			// or_cont(E.pk(), V.hash())
			var sc10 = new Script("027b4d201fe93fd448e9bed73c58897fac38329357bd3f94378df39fa8d2e3d247 OP_CHECKSIG OP_NOTIF OP_SIZE 20 OP_EQUALVERIFY OP_SHA256 e014f27cb7ebc9538ec2a02a65437701df1a4ed8b6f125af8dc8528664ee295d OP_EQUALVERIFY OP_ENDIF");
			var ast10 = AstElem.NewOrCont(
				AstElem.NewPk(new PubKey("027b4d201fe93fd448e9bed73c58897fac38329357bd3f94378df39fa8d2e3d247")),
				AstElem.NewHashV(uint256.Parse("e014f27cb7ebc9538ec2a02a65437701df1a4ed8b6f125af8dc8528664ee295d"))
			);
			DeserializationTestCore(sc10, ast10);

			// true(or_cont(E.pk(), V.hash()))
			var sc11 = new Script("027b4d201fe93fd448e9bed73c58897fac38329357bd3f94378df39fa8d2e3d247 OP_CHECKSIG OP_NOTIF OP_SIZE 20 OP_EQUALVERIFY OP_SHA256 e014f27cb7ebc9538ec2a02a65437701df1a4ed8b6f125af8dc8528664ee295d OP_EQUALVERIFY OP_ENDIF 1");
			var ast11 = AstElem.NewTrue(ast10);
			DeserializationTestCore(sc11, ast11);

			// and_cat(or_cont(pk(), hash_v()), true(or_cont(pk(), hash())))
			// Do not compare equality in this case.
			// Why? because the following two will result to exactly the same Script,
			// it is impossible to deserialize to the same representation.
			// 1. and_cat(a, true(b))
			// 2. true(and_cat(a, b))
			var sc12 = new Script("02619434bc0b8d19236d4894e87878adab38c912947deb1784afabf4097ccb250a OP_CHECKSIG OP_NOTIF OP_SIZE 20 OP_EQUALVERIFY OP_SHA256 3570a42e0c47d105e36da8dcba447fb3911b563468f2d00b3fc9a7e216a07eb9 OP_EQUALVERIFY OP_ENDIF  027b4d201fe93fd448e9bed73c58897fac38329357bd3f94378df39fa8d2e3d247 OP_CHECKSIG OP_NOTIF OP_SIZE 20 OP_EQUALVERIFY OP_SHA256 e014f27cb7ebc9538ec2a02a65437701df1a4ed8b6f125af8dc8528664ee295d OP_EQUALVERIFY OP_ENDIF 1");
			var ast12 = AstElem.NewAndCat(
				AstElem.NewOrCont(
					AstElem.NewPk(new PubKey("02619434bc0b8d19236d4894e87878adab38c912947deb1784afabf4097ccb250a")),
					AstElem.NewHashV(uint256.Parse("3570a42e0c47d105e36da8dcba447fb3911b563468f2d00b3fc9a7e216a07eb9"))
				),
				ast11
			);
			MiniscriptScriptParser.ParseScript(sc12);
			Assert.Equal(sc12, ast12.ToScript());

			// thresh_v(1, unlikely(true(hash_v())), time_w)
			var sc13 = new Script("OP_IF OP_SIZE 20 OP_EQUALVERIFY OP_SHA256 306442ccaa3b19a381bcf07a5893c7512fb99e280690cbffdd67a2d6b43e1c57 OP_EQUALVERIFY 1 OP_ELSE 0 OP_ENDIF OP_SWAP OP_DUP OP_IF 0 OP_CSV OP_DROP OP_ENDIF OP_ADD 1 OP_EQUALVERIFY");
			var ast13 = AstElem.NewThreshV(
					1,
					new AstElem[] {
							AstElem.NewUnlikely(
								AstElem.NewTrue(
									AstElem.NewHashV(uint256.Parse("306442ccaa3b19a381bcf07a5893c7512fb99e280690cbffdd67a2d6b43e1c57")
									)
								)
							),
							AstElem.NewTimeW(0)
					}
				);
			DeserializationTestCore(sc13, ast13);
		}

		private void DeserializationTestCore(Script sc, AstElem ast)
		{
			var sc2 = ast.ToScript(); // serialization test
			Assert.Equal(sc, sc2);
			var ast2 = MiniscriptScriptParser.PAstElem.Parse(sc); // deserialization test
			Assert.Equal(ast, ast2);
		}

		private void DeserializationTestCore(string policyStr, bool assert = true)
			=> DeserializationTestCore(MiniscriptDSLParser.DSLParser.Parse(policyStr), assert);
		private void DeserializationTestCore(AbstractPolicy policy, bool assert = true)
		{
			var ast = CompiledNode.FromPolicy(policy).BestT(0.0, 0.0).Ast;
			var sc = ast.ToScript();
			var ast2 = MiniscriptScriptParser.ParseScript(sc);
			if (assert)
			{
				Assert.Equal(ast, ast2);
			}
		}

		// This is useful for finding failure case. But passing every single case is unnecessary.
		// (And probably impossible). so disable it for now.
		// e.g. How we distinguish `and_cat(and_cat(a, b), c)` and `and_cat(a, and_cat(b, c))` ?
		[Property(Skip="DoesNotHaveToPass")]
		[Trait("PropertyTest", "BidirectionalConversion")]
		public void ShouldDeserializeScriptOriginatesFromMiniscriptToOrigin(AbstractPolicy policy)
			=> DeserializationTestCore(policy);


		[Trait("PropertyTest", "BidirectionalConversion")]
		public void ShouldDeserializeScriptOriginatesFromMiniscript(AbstractPolicy policy)
			=> DeserializationTestCore(policy, false);


		[Property]
		[Trait("PropertyTest", "Verification")]
		public void ShouldSatisfyAstWithDummyProviders(AbstractPolicy policy)
		{
			var ast = CompiledNode.FromPolicy(policy).BestT(0.0, 0.0).Ast;
			var dummySig = TransactionSignature.Empty;
			var dummyPreImage = new uint256();
			ast.Satisfy(pk => dummySig, _ => dummyPreImage, 65535);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void ShouldThrowCorrectErrorWhenSatisfyCSV()
		{
			var pk = Keys[0].PubKey;
			Func<PubKey, TransactionSignature> dummySigProvider =
				actualPk => actualPk.Equals(pk) ? TransactionSignature.Empty : null;
			var ms = Miniscript.Parse($"and(time(1000),pk({pk}))");

			// case 1: No Age Provided
			Assert.False(ms.Ast.TrySatisfy(dummySigProvider, null, null, out var res, out var errors));
			Assert.Single(errors);
			Assert.Equal(SatisfyErrorCode.NoAgeProvided, errors[0].Code);

			// case 2: Disabled
			var seq2 = new Sequence(1000u | Sequence.SEQUENCE_LOCKTIME_DISABLE_FLAG);
			Assert.False(ms.Ast.TrySatisfy(dummySigProvider, null, seq2, out var res2, out var errors2));
			Assert.Single(errors2);
			Assert.Equal(SatisfyErrorCode.RelativeLockTimeDisabled, errors2[0].Code);

			// case 3: Relative locktime by time (Instead of blockheight).
			var seq3 = new Sequence(Sequence.SEQUENCE_LOCKTIME_TYPE_FLAG | (1500u >> Sequence.SEQUENCE_LOCKTIME_GRANULARITY));
			Assert.False(ms.Ast.TrySatisfy(dummySigProvider, null, seq3, out var res3, out var errors3));
			Assert.Single(errors3);
			Assert.Equal(SatisfyErrorCode.UnSupportedRelativeLockTimeType, errors3[0].Code);

			// case 4: Not satisfied.
			var seq4 = new Sequence(lockHeight: 999);
			Assert.False(ms.Ast.TrySatisfy(dummySigProvider, null, seq4, out var res4, out var errors4));
			Assert.Single(errors4);
			Assert.Equal(SatisfyErrorCode.LockTimeNotMet, errors4[0].Code);

			// case 5: Successful case.
			var seq5 = new Sequence(lockHeight: 1000);
			Assert.True(ms.Ast.TrySatisfy(dummySigProvider, null, seq5, out var res5, out var errors5));
			Assert.Empty(errors5);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]

		public void ShouldPlayWellWithTransactionBuilder_1()
		{
			// case1: simple timelocked multisig
			var dsl = $"and(time(100),multi(2, {Keys[0].PubKey}, {Keys[1].PubKey}))";
			var ms = Miniscript.Parse(dsl);
			var builder = Network.CreateTransactionBuilder();
			var coins = GetRandomCoinsForAllScriptType(Money.Coins(0.5m), ms.Script);
			builder.AddCoins(coins);
			builder.SendFees(Money.Coins(0.001m));
			builder.SendAll(Keys[2]);
			builder.AddKeys(Keys[0], Keys[1]);
			Assert.False(builder.Verify(builder.BuildTransaction(true)));
			builder.OptInRBF = true;
			Assert.False(builder.Verify(builder.BuildTransaction(true)));
			builder.SetRelativeLockTimeTo(coins, 99);
			Assert.False(builder.Verify(builder.BuildTransaction(true)));
			builder.SetRelativeLockTimeTo(coins, 100);
			var tx = builder.BuildTransaction(true);
			Assert.Empty(builder.Check(tx));
		}

		private Tuple<TransactionBuilder, List<ScriptCoin>> PrepareBuilder(Script sc)
		{
			var builder = Network.CreateTransactionBuilder();
			var coins = GetRandomCoinsForAllScriptType(Money.Coins(0.5m), sc);
			builder.AddCoins(coins)
				.SendFees(Money.Coins(0.001m))
				.SendAll(new Key()); // dummy output
			return Tuple.Create(builder, coins);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]

		public void ShouldPlayWellWithTransactionBuilder_2()
		{
			// case2: BIP199 HTLC
			var secret1 = new uint256(0xdeadbeef);
			var hash1 = new uint256(Hashes.SHA256(secret1.ToBytes()), false);
			var dsl = $"aor(and(hash({hash1}),pk({Keys[0].PubKey})),and(pk({Keys[1].PubKey}),time(10000)))";
			var ms = Miniscript.Parse(dsl);
			var dummy = Keys[2];

			// ------ 1: left side of redeem condition. revoking using hash preimage.
			var t = PrepareBuilder(ms.Script);
			var builder = t.Item1;
			builder.AddKeys(Keys[0]);
			// we have key for left side redeem condition. but no secret.
			Assert.False(builder.Verify(builder.BuildTransaction(true)));
			builder.AddPreimages(new uint256(0xdeadbeef111)); // wrong secret.
			Assert.False(builder.Verify(builder.BuildTransaction(true)));
			builder.AddPreimages(secret1); // now we have correct secret.
			Assert.True(builder.Verify(builder.BuildTransaction(true)));

			// --------- 2: right side. revoking after time.
			var t2 = PrepareBuilder(ms.Script);
			var b2 = t2.Item1;
			var coins = t2.Item2;
			b2.AddKeys(Keys[1]);
			// key itself is not enough
			Assert.False(b2.Verify(b2.BuildTransaction(true)));
			// Preimage does not help this time.
			b2.AddPreimages(secret1);
			Assert.False(b2.Verify(b2.BuildTransaction(true)));
			// but locktime does.
			b2.SetRelativeLockTimeTo(coins, 10000);
			Assert.True(b2.Verify(b2.BuildTransaction(true)));
		}

		[Property]
		[Trait("PropertyTest", "Verification")]
		public void ShouldNotThrowErrorWhenTryParsingScript(Script sc)
		{
			Miniscript.TryParseScript(sc, out var res);
		}

		[Property]
		[Trait("PropertyTest", "Verification")]
		public void ShouldNotThrowErrorWhenTryParsingDSL(NonNull<string> sc)
		{
			Miniscript.TryParse(sc.Get, out var res);
		}
	}
}