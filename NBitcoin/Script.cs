using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	/** Script verification flags */
	[Flags]
	public enum ScriptVerify
	{
		None = 0,
		P2SH = 1, // evaluate P2SH (BIP16) subscripts
		StrictEnc = 2, // enforce strict conformance to DER and SEC2 for signatures and pubkeys
		EvenS = 4, // enforce even S values in signatures (depends on STRICTENC)
		NoCache = 8, // do not store results in signature cache (but do query it)
	};

	/** Signature hash types/flags */
	public enum SigHash : uint
	{
		Undefined = 0,
		All = 1,
		None = 2,
		Single = 3,
		AnyoneCanPay = 0x80,
	};

	/** Script opcodes */
	public enum OpcodeType : byte
	{
		// push value
		OP_0 = 0x00,
		OP_FALSE = OP_0,
		OP_PUSHDATA1 = 0x4c,
		OP_PUSHDATA2 = 0x4d,
		OP_PUSHDATA4 = 0x4e,
		OP_1NEGATE = 0x4f,
		OP_RESERVED = 0x50,
		OP_1 = 0x51,
		OP_TRUE = OP_1,
		OP_2 = 0x52,
		OP_3 = 0x53,
		OP_4 = 0x54,
		OP_5 = 0x55,
		OP_6 = 0x56,
		OP_7 = 0x57,
		OP_8 = 0x58,
		OP_9 = 0x59,
		OP_10 = 0x5a,
		OP_11 = 0x5b,
		OP_12 = 0x5c,
		OP_13 = 0x5d,
		OP_14 = 0x5e,
		OP_15 = 0x5f,
		OP_16 = 0x60,

		// control
		OP_NOP = 0x61,
		OP_VER = 0x62,
		OP_IF = 0x63,
		OP_NOTIF = 0x64,
		OP_VERIF = 0x65,
		OP_VERNOTIF = 0x66,
		OP_ELSE = 0x67,
		OP_ENDIF = 0x68,
		OP_VERIFY = 0x69,
		OP_RETURN = 0x6a,

		// stack ops
		OP_TOALTSTACK = 0x6b,
		OP_FROMALTSTACK = 0x6c,
		OP_2DROP = 0x6d,
		OP_2DUP = 0x6e,
		OP_3DUP = 0x6f,
		OP_2OVER = 0x70,
		OP_2ROT = 0x71,
		OP_2SWAP = 0x72,
		OP_IFDUP = 0x73,
		OP_DEPTH = 0x74,
		OP_DROP = 0x75,
		OP_DUP = 0x76,
		OP_NIP = 0x77,
		OP_OVER = 0x78,
		OP_PICK = 0x79,
		OP_ROLL = 0x7a,
		OP_ROT = 0x7b,
		OP_SWAP = 0x7c,
		OP_TUCK = 0x7d,

		// splice ops
		OP_CAT = 0x7e,
		OP_SUBSTR = 0x7f,
		OP_LEFT = 0x80,
		OP_RIGHT = 0x81,
		OP_SIZE = 0x82,

		// bit logic
		OP_INVERT = 0x83,
		OP_AND = 0x84,
		OP_OR = 0x85,
		OP_XOR = 0x86,
		OP_EQUAL = 0x87,
		OP_EQUALVERIFY = 0x88,
		OP_RESERVED1 = 0x89,
		OP_RESERVED2 = 0x8a,

		// numeric
		OP_1ADD = 0x8b,
		OP_1SUB = 0x8c,
		OP_2MUL = 0x8d,
		OP_2DIV = 0x8e,
		OP_NEGATE = 0x8f,
		OP_ABS = 0x90,
		OP_NOT = 0x91,
		OP_0NOTEQUAL = 0x92,

		OP_ADD = 0x93,
		OP_SUB = 0x94,
		OP_MUL = 0x95,
		OP_DIV = 0x96,
		OP_MOD = 0x97,
		OP_LSHIFT = 0x98,
		OP_RSHIFT = 0x99,

		OP_BOOLAND = 0x9a,
		OP_BOOLOR = 0x9b,
		OP_NUMEQUAL = 0x9c,
		OP_NUMEQUALVERIFY = 0x9d,
		OP_NUMNOTEQUAL = 0x9e,
		OP_LESSTHAN = 0x9f,
		OP_GREATERTHAN = 0xa0,
		OP_LESSTHANOREQUAL = 0xa1,
		OP_GREATERTHANOREQUAL = 0xa2,
		OP_MIN = 0xa3,
		OP_MAX = 0xa4,

		OP_WITHIN = 0xa5,

		// crypto
		OP_RIPEMD160 = 0xa6,
		OP_SHA1 = 0xa7,
		OP_SHA256 = 0xa8,
		OP_HASH160 = 0xa9,
		OP_HASH256 = 0xaa,
		OP_CODESEPARATOR = 0xab,
		OP_CHECKSIG = 0xac,
		OP_CHECKSIGVERIFY = 0xad,
		OP_CHECKMULTISIG = 0xae,
		OP_CHECKMULTISIGVERIFY = 0xaf,

		// expansion
		OP_NOP1 = 0xb0,
		OP_NOP2 = 0xb1,
		OP_NOP3 = 0xb2,
		OP_NOP4 = 0xb3,
		OP_NOP5 = 0xb4,
		OP_NOP6 = 0xb5,
		OP_NOP7 = 0xb6,
		OP_NOP8 = 0xb7,
		OP_NOP9 = 0xb8,
		OP_NOP10 = 0xb9,



		// template matching params
		OP_SMALLDATA = 0xf9,
		OP_SMALLINTEGER = 0xfa,
		OP_PUBKEYS = 0xfb,
		OP_PUBKEYHASH = 0xfd,
		OP_PUBKEY = 0xfe,

		OP_INVALIDOPCODE = 0xff,
	};


	public class Script : IBitcoinSerializable
	{

		internal byte[] _Script = new byte[0];
		public Script()
		{

		}
		public Script(params Op[] ops)
		{
			MemoryStream ms = new MemoryStream();
			foreach(var op in ops)
			{
				op.WriteTo(ms);
			}
			_Script = ms.ToArray();
		}
		public Script(string script)
		{
			_Script = Parse(script);
		}

		private static byte[] Parse(string script)
		{
			var reader = new StringReader(script);
			MemoryStream result = new MemoryStream();
			while(reader.Peek() != -1)
			{
				Op.Read(reader).WriteTo(result);
			}
			return result.ToArray();
		}

		public Script(byte[] data)
		{
			_Script = data.ToArray();
		}


		public int Length
		{
			get
			{
				return _Script.Length;
			}
		}




		public ScriptReader CreateReader(bool ignoreErrors = false)
		{
			return new ScriptReader(_Script)
			{
				IgnoreIncoherentPushData = ignoreErrors
			};
		}


		public int FindAndDelete(OpcodeType op)
		{
			return FindAndDelete(new Op()
			{
				Code = op
			});
		}
		public int FindAndDelete(Op op)
		{
			if(op == null)
				return 0;
			return FindAndDelete(o => o.Code == op.Code && Utils.ArrayEqual(o.PushData, op.PushData));
		}

		public int FindAndDelete(byte[] pushedData)
		{
			if(pushedData.Length == 0)
				return 0;
			var standardOp = Op.GetPushOp(pushedData);
			return FindAndDelete(op =>
							op.Code == standardOp.Code &&
							op.PushData != null && Utils.ArrayEqual(op.PushData, pushedData));
		}
		internal int FindAndDelete(Func<Op, bool> predicate)
		{
			int nFound = 0;
			List<Op> operations = new List<Op>();
			foreach(var op in this.ToOps())
			{
				var shouldDelete = predicate(op);
				if(!shouldDelete)
				{
					operations.Add(op);
				}
				else
					nFound++;
			}
			if(nFound == 0)
				return 0;
			_Script = new Script(operations.ToArray())._Script;
			return nFound;
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWriteAsVarString(ref _Script);
		}

		#endregion

		public string ToHex()
		{
			return Encoders.Hex.EncodeData(_Script);
		}

		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();
			ScriptReader reader = new ScriptReader(_Script)
			{
				IgnoreIncoherentPushData = true
			};

			Op op = null;
			while((op = reader.Read()) != null)
			{
				builder.Append(" ");
				builder.Append(op.ToString());
			}

			return builder == null ? "" : builder.ToString().Trim();
		}

		public bool IsPushOnly
		{
			get
			{
				foreach(var script in CreateReader(true).ToEnumerable())
				{
					if(script.PushData == null)
						return false;
				}
				return true;
			}
		}

		public bool HasCanonicalPushes
		{
			get
			{
				foreach(var op in CreateReader(true).ToEnumerable())
				{
					if(op.IncompleteData)
						return false;
					if(op.Code > OpcodeType.OP_16)
						continue;
					if(op.Code < OpcodeType.OP_PUSHDATA1 && op.Code > OpcodeType.OP_0 && (op.PushData.Length == 1 && op.PushData[0] <= 16))
						// Could have used an OP_n code, rather than a 1-byte push.
						return false;
					if(op.Code == OpcodeType.OP_PUSHDATA1 && op.PushData.Length < (byte)OpcodeType.OP_PUSHDATA1)
						// Could have used a normal n-byte push, rather than OP_PUSHDATA1.
						return false;
					if(op.Code == OpcodeType.OP_PUSHDATA2 && op.PushData.Length <= 0xFF)
						// Could have used an OP_PUSHDATA1.
						return false;
					if(op.Code == OpcodeType.OP_PUSHDATA4 && op.PushData.Length <= 0xFFFF)
						// Could have used an OP_PUSHDATA2.
						return false;
				}
				return true;
			}
		}


		//https://en.bitcoin.it/wiki/OP_CHECKSIG
		public uint256 SignatureHash(Transaction txTo, int nIn, SigHash nHashType)
		{
			if(nIn >= txTo.VIn.Count)
			{
				Utils.log("ERROR: SignatureHash() : nIn=" + nIn + " out of range\n");
				return 1;
			}

			// Check for invalid use of SIGHASH_SINGLE
			if(nHashType == SigHash.Single)
			{
				if(nIn >= txTo.VOut.Count)
				{
					Utils.log("ERROR: SignatureHash() : nOut=" + nIn + " out of range\n");
					return 1;
				}
			}

			var scriptCopy = new Script(_Script);
			scriptCopy.FindAndDelete(OpcodeType.OP_CODESEPARATOR);

			var txCopy = new Transaction(txTo.ToBytes());
			//Set all TxIn script to empty string
			foreach(var txin in txCopy.VIn)
			{
				txin.ScriptSig = new Script();
			}
			//Copy subscript into the txin script you are checking
			txCopy.VIn[nIn].ScriptSig = scriptCopy;

			if(((int)nHashType & 31) == (int)SigHash.None)
			{
				//The output of txCopy is set to a vector of zero size.
				txCopy.VOut.Clear();
				//All other inputs aside from the current input in txCopy have their nSequence index set to zero
				for(int i = 0 ; i < txCopy.VIn.Count ; i++)
				{
					if(i == nIn)
						continue;
					txCopy.VIn[i].Sequence = 0;
				}
			}

			if(((int)nHashType & 31) == (int)SigHash.Single)
			{
				//The output of txCopy is resized to the size of the current input index+1.
				var remainingOut = txCopy.VOut.Take(nIn + 1).ToArray();
				txCopy.VOut.Clear();
				txCopy.VOut.AddRange(remainingOut);
				//All other txCopy outputs aside from the output that is the same as the current input index are set to a blank script and a value of (long) -1.
				for(int i = 0 ; i < txCopy.VOut.Count ; i++)
				{
					if(i == nIn)
						continue;
					txCopy.VOut[i] = new TxOut();
				}
				for(int i = 0 ; i < txCopy.VIn.Count ; i++)
				{
					//All other txCopy inputs aside from the current input are set to have an nSequence index of zero.
					if(i == nIn)
						continue;
					txCopy.VIn[i].Sequence = 0;
				}
			}

			if(((int)nHashType & (int)SigHash.AnyoneCanPay) != 0)
			{
				//The txCopy input vector is resized to a length of one.
				var script = txCopy.VIn[nIn];
				txCopy.VIn.Clear();
				txCopy.VIn.Add(script);
				//The subScript (lead in by its length as a var-integer encoded!) is set as the first and only member of this vector.
				txCopy.VIn[0].ScriptSig = scriptCopy;
			}


			//Serialize TxCopy, append 4 byte hashtypecode
			MemoryStream ms = new MemoryStream();
			BitcoinStream bitcoinStream = new BitcoinStream(ms, true);
			txCopy.ReadWrite(bitcoinStream);
			bitcoinStream.ReadWrite((uint)nHashType);

			var hashed = ms.ToArray();
			return Hashes.Hash256(hashed);
		}

		public static Script operator +(Script a, int value)
		{
			return a + Utils.BigIntegerToBytes(value);
		}

		public static Script operator +(Script a, IEnumerable<byte> bytes)
		{
			if(a == null)
				return new Script(Op.GetPushOp(bytes.ToArray()));
			return a + Op.GetPushOp(bytes.ToArray());
		}
		public static Script operator +(Script a, Op op)
		{
			if(a == null)
				return new Script(op);
			return new Script(a._Script.Concat(op.ToBytes()).ToArray());
		}

		public static Script operator +(Script a, IEnumerable<Op> op)
		{
			if(a == null)
				return new Script(op.ToArray());
			return new Script(a._Script.Concat(new Script(op.ToArray())._Script).ToArray());
		}

		public IEnumerable<Op> ToOps()
		{
			ScriptReader reader = new ScriptReader(_Script)
			{
				IgnoreIncoherentPushData = true
			};
			return reader.ToEnumerable();
		}

		public uint GetSigOpCount(bool fAccurate)
		{
			uint n = 0;
			Op lastOpcode = null;
			foreach(var op in ToOps())
			{
				if(op.Code == OpcodeType.OP_CHECKSIG || op.Code == OpcodeType.OP_CHECKSIGVERIFY)
					n++;
				else if(op.Code == OpcodeType.OP_CHECKMULTISIG || op.Code == OpcodeType.OP_CHECKMULTISIGVERIFY)
				{
					if(fAccurate && lastOpcode.Code >= OpcodeType.OP_1 && lastOpcode.Code <= OpcodeType.OP_16)
						n += (lastOpcode.PushData == null || lastOpcode.PushData.Length == 0) ? 0U : (uint)lastOpcode.PushData[0];
					else
						n += 20;
				}
				lastOpcode = op;
			}
			return n;
		}

		public KeyId ID
		{
			get
			{
				return new KeyId(Hashes.Hash160(_Script));
			}
		}

		public bool IsPayToScriptHash
		{
			get
			{
				return new PayToScriptHashScriptTemplate().CheckScripPubKey(this);
			}
		}
		public uint GetSigOpCount(Script scriptSig)
		{
			if(!IsPayToScriptHash)
				return GetSigOpCount(true);
			// This is a pay-to-script-hash scriptPubKey;
			// get the last item that the scriptSig
			// pushes onto the stack:
			var validSig = new PayToScriptHashScriptTemplate()
			{
				VerifyRedeemScript = false
			}.CheckScriptSig(scriptSig, null);
			if(!validSig)
				return 0;
			/// ... and return its opcount:
			return new Script(scriptSig.ToOps().Last().PushData).GetSigOpCount(true);
		}

		public PubKey GetSourcePubKey()
		{
			var template = new PayToPubkeyHashScriptTemplate();
			var result = template.ExtractScriptSigParameters(this);
			return result == null ? null : result.PublicKey;
		}

		public KeyId GetDestination()
		{
			return new PayToPubkeyHashScriptTemplate().ExtractScriptPubKeyParameters(this);
		}

		public byte[] ToRawScript()
		{
			return _Script.ToArray();
		}

		public static bool VerifyScript(Script scriptSig, Script scriptPubKey, Transaction tx, int i, ScriptVerify scriptVerify = ScriptVerify.StrictEnc | ScriptVerify.P2SH, SigHash sigHash = SigHash.Undefined)
		{
			ScriptEvaluationContext eval = new ScriptEvaluationContext();
			eval.SigHash = sigHash;
			eval.ScriptVerify = scriptVerify;
			return eval.VerifyScript(scriptSig, scriptPubKey, tx, i);
		}
	}
}
