using System;

namespace NBitcoin.Scripting.Miniscript
{
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
		{
			ScriptToken token = obj as ScriptToken;
			if (token != null)
			{
				return Equals(token);
			}
			return false;
		}
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

		public static ScriptToken FromScript()
		{
			throw new Exception("");
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
