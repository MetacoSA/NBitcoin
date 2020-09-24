using System.Threading.Tasks;
using FsCheck;
using FsCheck.Xunit;
using NBitcoin.RPC;
using NBitcoin.Scripting;
using Xunit;
using NBitcoin.Tests.Generators;
using Newtonsoft.Json.Linq;

namespace NBitcoin.Tests
{
	public class RPCPropertyTests
	{
		public RPCPropertyTests()
		{
			Arb.Register<RegtestOutputDescriptorGenerator>();
		}

		[Property(MaxTest = 5)]
		[Trait("PropertyTest", "RPC")]
		public void ShouldAlwaysCreateBitcoinCoreAcceptableOutputDescriptor(OutputDescriptor od)
		{
			using var builder = NodeBuilderEx.Create();
			var cli = builder.CreateNode().CreateRPCClient();
			builder.StartAll();
			cli.SendCommand("getdescriptorinfo", od.ToString());
		}
	}
}
