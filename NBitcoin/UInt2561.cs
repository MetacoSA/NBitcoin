
using System;
using System.Linq;
using System.IO;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;

namespace NBitcoin
{
	public class uint256 :  IBitcoinSerializable
	{
		public uint256()
		{
			for(int i = 0 ; i < WIDTH ; i++)
				pn[i] = 0;
		}

		public uint256(uint256 b)
		{
			for(int i = 0 ; i < WIDTH ; i++)
				pn[i] = b.pn[i];
		}

		private static readonly HexEncoder Encoder = new HexEncoder();
		private const int WIDTH = 256 / 32;
		private const int WIDTH_BYTE = 256 / 8;
		private UInt32[] pn = new UInt32[WIDTH];

		internal void SetHex(string str)
		{
			Array.Clear(pn, 0, pn.Length);
			str = str.Trim();

			if (str.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
				str = str.Substring(2);

			SetBytes(Encoder.DecodeData(str).Reverse().ToArray());
		}

		public byte GetByte(int index)
		{
			var uintIndex = index / sizeof(uint);
			var byteIndex = index % sizeof(uint);
			var value = pn[uintIndex];
			return (byte)(value >> (byteIndex * 8));
		}

		private void SetBytes(byte[] arr)
		{
			for (var i = 0; i < WIDTH && i < arr.Length / 4; i++)
			{
				pn[i] = Utils.ToUInt32(arr, 4 * i, true);
			}
		}

		public override string ToString()
		{ 
			return Encoder.EncodeData(ToBytes().Reverse().ToArray());
		}

		public uint256(ulong b)
		{
			pn[0] = (uint)b;
			pn[1] = (uint)(b >> 32);
			for (int i = 2; i < WIDTH; i++)
				pn[i] = 0;
		}

		public uint256(byte[] vch, bool lendian = true)
		{
			if (vch.Length != WIDTH_BYTE)
			{
				throw new FormatException("the byte array should be 256 byte long");
			}

			if(!lendian)
				vch = vch.Reverse().ToArray();

			SetBytes(vch);
		}

		public uint256(string str)
		{
			SetHex(str);
		}

		public uint256(byte[] vch)
			:this(vch, true)
		{
		}

		public override bool Equals(object obj)
		{
			var item = obj as uint256;
			if(item == null)
				return false;
			return AreEquals(pn, item.pn);
		}

		public static bool operator ==(uint256 a, uint256 b)
		{
			if(System.Object.ReferenceEquals(a, b))
				return true;
			if(((object)a == null) || ((object)b == null))
				return false;
			return AreEquals(a.pn, b.pn);
		}

		private static bool AreEquals(uint[] ar1, uint[] ar2)
		{
			if(ar1.Length != ar2.Length)
				return false;
			for(int i = 0 ; i < ar1.Length ; i++)
			{
				if(ar1[i] != ar2[i])
					return false;
			}
			return true;
		}

		public static bool operator <(uint256 a, uint256 b)
		{
			return Comparison(a, b) < 0;
		}

		public static bool operator >(uint256 a, uint256 b)
		{
			return Comparison(a, b) > 0;
		}

		public static bool operator <=(uint256 a, uint256 b)
		{
			return Comparison(a, b) <= 0;
		}

		public static bool operator >=(uint256 a, uint256 b)
		{
			return Comparison(a, b) >= 0;
		}

		private static int Comparison(uint256 a, uint256 b)
		{
			 for (int i = WIDTH-1; i >= 0; i--)
			{
				if (a.pn[i] < b.pn[i])
					return -1;
				if (a.pn[i] > b.pn[i])
					return 1;
			}
			return 0;
		}

		public static bool operator !=(uint256 a, uint256 b)
		{
			return !(a == b);
		}

		public static bool operator ==(uint256 a, ulong b)
		{
			return (a == new uint256(b));
		}

		public static bool operator !=(uint256 a, ulong b)
		{
			return !(a == new uint256(b));
		}

		public static uint256 operator ^(uint256 a, uint256 b)
		{
			var c = new uint256();
			c.pn = new uint[a.pn.Length];
			for(int i = 0 ; i < c.pn.Length ; i++)
			{
				c.pn[i] = a.pn[i] ^ b.pn[i];
			}
			return c;
		}

		public static bool operator!(uint256 a)
		{
			for (int i = 0; i < WIDTH; i++)
				if (a.pn[i] != 0)
					return false;
			return true;
		}

		public static uint256 operator-(uint256 a, uint256 b)
		{
			return a + (-b);
		}

		public static uint256 operator+(uint256 a, uint256 b)
		{
			var result = new uint256();
			ulong carry = 0;
			for (int i = 0; i < WIDTH; i++)
			{
				ulong n = carry + a.pn[i] + b.pn[i];
				result.pn[i] = (uint)(n & 0xffffffff);
				carry = n >> 32;
			}
			return result;
		}

		public static uint256 operator+(uint256 a, ulong b)
		{
			return a + new uint256(b);
		}

		public static implicit operator uint256(ulong value)
		{
			return new uint256(value);
		}

		public static uint256 operator &(uint256 a, uint256 b)
		{
			var n = new uint256(a);
			for(int i = 0 ; i < WIDTH ; i++)
				n.pn[i] &= b.pn[i];
			return n;
		}

		public static uint256 operator |(uint256 a, uint256 b)
		{
			var n = new uint256(a);
			for(int i = 0 ; i < WIDTH ; i++)
				n.pn[i] |= b.pn[i];
			return n;
		}

		public static uint256 operator <<(uint256 a, int shift)
		{
			var result = new uint256();
			int k = shift / 32;
			shift = shift % 32;
			for(int i = 0 ; i < WIDTH ; i++)
			{
				if(i + k + 1 < WIDTH && shift != 0)
					result.pn[i + k + 1] |= (a.pn[i] >> (32 - shift));
				if(i + k < WIDTH)
					result.pn[i + k] |= (a.pn[i] << shift);
			}
			return result;
		}

		public static uint256 operator >>(uint256 a, int shift)
		{
			var result = new uint256();
			int k = shift / 32;
			shift = shift % 32;
			for(int i = 0 ; i < WIDTH ; i++)
			{
				if(i - k - 1 >= 0 && shift != 0)
					result.pn[i - k - 1] |= (a.pn[i] << (32 - shift));
				if(i - k >= 0)
					result.pn[i - k] |= (a.pn[i] >> shift);
			}
			return result;
		}

		
		public static uint256 operator ~(uint256 a)
		{
			var b = new uint256();
			for(int i = 0 ; i < b.pn.Length ; i++)
			{
				b.pn[i] = ~a.pn[i];
			}
			return b;
		}

		public static uint256 operator -(uint256 a)
		{
			var b = new uint256();
			for(int i = 0 ; i < b.pn.Length ; i++)
			{
				b.pn[i] = ~a.pn[i];
			}
			b++;
			return b;
		}

		public static uint256 operator ++(uint256 a)
		{
			return a + 1;
		}

		public static uint256 operator --(uint256 a)
		{
			return a - 1;
		}
		
		public byte[] ToBytes(bool lendian = true)
		{
			var arr = new byte[WIDTH_BYTE];
			for (int i = 0; i < WIDTH; i++)
			{
				Buffer.BlockCopy(Utils.ToBytes(pn[i], true), 0, arr, 4 * i, 4);
			}
			if (!lendian)
				Array.Reverse(arr);
			return arr;
		}

		public void ReadWrite(BitcoinStream stream)
		{
			if(stream.Serializing)
			{
				var b = ToBytes();
				stream.ReadWrite(ref b);
			}
			else
			{
				byte[] b = new byte[WIDTH_BYTE];
				stream.ReadWrite(ref b);
				this.pn = new uint256(b).pn;
			}
		}

		public int GetSerializeSize(int nType=0, ProtocolVersion protocolVersion = ProtocolVersion.PROTOCOL_VERSION)
		{
			return WIDTH_BYTE;
		}

		public int Size
		{
			get
			{
				return WIDTH_BYTE;
			}
		}

		public ulong GetLow64()
		{
			return pn[0] | (ulong)pn[1] << 32;
		}

		public uint GetLow32()
		{
			return pn[0];
		}

		public override int GetHashCode()
		{
			int hash = 17;
			foreach(var element in pn)
			{
				hash = hash * 31 + element.GetHashCode();
			}
			return hash;
		}
	}
	public class uint160 :  IBitcoinSerializable
	{
		public uint160()
		{
			for(int i = 0 ; i < WIDTH ; i++)
				pn[i] = 0;
		}

		public uint160(uint160 b)
		{
			for(int i = 0 ; i < WIDTH ; i++)
				pn[i] = b.pn[i];
		}

		private static readonly HexEncoder Encoder = new HexEncoder();
		private const int WIDTH = 160 / 32;
		private const int WIDTH_BYTE = 160 / 8;
		private UInt32[] pn = new UInt32[WIDTH];

		internal void SetHex(string str)
		{
			Array.Clear(pn, 0, pn.Length);
			str = str.Trim();

			if (str.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
				str = str.Substring(2);

			SetBytes(Encoder.DecodeData(str).Reverse().ToArray());
		}

		public byte GetByte(int index)
		{
			var uintIndex = index / sizeof(uint);
			var byteIndex = index % sizeof(uint);
			var value = pn[uintIndex];
			return (byte)(value >> (byteIndex * 8));
		}

		private void SetBytes(byte[] arr)
		{
			for (var i = 0; i < WIDTH && i < arr.Length / 4; i++)
			{
				pn[i] = Utils.ToUInt32(arr, 4 * i, true);
			}
		}

		public override string ToString()
		{ 
			return Encoder.EncodeData(ToBytes().Reverse().ToArray());
		}

		public uint160(ulong b)
		{
			pn[0] = (uint)b;
			pn[1] = (uint)(b >> 32);
			for (int i = 2; i < WIDTH; i++)
				pn[i] = 0;
		}

		public uint160(byte[] vch, bool lendian = true)
		{
			if (vch.Length != WIDTH_BYTE)
			{
				throw new FormatException("the byte array should be 160 byte long");
			}

			if(!lendian)
				vch = vch.Reverse().ToArray();

			SetBytes(vch);
		}

		public uint160(string str)
		{
			SetHex(str);
		}

		public uint160(byte[] vch)
			:this(vch, true)
		{
		}

		public override bool Equals(object obj)
		{
			var item = obj as uint160;
			if(item == null)
				return false;
			return AreEquals(pn, item.pn);
		}

		public static bool operator ==(uint160 a, uint160 b)
		{
			if(System.Object.ReferenceEquals(a, b))
				return true;
			if(((object)a == null) || ((object)b == null))
				return false;
			return AreEquals(a.pn, b.pn);
		}

		private static bool AreEquals(uint[] ar1, uint[] ar2)
		{
			if(ar1.Length != ar2.Length)
				return false;
			for(int i = 0 ; i < ar1.Length ; i++)
			{
				if(ar1[i] != ar2[i])
					return false;
			}
			return true;
		}

		public static bool operator <(uint160 a, uint160 b)
		{
			return Comparison(a, b) < 0;
		}

		public static bool operator >(uint160 a, uint160 b)
		{
			return Comparison(a, b) > 0;
		}

		public static bool operator <=(uint160 a, uint160 b)
		{
			return Comparison(a, b) <= 0;
		}

		public static bool operator >=(uint160 a, uint160 b)
		{
			return Comparison(a, b) >= 0;
		}

		private static int Comparison(uint160 a, uint160 b)
		{
			 for (int i = WIDTH-1; i >= 0; i--)
			{
				if (a.pn[i] < b.pn[i])
					return -1;
				if (a.pn[i] > b.pn[i])
					return 1;
			}
			return 0;
		}

		public static bool operator !=(uint160 a, uint160 b)
		{
			return !(a == b);
		}

		public static bool operator ==(uint160 a, ulong b)
		{
			return (a == new uint160(b));
		}

		public static bool operator !=(uint160 a, ulong b)
		{
			return !(a == new uint160(b));
		}

		public static uint160 operator ^(uint160 a, uint160 b)
		{
			var c = new uint160();
			c.pn = new uint[a.pn.Length];
			for(int i = 0 ; i < c.pn.Length ; i++)
			{
				c.pn[i] = a.pn[i] ^ b.pn[i];
			}
			return c;
		}

		public static bool operator!(uint160 a)
		{
			for (int i = 0; i < WIDTH; i++)
				if (a.pn[i] != 0)
					return false;
			return true;
		}

		public static uint160 operator-(uint160 a, uint160 b)
		{
			return a + (-b);
		}

		public static uint160 operator+(uint160 a, uint160 b)
		{
			var result = new uint160();
			ulong carry = 0;
			for (int i = 0; i < WIDTH; i++)
			{
				ulong n = carry + a.pn[i] + b.pn[i];
				result.pn[i] = (uint)(n & 0xffffffff);
				carry = n >> 32;
			}
			return result;
		}

		public static uint160 operator+(uint160 a, ulong b)
		{
			return a + new uint160(b);
		}

		public static implicit operator uint160(ulong value)
		{
			return new uint160(value);
		}

		public static uint160 operator &(uint160 a, uint160 b)
		{
			var n = new uint160(a);
			for(int i = 0 ; i < WIDTH ; i++)
				n.pn[i] &= b.pn[i];
			return n;
		}

		public static uint160 operator |(uint160 a, uint160 b)
		{
			var n = new uint160(a);
			for(int i = 0 ; i < WIDTH ; i++)
				n.pn[i] |= b.pn[i];
			return n;
		}

		public static uint160 operator <<(uint160 a, int shift)
		{
			var result = new uint160();
			int k = shift / 32;
			shift = shift % 32;
			for(int i = 0 ; i < WIDTH ; i++)
			{
				if(i + k + 1 < WIDTH && shift != 0)
					result.pn[i + k + 1] |= (a.pn[i] >> (32 - shift));
				if(i + k < WIDTH)
					result.pn[i + k] |= (a.pn[i] << shift);
			}
			return result;
		}

		public static uint160 operator >>(uint160 a, int shift)
		{
			var result = new uint160();
			int k = shift / 32;
			shift = shift % 32;
			for(int i = 0 ; i < WIDTH ; i++)
			{
				if(i - k - 1 >= 0 && shift != 0)
					result.pn[i - k - 1] |= (a.pn[i] << (32 - shift));
				if(i - k >= 0)
					result.pn[i - k] |= (a.pn[i] >> shift);
			}
			return result;
		}

		
		public static uint160 operator ~(uint160 a)
		{
			var b = new uint160();
			for(int i = 0 ; i < b.pn.Length ; i++)
			{
				b.pn[i] = ~a.pn[i];
			}
			return b;
		}

		public static uint160 operator -(uint160 a)
		{
			var b = new uint160();
			for(int i = 0 ; i < b.pn.Length ; i++)
			{
				b.pn[i] = ~a.pn[i];
			}
			b++;
			return b;
		}

		public static uint160 operator ++(uint160 a)
		{
			return a + 1;
		}

		public static uint160 operator --(uint160 a)
		{
			return a - 1;
		}
		
		public byte[] ToBytes(bool lendian = true)
		{
			var arr = new byte[WIDTH_BYTE];
			for (int i = 0; i < WIDTH; i++)
			{
				Buffer.BlockCopy(Utils.ToBytes(pn[i], true), 0, arr, 4 * i, 4);
			}
			if (!lendian)
				Array.Reverse(arr);
			return arr;
		}

		public void ReadWrite(BitcoinStream stream)
		{
			if(stream.Serializing)
			{
				var b = ToBytes();
				stream.ReadWrite(ref b);
			}
			else
			{
				byte[] b = new byte[WIDTH_BYTE];
				stream.ReadWrite(ref b);
				this.pn = new uint160(b).pn;
			}
		}

		public int GetSerializeSize(int nType=0, ProtocolVersion protocolVersion = ProtocolVersion.PROTOCOL_VERSION)
		{
			return WIDTH_BYTE;
		}

		public int Size
		{
			get
			{
				return WIDTH_BYTE;
			}
		}

		public ulong GetLow64()
		{
			return pn[0] | (ulong)pn[1] << 32;
		}

		public uint GetLow32()
		{
			return pn[0];
		}

		public override int GetHashCode()
		{
			int hash = 17;
			foreach(var element in pn)
			{
				hash = hash * 31 + element.GetHashCode();
			}
			return hash;
		}
	}
}