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
	public class Scope : IDisposable
	{
		Action close;
		public Scope(Action open, Action close)
		{
			this.close = close;
			open();
		}

		#region IDisposable Members

		public void Dispose()
		{
			close();
		}

		#endregion

		public static IDisposable Nothing
		{
			get
			{
				return new Scope(() =>
				{
				}, () =>
				{
				});
			}
		}
	}
	public class BitcoinStream
	{
		int _MaxArraySize = Int32.MaxValue;
		public int MaxArraySize
		{
			get
			{
				return _MaxArraySize;
			}
			set
			{
				_MaxArraySize = value;
			}
		}

		//ReadWrite<T>(ref T data)
		static MethodInfo _ReadWriteTyped;
		static BitcoinStream()
		{
			_ReadWriteTyped =
				typeof(BitcoinStream)
				.GetMethods()
				.Where(m => m.Name == "ReadWrite")
				.Where(m => m.IsGenericMethodDefinition)
				.Where(m => m.GetParameters().Length == 1)
				.Where(m => m.GetParameters().Any(p => p.ParameterType.IsByRef))
				.First();

		}

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

		public void ReadWrite(Type type, ref object obj)
		{
			try
			{
				var parameters = new object[] { obj };
				_ReadWriteTyped.MakeGenericMethod(type).Invoke(this, parameters);
				obj = parameters[0];
			}
			catch(TargetInvocationException ex)
			{
				throw ex.InnerException;
			}
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
			else if(data is bool)
			{
				byte d = (bool)(object)data ? (byte)1 : (byte)0;
				ReadWriteByte(ref d);
				data = (T)(object)(d == 0 ? false : true);
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
			try
			{
				var elementType = data.GetType().GetElementType();
				var parameters = new object[] { data };
				this.GetType().GetMethod("ReadWriteArray", BindingFlags.NonPublic | BindingFlags.Instance)
					.MakeGenericMethod(elementType)
					.Invoke(this, parameters);
				data = (Array)parameters[0];
			}
			catch(TargetInvocationException ex)
			{
				throw ex.InnerException;
			}
		}

		private void ReadWriteArray<T>(ref T[] data)
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

		private void ReadWriteListUntyped<T>(ref T data)
		{
			try
			{
				var elementType = data.GetType().GetGenericArguments()[0];
				var parameters = new object[] { data };

				this.GetType().GetMethod("ReadWriteList", BindingFlags.NonPublic | BindingFlags.Instance)
					.MakeGenericMethod(elementType)
					.Invoke(this, parameters);

				data = (T)(object)parameters[0];
			}
			catch(TargetInvocationException ex)
			{
				throw ex.InnerException;
			}
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
				int readen = 0;
				while(data.Length != readen)
				{
					ReadCancellationToken.ThrowIfCancellationRequested();
					NetworkStream netStream = Inner as NetworkStream;
					if(netStream != null) //NetworkStream blocks if no data is coming, polling IsDataAvailable give a chance to cancel the call
					{
						if(!netStream.DataAvailable)
						{
							ReadCancellationToken.WaitHandle.WaitOne(500);
							continue;
						}
					}
					var justRead = Inner.Read(data, readen, data.Length - readen);
					if(justRead == -1)
						throw new EndOfStreamException("No more byte to read");
					readen += justRead;
				}
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

		public IDisposable BigEndianScope()
		{
			var old = IsBigEndian;
			return new Scope(() =>
			{
				IsBigEndian = true;
			},
			() =>
			{
				IsBigEndian = old;
			});
		}

		ProtocolVersion _ProtocolVersion = ProtocolVersion.PROTOCOL_VERSION;
		public ProtocolVersion ProtocolVersion
		{
			get
			{
				return _ProtocolVersion;
			}
			set
			{
				_ProtocolVersion = value;
			}
		}


		public IDisposable ProtocolVersionScope(ProtocolVersion version)
		{
			var old = ProtocolVersion;
			return new Scope(() =>
			{
				ProtocolVersion = version;
			},
			() =>
			{
				ProtocolVersion = old;
			});
		}

		public void CopyParameters(BitcoinStream stream)
		{
			ProtocolVersion = stream.ProtocolVersion;
			IsBigEndian = stream.IsBigEndian;
			MaxArraySize = stream.MaxArraySize;
		}

		private bool _NetworkFormat;
		public bool NetworkFormat
		{
			get
			{
				return _NetworkFormat;
			}
		}

		public IDisposable NetworkFormatScope(bool value)
		{
			var old = _NetworkFormat;
			return new Scope(() =>
			{
				_NetworkFormat = value;
			}, () =>
			{
				_NetworkFormat = old;
			});
		}

		public System.Threading.CancellationToken ReadCancellationToken
		{
			get;
			set;
		}
	}
}
