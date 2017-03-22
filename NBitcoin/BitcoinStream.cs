using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NBitcoin.Protocol;

namespace NBitcoin
{
	public enum SerializationType
	{
		Disk,
		Network,
		Hash
	}
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
		int _MaxArraySize = 1024 * 1024;
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
			_ReadWriteTyped = typeof(BitcoinStream)
			.GetTypeInfo()
			.DeclaredMethods
			.Where(m => m.Name == "ReadWrite")
			.Where(m => m.IsGenericMethodDefinition)
			.Where(m => m.GetParameters().Length == 1)
			.Where(m => m.GetParameters().Any(p => p.ParameterType.IsByRef && p.ParameterType.HasElementType && !p.ParameterType.GetElementType().IsArray))
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

		public Script ReadWrite(Script data)
		{
			if(Serializing)
			{
				var bytes = data == null ? Script.Empty.ToBytes(true) : data.ToBytes(true);
				ReadWriteAsVarString(ref bytes);
				return data;
			}
			else
			{
				var varString = new VarString();
				varString.ReadWrite(this);
				return Script.FromBytesUnsafe(varString.GetString(true));
			}
		}

		public void ReadWrite(ref Script script)
		{
			if(Serializing)
				ReadWrite(script);
			else
				script = ReadWrite(script);
		}

		public T ReadWrite<T>(T data) where T : IBitcoinSerializable
		{
			ReadWrite<T>(ref data);
			return data;
		}

		public void ReadWriteAsVarString(ref byte[] bytes)
		{
			if(Serializing)
			{
				VarString str = new VarString(bytes);
				str.ReadWrite(this);
			}
			else
			{
				VarString str = new VarString();
				str.ReadWrite(this);
				bytes = str.GetString(true);
			}
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

		public void ReadWriteStruct<T>(ref T data) where T : struct, IBitcoinSerializable
		{
			data.ReadWrite(this);
		}
		public void ReadWriteStruct<T>(T data) where T : struct, IBitcoinSerializable
		{
			data.ReadWrite(this);
		}

		public void ReadWrite<T>(ref T data) where T : IBitcoinSerializable
		{
			var obj = data;
			if(obj == null)
				obj = Activator.CreateInstance<T>();
			obj.ReadWrite(this);
			if(!Serializing)
				data = obj;
		}

		public void ReadWrite<T>(ref List<T> list) where T : IBitcoinSerializable, new()
		{
			ReadWriteList<List<T>, T>(ref list);
		}

		public void ReadWrite<TList, TItem>(ref TList list)
			where TList : List<TItem>, new()
			where TItem : IBitcoinSerializable, new()
		{
			ReadWriteList<TList, TItem>(ref list);
		}

		private void ReadWriteList<TList, TItem>(ref TList data)
			where TList : List<TItem>, new()
			where TItem : IBitcoinSerializable, new()
		{
			var dataArray = data == null ? null : data.ToArray();
			if(Serializing && dataArray == null)
			{
				dataArray = new TItem[0];
			}
			ReadWriteArray(ref dataArray);
			if(!Serializing)
			{
				if(data == null)
					data = new TList();
				else
					data.Clear();
				data.AddRange(dataArray);
			}
		}

		public void ReadWrite(ref byte[] arr)
		{
			ReadWriteBytes(ref arr);
		}
		public void ReadWrite(ref byte[] arr, int offset, int count)
		{
			ReadWriteBytes(ref arr, offset, count);
		}
		public void ReadWrite<T>(ref T[] arr) where T : IBitcoinSerializable, new()
		{
			ReadWriteArray<T>(ref arr);
		}

		private void ReadWriteNumber(ref long value, int size)
		{
			ulong uvalue = unchecked((ulong)value);
			ReadWriteNumber(ref uvalue, size);
			value = unchecked((long)uvalue);
		}

		private void ReadWriteNumber(ref ulong value, int size)
		{
			var bytes = new byte[size];

			for(int i = 0; i < size; i++)
			{
				bytes[i] = (byte)(value >> i * 8);
			}
			if(IsBigEndian)
				Array.Reverse(bytes);
			ReadWriteBytes(ref bytes);
			if(IsBigEndian)
				Array.Reverse(bytes);
			ulong valueTemp = 0;
			for(int i = 0; i < bytes.Length; i++)
			{
				var v = (ulong)bytes[i];
				valueTemp += v << (i * 8);
			}
			value = valueTemp;
		}

		private void ReadWriteBytes(ref byte[] data, int offset = 0, int count = -1)
		{
			if(data == null) throw new ArgumentNullException("data");

			if(data.Length == 0) return;

			count = count == -1 ? data.Length : count;

			if(count == 0) return;

			if(Serializing)
			{
				Inner.Write(data, offset, count);
				Counter.AddWritten(count);
			}
			else
			{
				var readen = Inner.ReadEx(data, offset, count, ReadCancellationToken);
				if(readen == 0)
					throw new EndOfStreamException("No more byte to read");
				Counter.AddReaden(readen);

			}
		}
		private PerformanceCounter _Counter;
		public PerformanceCounter Counter
		{
			get
			{
				if(_Counter == null)
					_Counter = new PerformanceCounter();
				return _Counter;
			}
		}
		private void ReadWriteByte(ref byte data)
		{
			if(Serializing)
			{
				Inner.WriteByte(data);
				Counter.AddWritten(1);
			}
			else
			{
				var readen = Inner.ReadByte();
				if(readen == -1)
					throw new EndOfStreamException("No more byte to read");
				data = (byte)readen;
				Counter.AddReaden(1);
			}
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

		TransactionOptions _TransactionSupportedOptions = TransactionOptions.All;
		public TransactionOptions TransactionOptions
		{
			get
			{
				return _TransactionSupportedOptions;
			}
			set
			{
				_TransactionSupportedOptions = value;
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
			if(stream == null)
				throw new ArgumentNullException("stream");
			ProtocolVersion = stream.ProtocolVersion;
			IsBigEndian = stream.IsBigEndian;
			MaxArraySize = stream.MaxArraySize;
			Type = stream.Type;
		}


		public SerializationType Type
		{
			get;
			set;
		}

		public IDisposable SerializationTypeScope(SerializationType value)
		{
			var old = Type;
			return new Scope(() =>
			{
				Type = value;
			}, () =>
			{
				Type = old;
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
