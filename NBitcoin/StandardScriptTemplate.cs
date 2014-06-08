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

	public class TxNullDataTemplate : ScriptTemplate
	{
		public override bool CheckScriptPubKey(Script scriptPubKey)
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

		public Script GenerateScriptPubKey(byte[] data)
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

	public class PayToMultiSigTemplateParameters
	{
		public int SignatureCount
		{
			get;
			set;
		}
		public PubKey[] PubKeys
		{
			get;
			set;
		}
	}
	public class PayToMultiSigTemplate : ScriptTemplate
	{
		public Script GenerateScriptPubKey(int sigCount, PubKey[] keys)
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
		public override bool CheckScriptPubKey(Script scriptPubKey)
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

		public PayToMultiSigTemplateParameters ExtractScriptPubKeyParameters(Script scriptPubKey)
		{
			if(!CheckScriptPubKey(scriptPubKey))
				return null;

			var ops = scriptPubKey.ToOps().ToArray();
			var sigCount = (byte)ops[0].PushData[0];
			List<PubKey> keys = new List<PubKey>();
			for(int i = 1 ; i < ops.Length ; i++)
			{
				if(!PubKey.IsValidSize(ops[i].PushData.Length))
					break;
				keys.Add(new PubKey(ops[i].PushData));
			}

			return new PayToMultiSigTemplateParameters()
			{
				SignatureCount = sigCount,
				PubKeys = keys.ToArray()
			};
		}

		public override bool CheckScriptSig(Script scriptSig, Script scriptPubKey)
		{
			if(!scriptSig.IsPushOnly)
				return false;
			if(!CheckScriptPubKey(scriptPubKey))
				return false;

			var sigCountExpected = scriptPubKey.ToOps().First().GetValue();
			var sigOps = scriptSig.ToOps().ToArray();
			return sigOps[0].Code == OpcodeType.OP_0 ?
									sigCountExpected == sigOps.Length - 1 : sigCountExpected == sigOps.Length;
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
	public class PayToScriptHashTemplate : ScriptTemplate
	{
		public PayToScriptHashTemplate()
		{
			VerifyRedeemScript = true;
		}
		public bool VerifyRedeemScript
		{
			get;
			set;
		}
		public Script GenerateScriptPubKey(ScriptId scriptId)
		{
			return new Script(
				OpcodeType.OP_HASH160,
				Op.GetPushOp(scriptId.ToBytes()),
				OpcodeType.OP_EQUAL);
		}
		public Script GenerateScriptPubKey(Script scriptPubKey)
		{
			return GenerateScriptPubKey(scriptPubKey.ID);
		}

		public override bool CheckScriptPubKey(Script scriptPubKey)
		{
			var ops = scriptPubKey.ToOps().ToArray();
			if(ops.Length != 3)
				return false;
			return ops[0].Code == OpcodeType.OP_HASH160 &&
				   ops[1].Code == (OpcodeType)0x14 &&
				   ops[2].Code == OpcodeType.OP_EQUAL;
		}

		public Script GenerateScriptSig(Op[] ops, Script script)
		{
			var pushScript = Op.GetPushOp(script._Script);
			return new Script(ops.Concat(new[] { pushScript }).ToArray());
		}


		public Script GenerateScriptSig(TransactionSignature[] signatures, Script redeemScript)
		{
			List<Op> ops = new List<Op>();
			foreach(var sig in signatures)
			{
				ops.Add(Op.GetPushOp(sig.ToBytes()));
			}
			return GenerateScriptSig(ops.ToArray(), redeemScript);
		}
		public Script GenerateScriptSig(ECDSASignature[] signatures, Script redeemScript)
		{
			return GenerateScriptSig(signatures.Select(s => new TransactionSignature(s, SigHash.All)).ToArray(), redeemScript);
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

		public ScriptId ExtractScriptPubKeyParameters(Script scriptPubKey)
		{
			if(!this.CheckScriptPubKey(scriptPubKey))
				return null;
			return new ScriptId(scriptPubKey.ToOps().Skip(1).First().PushData);
		}
	}
	public class PayToPubkeyTemplate : ScriptTemplate
	{
		public Script GenerateScriptPubKey(PubKey pubkey)
		{
			return new Script(
					Op.GetPushOp(pubkey.ToBytes()),
					OpcodeType.OP_CHECKSIG
				);
		}
		public override bool CheckScriptPubKey(Script scriptPubKey)
		{
			var ops = scriptPubKey.ToOps().ToList();
			if(ops.Count != 2)
				return false;
			return ops[0].PushData != null && PubKey.IsValidSize(ops[0].PushData.Length) &&
				   ops[1].Code == OpcodeType.OP_CHECKSIG;
		}

		public Script GenerateScriptSig(ECDSASignature signature)
		{
			return GenerateScriptSig(new TransactionSignature(signature, SigHash.All));
		}
		public Script GenerateScriptSig(TransactionSignature signature)
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

		public PubKey ExtractScriptPubKeyParameters(Script script)
		{
			if(!CheckScriptPubKey(script))
				return null;
			return new PubKey(script.ToOps().ToArray()[0].PushData);
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
	public class PayToPubkeyHashTemplate : ScriptTemplate
	{
		public Script GenerateScriptPubKey(BitcoinAddress address)
		{
			if(address == null)
				throw new ArgumentNullException("address");
			return GenerateScriptPubKey(address.ID);
		}
		public Script GenerateScriptPubKey(PubKey pubKey)
		{
			if(pubKey == null)
				throw new ArgumentNullException("pubKey");
			return GenerateScriptPubKey(pubKey.ID);
		}
		public Script GenerateScriptPubKey(KeyId pubkeyHash)
		{
			return new Script(
					OpcodeType.OP_DUP,
					OpcodeType.OP_HASH160,
					Op.GetPushOp(pubkeyHash.ToBytes()),
					OpcodeType.OP_EQUALVERIFY,
					OpcodeType.OP_CHECKSIG
				);
		}

		public Script GenerateScriptSig(TransactionSignature signature, PubKey publicKey)
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

		public override bool CheckScriptPubKey(Script scriptPubKey)
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
			if(!CheckScriptPubKey(script))
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
		public abstract bool CheckScriptPubKey(Script scriptPubKey);
		public abstract bool CheckScriptSig(Script scriptSig, Script scriptPubKey);
		public abstract TxOutType Type
		{
			get;
		}
	}
}
