using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using Xunit;
using NBitcoin;
using NBitcoin.Crypto;
using NBitcoin.Tests.Generators;

namespace NBitcoin.Tests.PropertyTest
{
	public class SignatureTests
	{
		public SignatureTests()
		{
			Arb.Register<CryptoGenerator>();
		}

		[Property(MaxTest = 10)]
		[Trait("UnitTest", "UnitTest")]
		public void SerializeDeserializeCompact (ECDSASignature signature)
		{
			var b = signature.ToCompact();
			ECDSASignature.TryParseFromCompact(b, out var signature2);
			Assert.True(signature.ToDER().SequenceEqual(signature2.ToDER()));
		}
	}
}
