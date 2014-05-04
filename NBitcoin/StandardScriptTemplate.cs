using NBitcoin.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	//TODO : Is*Conform can be used to parses the script

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

	public class TxNullDataScriptTemplate : ScriptTemplate
	{
		public override bool CheckScripPubKey(Script scriptPubKey)
		{
			var ops = scriptPubKey.ToOps().ToList();
			if(ops.Count < 1)
				return false;
			if(ops[0].Code != OpcodeType.OP_RETURN)
				return false;
			if(ops.Count == 2)
			{
				return ops[1].PushData != null && ops[1].PushData.Length <= 40;
			}
			return true;
		}

		public override bool CheckScriptSig(Script scriptSig, Script scriptPubKey)
		{
			return false;
		}

		public Script GenerateOutputScript(byte[] data)
		{
			if(data == null)
				throw new ArgumentNullException("data");
			if(data.Length > 40)
				throw new ArgumentOutOfRangeException("data", "Data should have a maximum size of 40 bytes");

			return new Script(OpcodeType.OP_RETURN,
							  Op.GetPushOp(data));
		}

		public override TxOutType Type
		{
			get
			{
				return TxOutType.TX_NULL_DATA;
			}
		}
	}
	public class PayToMultiSigScriptTemplate : ScriptTemplate
	{
		public Script GenerateOutputScript(int sigCount, PubKey[] keys)
		{
			List<Op> ops = new List<Op>();
			var push = Op.GetPushOp(sigCount);
			if(!push.IsSmallUInt)
				throw new ArgumentOutOfRangeException("sigCount should be less or equal to 16");
			ops.Add(push);
			var keyCount = Op.GetPushOp(keys.Length);
			if(!keyCount.IsSmallUInt)
				throw new ArgumentOutOfRangeException("key count should be less or equal to 16");
			foreach(var key in keys)
			{
				ops.Add(Op.GetPushOp(key.ToBytes()));
			}
			ops.Add(keyCount);
			ops.Add(OpcodeType.OP_CHECKMULTISIG);
			return new Script(ops.ToArray());
		}
		public override bool CheckScripPubKey(Script scriptPubKey)
		{
			var ops = scriptPubKey.ToOps().ToArray();
			if(ops.Length < 3)
				return false;

			var sigCount = ops[0];
			if(!sigCount.IsSmallUInt)
				return false;

			var expectedKeyCount = 0;
			var keyCountIndex = 0;
			for(int i = 1 ; i < ops.Length ; i++)
			{
				if(ops[i].PushData == null)
					return false;
				if(!PubKey.IsValidSize(ops[i].PushData.Length))
				{
					keyCountIndex = i;
					break;
				}
				expectedKeyCount++;
			}
			if(!ops[keyCountIndex].IsSmallUInt)
				return false;
			if(ops[keyCountIndex].GetValue() != expectedKeyCount)
				return false;
			return ops[keyCountIndex + 1].Code == OpcodeType.OP_CHECKMULTISIG &&
				  keyCountIndex + 1 == ops.Length - 1;
		}

		public override bool CheckScriptSig(Script scriptSig, Script scriptPubKey)
		{
			if(!scriptSig.IsPushOnly)
				return false;
			if(!CheckScripPubKey(scriptPubKey))
				return false;

			var sigCountExpected = scriptPubKey.ToOps().First().GetValue();
			return sigCountExpected == scriptSig.ToOps().Count();
		}

		public override TxOutType Type
		{
			get
			{
				return TxOutType.TX_MULTISIG;
			}
		}
	}

	//https://github.com/bitcoin/bips/blob/master/bip-0016.mediawiki
	public class PayToScriptHashScriptTemplate : ScriptTemplate
	{
		public PayToScriptHashScriptTemplate()
		{
			VerifyRedeemScript = true;
		}
		public bool VerifyRedeemScript
		{
			get;
			set;
		}
		public Script GenerateOutputScript(Script scriptPubKey)
		{
			return new Script(
				OpcodeType.OP_HASH160,
				Op.GetPushOp(scriptPubKey.ID.ToBytes()),
				OpcodeType.OP_EQUAL);
		}

		public override bool CheckScripPubKey(Script scriptPubKey)
		{
			var ops = scriptPubKey.ToOps().ToArray();
			if(ops.Length != 3)
				return false;
			return ops[0].Code == OpcodeType.OP_HASH160 &&
				   ops[1].Code == (OpcodeType)0x14 &&
				   ops[2].Code == OpcodeType.OP_EQUAL;
		}

		public Script GenerateInputScript(Op[] ops, Script script)
		{
			var pushScript = Op.GetPushOp(script._Script);
			return new Script(ops.Concat(new[] { pushScript }).ToArray());
		}


		public Script GenerateInputScript(TransactionSignature[] signatures, Script redeemScript)
		{
			List<Op> ops = new List<Op>();
			foreach(var sig in signatures)
			{
				ops.Add(Op.GetPushOp(sig.ToBytes()));
			}
			return GenerateInputScript(ops.ToArray(), redeemScript);
		}
		public Script GenerateInputScript(ECDSASignature[] signatures, Script redeemScript)
		{
			return GenerateInputScript(signatures.Select(s => new TransactionSignature(s, SigHash.All)).ToArray(), redeemScript);
		}
		public override bool CheckScriptSig(Script scriptSig, Script scriptPubKey)
		{
			var ops = scriptSig.ToOps().ToArray();
			if(ops.Length == 0)
				return false;
			if(!scriptSig.IsPushOnly)
				return false;
			if(!VerifyRedeemScript)
				return true;
			var redeemScript = new Script(ops.Last().PushData);
			var template = StandardScripts.GetTemplateFromScriptPubKey(redeemScript);
			return template != null && template.Type != TxOutType.TX_SCRIPTHASH;
		}

		public override TxOutType Type
		{
			get
			{
				return TxOutType.TX_SCRIPTHASH;
			}
		}
	}
	public class PayToPubkeyScriptTemplate : ScriptTemplate
	{
		public Script GenerateOutputScript(PubKey pubkey)
		{
			return new Script(
					Op.GetPushOp(pubkey.ToBytes()),
					OpcodeType.OP_CHECKSIG
				);
		}
		public override bool CheckScripPubKey(Script scriptPubKey)
		{
			var ops = scriptPubKey.ToOps().ToList();
			if(ops.Count != 2)
				return false;
			return ops[0].PushData != null && PubKey.IsValidSize(ops[0].PushData.Length) &&
				   ops[1].Code == OpcodeType.OP_CHECKSIG;
		}

		public Script GenerateInputScript(ECDSASignature signature)
		{
			return GenerateInputScript(new TransactionSignature(signature, SigHash.All));
		}
		public Script GenerateInputScript(TransactionSignature signature)
		{
			return new Script(
				Op.GetPushOp(signature.ToBytes())
				);
		}

		public override bool CheckScriptSig(Script scriptSig, Script scriptPubKey)
		{
			var ops = scriptSig.ToOps().ToList();
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

	public class PayToPubkeyHashScriptSigParameters
	{
		public TransactionSignature TransactionSignature
		{
			get;
			set;
		}
		public PubKey PublicKey
		{
			get;
			set;
		}
	}
	public class PayToPubkeyHashScriptTemplate : ScriptTemplate
	{
		public Script GenerateOutputScript(BitcoinAddress address)
		{
			if(address == null)
				throw new ArgumentNullException("address");
			return GenerateOutputScript(address.ID);
		}
		public Script GenerateOutputScript(PubKey pubKey)
		{
			if(pubKey == null)
				throw new ArgumentNullException("pubKey");
			return GenerateOutputScript(pubKey.ID);
		}
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

		public Script GenerateInputScript(TransactionSignature signature, PubKey publicKey)
		{
			if(signature == null)
				throw new ArgumentNullException("signature");
			if(publicKey == null)
				throw new ArgumentNullException("publicKey");
			return new Script(
				Op.GetPushOp(signature.ToBytes()),
				Op.GetPushOp(publicKey.ToBytes())
				);
		}

		public override bool CheckScripPubKey(Script scriptPubKey)
		{
			var ops = scriptPubKey.ToOps().ToArray();
			if(ops.Length != 5)
				return false;
			return ops[0].Code == OpcodeType.OP_DUP &&
				   ops[1].Code == OpcodeType.OP_HASH160 &&
				   ops[2].PushData != null && ops[2].PushData.Length == 0x14 &&
				   ops[3].Code == OpcodeType.OP_EQUALVERIFY &&
				   ops[4].Code == OpcodeType.OP_CHECKSIG;
		}
		public KeyId ExtractScriptPubKeyParameters(Script script)
		{
			if(!CheckScripPubKey(script))
				return null;
			return new KeyId(script.ToOps().Skip(2).First().PushData);
		}

		public override bool CheckScriptSig(Script scriptSig, Script scriptPubKey)
		{
			var ops = scriptSig.ToOps().ToArray();
			if(ops.Length != 2)
				return false;
			return ops[0].PushData != null &&
				   ops[1].PushData != null && PubKey.IsValidSize(ops[1].PushData.Length);
		}

		public bool CheckScriptSig(Script scriptSig)
		{
			return CheckScriptSig(scriptSig, null);
		}

		

		public PayToPubkeyHashScriptSigParameters ExtractScriptSigParameters(Script scriptSig)
		{
			if(!CheckScriptSig(scriptSig))
				return null;
			var ops = scriptSig.ToOps().ToArray();
			return new PayToPubkeyHashScriptSigParameters()
			{
				TransactionSignature = new TransactionSignature(ops[0].PushData),
				PublicKey = new PubKey(ops[1].PushData),
			};
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
		public abstract bool CheckScripPubKey(Script scriptPubKey);
		public abstract bool CheckScriptSig(Script scriptSig, Script scriptPubKey);
		public abstract TxOutType Type
		{
			get;
		}
	}
}
