using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using NBitcoin.Scripting.Parser;

namespace NBitcoin.Scripting.Miniscript
{
	/// <summary>
	/// Precursor of the actual miniscript fragment when we deserialize from the raw script from the raw script.
	/// </summary>
	internal class ScriptToken : IEquatable<ScriptToken>
	{
		internal static class Tags
		{
			public const int BoolAnd = 0;

			public const int BoolOr = 1;

			public const int Add = 2;

			public const int Equal = 3;

			public const int CheckSig = 4;

			public const int CheckMultiSig = 5;

			public const int CheckSequenceVerify = 6;

			public const int CheckLocktimeVerify = 7;


			public const int FromAltStack = 8;

			public const int ToAltStack = 9;

			public const int Drop = 10;

			public const int Dup = 11;

			public const int If = 12;

			public const int IfDup = 13;

			public const int NotIf = 14;

			public const int Else = 15;

			public const int EndIf = 16;

			public const int ZeroNotEqual = 17;

			public const int Size = 18;

			public const int Swap = 19;

			public const int Verify = 20;

			public const int Ripemd160 = 21;

			public const int Hash160 = 22;

			public const int Sha256 = 23;

			public const int Hash256 = 24;

			public const int Number = 25;

			public const int Hash20 = 26;

			public const int Hash32 = 27;

			public const int Pk = 28;

		}

		private ScriptToken(int tag) => Tag = tag;

		internal int Tag { get; }

		public static ScriptToken BoolAnd { get; } = new ScriptToken(Tags.BoolAnd);
		public static ScriptToken BoolOr { get; } = new ScriptToken(Tags.BoolOr);
		public static ScriptToken Add { get; } = new ScriptToken(Tags.Add);
		public static ScriptToken Equal { get; } = new ScriptToken(Tags.Equal);
		public static ScriptToken CheckSig { get; } = new ScriptToken(Tags.CheckSig);
		public static ScriptToken CheckMultiSig { get; } = new ScriptToken(Tags.CheckMultiSig);
		public static ScriptToken CheckSequenceVerify { get; } = new ScriptToken(Tags.CheckSequenceVerify);
		public static ScriptToken CheckLocktimeVerify { get; } = new ScriptToken(Tags.CheckLocktimeVerify);
		public static ScriptToken FromAltStack { get; } = new ScriptToken(Tags.FromAltStack);
		public static ScriptToken ToAltStack { get; } = new ScriptToken(Tags.ToAltStack);
		public static ScriptToken Drop { get; } = new ScriptToken(Tags.Drop);
		public static ScriptToken Dup { get; } = new ScriptToken(Tags.Dup);
		public static ScriptToken If { get; } = new ScriptToken(Tags.If);
		public static ScriptToken IfDup { get; } = new ScriptToken(Tags.IfDup);
		public static ScriptToken NotIf { get; } = new ScriptToken(Tags.NotIf);
		public static ScriptToken Else { get; } = new ScriptToken(Tags.Else);
		public static ScriptToken EndIf { get; } = new ScriptToken(Tags.EndIf);
		public static ScriptToken ZeroNotEqual { get; } = new ScriptToken(Tags.ZeroNotEqual);
		public static ScriptToken Size { get; } = new ScriptToken(Tags.Size);
		public static ScriptToken Swap { get; } = new ScriptToken(Tags.Swap);
		public static ScriptToken Verify { get; } = new ScriptToken(Tags.Verify);
		public static ScriptToken Ripemd160 { get; } = new ScriptToken(Tags.Ripemd160);
		public static ScriptToken Hash160 { get; } = new ScriptToken(Tags.Hash160);
		public static ScriptToken Sha256 { get; } = new ScriptToken(Tags.Sha256);
		public static ScriptToken Hash256 { get; } = new ScriptToken(Tags.Hash256);

		internal class Number : ScriptToken
		{
			internal uint Item { get; }
			internal Number(uint item) : base(Tags.Number) => Item = item;
		}

		internal class Hash20 : ScriptToken
		{
			internal uint160 Item { get; }
			internal Hash20(uint160 item) : base(Tags.Hash20)
			{
				Item = item;
			}
		}
		internal class Hash32 : ScriptToken
		{
			internal uint256 Item { get; }
			internal Hash32(uint256 item) : base(Tags.Hash32) => Item = item;
		}
		internal class Pk : ScriptToken
		{
			internal PubKey Item { get; }
			internal Pk(PubKey item) : base(Tags.Pk) => Item = item;
		}


		public sealed override int GetHashCode()
		{
			int num = 0;
			switch (Tag)
			{
				case Tags.Number:
					{
						Number number = (Number)this;
						num = Tags.Number;
						return -1640531527 + ((int)number.Item + ((num << 6) + (num >> 2)));
					}
				case Tags.Hash20:
					{
						Hash20 hash160 = (Hash20)this;
						num = Tags.Hash20;
						return -1640531527 + (hash160.Item.GetHashCode()) + ((num << 6) + (num >> 2));
					}
				case Tags.Hash32:
					{
						Hash32 sha256Hash = (Hash32)this;
						num = Tags.Hash32;
						return -1640531527 + (sha256Hash.Item.GetHashCode()) + ((num << 6) + (num >> 2));
					}
				case Tags.Pk:
					{
						Pk pk = (Pk)this;
						num = Tags.Hash32;
						return -1640531527 + (pk.Item.GetHashCode()) + ((num << 6) + (num >> 2));
					}
				default:
					return Tag;
			}
		}

		public sealed override bool Equals(object obj)
			=> Equals(obj as ScriptToken);
		public bool Equals(ScriptToken obj)
		{
			if (obj == null)
				return false;
			int tag = Tag;
			int tag2 = obj.Tag;
			if (tag == tag2)
			{
				switch (Tag)
				{
					case Tags.Number:
						{
							Number number = (Number)this;
							Number number2 = (Number)obj;
							return number.Item == number2.Item;
						}
					case Tags.Hash20:
						{
							Hash20 hash160 = (Hash20)this;
							Hash20 hash160Hash2 = (Hash20)obj;
							return hash160.Item == hash160Hash2.Item;
						}
					case Tags.Hash32:
						{
							Hash32 sha256Hash = (Hash32)this;
							Hash32 sha256Hash2 = (Hash32)obj;
							return sha256Hash.Item == sha256Hash2.Item;
						}
					case Tags.Pk:
						{
							Pk pk = (Pk)this;
							Pk pk2 = (Pk)obj;
							return pk.Item == pk2.Item;
						}
					default:
						return true;
				}
			}
			return false;
		}

		public static ScriptToken[] FromScript(Script script)
		{
			var ret = new List<ScriptToken>();
			foreach (var s in script.ToOps())
			{
				switch (s.Code)
				{
					case OpcodeType.OP_BOOLAND:
						ret.Add(BoolAnd);
						break;
					case OpcodeType.OP_BOOLOR:
						ret.Add(BoolOr);
						break;
					case OpcodeType.OP_EQUAL:
						ret.Add(Equal);
						break;
					case OpcodeType.OP_EQUALVERIFY:
						ret.Add(Equal);
						ret.Add(Verify);
						break;
					case OpcodeType.OP_CHECKSIG:
						ret.Add(CheckSig);
						break;
					case OpcodeType.OP_CHECKSIGVERIFY:
						ret.Add(CheckSig);
						ret.Add(Verify);
						break;
					case OpcodeType.OP_CHECKMULTISIG:
						ret.Add(CheckMultiSig);
						break;
					case OpcodeType.OP_CHECKMULTISIGVERIFY:
						ret.Add(CheckMultiSig);
						ret.Add(Verify);
						break;
					case OpcodeType.OP_CHECKSEQUENCEVERIFY:
						ret.Add(CheckSequenceVerify);
						break;
					case OpcodeType.OP_CHECKLOCKTIMEVERIFY:
						ret.Add(CheckLocktimeVerify);
						break;
					case OpcodeType.OP_FROMALTSTACK:
						ret.Add(FromAltStack);
						break;
					case OpcodeType.OP_TOALTSTACK:
						ret.Add(ToAltStack);
						break;
					case OpcodeType.OP_DROP:
						ret.Add(Drop);
						break;
					case OpcodeType.OP_DUP:
						ret.Add(Dup);
						break;
					case OpcodeType.OP_ADD:
						ret.Add(Add);
						break;
					case OpcodeType.OP_IF:
						ret.Add(If);
						break;
					case OpcodeType.OP_NOTIF:
						ret.Add(NotIf);
						break;
					case OpcodeType.OP_ELSE:
						ret.Add(Else);
						break;
					case OpcodeType.OP_ENDIF:
						ret.Add(EndIf);
						break;
					case OpcodeType.OP_0NOTEQUAL:
						ret.Add(ZeroNotEqual);
						break;
					case OpcodeType.OP_SIZE:
						ret.Add(Size);
						break;
					case OpcodeType.OP_SWAP:
						ret.Add(Swap);
						break;
					case OpcodeType.OP_VERIFY:
						var last = ret.LastOrDefault();
						if (last.Equals(Equal) || last.Equals(CheckSig) || last.Equals(CheckMultiSig))
							throw new ParsingException($"Miniscript must use truncated OP_*VERIFY but got separated representation {last} and OP_VERIFY when deserializing");
						ret.Add(Verify);
						break;
					case OpcodeType.OP_SHA256:
						ret.Add(Sha256);
						break;
					case OpcodeType.OP_HASH256:
						ret.Add(Sha256);
						break;
					case OpcodeType.OP_RIPEMD160:
						ret.Add(Ripemd160);
						break;
					case OpcodeType.OP_HASH160:
						ret.Add(Hash160);
						break;
					case OpcodeType.OP_0:
						ret.Add(new Number(0u));
						break;
					case OpcodeType.OP_1:
						ret.Add(new Number(1u));
						break;
					case OpcodeType.OP_2:
						ret.Add(new Number(2u));
						break;
					case OpcodeType.OP_3:
						ret.Add(new Number(3u));
						break;
					case OpcodeType.OP_4:
						ret.Add(new Number(4u));
						break;
					case OpcodeType.OP_5:
						ret.Add(new Number(5u));
						break;
					case OpcodeType.OP_6:
						ret.Add(new Number(6u));
						break;
					case OpcodeType.OP_7:
						ret.Add(new Number(7u));
						break;
					case OpcodeType.OP_8:
						ret.Add(new Number(8u));
						break;
					case OpcodeType.OP_9:
						ret.Add(new Number(9u));
						break;
					case OpcodeType.OP_10:
						ret.Add(new Number(10u));
						break;
					case OpcodeType.OP_11:
						ret.Add(new Number(11u));
						break;
					case OpcodeType.OP_12:
						ret.Add(new Number(12u));
						break;
					case OpcodeType.OP_13:
						ret.Add(new Number(13u));
						break;
					case OpcodeType.OP_14:
						ret.Add(new Number(14u));
						break;
					case OpcodeType.OP_15:
						ret.Add(new Number(15u));
						break;
					case OpcodeType.OP_16:
						ret.Add(new Number(16u));
						break;
					default:
						if (0x01 <= (byte) s.Code && (byte) s.Code < 0x48)
							ret.Add(GetItem(s));
						else if (0x48 <= (byte)s.Code)
							throw new ParsingException($"Miniscript does not support pushdata bigger than 33. Got {s}");
						else
							throw new ParsingException($"Unknown Opcode to Miniscript {s.Name}");
						break;
				}
			}
			ret.Reverse();
			return ret.ToArray();
		}

		private static ScriptToken GetItem(Op op)
		{
			if (op.PushData.Length == 20)
				return new Hash20(new uint160(op.PushData, false));
			if (op.PushData.Length == 32)
				return new Hash32(new uint256(op.PushData, false));
			if (op.PushData.Length == 33)
			{
				try
				{
					return new Pk(new PubKey(op.PushData));
				}
				catch (FormatException ex)
				{
					throw new ParsingException("Invalid Public Key", ex);
				}
			}

			var i = op.GetInt();
			if (i.HasValue)
				return new Number((uint)i.Value);
			throw new ParsingException($"Invalid push with Opcode {op}");
		}

		public override string ToString()
		{
			switch (this.Tag)
			{
				case Tags.BoolAnd:
					return "BoolAnd";
				case Tags.BoolOr:
					return "BoolAnd";
				case Tags.Add:
					return "Add";
				case Tags.Equal:
					return "Equal";
				case Tags.CheckSig:
					return "CheckSig";
				case Tags.CheckMultiSig:
					return "CheckMultiSig";
				case Tags.CheckSequenceVerify:
					return "CheckSequenceVerify";
				case Tags.CheckLocktimeVerify:
					return "CheckLocktimeVerify";
				case Tags.FromAltStack:
					return "FromAltStack";
				case Tags.ToAltStack:
					return "ToAltStack";
				case Tags.Drop:
					return "Drop";
				case Tags.Dup:
					return "Dup";
				case Tags.If:
					return "If";
				case Tags.IfDup:
					return "IfDup";
				case Tags.NotIf:
					return "NotIf";
				case Tags.Else:
					return "Else";
				case Tags.EndIf:
					return "EndIf";
				case Tags.ZeroNotEqual:
					return "ZeroNotEqual";
				case Tags.Size:
					return "Size";
				case Tags.Swap:
					return "Swap";
				case Tags.Verify:
					return "Verify";
				case Tags.Ripemd160:
					return "Ripemd160";
				case Tags.Hash160:
					return "Hash160";
				case Tags.Sha256:
					return "Sha256";
				case Tags.Hash256:
					return "Hash256";
				case Tags.Number:
					var n = ((Number)this).Item;
					return $"Number({n})";
				case Tags.Hash20:
					var hash20 = ((Hash20)this).Item;
					return $"Hash20({hash20})";
				case Tags.Hash32:
					var hash32 = ((Hash32)this).Item;
					return $"Hash30({hash32})";
				case Tags.Pk:
					var pk = ((Pk)this).Item;
					return $"Pk({pk})";
			}
			throw new Exception("Unreachable");
		}
	}
}
