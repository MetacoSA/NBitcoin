using System.Collections.Generic;
using FsCheck;
using NBitcoin;
namespace NBitcoin.Tests.Generators
{
	public class StandardScriptGenerator
	{
		public static Gen<Script> FromKey(PubKey key)
		{
			return Gen.Constant(new Script());
		}
	}
}