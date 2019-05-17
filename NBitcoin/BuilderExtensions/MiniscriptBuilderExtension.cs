using System;
using System.Collections.Generic;
using System.Linq;
using NBitcoin.Scripting;

namespace NBitcoin.BuilderExtensions
{
	public class MiniscriptBuilderExtension : BuilderExtension
	{
		// Probably it is impossible to Combine items in generic way.
		public override bool CanCombineScriptSig(Script scriptPubKey, Script a, Script b)
			=> false;

		public override bool CanDeduceScriptPubKey(Script scriptSig)
			=> false;

		public override bool CanEstimateScriptSigSize(Script scriptPubKey)
			=> Miniscript.TryParseScript(scriptPubKey, out var _);

		public override bool CanGenerateScriptSig(Script scriptPubKey)
			=> Miniscript.TryParseScript(scriptPubKey, out var _);

		public override Script CombineScriptSig(Script scriptPubKey, Script a, Script b)
		{
			throw new System.NotImplementedException();
		}

		public override Script DeduceScriptPubKey(Script scriptSig)
		{
			throw new System.NotImplementedException();
		}

		public override int EstimateScriptSigSize(Script scriptPubKey)
		{
			var ms = Miniscript.ParseScript(scriptPubKey);
			return (int)ms.MaxSatisfactionSize(2);
		}

		public override Script GenerateScriptSig(
			Script scriptPubKey,
			IKeyRepository keyRepo,
			ISigner signer,
			ISha256PreimageRepository preimageRepo,
			Sequence? sequence
			)
		{
			var ms = Miniscript.ParseScript(scriptPubKey);
			Func<PubKey, TransactionSignature > signatureProvider =
				(pk) => keyRepo.FindKey(pk.ScriptPubKey) == null ? null : signer.Sign(pk);
			if(!ms.Ast.TrySatisfy(signatureProvider, preimageRepo.FindPreimage, sequence, out var items, out var _))
				return null;
			var ops = new List<Op>();
			foreach (var i in items)
			{
				ops.Add(Op.GetPushOp(i));
			}
			return new Script(ops);
		}

		public override bool IsCompatibleKey(PubKey publicKey, Script scriptPubKey)
			=> scriptPubKey.GetAllPubKeys().Any(pk => publicKey.Equals(pk));
	}
}