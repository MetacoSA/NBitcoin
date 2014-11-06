using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ooo = NBitcoin.BouncyCastle.Math;

namespace NBitcoin
{
	public class Op
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
		internal static bool IsPushCode(OpcodeType opcode)
		{
			return 0 <= opcode && opcode <= OpcodeType.OP_16 && opcode != OpcodeType.OP_RESERVED;
		}

		static Dictionary<string, OpcodeType> _OpcodeByName;
		static Op()
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

		public static Op GetPushOp(BigInteger data)
		{
			return GetPushOp(Utils.BigIntegerToBytes(data));
		}
		public static Op GetPushOp(byte[] data)
		{
			Op op = new Op();
			op.PushData = data;
			if(data.Length == 0)
				op.Code = OpcodeType.OP_0;
			else if(data.Length == 1 && (byte)1 <= data[0] && data[0] <= (byte)16)
				op.Code = (OpcodeType)(data[0] + (byte)OpcodeType.OP_1 - 1);
			else if(data.Length == 1 && (byte)0x81 == data[0])
				op.Code = OpcodeType.OP_1NEGATE;
			else if(0x01 <= data.Length && data.Length <= 0x4b)
				op.Code = (OpcodeType)(byte)data.Length;
			else if(data.Length <= 0xFF)
				op.Code = OpcodeType.OP_PUSHDATA1;
			else if(data.LongLength <= 0xFFFF)
				op.Code = OpcodeType.OP_PUSHDATA2;
			else if(data.LongLength <= 0xFFFFFFFF)
				op.Code = OpcodeType.OP_PUSHDATA4;
			else
				throw new NotSupportedException("Data length should not be bigger than 0xFFFFFFFF");
			return op;
		}

		internal Op()
		{

		}
		string _Name;
		public string Name
		{
			get
			{
				if(_Name == null)
					_Name = GetOpName(Code);
				return _Name;
			}
		}
		public OpcodeType Code
		{
			get;
			set;
		}
		public byte[] PushData
		{
			get;
			set;
		}

		private void PushDataToStream(byte[] data, Stream result)
		{
			var bitStream = new BitcoinStream(result, true);

			if(Code == OpcodeType.OP_0)
			{
				//OP_0 already pushed
				return;
			}

			if(OpcodeType.OP_1 <= Code && Code <= OpcodeType.OP_16)
			{
				//OP_1 to OP_16 already pushed
				return;
			}
			if(Code == OpcodeType.OP_1NEGATE)
			{
				//OP_1Negate already pushed
				return;
			}

			if(0x01 <= (byte)Code && (byte)Code <= 0x4b)
			{
				//Data length already pushed
			}
			else if(Code == OpcodeType.OP_PUSHDATA1)
			{
				bitStream.ReadWrite((byte)data.Length);
			}
			else if(Code == OpcodeType.OP_PUSHDATA2)
			{
				bitStream.ReadWrite((ushort)data.Length);
			}
			else if(Code == OpcodeType.OP_PUSHDATA4)
			{
				bitStream.ReadWrite((uint)data.Length);
			}
			else
				throw new NotSupportedException("Data length should not be bigger than 0xFFFFFFFF");
			result.Write(data, 0, data.Length);
		}
		internal static byte[] ReadData(Op op, Stream stream, bool ignoreWrongPush = false)
		{
			var opcode = op.Code;
			uint len = 0;
			BitcoinStream bitStream = new BitcoinStream(stream, false);
			if(opcode == 0)
				return new byte[0];

			if((byte)OpcodeType.OP_1 <= (byte)opcode && (byte)opcode <= (byte)OpcodeType.OP_16)
			{
				return new byte[] { (byte)(opcode - OpcodeType.OP_1 + 1) };
			}

			if(opcode == OpcodeType.OP_1NEGATE)
			{
				return new byte[] { 0x81 };
			}

			try
			{
				if(0x01 <= (byte)opcode && (byte)opcode <= 0x4b)
					len = (uint)opcode;
				else if(opcode == OpcodeType.OP_PUSHDATA1)
					len = bitStream.ReadWrite((byte)0);
				else if(opcode == OpcodeType.OP_PUSHDATA2)
					len = bitStream.ReadWrite((ushort)0);
				else if(opcode == OpcodeType.OP_PUSHDATA4)
					len = bitStream.ReadWrite((uint)0);
				else
					throw new FormatException("Invalid opcode for pushing data : " + opcode);
			}
			catch(EndOfStreamException)
			{
				if(!ignoreWrongPush)
					throw new FormatException("Incomplete script");
				op.IncompleteData = true;
				return new byte[0];
			}

			if(stream.CanSeek && stream.Length - stream.Position < len)
			{
				len = (uint)(stream.Length - stream.Position);
				if(!ignoreWrongPush)
					throw new FormatException("Not enough bytes pushed with " + opcode.ToString() + " expected " + len + " but got " + len);
				op.IncompleteData = true;
			}
			byte[] data = new byte[len];
			var readen = stream.Read(data, 0, data.Length);
			if(readen != data.Length && !ignoreWrongPush)
				throw new FormatException("Not enough bytes pushed with " + opcode.ToString() + " expected " + len + " but got " + readen);
			else if(readen != data.Length)
			{
				op.IncompleteData = true;
				Array.Resize(ref data, readen);
			}
			return data;
		}

		public byte[] ToBytes()
		{
			MemoryStream ms = new MemoryStream();
			WriteTo(ms);
			return ms.ToArray();
		}

		public override string ToString()
		{
			if(PushData != null)
			{
				if(PushData.Length == 0)
					return "0";
				var result = Encoders.Hex.EncodeData(PushData);
				return result.Length == 2 && result[0] == '0' ? result.Substring(1) : result;
			}
			else if(Name == "OP_UNKNOWN")
			{
				return Name + "(" + string.Format("0x{0:x2}", (byte)Code) + ")";
			}
			else
			{
				return Name;
			}
		}

		public void WriteTo(Stream stream)
		{
			stream.WriteByte((byte)Code);
			if(PushData != null)
			{
				PushDataToStream(PushData, stream);
			}
		}

		static string unknown = "OP_UNKNOWN(0x";
		internal static Op Read(TextReader textReader)
		{
			MemoryStream ms = new MemoryStream();
			var opname = ReadWord(textReader);
			var opcode = GetOpCode(opname);

			if(
				(opcode == OpcodeType.OP_INVALIDOPCODE || Op.IsPushCode(opcode))
				&& !opname.StartsWith(unknown)
				&& opname != "OP_INVALIDOPCODE")
			{
				if(opcode == OpcodeType.OP_0)
					return GetPushOp(new byte[0]);
				return GetPushOp(Encoders.Hex.DecodeData(opname.Length == 1 ? "0" + opname : opname));
			}
			else if(opname.StartsWith(unknown))
			{
				try
				{
					if(opname.StartsWith(unknown))
					{
						opcode = (OpcodeType)(Encoders.Hex.DecodeData(opname.Substring(unknown.Length, 2))[0]);
					}
				}
				catch(Exception ex)
				{
					throw new FormatException("Invalid unknown opcode", ex);
				}
			}

			return new Op()
			{
				Code = opcode
			};
		}

		public static implicit operator Op(OpcodeType codeType)
		{
			if(!IsPushCode(codeType))
				return new Op()
				{
					Code = codeType,
				};
			else
			{
				if(OpcodeType.OP_1 <= codeType && codeType <= OpcodeType.OP_16)
				{
					return new Op()
					{
						Code = codeType,
						PushData = new byte[] { (byte)((byte)codeType - (byte)OpcodeType.OP_1 + 1) }
					};
				}
				else if(codeType == OpcodeType.OP_0)
				{
					return new Op()
					{
						Code = codeType,
						PushData = new byte[0]
					};
				}
				else if(codeType == OpcodeType.OP_1NEGATE)
				{
					return new Op()
					{
						Code = codeType,
						PushData = new byte[] { 0x81 }
					};
				}
				else
				{
					throw new InvalidOperationException("Push OP without any data provided detected, Op.PushData instead");
				}
			}
		}

		private static string ReadWord(TextReader textReader)
		{
			StringBuilder builder = new StringBuilder();
			int r;
			while((r = textReader.Read()) != -1)
			{
				var ch = (char)r;
				bool isSpace = DataEncoder.IsSpace(ch);
				if(isSpace && builder.Length == 0)
					continue;
				if(isSpace && builder.Length != 0)
					break;
				builder.Append((char)r);
			}
			return builder.ToString();
		}

		public bool IncompleteData
		{
			get;
			set;
		}

		public bool IsSmallUInt
		{
			get
			{
				return Code == OpcodeType.OP_0 ||
						OpcodeType.OP_1 <= Code && Code <= OpcodeType.OP_16;
			}
		}
		public bool IsSmallInt
		{
			get
			{
				return IsSmallUInt || Code == OpcodeType.OP_1NEGATE;
			}
		}
		public BigInteger? GetValue()
		{
			if(PushData == null)
				return null;
			return Utils.BytesToBigInteger(PushData);
		}
	}
	public class ScriptReader
	{
		public bool IgnoreIncoherentPushData
		{
			get;
			set;
		}
		private readonly Stream _Inner;
		public Stream Inner
		{
			get
			{
				return _Inner;
			}
		}
		public ScriptReader(Stream stream)
		{
			if(stream == null)
				throw new ArgumentNullException("stream");
			_Inner = stream;
		}
		public ScriptReader(byte[] data)
			: this(new MemoryStream(data))
		{

		}


		public Op Read()
		{
			var b = Inner.ReadByte();
			if(b == -1)
				return null;
			var opcode = (OpcodeType)b;
			if(Op.IsPushCode(opcode))
			{
				Op op = new Op();
				op.Code = opcode;
				op.PushData = Op.ReadData(op, Inner, IgnoreIncoherentPushData);
				if(op.IncompleteData == true)
					return null;
				return op;
			}
			return new Op()
			{
				Code = opcode
			};
		}



		public IEnumerable<Op> ToEnumerable()
		{
			Op code;
			while((code = Read()) != null)
			{
				yield return code;
			}
		}
	}
}
