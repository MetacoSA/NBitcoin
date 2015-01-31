
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NBitcoin.Protocol;

#if !PORTABLE
using System.Net.Sockets;
#endif

namespace NBitcoin
{
	public partial class BitcoinStream
	{
		
		private void ReadWriteArray<T>(ref T[] data) where T : IBitcoinSerializable
		{
			if(data == null && Serializing)
				throw new ArgumentNullException("Impossible to serialize a null array");
			var length = new VarInt(data == null ? 0 : (ulong)data.Length);
			ReadWrite(ref length);

			if(length.ToLong() > (uint)MaxArraySize)
				throw new ArgumentOutOfRangeException("Array size not big");
			if(!Serializing)
				data = new T[length.ToLong()];
			for(int i = 0 ; i < data.Length ; i++)
			{
				T obj = data[i];
				ReadWrite(ref obj);
				data[i] = obj;
			}
		}

		
		private void ReadWriteArray(ref ulong[] data)
		{
			if(data == null && Serializing)
				throw new ArgumentNullException("Impossible to serialize a null array");
			var length = new VarInt(data == null ? 0 : (ulong)data.Length);
			ReadWrite(ref length);

			if(length.ToLong() > (uint)MaxArraySize)
				throw new ArgumentOutOfRangeException("Array size not big");
			if(!Serializing)
				data = new ulong[length.ToLong()];
			for(int i = 0 ; i < data.Length ; i++)
			{
				ulong obj = data[i];
				ReadWrite(ref obj);
				data[i] = obj;
			}
		}

		
		private void ReadWriteArray(ref ushort[] data)
		{
			if(data == null && Serializing)
				throw new ArgumentNullException("Impossible to serialize a null array");
			var length = new VarInt(data == null ? 0 : (ulong)data.Length);
			ReadWrite(ref length);

			if(length.ToLong() > (uint)MaxArraySize)
				throw new ArgumentOutOfRangeException("Array size not big");
			if(!Serializing)
				data = new ushort[length.ToLong()];
			for(int i = 0 ; i < data.Length ; i++)
			{
				ushort obj = data[i];
				ReadWrite(ref obj);
				data[i] = obj;
			}
		}

		
		private void ReadWriteArray(ref uint[] data)
		{
			if(data == null && Serializing)
				throw new ArgumentNullException("Impossible to serialize a null array");
			var length = new VarInt(data == null ? 0 : (ulong)data.Length);
			ReadWrite(ref length);

			if(length.ToLong() > (uint)MaxArraySize)
				throw new ArgumentOutOfRangeException("Array size not big");
			if(!Serializing)
				data = new uint[length.ToLong()];
			for(int i = 0 ; i < data.Length ; i++)
			{
				uint obj = data[i];
				ReadWrite(ref obj);
				data[i] = obj;
			}
		}

		
		private void ReadWriteArray(ref byte[] data)
		{
			if(data == null && Serializing)
				throw new ArgumentNullException("Impossible to serialize a null array");
			var length = new VarInt(data == null ? 0 : (ulong)data.Length);
			ReadWrite(ref length);

			if(length.ToLong() > (uint)MaxArraySize)
				throw new ArgumentOutOfRangeException("Array size not big");
			if(!Serializing)
				data = new byte[length.ToLong()];
			for(int i = 0 ; i < data.Length ; i++)
			{
				byte obj = data[i];
				ReadWrite(ref obj);
				data[i] = obj;
			}
		}

		
		private void ReadWriteArray(ref long[] data)
		{
			if(data == null && Serializing)
				throw new ArgumentNullException("Impossible to serialize a null array");
			var length = new VarInt(data == null ? 0 : (ulong)data.Length);
			ReadWrite(ref length);

			if(length.ToLong() > (uint)MaxArraySize)
				throw new ArgumentOutOfRangeException("Array size not big");
			if(!Serializing)
				data = new long[length.ToLong()];
			for(int i = 0 ; i < data.Length ; i++)
			{
				long obj = data[i];
				ReadWrite(ref obj);
				data[i] = obj;
			}
		}

		
		private void ReadWriteArray(ref short[] data)
		{
			if(data == null && Serializing)
				throw new ArgumentNullException("Impossible to serialize a null array");
			var length = new VarInt(data == null ? 0 : (ulong)data.Length);
			ReadWrite(ref length);

			if(length.ToLong() > (uint)MaxArraySize)
				throw new ArgumentOutOfRangeException("Array size not big");
			if(!Serializing)
				data = new short[length.ToLong()];
			for(int i = 0 ; i < data.Length ; i++)
			{
				short obj = data[i];
				ReadWrite(ref obj);
				data[i] = obj;
			}
		}

		
		private void ReadWriteArray(ref int[] data)
		{
			if(data == null && Serializing)
				throw new ArgumentNullException("Impossible to serialize a null array");
			var length = new VarInt(data == null ? 0 : (ulong)data.Length);
			ReadWrite(ref length);

			if(length.ToLong() > (uint)MaxArraySize)
				throw new ArgumentOutOfRangeException("Array size not big");
			if(!Serializing)
				data = new int[length.ToLong()];
			for(int i = 0 ; i < data.Length ; i++)
			{
				int obj = data[i];
				ReadWrite(ref obj);
				data[i] = obj;
			}
		}

		

		
		private void ReadWriteList(ref List<ulong> data)
		{
			var dataArray = data == null ? null : data.ToArray();
			if(Serializing && dataArray == null)
			{
				dataArray = new ulong[0];
			}
			ReadWriteArray(ref dataArray);
			if(!Serializing)
			{
				if(data == null)
					data = new List<ulong>();
				else
					data.Clear();
				data.AddRange(dataArray);
			}
		}

		
		private void ReadWriteList(ref List<ushort> data)
		{
			var dataArray = data == null ? null : data.ToArray();
			if(Serializing && dataArray == null)
			{
				dataArray = new ushort[0];
			}
			ReadWriteArray(ref dataArray);
			if(!Serializing)
			{
				if(data == null)
					data = new List<ushort>();
				else
					data.Clear();
				data.AddRange(dataArray);
			}
		}

		
		private void ReadWriteList(ref List<uint> data)
		{
			var dataArray = data == null ? null : data.ToArray();
			if(Serializing && dataArray == null)
			{
				dataArray = new uint[0];
			}
			ReadWriteArray(ref dataArray);
			if(!Serializing)
			{
				if(data == null)
					data = new List<uint>();
				else
					data.Clear();
				data.AddRange(dataArray);
			}
		}

		
		private void ReadWriteList(ref List<byte> data)
		{
			var dataArray = data == null ? null : data.ToArray();
			if(Serializing && dataArray == null)
			{
				dataArray = new byte[0];
			}
			ReadWriteArray(ref dataArray);
			if(!Serializing)
			{
				if(data == null)
					data = new List<byte>();
				else
					data.Clear();
				data.AddRange(dataArray);
			}
		}

		
		private void ReadWriteList(ref List<long> data)
		{
			var dataArray = data == null ? null : data.ToArray();
			if(Serializing && dataArray == null)
			{
				dataArray = new long[0];
			}
			ReadWriteArray(ref dataArray);
			if(!Serializing)
			{
				if(data == null)
					data = new List<long>();
				else
					data.Clear();
				data.AddRange(dataArray);
			}
		}

		
		private void ReadWriteList(ref List<short> data)
		{
			var dataArray = data == null ? null : data.ToArray();
			if(Serializing && dataArray == null)
			{
				dataArray = new short[0];
			}
			ReadWriteArray(ref dataArray);
			if(!Serializing)
			{
				if(data == null)
					data = new List<short>();
				else
					data.Clear();
				data.AddRange(dataArray);
			}
		}

		
		private void ReadWriteList(ref List<int> data)
		{
			var dataArray = data == null ? null : data.ToArray();
			if(Serializing && dataArray == null)
			{
				dataArray = new int[0];
			}
			ReadWriteArray(ref dataArray);
			if(!Serializing)
			{
				if(data == null)
					data = new List<int>();
				else
					data.Clear();
				data.AddRange(dataArray);
			}
		}

		
		
		public void ReadWrite(ref ulong[] data)
		{
			ReadWriteArray(ref data);
		}

		
		public void ReadWrite(ref ushort[] data)
		{
			ReadWriteArray(ref data);
		}

		
		public void ReadWrite(ref uint[] data)
		{
			ReadWriteArray(ref data);
		}

		
		public void ReadWrite(ref long[] data)
		{
			ReadWriteArray(ref data);
		}

		
		public void ReadWrite(ref short[] data)
		{
			ReadWriteArray(ref data);
		}

		
		public void ReadWrite(ref int[] data)
		{
			ReadWriteArray(ref data);
		}

		

			
		public void ReadWrite(ref ulong data)
		{
			ulong l = (ulong)data;
			ReadWriteNumber(ref l, sizeof(ulong));
			if(!Serializing)
				data = (ulong)l;
		}

		public ulong ReadWrite(ulong data)
		{
			ReadWrite(ref data);
			return data;
		}

		
		public void ReadWrite(ref ushort data)
		{
			ulong l = (ulong)data;
			ReadWriteNumber(ref l, sizeof(ushort));
			if(!Serializing)
				data = (ushort)l;
		}

		public ushort ReadWrite(ushort data)
		{
			ReadWrite(ref data);
			return data;
		}

		
		public void ReadWrite(ref uint data)
		{
			ulong l = (ulong)data;
			ReadWriteNumber(ref l, sizeof(uint));
			if(!Serializing)
				data = (uint)l;
		}

		public uint ReadWrite(uint data)
		{
			ReadWrite(ref data);
			return data;
		}

		

			
		public void ReadWrite(ref long data)
		{
			long l = (long)data;
			ReadWriteNumber(ref l, sizeof(long));
			if(!Serializing)
				data = (long)l;
		}

		public long ReadWrite(long data)
		{
			ReadWrite(ref data);
			return data;
		}

		
		public void ReadWrite(ref short data)
		{
			long l = (long)data;
			ReadWriteNumber(ref l, sizeof(short));
			if(!Serializing)
				data = (short)l;
		}

		public short ReadWrite(short data)
		{
			ReadWrite(ref data);
			return data;
		}

		
		public void ReadWrite(ref int data)
		{
			long l = (long)data;
			ReadWriteNumber(ref l, sizeof(int));
			if(!Serializing)
				data = (int)l;
		}

		public int ReadWrite(int data)
		{
			ReadWrite(ref data);
			return data;
		}

			}
}