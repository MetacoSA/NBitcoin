using NBitcoin.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public enum TxOutType
	{
		TX_NONSTANDARD,
		// 'standard' transaction types:
		TX_PUBKEY,
		TX_PUBKEYHASH,
		TX_SCRIPTHASH,
		TX_MULTISIG,
		TX_NULL_DATA,
	};

	//TODO : Is*Conform can be used to parses the script
	public class PayToPubkeyScriptTemplate : ScriptTemplate
	{
		public Script GenerateOutputScript(PubKey pubkey)
		{
			return new Script(
					Op.GetPushOp(pubkey.ToBytes()),
					OpcodeType.OP_CHECKSIG
				);
		}
		public override bool IsOutputConform(Script script)
		{
			var ops = script.ToOps().ToList();
			if(ops.Count != 2)
				return false;
			return ops[0].PushData != null && PubKey.IsValidSize(ops[0].PushData.Length) &&
				   ops[1].Code == OpcodeType.OP_CHECKSIG;
		}

		public Script GenerateInputScript(ECDSASignature signature)
		{
			signature = signature.MakeCanonical();
			return new Script(
				Op.GetPushOp(signature.ToDER())
				);
		}

		public override bool IsInputConform(Script script)
		{
			var ops = script.ToOps().ToList();
			if(ops.Count != 1)
				return false;

			return ops[0].PushData != null;
		}

		public override TxOutType Type
		{
			get
			{
				return TxOutType.TX_PUBKEY;
			}
		}
	}
	public class PayToPubkeyHashScriptTemplate : ScriptTemplate
	{
		public Script GenerateOutputScript(KeyId pubkeyHash)
		{
			return new Script(
					OpcodeType.OP_DUP,
					OpcodeType.OP_HASH160,
					Op.GetPushOp(pubkeyHash.ToBytes()),
					OpcodeType.OP_EQUALVERIFY,
					OpcodeType.OP_CHECKSIG
				);
		}

		public Script GenerateInputScript(ECDSASignature signature, PubKey publicKey)
		{
			signature = signature.MakeCanonical();
			return new Script(
				Op.GetPushOp(signature.ToDER()),
				Op.GetPushOp(publicKey.ToBytes())
				);
		}

		public override bool IsOutputConform(Script script)
		{
			var ops = script.ToOps().ToArray();
			if(ops.Length != 5)
				return false;
			return ops[0].Code == OpcodeType.OP_DUP &&
				   ops[1].Code == OpcodeType.OP_HASH160 &&
				   ops[2].PushData != null && ops[2].PushData.Length == 0x14 &&
				   ops[3].Code == OpcodeType.OP_EQUALVERIFY &&
				   ops[4].Code == OpcodeType.OP_CHECKSIG;
		}

		public override bool IsInputConform(Script script)
		{
			var ops = script.ToOps().ToArray();
			if(ops.Length != 2)
				return false;
			return ops[0].PushData != null &&
				   ops[1].PushData != null && PubKey.IsValidSize(ops[1].PushData.Length);
		}

		public override TxOutType Type
		{
			get
			{
				return TxOutType.TX_PUBKEYHASH;
			}
		}
	}
	public abstract class ScriptTemplate
	{
		public abstract bool IsOutputConform(Script script);
		public abstract bool IsInputConform(Script script);
		public abstract TxOutType Type
		{
			get;
		}
	}
}
