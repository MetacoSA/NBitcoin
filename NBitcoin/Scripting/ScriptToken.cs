using System;

namespace NBitcoin.Scripting
{
	internal class ScriptToken : IEquatable<ScriptToken>
	{
		internal static class Tags
		{
			public const int BoolAnd = 0;

			public const int BoolOr = 1;

			public const int Add = 2;

			public const int Equal = 3;

			public const int EqualVerify = 4;

			public const int CheckSig = 5;

			public const int CheckSigVerify = 6;

			public const int CheckMultiSig = 7;

			public const int CheckMultiSigVerify = 8;

			public const int CheckSequenceVerify = 9;

			public const int FromAltStack = 10;

			public const int ToAltStack = 11;

			public const int Drop = 12;

			public const int Dup = 13;

			public const int If = 14;

			public const int IfDup = 15;

			public const int NotIf = 16;

			public const int Else = 17;

			public const int EndIf = 18;

			public const int ZeroNotEqual = 19;

			public const int Size = 20;

			public const int Swap = 21;

			public const int Verify = 23;

			public const int Hash160 = 24;

			public const int Sha256 = 25;

			public const int Number = 26;

			public const int Hash160Hash = 27;

			public const int Sha256Hash = 28;

			public const int Pk = 29;

		}

		private ScriptToken(int tag) => Tag = tag;

		internal int Tag { get; }

		internal static readonly ScriptToken _unique_BoolAnd = new ScriptToken(0);
		internal static ScriptToken BoolAnd { get { return _unique_BoolAnd; } }
		internal static readonly ScriptToken _unique_BoolOr = new ScriptToken(1);
		internal static ScriptToken BoolOr { get { return _unique_BoolOr; } }

		internal static readonly ScriptToken _unique_Add = new ScriptToken(2);
		internal static ScriptToken Add { get { return _unique_Add; } }

		internal static readonly ScriptToken _unique_Equal = new ScriptToken(3);
		internal static ScriptToken Equal { get { return _unique_Equal; } }

		internal static readonly ScriptToken _unique_EqualVerify = new ScriptToken(4);
		internal static ScriptToken EqualVerify { get { return _unique_EqualVerify; } }

		internal static readonly ScriptToken _unique_CheckSig = new ScriptToken(5);
		internal static ScriptToken CheckSig { get { return _unique_CheckSig; } }

		internal static readonly ScriptToken _unique_CheckSigVerify = new ScriptToken(6);
		internal static ScriptToken CheckSigVerify { get { return _unique_CheckSigVerify; } }
		internal static readonly ScriptToken _unique_CheckMultiSig = new ScriptToken(7);
		internal static ScriptToken CheckMultiSig { get { return _unique_CheckMultiSig; } }
		internal static readonly ScriptToken _unique_CheckMultiSigVerify = new ScriptToken(8);
		internal static ScriptToken CheckMultiSigVerify { get { return _unique_CheckMultiSigVerify; } }

		internal static readonly ScriptToken _unique_CheckSequenceVerify = new ScriptToken(9);
		internal static ScriptToken CheckSequenceVerify { get { return _unique_CheckSequenceVerify; } }
		internal static readonly ScriptToken _unique_FromAltStack = new ScriptToken(10);
		internal static ScriptToken FromAltStack { get { return _unique_FromAltStack; } }
		internal static readonly ScriptToken _unique_ToAltStack = new ScriptToken(11);
		internal static ScriptToken ToAltStack { get { return _unique_ToAltStack; } }

		internal static readonly ScriptToken _unique_Drop = new ScriptToken(12);
		internal static ScriptToken Drop { get { return _unique_Drop; } }

		internal static readonly ScriptToken _unique_Dup = new ScriptToken(13);
		internal static ScriptToken Dup { get { return _unique_Dup; } }

		internal static readonly ScriptToken _unique_If = new ScriptToken(14);
		internal static ScriptToken If { get { return _unique_If; } }
		internal static readonly ScriptToken _unique_IfDup = new ScriptToken(15);
		internal static ScriptToken IfDup { get { return _unique_IfDup; } }

		internal static readonly ScriptToken _unique_NotIf = new ScriptToken(16);
		internal static ScriptToken NotIf { get { return _unique_NotIf; } }

		internal static readonly ScriptToken _unique_Else = new ScriptToken(17);
		internal static ScriptToken Else { get { return _unique_Else; } }

		internal static readonly ScriptToken _unique_EndIf = new ScriptToken(18);
		internal static ScriptToken EndIf { get { return _unique_EndIf; } }
		internal static readonly ScriptToken _unique_ZeroNotEqual = new ScriptToken(19);
		internal static ScriptToken ZeroNotEqual { get { return _unique_ZeroNotEqual; } }

		internal static readonly ScriptToken _unique_Size = new ScriptToken(20);
		internal static ScriptToken Size { get { return _unique_Size; } }

		internal static readonly ScriptToken _unique_Swap = new ScriptToken(21);
		internal static ScriptToken Swap { get { return _unique_Swap; } }
		internal static readonly ScriptToken _unique_Verify = new ScriptToken(23);
		internal static ScriptToken Verify { get { return _unique_Verify; } }
		internal static readonly ScriptToken _unique_Hash160 = new ScriptToken(24);
		internal static ScriptToken Hash160 { get { return _unique_Hash160; } }
		internal static readonly ScriptToken _unique_Sha256 = new ScriptToken(25);
		internal static ScriptToken Sha256 { get { return _unique_Sha256; } }
		internal class Number : ScriptToken
		{
			internal uint Item { get; }
			internal Number(uint item) : base(26) => Item = item;
		}

		internal class Hash160Hash : ScriptToken
		{
			internal uint160 Item { get; }
			internal Hash160Hash(uint160 item) : base(27)
			{
				Item = item;
			}
		}
		internal class Sha256Hash : ScriptToken
		{
			internal uint256 Item { get; }
			internal Sha256Hash(uint256 item) : base(28) => Item = item;
		}
		internal class Pk : ScriptToken
		{
			internal PubKey Item { get; }
			internal Pk(PubKey item) : base(29) => Item = item;
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
				case Tags.EqualVerify:
					return "EqualVerify";
				case Tags.CheckSig:
					return "CheckSig";
				case Tags.CheckSigVerify:
					return "CheckSigVerify";
				case Tags.CheckMultiSig:
					return "CheckMultiSig";
				case Tags.CheckMultiSigVerify:
					return "CheckMultiSigVerify";
				case Tags.CheckSequenceVerify:
					return "CheckSequenceVerify";
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
				case Tags.Hash160:
					return "Hash160";
				case Tags.Sha256:
					return "Sha256";
				case Tags.Number:
					var n = ((Number)this).Item;
					return $"Number({n})";
				case Tags.Hash160Hash:
					var hash160 = ((Hash160Hash)this).Item;
					return $"Hash160Hash({hash160})";
				case Tags.Sha256Hash:
					var sha256 = ((Sha256Hash)this).Item;
					return $"Sha256Hash({sha256})";
				case Tags.Pk:
					var pk = ((Pk)this).Item;
					return $"Pk({pk})";
			}
			throw new Exception("Unreachable");
		}

		public sealed override int GetHashCode()
		{
			if (this != null)
			{
				int num = 0;
				switch (Tag)
				{
					case 26:
						{
							Number number = (Number)this;
							num = 26;
							return -1640531527 + ((int)number.Item + ((num << 6) + (num >> 2)));
						}
					case 27:
						{
							Hash160Hash hash160Hash = (Hash160Hash)this;
							num = 27;
							return -1640531527 + (hash160Hash.Item.GetHashCode()) + ((num << 6) + (num >> 2));
						}
					case 28:
						{
							Sha256Hash sha256Hash = (Sha256Hash)this;
							num = 28;
							return -1640531527 + (sha256Hash.Item.GetHashCode()) + ((num << 6) + (num >> 2));
						}
					case 29:
						{
							Pk pk = (Pk)this;
							num = 29;
							return -1640531527 + (pk.Item.GetHashCode()) + ((num << 6) + (num >> 2));
						}
					default:
						return Tag;
				}
			}
			return 0;
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
			if (this != null)
			{
				if (obj != null)
				{
					int tag = Tag;
					int tag2 = obj.Tag;
					if (tag == tag2)
					{
						switch (Tag)
						{
							case 26:
								{
									Number number = (Number)this;
									Number number2 = (Number)obj;
									return number.Item == number2.Item;
								}
							case 27:
								{
									Hash160Hash hash160Hash = (Hash160Hash)this;
									Hash160Hash hash160Hash2 = (Hash160Hash)obj;
									return hash160Hash.Item == hash160Hash2.Item;
								}
							case 28:
								{
									Sha256Hash sha256Hash = (Sha256Hash)this;
									Sha256Hash sha256Hash2 = (Sha256Hash)obj;
									return sha256Hash.Item == sha256Hash2.Item;
								}
							case 29:
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
				return false;
			}
			return obj == null;
		}

	}
}