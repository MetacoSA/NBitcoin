using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class BitcoinStream
	{
		private readonly Stream _Inner;
		public Stream Inner
		{
			get
			{
				return _Inner;
			}
		}

		private readonly bool _Serializing;
		public bool Serializing
		{
			get
			{
				return _Serializing;
			}
		}
		public BitcoinStream(Stream inner, bool serializing)
		{
			_Serializing = serializing;
			_Inner = inner;
		}

		public BitcoinStream(byte[] bytes)
			: this(new MemoryStream(bytes), false)
		{
		}

		public T ReadWrite<T>(T data)
		{
			ReadWrite<T>(ref data);
			return data;
		}

		public void ReadWriteAsVarString(ref byte[] bytes)
		{
			VarString str = new VarString(bytes);
			ReadWrite(ref str);
			bytes = str.GetString();
		}
		public void ReadWrite<T>(ref T data)
		{
			if(typeof(IBitcoinSerializable).IsAssignableFrom(typeof(T)))
			{
				if(data == null)
					data = Activator.CreateInstance<T>();
				((IBitcoinSerializable)data).ReadWrite(this);
			}
			else if(data is byte)
			{
				var d = (byte)(object)data;
				ReadWriteByte(ref d);
				data = (T)(object)d;
			}
			else if(typeof(byte[]).IsAssignableFrom(typeof(T)))
			{
				var d = (byte[])(object)data;
				ReadWriteBytes(ref d);
				data = (T)(object)d;
			}
			else if(typeof(Array).IsAssignableFrom(typeof(T)))
			{
				var d = (Array)(object)data;
				ReadWriteArrayUntyped(ref d);
				data = (T)(object)d;
			}
			else if(typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
			{
				ReadWriteListUntyped(ref data);
			}
			else if(IsUNumber<T>() || IsNumber<T>())
			{
				ReadWriteNumber(ref data);
			}
			else
				throw new NotSupportedException("Type not supported " + typeof(T).FullName + ", implement IBitcoinSerializable");
		}

		

		private void ReadWriteArrayUntyped(ref Array data)
		{
			var elementType = data.GetType().GetElementType();
			var parameters = new object[] { data };
			this.GetType().GetMethod("ReadWriteArray", BindingFlags.NonPublic | BindingFlags.Instance)
				.MakeGenericMethod(elementType)
				.Invoke(this, parameters);
			data = (Array)parameters[0];
		}

		private void ReadWriteArray<T>(ref T[] data)
		{
			if(data == null && Serializing)
				throw new ArgumentNullException("Impossible to serialize a null array");


			var length = new VarInt(data == null ? 0 : (ulong)data.Length);
			ReadWrite(ref length);

			if(!Serializing)
				data = new T[length.ToLong()];

			for(int i = 0 ; i < data.Length ; i++)
			{
				T obj = data[i];
				ReadWrite(ref obj);
				data[i] = obj;
			}
		}

		private void ReadWriteListUntyped<T>(ref T data)
		{
			var elementType = data.GetType().GetGenericArguments()[0];
			var parameters = new object[] { data };

			this.GetType().GetMethod("ReadWriteList", BindingFlags.NonPublic | BindingFlags.Instance)
				.MakeGenericMethod(elementType)
				.Invoke(this, parameters);

			data = (T)(object)parameters[0];
		}
		private void ReadWriteList<T>(ref List<T> data)
		{
			if(data == null && Serializing)
				throw new ArgumentNullException("Impossible to serialize a null list");

			var dataArray = data.ToArray();
			ReadWriteArray(ref dataArray);
			if(!Serializing)
				data = dataArray.ToList();
		}


		private void ReadWriteNumber<T>(ref T data)
		{
			int size = 0;
			if(data is ushort || data is short)
				size = 2;
			else if(data is uint || data is int)
				size = 4;
			else if(data is ulong || data is long)
				size = 8;
			else
				throw new NotSupportedException("Type not supported " + typeof(T).FullName);

			ulong value = 0;
			if(IsUNumber<T>())
			{
				value = (ulong)Convert.ChangeType(data, typeof(ulong));
			}
			else
			{
				value = (ulong)(long)Convert.ChangeType(data, typeof(long));
			}
			var bytes = new byte[size];

			for(int i = 0 ; i < size ; i++)
			{
				bytes[i] = (byte)(value >> i * 8);
			}
			if(IsBigEndian)
				Array.Reverse(bytes);
			ReadWriteBytes(ref bytes);
			if(IsBigEndian)
				Array.Reverse(bytes);
			ulong valueTemp = 0;
			for(int i = 0 ; i < bytes.Length ; i++)
			{
				var v = (ulong)bytes[i];
				valueTemp += v << (i * 8);
			}
			value = valueTemp;

			if(IsUNumber<T>())
			{
				data = (T)Convert.ChangeType(value, typeof(T));
			}
			else
			{
				data = (T)Convert.ChangeType((long)value, typeof(T));
			}
		}

		private void ReadWriteBytes(ref byte[] data)
		{
			if(Serializing)
			{
				Inner.Write(data, 0, data.Length);
			}
			else
			{
				Inner.Read(data, 0, data.Length);
			}
		}

		private void ReadWriteByte(ref byte data)
		{
			if(Serializing)
			{
				Inner.WriteByte(data);
			}
			else
				data = (byte)Inner.ReadByte();
		}


		static Type[] unumberTypes = new[] { typeof(byte), typeof(ushort), typeof(uint), typeof(ulong) };
		private bool IsUNumber<T>()
		{
			return unumberTypes.Contains(typeof(T));
		}
		static Type[] numberTypes = new[] { typeof(short), typeof(int), typeof(long) };
		private bool IsNumber<T>()
		{
			return numberTypes.Contains(typeof(T));
		}

		public bool IsBigEndian
		{
			get;
			set;
		}

		class EndianScope : IDisposable
		{
			bool old;
			BitcoinStream stream;
			public EndianScope(BitcoinStream stream, bool value)
			{
				this.stream = stream;
				old = stream.IsBigEndian;
				stream.IsBigEndian = value;
			}

			#region IDisposable Members

			public void Dispose()
			{
				stream.IsBigEndian = old;
			}

			#endregion
		}
		public IDisposable BigEndianScope()
		{
			return new EndianScope(this, true);
		}
	}
}
