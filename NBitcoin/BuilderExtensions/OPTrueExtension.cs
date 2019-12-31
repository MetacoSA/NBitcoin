using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.BuilderExtensions
{
	public class OPTrueExtension : BuilderExtension
	{
		public override bool CanCombineScriptSig(Script scriptPubKey, Script a, Script b)
		{
			return false;
		}

		public override bool CanDeduceScriptPubKey(Script scriptSig)
		{
			return false;
		}

		public override bool CanEstimateScriptSigSize(Script scriptPubKey)
		{
			return CanGenerateScriptSig(scriptPubKey);
		}

		public override bool CanGenerateScriptSig(Script scriptPubKey)
		{
			return scriptPubKey.Length == 1 && scriptPubKey.ToBytes(true)[0] == (byte)OpcodeType.OP_TRUE;
		}

		public override Script CombineScriptSig(Script scriptPubKey, Script a, Script b)
		{
			throw new NotSupportedException();
		}

		public override Script DeduceScriptPubKey(Script scriptSig)
		{
			throw new NotSupportedException();
		}

		public override int EstimateScriptSigSize(Script scriptPubKey)
		{
			return 1;
		}

		public override Script GenerateScriptSig(Script scriptPubKey, IKeyRepository keyRepo, ISigner signer)
		{
			return Script.Empty;
		}

		public override bool IsCompatibleKey(PubKey publicKey, Script scriptPubKey)
		{
			return false;
		}
	}
}
