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
	public partial class BitcoinStream
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

		public T ReadWrite<T>(T data) where T : IBitcoinSerializable
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

		public void ReadWrite(ref byte data)
		{
			ReadWriteByte(ref data);
		}
		public byte ReadWrite(byte data)
		{
			ReadWrite(ref data);
			return data;
		}

		public void ReadWrite(ref bool data)
		{
			byte d = data ? (byte)1 : (byte)0;
			ReadWriteByte(ref d);
			data = (d == 0 ? false : true);
		}


		public void ReadWrite<T>(ref T data) where T : IBitcoinSerializable
		{
			if(data == null)
				data = Activator.CreateInstance<T>();
			((IBitcoinSerializable)data).ReadWrite(this);
		}

		public void ReadWrite<T>(ref List<T> list) where T : IBitcoinSerializable
		{
			ReadWriteList<T>(ref list);
		}
		public void ReadWrite(ref byte[] arr)
		{
			ReadWriteBytes(ref arr);
		}
		public void ReadWrite<T>(ref T[] arr) where T : IBitcoinSerializable
		{
			ReadWriteArray<T>(ref arr);
		}

		//private void ReadWriteList<T>(ref List<T> data)
		//{
		//	if(data == null && Serializing)
		//		throw new ArgumentNullException("Impossible to serialize a null list");

		//	var dataArray = data.ToArray();
		//	ReadWriteArray(ref dataArray);
		//	if(!Serializing)
		//		data = dataArray.ToList();
		//}


		private void ReadWriteNumber<T>(ref T data, int size, bool unsigned)
		{
			ulong value = 0;
			if(unsigned)
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

			if(unsigned)
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
				var readen = Inner.ReadEx(data, 0, data.Length, ReadCancellationToken);
				if(readen == -1)
					throw new EndOfStreamException("No more byte to read");

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


		public void ReadWrite(ref ushort data)
		{
			ReadWriteNumber(ref data, sizeof(ushort), true);
		}
		public ushort ReadWrite(ushort data)
		{
			ReadWrite(ref data);
			return data;
		}
		public void ReadWrite(ref uint data)
		{
			ReadWriteNumber(ref data, sizeof(uint), true);
		}
		public uint ReadWrite(uint data)
		{
			ReadWrite(ref data);
			return data;
		}
		public void ReadWrite(ref ulong data)
		{
			ReadWriteNumber(ref data, sizeof(ulong), true);
		}
		public ulong ReadWrite(ulong data)
		{
			ReadWrite(ref data);
			return data;
		}

		public short ReadWrite(short data)
		{
			ReadWrite(ref data);
			return data;
		}
		public void ReadWrite(ref short data)
		{
			ReadWriteNumber(ref data, sizeof(short), false);
		}
		public int ReadWrite(int data)
		{
			ReadWrite(ref data);
			return data;
		}
		public void ReadWrite(ref int data)
		{
			ReadWriteNumber(ref data, sizeof(int), false);
		}
		public long ReadWrite(long data)
		{
			ReadWrite(ref data);
			return data;
		}
		public void ReadWrite(ref long data)
		{
			ReadWriteNumber(ref data, sizeof(long), false);
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

		public void ReadWriteAsVarInt(ref uint val)
		{
			ulong vallong = val;
			ReadWriteAsVarInt(ref vallong);
			if(!Serializing)
				val = (uint)vallong;
		}
		public void ReadWriteAsVarInt(ref ulong val)
		{
			var value = new VarInt(val);
			ReadWrite(ref value);
			if(!Serializing)
				val = value.ToLong();
		}

		public void ReadWriteAsCompactVarInt(ref uint val)
		{
			var value = new CompactVarInt(val, sizeof(uint));
			ReadWrite(ref value);
			if(!Serializing)
				val = (uint)value.ToLong();
		}
		public void ReadWriteAsCompactVarInt(ref ulong val)
		{
			var value = new CompactVarInt(val, sizeof(ulong));
			ReadWrite(ref value);
			if(!Serializing)
				val = value.ToLong();
		}
	}
}
