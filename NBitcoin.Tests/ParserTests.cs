using NBitcoin.Scripting.Parser;
using Xunit;

namespace NBitcoin.Tests
{
	/// <summary>
	/// Super basic parser just for checking sanity of parser combinator
	/// </summary>
	internal static class ParserForTest
	{
		internal enum TokenType
		{
			Foo,
			Bar
		}

		public static Parser<char, TokenType> PToken() =>
			from _prefix in Parse.String("prefix")
			from _l in Parse.Char('(').Token()
			from foo in Parse.String("foo").Return(TokenType.Foo)
			from _r in Parse.Char(')').Token()
			select foo;
	}
	public class ParserTests
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void BasicParserTest()
		{
			ParserForTest.PToken().Parse("prefix(foo)", Network.RegTest);
			ParserForTest.PToken().Parse("prefix  (foo)", Network.RegTest);
			ParserForTest.PToken().Parse("prefix  (foo)   ", Network.RegTest);
			ParserForTest.PToken().Parse("prefix  (   foo   )   ", Network.RegTest);

			// input must be enumerable
			foreach (var p in new StringInput("123"))
				Assert.Contains(p, "123".ToCharArray());

			foreach (var p in new ScriptInput(new Script("OP_ADD OP_EQUALVERIFY")))
				Assert.NotNull(p);
		}
	}
}
