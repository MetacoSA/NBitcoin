using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	class BitReader
	{
		BitArray array;
		public BitReader(byte[] data, int bitCount)
		{
			BitWriter writer = new BitWriter();
			writer.Write(data, bitCount);
			array = writer.ToBitArray();
		}

		public BitReader(BitArray array)
		{
			this.array = new BitArray(array.Count);
			for(int i = 0 ; i < array.Count ; i++)
				this.array.Set(i, array.Get(i));
		}

		public BitReader(int[] indices)
		{
			BitWriter writer = new BitWriter();
			writer.Write(indices);
			array = writer.ToBitArray();
		}

		public bool Read()
		{
			var v = array.Get(Position);
			Position++;
			return v;
		}

		public int Position
		{
			get;
			set;
		}

		public uint ReadUInt(int bitCount)
		{
			uint value = 0;
			for(int i = 0 ; i < bitCount ; i++)
			{
				var v = Read() ? 1U : 0U;
				value += (v << i);
			}
			return value;
		}

		public int Count
		{
			get
			{
				return array.Length;
			}
		}

		public BitArray ToBitArray()
		{
			BitArray result = new BitArray(array.Length);
			for(int i = 0 ; i < array.Length ; i++)
				result.Set(i, array.Get(i));
			return result;
		}

		public BitWriter ToWriter()
		{
			var writer = new BitWriter();
			writer.Write(array);
			return writer;
		}
	}
	class BitWriter
	{
		List<bool> values = new List<bool>();
		public int Count
		{
			get
			{
				return values.Count;
			}
		}
		public void Write(bool value)
		{
			values.Insert(Position, value);
			_Position++;
		}

		internal void Write(byte[] bytes)
		{
			Write(bytes, bytes.Length * 8);
		}

		public void Write(byte[] bytes, int bitCount)
		{
			bytes = SwapEndianBytes(bytes);
			BitArray array = new BitArray(bytes);
			values.InsertRange(Position, array.OfType<bool>().Take(bitCount));
			_Position += bitCount;
		}

		public byte[] ToBytes()
		{
			var array = ToBitArray();
			var bytes = ToByteArray(array);
			bytes = SwapEndianBytes(bytes);
			return bytes;
		}

		//BitArray.CopyTo do not exist in portable lib
		static byte[] ToByteArray(BitArray bits)
		{
			int arrayLength = bits.Length / 8;
			if(bits.Length % 8 != 0)
				arrayLength++;
			byte[] array = new byte[arrayLength];

			for(int i = 0 ; i < bits.Length ; i++)
			{
				int b = i / 8;
				int offset = i % 8;
				array[b] |= bits.Get(i) ? (byte)(1 << offset) : (byte)0;
			}
			return array;
		}


		public void Write(int[] indices)
		{
			Write(indices, indices.Length * 11);
		}
		public void Write(int[] indices, int bitCount)
		{
			foreach(var i in indices)
			{
				for(int p = 0 ; p < 11 ; p++)
				{
					if(bitCount <= 0)
						return;
					var v = (i & (1 << (10 - p))) != 0;
					Write(v);
					bitCount--;
				}
			}
		}

		public BitArray ToBitArray()
		{
			return new BitArray(values.ToArray());
		}

		public int[] ToIntegers()
		{
			return
				values
				.Select((v, i) => new
				{
					Group = i / 11,
					Value = v ? 1 << (10 - (i % 11)) : 0
				})
				.GroupBy(_ => _.Group, _ => _.Value)
				.Select(g => g.Sum())
				.ToArray();
		}


		static byte[] SwapEndianBytes(byte[] bytes)
		{
			byte[] output = new byte[bytes.Length];

			int index = 0;

			foreach(byte b in bytes)
			{
				byte[] ba = { b };
				BitArray bits = new BitArray(ba);

				int newByte = 0;
				if(bits.Get(7))
					newByte++;
				if(bits.Get(6))
					newByte += 2;
				if(bits.Get(5))
					newByte += 4;
				if(bits.Get(4))
					newByte += 8;
				if(bits.Get(3))
					newByte += 16;
				if(bits.Get(2))
					newByte += 32;
				if(bits.Get(1))
					newByte += 64;
				if(bits.Get(0))
					newByte += 128;

				output[index] = Convert.ToByte(newByte);

				index++;
			}

			//I love lamp
			return output;
		}



		public void Write(uint value, int bitCount)
		{
			for(int i = 0 ; i < bitCount ; i++)
			{
				Write((value & 1) == 1);
				value = value >> 1;
			}
		}

		int _Position;
		public int Position
		{
			get
			{
				return _Position;
			}
			set
			{
				_Position = value;
			}
		}

		internal void Write(BitReader reader, int bitCount)
		{
			for(int i = 0 ; i < bitCount ; i++)
			{
				Write(reader.Read());
			}
		}

		public void Write(BitArray bitArray)
		{
			foreach(bool bit in bitArray)
			{
				Write(bit);
			}
		}

		public void Write(BitReader reader)
		{
			Write(reader, reader.Count - reader.Position);
		}

		public BitReader ToReader()
		{
			return new BitReader(ToBitArray());
		}
	}

}
