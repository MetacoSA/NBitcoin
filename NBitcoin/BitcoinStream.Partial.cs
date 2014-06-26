
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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

		

		
		private void ReadWriteList<T>(ref List<T> data) where T : IBitcoinSerializable
		{
			var dataArray = data.ToArray();
			ReadWriteArray(ref dataArray);
			if(!Serializing)
			data = dataArray.ToList();
		}

		
		private void ReadWriteList(ref List<ulong> data)
		{
			var dataArray = data.ToArray();
			ReadWriteArray(ref dataArray);
			if(!Serializing)
			data = dataArray.ToList();
		}

		
		private void ReadWriteList(ref List<ushort> data)
		{
			var dataArray = data.ToArray();
			ReadWriteArray(ref dataArray);
			if(!Serializing)
			data = dataArray.ToList();
		}

		
		private void ReadWriteList(ref List<uint> data)
		{
			var dataArray = data.ToArray();
			ReadWriteArray(ref dataArray);
			if(!Serializing)
			data = dataArray.ToList();
		}

		
		private void ReadWriteList(ref List<byte> data)
		{
			var dataArray = data.ToArray();
			ReadWriteArray(ref dataArray);
			if(!Serializing)
			data = dataArray.ToList();
		}

		
		private void ReadWriteList(ref List<long> data)
		{
			var dataArray = data.ToArray();
			ReadWriteArray(ref dataArray);
			if(!Serializing)
			data = dataArray.ToList();
		}

		
		private void ReadWriteList(ref List<short> data)
		{
			var dataArray = data.ToArray();
			ReadWriteArray(ref dataArray);
			if(!Serializing)
			data = dataArray.ToList();
		}

		
		private void ReadWriteList(ref List<int> data)
		{
			var dataArray = data.ToArray();
			ReadWriteArray(ref dataArray);
			if(!Serializing)
			data = dataArray.ToList();
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

			}
}