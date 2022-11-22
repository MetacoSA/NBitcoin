using NBitcoin.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if !NOSOCKET
using System.Net.Sockets;
#endif
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;



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
		int _MaxArraySize = 1024 * 1024 * 4;
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
		internal static MethodInfo _ReadWriteTyped;
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

#if !NOSOCKET
		private readonly bool _IsNetworkStream;
#endif
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
#if !NOSOCKET
			_IsNetworkStream = inner is NetworkStream;
#endif
			_Inner = inner;
		}

		public BitcoinStream(byte[] bytes)
			: this(new MemoryStream(bytes), false)
		{
		}
		public BitcoinStream(byte[] bytes, int offset, int length)
			: this(new MemoryStream(bytes, offset, length), false)
		{
		}

		public Script ReadWrite(Script data)
		{
			if (Serializing)
			{
				var bytes = data == null ? Script.Empty.ToBytes(true) : data.ToBytes(true);
				ReadWriteAsVarString(ref bytes);
				return data;
			}
			else
			{
				byte[] bytes = null;
				VarString.StaticRead(this, ref bytes);
				return Script.FromBytesUnsafe(bytes);
			}
		}

		public void ReadWrite(ref Script script)
		{
			if (Serializing)
				ReadWrite(script);
			else
				script = ReadWrite(script);
		}

		public T ReadWrite<T>(T data) where T : IBitcoinSerializable
		{
			ReadWrite<T>(ref data);
			return data;
		}


		ConsensusFactory _ConsensusFactory = Consensus.Main.ConsensusFactory;

		/// <summary>
		/// Set the format to use when serializing and deserializing consensus related types.
		/// </summary>
		public ConsensusFactory ConsensusFactory
		{
			get
			{
				return _ConsensusFactory;
			}
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				_ConsensusFactory = value;
			}
		}

		public void ReadWriteAsVarString(ref byte[] bytes)
		{
			if (Serializing)
			{
				VarString.StaticWrite(this, bytes);
			}
			else
			{
				VarString.StaticRead(this, ref bytes);
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
			catch (TargetInvocationException ex)
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
			if (obj == null)
			{
				if (!ConsensusFactory.TryCreateNew<T>(out obj))
					obj = Activator.CreateInstance<T>();
			}
			obj.ReadWrite(this);
			if (!Serializing)
				data = obj;
		}

		public void ReadWrite<T>(ref List<T> list) where T : IBitcoinSerializable
		{
			int listLen = 0;
			if (Serializing)
			{
				var len = list == null ? 0 : (ulong)list.Count;
				if (len > (uint)MaxArraySize)
					throw new ArgumentOutOfRangeException("Array size too big");
				VarInt.StaticWrite(this, len);
				if (len == 0)
					return;
				listLen = (int)len;
				foreach (var obj in list)
				{
					ReadWrite(obj);
				}
			}
			else
			{
				var len = VarInt.StaticRead(this);
				if (len > (uint)MaxArraySize)
					throw new ArgumentOutOfRangeException("Array size too big");
				listLen = (int)len;
				list = new List<T>(listLen);
				for (int i = 0; i < listLen; i++)
				{
					T obj = default;
					ReadWrite(ref obj);
					list.Add(obj);
				}
			}
		}
		public void ReadWrite(ref TxInList list)
		{
			int listLen = 0;
			if (Serializing)
			{
				var len = list == null ? 0 : (ulong)list.Count;
				if (len > (uint)MaxArraySize)
					throw new ArgumentOutOfRangeException("Array size too big");
				VarInt.StaticWrite(this, len);
				if (len == 0)
					return;
				listLen = (int)len;
				foreach (var obj in list)
				{
					ReadWrite(obj);
				}
			}
			else
			{
				var len = VarInt.StaticRead(this);
				if (len > (uint)MaxArraySize)
					throw new ArgumentOutOfRangeException("Array size too big");
				listLen = (int)len;
				list = new TxInList(listLen);
				for (int i = 0; i < listLen; i++)
				{
					TxIn obj = default;
					ReadWrite(ref obj);
					list.Add(obj);
				}
			}
		}
		public void ReadWrite(ref TxOutList list)
		{
			int listLen = 0;
			if (Serializing)
			{
				var len = list == null ? 0 : (ulong)list.Count;
				if (len > (uint)MaxArraySize)
					throw new ArgumentOutOfRangeException("Array size too big");
				VarInt.StaticWrite(this, len);
				if (len == 0)
					return;
				listLen = (int)len;
				foreach (var obj in list)
				{
					ReadWrite(obj);
				}
			}
			else
			{
				var len = VarInt.StaticRead(this);
				if (len > (uint)MaxArraySize)
					throw new ArgumentOutOfRangeException("Array size too big");
				listLen = (int)len;
				list = new TxOutList(listLen);
				for (int i = 0; i < listLen; i++)
				{
					TxOut obj = default;
					ReadWrite(ref obj);
					list.Add(obj);
				}
			}
		}

		public void ReadWrite(ref byte[] arr)
		{
			ReadWriteBytes(ref arr);
		}
#if HAS_SPAN
		public void ReadWrite(ref Span<byte> arr)
		{
			ReadWriteBytes(arr);
		}
#endif
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

#if HAS_SPAN
		private void ReadWriteNumber(ref ulong value, int size)
		{
			if(_IsNetworkStream && ReadCancellationToken.CanBeCanceled)
			{
				ReadWriteNumberInefficient(ref value, size);
				return;
			}
			Span<byte> bytes = stackalloc byte[size];
			for(int i = 0; i < size; i++)
			{
				bytes[i] = (byte)(value >> i * 8);
			}
			if(IsBigEndian)
				bytes.Reverse();
			ReadWriteBytes(bytes);
			if(IsBigEndian)
				bytes.Reverse();
			ulong valueTemp = 0;
			for(int i = 0; i < bytes.Length; i++)
			{
				var v = (ulong)bytes[i];
				valueTemp += v << (i * 8);
			}
			value = valueTemp;
		}
#endif

#if !HAS_SPAN
		private void ReadWriteNumber(ref ulong value, int size)
#else
		private void ReadWriteNumberInefficient(ref ulong value, int size)
#endif
		{
			var bytes = new byte[size];

			for (int i = 0; i < size; i++)
			{
				bytes[i] = (byte)(value >> i * 8);
			}
			if (IsBigEndian)
				Array.Reverse(bytes);
			ReadWriteBytes(ref bytes);
			if (IsBigEndian)
				Array.Reverse(bytes);
			ulong valueTemp = 0;
			for (int i = 0; i < bytes.Length; i++)
			{
				var v = (ulong)bytes[i];
				valueTemp += v << (i * 8);
			}
			value = valueTemp;
		}

#if HAS_SPAN
		internal void ReadWriteBytes(ref byte[] data, int offset = 0, int count = -1)
		{
			if(data == null)
				throw new ArgumentNullException(nameof(data));
			if(data.Length == 0)
				return;
			count = count == -1 ? data.Length : count;
			if(count == 0)
				return;
			ReadWriteBytes(new Span<byte>(data, offset, count));
		}

		private void ReadWriteBytes(Span<byte> data)
		{
			if(Serializing)
			{
				Inner.Write(data);
				Counter.AddWritten(data.Length);
			}
			else
			{
				var readen = Inner.ReadEx(data, ReadCancellationToken);
				if(readen == 0)
					throw new EndOfStreamException("No more byte to read");
				Counter.AddReaden(readen);

			}
		}
#else
		internal void ReadWriteBytes(ref byte[] data, int offset = 0, int count = -1)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			if (data.Length == 0)
				return;

			count = count == -1 ? data.Length : count;

			if (count == 0)
				return;

			if (Serializing)
			{
				Inner.Write(data, offset, count);
				Counter.AddWritten(count);
			}
			else
			{
				var readen = Inner.ReadEx(data, offset, count, ReadCancellationToken);
				if (readen == 0)
					throw new EndOfStreamException("No more byte to read");
				Counter.AddReaden(readen);

			}
		}
#endif
		private PerformanceCounter _Counter;
		public PerformanceCounter Counter
		{
			get
			{
				if (_Counter == null)
					_Counter = new PerformanceCounter();
				return _Counter;
			}
		}
		private void ReadWriteByte(ref byte data)
		{
			if (Serializing)
			{
				Inner.WriteByte(data);
				Counter.AddWritten(1);
			}
			else
			{
				var readen = Inner.ReadByte();
				if (readen == -1)
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

		uint? _ProtocolVersion = null;
		public uint? ProtocolVersion
		{
			get
			{
				return _ProtocolVersion;
			}
			set
			{
				_ProtocolVersion = value;
				_ProtocolCapabilities = null;
			}
		}

		ProtocolCapabilities _ProtocolCapabilities;
		public ProtocolCapabilities ProtocolCapabilities
		{
			get
			{
				var capabilities = _ProtocolCapabilities;
				if (capabilities == null)
				{
					capabilities = ProtocolVersion == null ? ProtocolCapabilities.CreateSupportAll() : ConsensusFactory.GetProtocolCapabilities(ProtocolVersion.Value);
					_ProtocolCapabilities = capabilities;
				}
				return capabilities;
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

		public IDisposable ProtocolVersionScope(uint? version)
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

		public void CopyParameters(BitcoinStream from)
		{
			if (from == null)
				throw new ArgumentNullException(nameof(from));
			ProtocolVersion = from.ProtocolVersion;
			ConsensusFactory = from.ConsensusFactory;
			_ProtocolCapabilities = from._ProtocolCapabilities;
			IsBigEndian = from.IsBigEndian;
			MaxArraySize = from.MaxArraySize;
			Type = from.Type;
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

		public IDisposable ConsensusFactoryScope(ConsensusFactory consensusFactory)
		{
			var old = ConsensusFactory;
			return new Scope(() =>
			{
				ConsensusFactory = consensusFactory;
			}, () =>
			{
				ConsensusFactory = old;
			});
		}

		public System.Threading.CancellationToken ReadCancellationToken
		{
			get;
			set;
		}

		public void ReadWriteAsVarInt(ref uint val)
		{
			if (Serializing)
				VarInt.StaticWrite(this, val);
			else
				val = (uint)Math.Min(uint.MaxValue, VarInt.StaticRead(this));
		}
		public void ReadWriteAsVarInt(ref ulong val)
		{
			if (Serializing)
				VarInt.StaticWrite(this, val);
			else
				val = VarInt.StaticRead(this);
		}

		public void ReadWriteAsCompactVarInt(ref uint val)
		{
			var value = new CompactVarInt(val, sizeof(uint));
			ReadWrite(ref value);
			if (!Serializing)
				val = (uint)value.ToLong();
		}
		public void ReadWriteAsCompactVarInt(ref ulong val)
		{
			var value = new CompactVarInt(val, sizeof(ulong));
			ReadWrite(ref value);
			if (!Serializing)
				val = value.ToLong();
		}
	}
}
