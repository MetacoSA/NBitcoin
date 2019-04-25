using Xunit;
using NBitcoin.Miniscript;
using NBitcoin.Miniscript.Parser;
using FsCheck.Xunit;
using FsCheck;
using NBitcoin.Tests.Generators;

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

	}
}