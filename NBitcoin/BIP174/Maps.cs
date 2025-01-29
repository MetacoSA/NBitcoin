#nullable enable
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin;

class Maps : List<Map>
{
	public static Maps Load(BitcoinStream data)
	{
		if (data.Serializing)
			throw new InvalidOperationException("This method is for deserialization only");
		Maps maps = new Maps();
		while (data.Inner.CanRead && data.Inner.Position < data.Inner.Length)
		{
			var map = maps.NewMap();
			while (data.Inner.Position != data.Inner.Length)
			{
				var key = Array.Empty<byte>();
				var value = Array.Empty<byte>();
				//peek the next byte
				var next = data.Inner.ReadByte();
				if (next == PSBTConstants.PSBT_SEPARATOR)
					break;

				data.Inner.Position--;
				try
				{
					data.ReadWriteAsVarString(ref key);
					data.ReadWriteAsVarString(ref value);
				}
				catch (EndOfStreamException e)
				{
					throw new FormatException("Malformed PSBT", e);
				}
				if (!map.TryAdd(key, value))
				{
					throw new FormatException("Duplicate key in PSBT");
				}
			}
		}
		return maps;
	}
	public Maps()
	{

	}

	public void ThrowIfInvalidKeysLeft()
	{
		foreach (var m in this)
			m.ThrowIfInvalidKeysLeft();
	}

	public Map NewMap()
	{
		Map map = new Map();
		this.Add(map);
		return map;
	}

	public Map Global => this[0];

	public void ToBytes(Stream stream)
	{
		foreach (var map in this)
		{
			map.ToBytes(stream);
			stream.WriteByte(PSBTConstants.PSBT_SEPARATOR);
		}
	}
}
public class Map : System.Collections.Generic.SortedDictionary<byte[], byte[]>
{
	public Map() : base(BytesComparer.Instance)
	{

	}

	public void Add<T>(byte[] key, T val)
	{
		if (typeof(T) == typeof(byte[]))
		{
			base.Add(key, (byte[])(object)val!);
		}
		else if (typeof(T) == typeof(uint))
		{
			base.Add(key, Utils.ToBytes((uint)(object)val!, true));
		}
		else if (typeof(T) == typeof(int))
		{
			base.Add(key, Utils.ToBytes((uint)(int)(object)val!, true));
		}
		else if (typeof(T) == typeof(ulong))
		{
			base.Add(key, Utils.ToBytes((ulong)(object)val!, true));
		}
		else if (typeof(T) == typeof(long))
		{
			base.Add(key, Utils.ToBytes((ulong)(long)(object)val!, true));
		}
		else if (typeof(T) == typeof(VarInt))
		{
			base.Add(key, ((VarInt)(object)val!).ToBytes());
		}
	}

	public IEnumerable<KeyValuePair<byte[], T>> RemoveAll<T>(byte prefixKey) => RemoveAll<T>([prefixKey]);
	public IEnumerable<KeyValuePair<byte[],T>> RemoveAll<T>(byte[] prefixKey)
	{
		var keys = this.Keys.Where(k => StartWith(prefixKey, k)).ToList();
		foreach (var k in keys)
		{
			if(TryRemove<T>(k, out var v))
				yield return new KeyValuePair<byte[], T>(k, v);
		}
	}

	private static bool StartWith(byte[] prefix, byte[] data)
	{
		if (prefix.Length > data.Length)
			return false;

		for (int i = 0; i < prefix.Length; i++)
		{
			if (data[i] != prefix[i])
				return false;
		}
		return true;
	}

	public void ThrowIfInvalidKeysLeft()
	{
		var readen = new HashSet<byte>(singleByteKeys);
		foreach (var kv in this)
		{
			if (readen.Contains(kv.Key[0]))
				throw new FormatException("Invalid PSBT, unexpected key " + Encoders.Hex.EncodeData(kv.Key));
		}
	}
	List<byte> singleByteKeys = new();
	public bool TryRemove<T>(byte key, [MaybeNullWhen(false)] out T value)
	{
		singleByteKeys.Add(key);
		return TryRemove<T>([key], out value);
	}
	bool TryRemove<T>(byte[] key, [MaybeNullWhen(false)] out T value)
	{
		value = default;
		object? val = null;
		if (typeof(T) == typeof(byte[]))
		{
			if (!TryGetValue(key, out var b)) return false;
			Remove(key);
			value = (T?)(object)b;
			return value is not null;
		}

		if (!this.TryRemove<byte[]>(key, out byte[]? bytes))
			return false;
		var stream = new BitcoinStream(bytes);

		if (typeof(T) == typeof(int))
		{
			int v = 0;
			stream.ReadWrite(ref v);
			val = v;
		}
		else if (typeof(T) == typeof(uint))
		{
			uint v = 0;
			stream.ReadWrite(ref v);
			val = v;
		}
		else if (typeof(T) == typeof(byte))
		{
			byte v = bytes[0];
			val = v;
		}
		else if (typeof(T) == typeof(VarInt))
		{
			ulong v = 0;
			stream.ReadWriteAsVarInt(ref v);
			val = new VarInt(v);
		}
		else if (typeof(T) == typeof(long))
		{
			long v = 0;
			stream.ReadWrite(ref v);
			val = v;
		}
		else if (typeof(T) == typeof(ulong))
		{
			ulong v = 0;
			stream.ReadWrite(ref v);
			val = v;
		}
		value = (T?)val;
		return value is not null;
	}

	public void ToBytes(Stream stream)
	{
		var bs = new BitcoinStream(stream, true);
		foreach (var kv in this)
		{
			var k = kv.Key;
			var v = kv.Value;
			bs.ReadWriteAsVarString(ref k);
			bs.ReadWriteAsVarString(ref v);
		}
	}
	public byte[] ToBytes()
	{
		MemoryStream ms = new MemoryStream();
		ToBytes(ms);
		return ms.ToArrayEfficient();
	}
}
