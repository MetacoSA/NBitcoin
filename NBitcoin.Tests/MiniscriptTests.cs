using Xunit;
using NBitcoin.Miniscript;
using NBitcoin.Miniscript.Parser;
using FsCheck.Xunit;
using FsCheck;
using NBitcoin.Tests.Generators;
using System;

namespace NBitcoin.Tests
{
    public class MiniscriptTests
    {
		public MiniscriptTests() => Arb.Register<AbstractPolicyGenerator>();

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void DSLParserTests()
		{
			var pk = new Key().PubKey;
			var pk2 = new Key().PubKey;
			var pk3 = new Key().PubKey;
			DSLParserTestCore("time(100)", AbstractPolicy.NewTime(100));
			DSLParserTestCore($"pk({pk})", AbstractPolicy.NewCheckSig(pk));
			DSLParserTestCore($"multi(2,{pk2},{pk3})", AbstractPolicy.NewMulti(2, new PubKey[]{pk2, pk3}));
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
						AbstractPolicy.NewMulti(2, new PubKey[]{pk2, pk3})
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
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void DSLSubParserTest()
		{
			var pk = new Key().PubKey;
			var pk2 = new Key().PubKey;
			var pk3 = new Key().PubKey;
			var res = MiniscriptDSLParser.ThresholdExpr().Parse($"thres(2,time(100),multi(2,{pk2},{pk3}))");
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
		public void ScriptDeserializationTest()
		{
			var sc = new Script("027be8d8ffe2f50ab5afcebf29ec2c9c75b50334905c9d15046e051c81a4ddbc68 OP_CHECKSIG");
			MiniscriptScriptParser.PPk.Parse(sc);
			MiniscriptScriptParser.PAstElemCore.Parse(sc);
			DeserializationTestCore(MiniscriptDSLParser.ParseDSL("pk(027be8d8ffe2f50ab5afcebf29ec2c9c75b50334905c9d15046e051c81a4ddbc68)"));

			// and_cat(v.time(0), t.time(0))
			var sc2 = new Script("0 OP_CSV OP_DROP 0 OP_CSV");
			MiniscriptScriptParser.PAstElem.Parse(sc2);

			var sc4 = new Script("0 OP_CSV");
			MiniscriptScriptParser.PTimeT.Parse(sc4);
			// or_if(t.time(0), t.time(0))
			var sc3 = new Script("OP_IF 0 OP_CSV OP_ELSE 0 OP_CSV OP_ENDIF");
			MiniscriptScriptParser.POrIf4.Parse(sc3);

			var sc5 = new Script("OP_DUP OP_IF 0 OP_CSV OP_DROP OP_ENDIF OP_SWAP 0209518deb4a2e7e0db86c611e4bbe4f8a6236478e8af5ac0e10cbc543dab2cfaf OP_CHECKSIG OP_ADD 1 OP_EQUAL");
			MiniscriptScriptParser.PThresh.Parse(sc5);

			// multi(2, 2)
			var sc9 = new Script("2 02e38a30edddfb98c5973427a84f8e04376bd26f9ffaf60924e983f6056e2f020d 02d5b294505603232507635867f07bb498d8021db5b46a8276b6dc2823460b6684 2 OP_CHECKMULTISIG");
		  Assert.True(MiniscriptScriptParser.PMulti.Parse(sc9).IsE());
			// wrap(multi(2, 2))
			var sc10 = new Script("OP_TOALTSTACK 2 02e38a30edddfb98c5973427a84f8e04376bd26f9ffaf60924e983f6056e2f020d 02d5b294505603232507635867f07bb498d8021db5b46a8276b6dc2823460b6684 2 OP_CHECKMULTISIG OP_FROMALTSTACK");
		  MiniscriptScriptParser.PWrap.Parse(sc10);
			// thresh(1, time(0), wrap(multi(2, 2)))
			var sc11 = new Script("OP_DUP OP_IF 0 OP_CSV OP_DROP OP_ENDIF OP_TOALTSTACK 2 02e38a30edddfb98c5973427a84f8e04376bd26f9ffaf60924e983f6056e2f020d 02d5b294505603232507635867f07bb498d8021db5b46a8276b6dc2823460b6684 2 OP_CHECKMULTISIG OP_FROMALTSTACK OP_ADD 1 OP_EQUAL");
		  MiniscriptScriptParser.PThresh.Parse(sc11);

			// and_casc(pk(02468ee57f149cbafe408a4c04cd7e76c03d23f6b1a6d1670a5c416f089dff61d8),time_f(0))
			var sc12 = new Script("02468ee57f149cbafe408a4c04cd7e76c03d23f6b1a6d1670a5c416f089dff61d8 OP_CHECKSIG OP_NOTIF 0 OP_ELSE 0 OP_CSV OP_0NOTEQUAL OP_ENDIF");
		  Assert.True(MiniscriptScriptParser.PAstElemCore.Parse(sc12).IsE());
			// wrap(and_casc(pk(02468ee57f149cbafe408a4c04cd7e76c03d23f6b1a6d1670a5c416f089dff61d8),time_f(0)))
			var sc13 = new Script("OP_TOALTSTACK 02468ee57f149cbafe408a4c04cd7e76c03d23f6b1a6d1670a5c416f089dff61d8 OP_CHECKSIG OP_NOTIF 0 OP_ELSE 0 OP_CSV OP_0NOTEQUAL OP_ENDIF OP_FROMALTSTACK ");
			MiniscriptScriptParser.PWrap.Parse(sc13);

			// or_cont(E.pk(), V.hash())
			var sc14_1 = new Script("027b4d201fe93fd448e9bed73c58897fac38329357bd3f94378df39fa8d2e3d247 OP_CHECKSIG OP_NOTIF OP_SIZE 20 OP_EQUALVERIFY OP_SHA256 e014f27cb7ebc9538ec2a02a65437701df1a4ed8b6f125af8dc8528664ee295d OP_EQUALVERIFY OP_ENDIF");
			MiniscriptScriptParser.POrCont.Parse(sc14_1);
			MiniscriptScriptParser.PAstElemCore.Parse(sc14_1);
			MiniscriptScriptParser.PAstElem.Parse(sc14_1);

			// true(or_cont(E.pk(), V.hash()))
			var sc14_2 = new Script("027b4d201fe93fd448e9bed73c58897fac38329357bd3f94378df39fa8d2e3d247 OP_CHECKSIG OP_NOTIF OP_SIZE 20 OP_EQUALVERIFY OP_SHA256 e014f27cb7ebc9538ec2a02a65437701df1a4ed8b6f125af8dc8528664ee295d OP_EQUALVERIFY OP_ENDIF 1");
			MiniscriptScriptParser.PTrue.Parse(sc14_2);

			// and_cat(or_cont(pk(), hash_v()), true(or_cont(pk(), hash())))
			var sc15 = new Script("02619434bc0b8d19236d4894e87878adab38c912947deb1784afabf4097ccb250a OP_CHECKSIG OP_NOTIF OP_SIZE 20 OP_EQUALVERIFY OP_SHA256 3570a42e0c47d105e36da8dcba447fb3911b563468f2d00b3fc9a7e216a07eb9 OP_EQUALVERIFY OP_ENDIF  027b4d201fe93fd448e9bed73c58897fac38329357bd3f94378df39fa8d2e3d247 OP_CHECKSIG OP_NOTIF OP_SIZE 20 OP_EQUALVERIFY OP_SHA256 e014f27cb7ebc9538ec2a02a65437701df1a4ed8b6f125af8dc8528664ee295d OP_EQUALVERIFY OP_ENDIF 1");
			MiniscriptScriptParser.PAstElem.Parse(sc15);

			// thresh_v(1, likely(true(hash_v())), time_w)
			var sc16 = new Script("OP_IF OP_SIZE 20 OP_EQUALVERIFY OP_SHA256 306442ccaa3b19a381bcf07a5893c7512fb99e280690cbffdd67a2d6b43e1c57 OP_EQUALVERIFY 1 OP_ELSE 0 OP_ENDIF OP_SWAP OP_DUP OP_IF 0 OP_CSV OP_DROP OP_ENDIF OP_ADD 1 OP_EQUALVERIFY");
			MiniscriptScriptParser.PAstElem.Parse(sc16);

		}

		private void DeserializationTestCore(AbstractPolicy policy)
		{
			var ast = CompiledNode.FromPolicy(policy).BestT(0.0, 0.0).Ast;
			MiniscriptScriptParser.ParseScript(ast.ToScript());
		}

		[Property]
		[Trait("PropertyTest", "Verification")]
		public void ShouldDeserializeScriptOriginatesFromMiniscript(AbstractPolicy policy)
			=> DeserializationTestCore(policy);
	}
}