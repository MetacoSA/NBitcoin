using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.BuilderExtensions
{
	public class OPTrueExtension : BuilderExtension
	{

		public override bool CanDeduceScriptPubKey(Script scriptSig)
		{
			return false;
		}

		public override bool CanEstimateScriptSigSize(ICoin coin)
		{
			return CanSign(coin.GetScriptCode());
		}

		private static bool CanSign(Script executedScript)
		{
			return executedScript.Length == 1 && executedScript.ToBytes(true)[0] == (byte)OpcodeType.OP_TRUE;
		}

		public override Script DeduceScriptPubKey(Script scriptSig)
		{
			throw new NotSupportedException();
		}

		public override int EstimateScriptSigSize(ICoin coin)
		{
			return 1;
		}

		public override bool IsCompatibleKey(IPubKey publicKey, Script scriptPubKey)
		{
			return false;
		}

		public override void Finalize(InputSigningContext inputSigningContext)
		{
			inputSigningContext.Input.FinalScriptSig = Script.Empty;
		}

		public override bool Match(ICoin coin, PSBTInput input)
		{
			return CanSign(coin.GetScriptCode());
		}

		public override void Sign(InputSigningContext inputSigningContext, IKeyRepository keyRepository, ISigner signer)
		{
		}
	}
}
