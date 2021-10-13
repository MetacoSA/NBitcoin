using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NBitcoin.TransactionBuilder;

namespace NBitcoin.BuilderExtensions
{
#nullable enable
	public interface ISigner
	{
		ITransactionSignature? Sign(IPubKey key);
	}
	public interface IKeyRepository
	{
		IPubKey? FindKey(Script scriptPubKey);
	}
#nullable restore

	/// <summary>
	/// Base extension class to derive from for extending the TransactionBuilder
	/// </summary>
	public abstract class BuilderExtension
	{
		public static TransactionSignature DummySignature = new TransactionSignature(Encoders.Hex.DecodeData("3045022100b9d685584f46554977343009c04b3091e768c23884fa8d2ce2fb59e5290aa45302203b2d49201c7f695f434a597342eb32dfd81137014fcfb3bb5edc7a19c77774d201"));

		public abstract bool Match(ICoin coin, PSBTInput input);

		public abstract void Sign(InputSigningContext inputSigningContext, IKeyRepository keyRepository, ISigner signer);

		public abstract Script DeduceScriptPubKey(Script scriptSig);
		public abstract bool CanDeduceScriptPubKey(Script scriptSig);

		public abstract bool CanEstimateScriptSigSize(ICoin coin);
		public abstract int EstimateScriptSigSize(ICoin coin);

		public abstract bool IsCompatibleKey(IPubKey publicKey, Script scriptPubKey);

		public abstract void Finalize(InputSigningContext inputSigningContext);

		public virtual void ExtractExistingSignatures(InputSigningContext inputSigningContext)
		{
		}

		public virtual void MergePartialSignatures(InputSigningContext inputSigningContext)
		{
		}
	}

#nullable enable
	public class InputSigningContext
	{
		internal InputSigningContext(TransactionSigningContext transactionSigningContext, ICoin coin, CoinOptions? coinOptions, PSBTInput input, TxIn? originalTxIn, BuilderExtension extension)
		{
			CoinOptions = coinOptions;
			Coin = coin;
			Input = input;
			Extension = extension;
			TransactionContext = transactionSigningContext;
			OriginalTxIn = originalTxIn;
		}
		internal TransactionSigningContext TransactionContext { get; }
		public TxIn? OriginalTxIn { get; }
		public BuilderExtension Extension { get; }
		public CoinOptions? CoinOptions { get; set; }
		public ICoin Coin { get; }
		public PSBTInput Input { get; }
	}
#nullable restore
}
