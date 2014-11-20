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
		private static readonly TxNullDataTemplate _Instance = new TxNullDataTemplate();
		public static TxNullDataTemplate Instance
		{
			get
			{
				return _Instance;
			}
		}
		protected override bool FastCheckScriptPubKey(Script scriptPubKey)
		{
			var bytes = scriptPubKey.ToRawScript(true);
			return bytes.Length >= 1 && bytes[0] == (byte)OpcodeType.OP_RETURN;
		}
		protected override bool CheckScriptPubKeyCore(Script scriptPubKey, Op[] scriptPubKeyOps)
		{
			var ops = scriptPubKeyOps;
			if(ops.Length < 1)
				return false;
			if(ops[0].Code != OpcodeType.OP_RETURN)
				return false;
			if(ops.Length == 2)
			{
				return ops[1].PushData != null && ops[1].PushData.Length <= 40;
				throw new NotSupportedException();
			}
			return true;
		}
		public byte[] ExtractScriptPubKeyParameters(Script scriptPubKey)
		{
			if(!FastCheckScriptPubKey(scriptPubKey))
				return null;
			var ops = scriptPubKey.ToOps().ToArray();
			if(ops.Length != 2)
				return null;
			if(ops[1].PushData == null || ops[1].PushData.Length > 40)
				return null;
			return ops[1].PushData;
		}

		protected override bool CheckScriptSigCore(Script scriptSig, Op[] scriptSigOps, Script scriptPubKey, Op[] scriptPubKeyOps)
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

		public byte[][] InvalidPubKeys
		{
			get;
			set;
		}
	}
	public class PayToMultiSigTemplate : ScriptTemplate
	{
		private static readonly PayToMultiSigTemplate _Instance = new PayToMultiSigTemplate();
		public static PayToMultiSigTemplate Instance
		{
			get
			{
				return _Instance;
			}
		}
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
		protected override bool CheckScriptPubKeyCore(Script scriptPubKey, Op[] scriptPubKeyOps)
		{
			var ops = scriptPubKeyOps;
			if(ops.Length < 3)
				return false;

			var sigCount = ops[0];
			if(!sigCount.IsSmallUInt)
				return false;
			var pubKeyCount = ops[ops.Length - 2];
			if(!pubKeyCount.IsSmallUInt)
				return false;
			var keyCount = (uint)pubKeyCount.GetValue();
			if(1 + keyCount + 1 + 1 != ops.Length)
				return false;
			for(int i = 1 ; i < keyCount + 1 ; i++)
			{
				if(ops[i].PushData == null)
					return false;
			}
			return ops[ops.Length - 1].Code == OpcodeType.OP_CHECKMULTISIG;
		}

		public PayToMultiSigTemplateParameters ExtractScriptPubKeyParameters(Script scriptPubKey)
		{
			if(!FastCheckScriptPubKey(scriptPubKey))
				return null;
			var ops = scriptPubKey.ToOps().ToArray();
			if(!CheckScriptPubKeyCore(scriptPubKey, ops))
				return null;

			var sigCount = (int)ops[0].GetValue();
			var keyCount = (int)ops[ops.Length - 2].GetValue();

			List<PubKey> keys = new List<PubKey>();
			List<byte[]> invalidKeys = new List<byte[]>();
			for(int i = 1 ; i < keyCount + 1 ; i++)
			{
				if(!PubKey.IsValidSize(ops[i].PushData.Length))
					invalidKeys.Add(ops[i].PushData);
				else
				{
					try
					{
						keys.Add(new PubKey(ops[i].PushData));
					}
					catch(FormatException)
					{
						invalidKeys.Add(ops[i].PushData);
					}
				}
			}

			return new PayToMultiSigTemplateParameters()
			{
				SignatureCount = sigCount,
				PubKeys = keys.ToArray(),
				InvalidPubKeys = invalidKeys.ToArray()
			};
		}

		protected override bool FastCheckScriptSig(Script scriptSig, Script scriptPubKey)
		{
			var bytes = scriptSig.ToRawScript(true);
			return bytes.Length >= 1 &&
				   bytes[0] == (byte)OpcodeType.OP_0;
		}

		protected override bool CheckScriptSigCore(Script scriptSig, Op[] scriptSigOps, Script scriptPubKey, Op[] scriptPubKeyOps)
		{
			if(!scriptSig.IsPushOnly)
				return false;
			if(scriptSigOps[0].Code != OpcodeType.OP_0)
				return false;
			if(scriptSigOps.Length == 1)
				return false;
			if(!scriptSigOps.Skip(1).All(s => TransactionSignature.ValidLength(s.PushData.Length) || s.Code == OpcodeType.OP_0))
				return false;
			if(scriptPubKeyOps != null)
			{
				if(!CheckScriptPubKeyCore(scriptPubKey, scriptPubKeyOps))
					return false;
				var sigCountExpected = scriptPubKeyOps[0].GetValue();
				return sigCountExpected == scriptSigOps.Length + 1;
			}
			return true;

		}

		public TransactionSignature[] ExtractScriptSigParameters(Script scriptSig)
		{
			if(!FastCheckScriptSig(scriptSig, null))
				return null;
			var ops = scriptSig.ToOps().ToArray();
			if(!CheckScriptSigCore(scriptSig, ops, null, null))
				return null;
			try
			{
				return ops.Skip(1).Select(i => i.Code == OpcodeType.OP_0 ? null : new TransactionSignature(i.PushData)).ToArray();
			}
			catch(FormatException)
			{
				return null;
			}
		}

		public override TxOutType Type
		{
			get
			{
				return TxOutType.TX_MULTISIG;
			}
		}

		public Script GenerateScriptSig(TransactionSignature[] signatures)
		{
			List<Op> ops = new List<Op>();
			ops.Add(OpcodeType.OP_0);
			foreach(var sig in signatures)
			{
				if(sig == null)
					ops.Add(OpcodeType.OP_0);
				else
					ops.Add(Op.GetPushOp(sig.ToBytes()));
			}
			return new Script(ops.ToArray());
		}
	}

	public class PayToScriptHashSigParameters
	{
		public Script RedeemScript
		{
			get;
			set;
		}
		public TransactionSignature[] Signatures
		{
			get;
			set;
		}
	}
	//https://github.com/bitcoin/bips/blob/master/bip-0016.mediawiki
	public class PayToScriptHashTemplate : ScriptTemplate
	{
		private static readonly PayToScriptHashTemplate _Instance = new PayToScriptHashTemplate();
		public static PayToScriptHashTemplate Instance
		{
			get
			{
				return _Instance;
			}
		}
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

		protected override bool FastCheckScriptPubKey(Script scriptPubKey)
		{
			var bytes = scriptPubKey.ToRawScript(true);
			return
				   bytes.Length >= 2 &&
				   bytes[0] == (byte)OpcodeType.OP_HASH160 &&
				   bytes[1] == 0x14;
		}

		protected override bool CheckScriptPubKeyCore(Script scriptPubKey, Op[] scriptPubKeyOps)
		{
			var ops = scriptPubKeyOps;
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
		public PayToScriptHashSigParameters ExtractScriptSigParameters(Script scriptSig)
		{
			var ops = scriptSig.ToOps().ToArray();
			if(!CheckScriptSigCore(scriptSig, ops, null, null))
				return null;
			try
			{
				var multiSig = ops.Length > 0 && ops[0].Code == OpcodeType.OP_0;
				PayToScriptHashSigParameters result = new PayToScriptHashSigParameters();
				result.Signatures =
					ops
					.Skip(multiSig ? 1 : 0)
					.Take(ops.Length - 1 - (multiSig ? 1 : 0))
					.Select(o => o.Code == OpcodeType.OP_0 ? null : new TransactionSignature(o.PushData))
					.ToArray();
				result.RedeemScript = new Script(ops[ops.Length - 1].PushData);
				return result;
			}
			catch(Exception)
			{
				return null;
			}
		}

		public Script GenerateScriptSig(TransactionSignature[] signatures, Script redeemScript)
		{
			List<Op> ops = new List<Op>();
			PayToMultiSigTemplate multiSigTemplate = new PayToMultiSigTemplate();
			bool multiSig = multiSigTemplate.CheckScriptPubKey(redeemScript);
			if(multiSig)
				ops.Add(OpcodeType.OP_0);
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
		protected override bool CheckScriptSigCore(Script scriptSig, Op[] scriptSigOps, Script scriptPubKey, Op[] scriptPubKeyOps)
		{
			var ops = scriptSigOps;
			if(ops.Length == 0)
				return false;
			if(!scriptSig.IsPushOnly)
				return false;
			if(!VerifyRedeemScript)
				return true;
			var redeemScript = new Script(ops[ops.Length - 1].PushData);
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
			if(!FastCheckScriptPubKey(scriptPubKey))
				return null;
			var ops = scriptPubKey.ToOps().ToArray();
			if(!CheckScriptPubKeyCore(scriptPubKey, ops))
				return null;
			return new ScriptId(ops[1].PushData);
		}
	}
	public class PayToPubkeyTemplate : ScriptTemplate
	{
		private static readonly PayToPubkeyTemplate _Instance = new PayToPubkeyTemplate();
		public static PayToPubkeyTemplate Instance
		{
			get
			{
				return _Instance;
			}
		}
		public Script GenerateScriptPubKey(PubKey pubkey)
		{
			return new Script(
					Op.GetPushOp(pubkey.ToBytes()),
					OpcodeType.OP_CHECKSIG
				);
		}
		protected override bool CheckScriptPubKeyCore(Script scriptPubKey, Op[] scriptPubKeyOps)
		{
			var ops = scriptPubKeyOps;
			if(ops.Length != 2)
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

		public TransactionSignature ExtractScriptSigParameters(Script scriptSig)
		{
			var ops = scriptSig.ToOps().ToArray();
			if(!CheckScriptSigCore(scriptSig, ops, null, null))
				return null;

			var data = ops[0].PushData;
			if(!TransactionSignature.ValidLength(data.Length))
				return null;
			try
			{
				return new TransactionSignature(data);
			}
			catch(FormatException)
			{
				return null;
			}
		}

		protected override bool CheckScriptSigCore(Script scriptSig, Op[] scriptSigOps, Script scriptPubKey, Op[] scriptPubKeyOps)
		{
			var ops = scriptSigOps;
			if(ops.Length != 1)
				return false;

			return ops[0].PushData != null && PubKey.IsValidSize(ops[0].PushData.Length);
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
			var ops = script.ToOps().ToArray();
			if(!CheckScriptPubKeyCore(script, ops))
				return null;
			try
			{
				return new PubKey(ops[0].PushData);
			}
			catch(FormatException)
			{
				return null;
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
	public class PayToPubkeyHashTemplate : ScriptTemplate
	{
		private static readonly PayToPubkeyHashTemplate _Instance = new PayToPubkeyHashTemplate();
		public static PayToPubkeyHashTemplate Instance
		{
			get
			{
				return _Instance;
			}
		}
		public Script GenerateScriptPubKey(BitcoinAddress address)
		{
			if(address == null)
				throw new ArgumentNullException("address");
			return GenerateScriptPubKey((KeyId)address.ID);
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

		protected override bool FastCheckScriptPubKey(Script scriptPubKey)
		{
			var bytes = scriptPubKey.ToRawScript(true);
			return bytes.Length >= 3 &&
				   bytes[0] == (byte)OpcodeType.OP_DUP &&
				   bytes[1] == (byte)OpcodeType.OP_HASH160 &&
				   bytes[2] == 0x14;
		}

		protected override bool CheckScriptPubKeyCore(Script scriptPubKey, Op[] scriptPubKeyOps)
		{
			var ops = scriptPubKeyOps;
			if(ops.Length != 5)
				return false;
			return ops[0].Code == OpcodeType.OP_DUP &&
				   ops[1].Code == OpcodeType.OP_HASH160 &&
				   ops[2].PushData != null && ops[2].PushData.Length == 0x14 &&
				   ops[3].Code == OpcodeType.OP_EQUALVERIFY &&
				   ops[4].Code == OpcodeType.OP_CHECKSIG;
		}
		public KeyId ExtractScriptPubKeyParameters(Script scriptPubKey)
		{
			var ops = scriptPubKey.ToOps().ToArray();
			if(!CheckScriptPubKeyCore(scriptPubKey, ops))
				return null;
			return new KeyId(ops[2].PushData);
		}

		protected override bool CheckScriptSigCore(Script scriptSig, Op[] scriptSigOps, Script scriptPubKey, Op[] scriptPubKeyOps)
		{
			var ops = scriptSigOps;
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
			var ops = scriptSig.ToOps().ToArray();
			if(!CheckScriptSigCore(scriptSig, ops, null, null))
				return null;
			try
			{
				return new PayToPubkeyHashScriptSigParameters()
				{
					TransactionSignature = new TransactionSignature(ops[0].PushData),
					PublicKey = new PubKey(ops[1].PushData),
				};
			}
			catch(FormatException)
			{
				return null;
			}
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
		public bool CheckScriptPubKey(Script scriptPubKey)
		{
			if(scriptPubKey == null)
				throw new ArgumentNullException("scriptPubKey");
			if(!FastCheckScriptPubKey(scriptPubKey))
				return false;
			return CheckScriptPubKeyCore(scriptPubKey, scriptPubKey.ToOps().ToArray());
		}

		protected virtual bool FastCheckScriptPubKey(Script scriptPubKey)
		{
			return true;
		}

		protected abstract bool CheckScriptPubKeyCore(Script scriptPubKey, Op[] scriptPubKeyOps);
		public bool CheckScriptSig(Script scriptSig, Script scriptPubKey)
		{
			if(scriptSig == null)
				throw new ArgumentNullException("scriptSig");

			if(!FastCheckScriptSig(scriptSig, scriptPubKey))
				return false;
			return CheckScriptSigCore(scriptSig, scriptSig.ToOps().ToArray(), scriptPubKey, scriptPubKey == null ? null : scriptPubKey.ToOps().ToArray());
		}

		protected virtual bool FastCheckScriptSig(Script scriptSig, Script scriptPubKey)
		{
			return true;
		}

		protected abstract bool CheckScriptSigCore(Script scriptSig, Op[] scriptSigOps, Script scriptPubKey, Op[] scriptPubKeyOps);
		public abstract TxOutType Type
		{
			get;
		}
	}
}
