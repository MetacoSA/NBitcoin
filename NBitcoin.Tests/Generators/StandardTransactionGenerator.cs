using NBitcoin.Policy;

namespace NBitcoin.Tests.Generators
{
	public class StandardTransactionGenerator
	{
		private static StandardTransactionPolicy _Policy;

		static StandardTransactionGenerator()
		{
			_Policy = new StandardTransactionPolicy();
		}
	}
}