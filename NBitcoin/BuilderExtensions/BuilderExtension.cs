using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.BuilderExtensions
{
	public interface ISigner
	{
		TransactionSignature Sign(PubKey key);
	}
	public interface IKeyRepository
	{
		PubKey FindKey(Script scriptPubKey);
	}

	/// <summary>
	/// Base extension class to derive from for extending the TransactionBuilder
	/// </summary>
	public abstract class BuilderExtension
	{
		public static PubKey DummyPubKey = new PubKey(Encoders.Hex.DecodeData("022c2b9e61169fb1b1f2f3ff15ad52a21745e268d358ba821d36da7d7cd92dee0e"));
		public static TransactionSignature DummySignature = new TransactionSignature(Encoders.Hex.DecodeData("3045022100b9d685584f46554977343009c04b3091e768c23884fa8d2ce2fb59e5290aa45302203b2d49201c7f695f434a597342eb32dfd81137014fcfb3bb5edc7a19c77774d201"));

		public abstract bool CanGenerateScriptSig(Script scriptPubKey);
		public abstract Script GenerateScriptSig(Script scriptPubKey, IKeyRepository keyRepo, ISigner signer);

		public abstract Script DeduceScriptPubKey(Script scriptSig);
		public abstract bool CanDeduceScriptPubKey(Script scriptSig);

		public abstract bool CanEstimateScriptSigSize(Script scriptPubKey);
		public abstract int EstimateScriptSigSize(Script scriptPubKey);

		public abstract bool CanCombineScriptSig(Script scriptPubKey, Script a, Script b);

		public abstract Script CombineScriptSig(Script scriptPubKey, Script a, Script b);

		public abstract bool IsCompatibleKey(PubKey publicKey, Script scriptPubKey);
	}
}
