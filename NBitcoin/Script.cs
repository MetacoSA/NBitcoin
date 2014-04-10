using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
	public enum SigHash : byte
	{
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
		//Copied from satoshi's code
		public static string GetOpName(OpcodeType opcode)
		{
			switch(opcode)
			{
				// push value
				case OpcodeType.OP_0:
					return "0";
				case OpcodeType.OP_PUSHDATA1:
					return "OP_PUSHDATA1";
				case OpcodeType.OP_PUSHDATA2:
					return "OP_PUSHDATA2";
				case OpcodeType.OP_PUSHDATA4:
					return "OP_PUSHDATA4";
				case OpcodeType.OP_1NEGATE:
					return "-1";
				case OpcodeType.OP_RESERVED:
					return "OP_RESERVED";
				case OpcodeType.OP_1:
					return "1";
				case OpcodeType.OP_2:
					return "2";
				case OpcodeType.OP_3:
					return "3";
				case OpcodeType.OP_4:
					return "4";
				case OpcodeType.OP_5:
					return "5";
				case OpcodeType.OP_6:
					return "6";
				case OpcodeType.OP_7:
					return "7";
				case OpcodeType.OP_8:
					return "8";
				case OpcodeType.OP_9:
					return "9";
				case OpcodeType.OP_10:
					return "10";
				case OpcodeType.OP_11:
					return "11";
				case OpcodeType.OP_12:
					return "12";
				case OpcodeType.OP_13:
					return "13";
				case OpcodeType.OP_14:
					return "14";
				case OpcodeType.OP_15:
					return "15";
				case OpcodeType.OP_16:
					return "16";

				// control
				case OpcodeType.OP_NOP:
					return "OP_NOP";
				case OpcodeType.OP_VER:
					return "OP_VER";
				case OpcodeType.OP_IF:
					return "OP_IF";
				case OpcodeType.OP_NOTIF:
					return "OP_NOTIF";
				case OpcodeType.OP_VERIF:
					return "OP_VERIF";
				case OpcodeType.OP_VERNOTIF:
					return "OP_VERNOTIF";
				case OpcodeType.OP_ELSE:
					return "OP_ELSE";
				case OpcodeType.OP_ENDIF:
					return "OP_ENDIF";
				case OpcodeType.OP_VERIFY:
					return "OP_VERIFY";
				case OpcodeType.OP_RETURN:
					return "OP_RETURN";

				// stack ops
				case OpcodeType.OP_TOALTSTACK:
					return "OP_TOALTSTACK";
				case OpcodeType.OP_FROMALTSTACK:
					return "OP_FROMALTSTACK";
				case OpcodeType.OP_2DROP:
					return "OP_2DROP";
				case OpcodeType.OP_2DUP:
					return "OP_2DUP";
				case OpcodeType.OP_3DUP:
					return "OP_3DUP";
				case OpcodeType.OP_2OVER:
					return "OP_2OVER";
				case OpcodeType.OP_2ROT:
					return "OP_2ROT";
				case OpcodeType.OP_2SWAP:
					return "OP_2SWAP";
				case OpcodeType.OP_IFDUP:
					return "OP_IFDUP";
				case OpcodeType.OP_DEPTH:
					return "OP_DEPTH";
				case OpcodeType.OP_DROP:
					return "OP_DROP";
				case OpcodeType.OP_DUP:
					return "OP_DUP";
				case OpcodeType.OP_NIP:
					return "OP_NIP";
				case OpcodeType.OP_OVER:
					return "OP_OVER";
				case OpcodeType.OP_PICK:
					return "OP_PICK";
				case OpcodeType.OP_ROLL:
					return "OP_ROLL";
				case OpcodeType.OP_ROT:
					return "OP_ROT";
				case OpcodeType.OP_SWAP:
					return "OP_SWAP";
				case OpcodeType.OP_TUCK:
					return "OP_TUCK";

				// splice ops
				case OpcodeType.OP_CAT:
					return "OP_CAT";
				case OpcodeType.OP_SUBSTR:
					return "OP_SUBSTR";
				case OpcodeType.OP_LEFT:
					return "OP_LEFT";
				case OpcodeType.OP_RIGHT:
					return "OP_RIGHT";
				case OpcodeType.OP_SIZE:
					return "OP_SIZE";

				// bit logic
				case OpcodeType.OP_INVERT:
					return "OP_INVERT";
				case OpcodeType.OP_AND:
					return "OP_AND";
				case OpcodeType.OP_OR:
					return "OP_OR";
				case OpcodeType.OP_XOR:
					return "OP_XOR";
				case OpcodeType.OP_EQUAL:
					return "OP_EQUAL";
				case OpcodeType.OP_EQUALVERIFY:
					return "OP_EQUALVERIFY";
				case OpcodeType.OP_RESERVED1:
					return "OP_RESERVED1";
				case OpcodeType.OP_RESERVED2:
					return "OP_RESERVED2";

				// numeric
				case OpcodeType.OP_1ADD:
					return "OP_1ADD";
				case OpcodeType.OP_1SUB:
					return "OP_1SUB";
				case OpcodeType.OP_2MUL:
					return "OP_2MUL";
				case OpcodeType.OP_2DIV:
					return "OP_2DIV";
				case OpcodeType.OP_NEGATE:
					return "OP_NEGATE";
				case OpcodeType.OP_ABS:
					return "OP_ABS";
				case OpcodeType.OP_NOT:
					return "OP_NOT";
				case OpcodeType.OP_0NOTEQUAL:
					return "OP_0NOTEQUAL";
				case OpcodeType.OP_ADD:
					return "OP_ADD";
				case OpcodeType.OP_SUB:
					return "OP_SUB";
				case OpcodeType.OP_MUL:
					return "OP_MUL";
				case OpcodeType.OP_DIV:
					return "OP_DIV";
				case OpcodeType.OP_MOD:
					return "OP_MOD";
				case OpcodeType.OP_LSHIFT:
					return "OP_LSHIFT";
				case OpcodeType.OP_RSHIFT:
					return "OP_RSHIFT";
				case OpcodeType.OP_BOOLAND:
					return "OP_BOOLAND";
				case OpcodeType.OP_BOOLOR:
					return "OP_BOOLOR";
				case OpcodeType.OP_NUMEQUAL:
					return "OP_NUMEQUAL";
				case OpcodeType.OP_NUMEQUALVERIFY:
					return "OP_NUMEQUALVERIFY";
				case OpcodeType.OP_NUMNOTEQUAL:
					return "OP_NUMNOTEQUAL";
				case OpcodeType.OP_LESSTHAN:
					return "OP_LESSTHAN";
				case OpcodeType.OP_GREATERTHAN:
					return "OP_GREATERTHAN";
				case OpcodeType.OP_LESSTHANOREQUAL:
					return "OP_LESSTHANOREQUAL";
				case OpcodeType.OP_GREATERTHANOREQUAL:
					return "OP_GREATERTHANOREQUAL";
				case OpcodeType.OP_MIN:
					return "OP_MIN";
				case OpcodeType.OP_MAX:
					return "OP_MAX";
				case OpcodeType.OP_WITHIN:
					return "OP_WITHIN";

				// crypto
				case OpcodeType.OP_RIPEMD160:
					return "OP_RIPEMD160";
				case OpcodeType.OP_SHA1:
					return "OP_SHA1";
				case OpcodeType.OP_SHA256:
					return "OP_SHA256";
				case OpcodeType.OP_HASH160:
					return "OP_HASH160";
				case OpcodeType.OP_HASH256:
					return "OP_HASH256";
				case OpcodeType.OP_CODESEPARATOR:
					return "OP_CODESEPARATOR";
				case OpcodeType.OP_CHECKSIG:
					return "OP_CHECKSIG";
				case OpcodeType.OP_CHECKSIGVERIFY:
					return "OP_CHECKSIGVERIFY";
				case OpcodeType.OP_CHECKMULTISIG:
					return "OP_CHECKMULTISIG";
				case OpcodeType.OP_CHECKMULTISIGVERIFY:
					return "OP_CHECKMULTISIGVERIFY";

				// expanson
				case OpcodeType.OP_NOP1:
					return "OP_NOP1";
				case OpcodeType.OP_NOP2:
					return "OP_NOP2";
				case OpcodeType.OP_NOP3:
					return "OP_NOP3";
				case OpcodeType.OP_NOP4:
					return "OP_NOP4";
				case OpcodeType.OP_NOP5:
					return "OP_NOP5";
				case OpcodeType.OP_NOP6:
					return "OP_NOP6";
				case OpcodeType.OP_NOP7:
					return "OP_NOP7";
				case OpcodeType.OP_NOP8:
					return "OP_NOP8";
				case OpcodeType.OP_NOP9:
					return "OP_NOP9";
				case OpcodeType.OP_NOP10:
					return "OP_NOP10";



				// template matching params
				case OpcodeType.OP_PUBKEYHASH:
					return "OP_PUBKEYHASH";
				case OpcodeType.OP_PUBKEY:
					return "OP_PUBKEY";
				case OpcodeType.OP_SMALLDATA:
					return "OP_SMALLDATA";

				case OpcodeType.OP_INVALIDOPCODE:
					return "OP_INVALIDOPCODE";
				default:
					return "OP_UNKNOWN";
			}
		}
		static Dictionary<string, OpcodeType> _OpcodeByName;
		static Script()
		{
			_OpcodeByName = new Dictionary<string, OpcodeType>();
			foreach(var code in Enum.GetValues(typeof(OpcodeType)).Cast<OpcodeType>().Distinct())
			{
				var name = GetOpName(code);
				if(name != "OP_UNKNOWN")
					_OpcodeByName.Add(name, code);
			}
		}
		public static OpcodeType GetOpCode(string name)
		{
			OpcodeType code;
			if(_OpcodeByName.TryGetValue(name, out code))
				return code;
			else
				return OpcodeType.OP_INVALIDOPCODE;
		}

		byte[] _Script = new byte[0];
		public Script()
		{

		}
		public Script(string script)
		{
			_Script = Parse(script);
		}

		private static byte[] Parse(string script)
		{
			MemoryStream result = new MemoryStream();
			var instructions = new Queue<string>(script.Split(DataEncoder.SpaceCharacters, StringSplitOptions.RemoveEmptyEntries));
			while(instructions.Count != 0)
			{
				var instruction = instructions.Dequeue();
				var opCode = GetOpCode(instruction);
				if(opCode != OpcodeType.OP_INVALIDOPCODE)
				{
					result.WriteByte((byte)opCode);
				}
				else
				{
					var data = Encoders.Hex.DecodeData(instruction);
					var bitStream = new BitcoinStream(result, true);
					if(data.Length == 0)
					{
						result.WriteByte((byte)OpcodeType.OP_0);
					}
					else if(0x01 <= data.Length && data.Length <= 0x4b)
					{
						result.WriteByte((byte)data.Length);
					}
					else if(data.Length <= 0xFF)
					{
						result.WriteByte((byte)OpcodeType.OP_PUSHDATA1);
						bitStream.ReadWrite((byte)data.Length);
					}
					else if(data.LongLength <= 0xFFFF)
					{
						result.WriteByte((byte)OpcodeType.OP_PUSHDATA2);
						bitStream.ReadWrite((ushort)data.Length);
					}
					else if(data.LongLength <= 0xFFFFFFFF)
					{
						result.WriteByte((byte)OpcodeType.OP_PUSHDATA4);
						bitStream.ReadWrite((uint)data.Length);
					}
					result.Write(data, 0, data.Length);
				}
			}
			return result.ToArray();
		}

		public Script(byte[] data)
		{
			_Script = data;
		}
		public static bool VerifyScript(Script scriptSig, Script scriptPubKey, Transaction txTo, int nIn, ScriptVerify flags, SigHash nHashType)
		{
			throw new NotImplementedException();
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			VarString str = new VarString(_Script);
			stream.ReadWrite(ref str);
			if(!stream.Serializing)
				_Script = str.GetString();
		}

		#endregion

		public string ToHex()
		{
			return Encoders.Hex.EncodeData(_Script);
		}

		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();
			MemoryStream ms = new MemoryStream(_Script);

			while(true)
			{
				builder.Append(" ");
				var b = ms.ReadByte();
				if(b == -1)
					break;
				var opcode = (OpcodeType)b;
				var name = GetOpName(opcode);
				if(IsPushData(opcode))
				{
					builder.Append(Encoders.Hex.EncodeData(ReadData(opcode, ms)));
				}
				else
				{
					builder.Append(name);
				}
			}

			return builder == null ? "" : builder.ToString().Trim();
		}

		static bool IsPushData(OpcodeType opcode)
		{
			return 0 <= opcode && opcode <= OpcodeType.OP_PUSHDATA4;
		}

		private static byte[] ReadData(OpcodeType opcode, Stream stream)
		{
			uint len = 0;
			BitcoinStream bitStream = new BitcoinStream(stream, false);
			if(opcode == 0)
				return new byte[0];
			if(0x01 <= (byte)opcode && (byte)opcode <= 0x4b)
				len = (uint)opcode;
			else if(opcode == OpcodeType.OP_PUSHDATA1)
				len = bitStream.ReadWrite((byte)0);
			else if(opcode == OpcodeType.OP_PUSHDATA2)
				len = bitStream.ReadWrite((ushort)0);
			else if(opcode == OpcodeType.OP_PUSHDATA4)
				len = bitStream.ReadWrite((uint)0);
			else
				throw new InvalidOperationException("Invalid opcode for pushing data : " + opcode);

			byte[] data = new byte[len];
			stream.Read(data, 0, data.Length);
			return data;
		}
	}
}
