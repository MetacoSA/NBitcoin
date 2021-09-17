
using NBitcoin.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public partial class BitcoinStream
	{
		private void ReadWriteArray<T>(ref T[] data) where T : IBitcoinSerializable
		{
			if (data == null && Serializing)
				throw new ArgumentNullException("Impossible to serialize a null array");

			if (Serializing)
			{
				var len = data == null ? 0 : (ulong)data.Length;
				if (len > (uint)MaxArraySize)
					throw new ArgumentOutOfRangeException("Array size too big");
				VarInt.StaticWrite(this, len);
				if (len == 0)
					return;
			}
			else
			{
				var len = VarInt.StaticRead(this);
				if (len > (uint)MaxArraySize)
					throw new ArgumentOutOfRangeException("Array size too big");
				data = new T[len];
			}
			for (int i = 0; i < data.Length; i++)
			{
				T obj = data[i];
				ReadWrite(ref obj);
				data[i] = obj;
			}
		}


		private void ReadWriteArray(ref ulong[] data)
		{
			if (data == null && Serializing)
				throw new ArgumentNullException("Impossible to serialize a null array");
			if (Serializing)
			{
				var len = data == null ? 0 : (ulong)data.Length;
				if (len > (uint)MaxArraySize)
					throw new ArgumentOutOfRangeException("Array size too big");
				VarInt.StaticWrite(this, len);
				if (len == 0)
					return;
			}
			else
			{
				var len = VarInt.StaticRead(this);
				if (len > (uint)MaxArraySize)
					throw new ArgumentOutOfRangeException("Array size too big");
				data = new ulong[len];
			}
			for (int i = 0; i < data.Length; i++)
			{
				ulong obj = data[i];
				ReadWrite(ref obj);
				data[i] = obj;
			}
		}


		private void ReadWriteArray(ref ushort[] data)
		{
			if (data == null && Serializing)
				throw new ArgumentNullException("Impossible to serialize a null array");
			if (Serializing)
			{
				var len = data == null ? 0 : (ulong)data.Length;
				if (len > (uint)MaxArraySize)
					throw new ArgumentOutOfRangeException("Array size too big");
				VarInt.StaticWrite(this, len);
				if (len == 0)
					return;
			}
			else
			{
				var len = VarInt.StaticRead(this);
				if (len > (uint)MaxArraySize)
					throw new ArgumentOutOfRangeException("Array size too big");
				data = new ushort[len];
			}
			for (int i = 0; i < data.Length; i++)
			{
				ushort obj = data[i];
				ReadWrite(ref obj);
				data[i] = obj;
			}
		}


		private void ReadWriteArray(ref uint[] data)
		{
			if (data == null && Serializing)
				throw new ArgumentNullException("Impossible to serialize a null array");
			if (Serializing)
			{
				var len = data == null ? 0 : (ulong)data.Length;
				if (len > (uint)MaxArraySize)
					throw new ArgumentOutOfRangeException("Array size too big");
				VarInt.StaticWrite(this, len);
				if (len == 0)
					return;
			}
			else
			{
				var len = VarInt.StaticRead(this);
				if (len > (uint)MaxArraySize)
					throw new ArgumentOutOfRangeException("Array size too big");
				data = new uint[len];
			}
			for (int i = 0; i < data.Length; i++)
			{
				uint obj = data[i];
				ReadWrite(ref obj);
				data[i] = obj;
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

		uint256.MutableUint256 _MutableUint256 = new uint256.MutableUint256(uint256.Zero);
		public void ReadWrite(ref uint256 value)
		{
			value = value ?? uint256.Zero;
			_MutableUint256.Value = value;
			_MutableUint256.ReadWrite(this);
			value = _MutableUint256.Value;
		}

		public void ReadWrite(uint256 value)
		{
			value = value ?? uint256.Zero;
			_MutableUint256.Value = value;
			_MutableUint256.ReadWrite(this);
			value = _MutableUint256.Value;
		}

		public void ReadWrite(ref List<uint256> value)
		{
			if (Serializing)
			{
				var list = value == null ? null : value.Select(v => v.AsBitcoinSerializable()).ToList();
				this.ReadWrite(ref list);
			}
			else
			{
				List<uint256.MutableUint256> list = null;
				this.ReadWrite(ref list);
				value = list.Select(l => l.Value).ToList();
			}
		}
		uint160.MutableUint160 _MutableUint160 = new uint160.MutableUint160(uint160.Zero);
		public void ReadWrite(ref uint160 value)
		{
			value = value ?? uint160.Zero;
			_MutableUint160.Value = value;
			this.ReadWrite(ref _MutableUint160);
			value = _MutableUint160.Value;
		}

		public void ReadWrite(uint160 value)
		{
			value = value ?? uint160.Zero;
			_MutableUint160.Value = value;
			this.ReadWrite(ref _MutableUint160);
			value = _MutableUint160.Value;
		}

		public void ReadWrite(ref List<uint160> value)
		{
			if (Serializing)
			{
				var list = value == null ? null : value.Select(v => v.AsBitcoinSerializable()).ToList();
				this.ReadWrite(ref list);
			}
			else
			{
				List<uint160.MutableUint160> list = null;
				this.ReadWrite(ref list);
				value = list.Select(l => l.Value).ToList();
			}
		}


		public void ReadWrite(ref ulong data)
		{
			ulong l = (ulong)data;
			ReadWriteNumber(ref l, sizeof(ulong));
			if (!Serializing)
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
			if (!Serializing)
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
			if (!Serializing)
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
			if (!Serializing)
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
			if (!Serializing)
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
			if (!Serializing)
				data = (int)l;
		}

		public int ReadWrite(int data)
		{
			ReadWrite(ref data);
			return data;
		}

	}
}
