using System;
using System.Globalization;
using System.Linq;

namespace NBitcoin
{

	/// <summary>
	/// Represent a path in the hierarchy of HD keys (BIP32)
	/// </summary>
	public class KeyPath
	{
		public KeyPath()
		{
			_Indexes = new uint[0];
		}

		/// <summary>
		/// Parse a KeyPath
		/// </summary>
		/// <param name="path">The KeyPath formated like 10/0/2'/3</param>
		/// <returns></returns>
		public static KeyPath Parse(string path)
		{
			var parts = path
				.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
				.Where(p => p != "m")
				.Select(ParseCore)
				.ToArray();
			return new KeyPath(parts);
		}

		public KeyPath(string path)
		{
			_Indexes =
				path
				.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
				.Where(p => p != "m")
				.Select(ParseCore)
				.ToArray();
		}

		public static KeyPath FromBytes(byte[] data)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			if (data.Length % 4 != 0)
				throw new ArgumentOutOfRangeException("data length is not suited for KeyPath");
			var depth = data.Length / 4;
			uint[] result = new uint[depth];
			for (int i = 0;  i < depth; i++)
			{
				result[i] = Utils.ToUInt32(data, i * 4, true);
			}

			return new KeyPath(result);
		}

		public byte[] ToBytes() =>
			Indexes.Count() == 0 ? new byte[0] : Indexes.Select(i => Utils.ToBytes(i, true)).Aggregate((a, b) => a.Concat(b)).ToArray();

		private static uint ParseCore(string i)
		{
			bool hardened = i.EndsWith("'");
			var nonhardened = hardened ? i.Substring(0, i.Length - 1) : i;
			var index = uint.Parse(nonhardened);
			return hardened ? index | 0x80000000u : index;
		}

		public KeyPath(params uint[] indexes)
		{
			_Indexes = indexes;
		}

		readonly uint[] _Indexes;
		public uint this[int index]
		{
			get
			{
				return _Indexes[index];
			}
		}

		public uint[] Indexes
		{
			get
			{
				return _Indexes.ToArray();
			}
		}

		public KeyPath Derive(int index, bool hardened)
		{
			if(index < 0)
				throw new ArgumentOutOfRangeException("index", "the index can't be negative");
			uint realIndex = (uint)index;
			realIndex = hardened ? realIndex | 0x80000000u : realIndex;
			return Derive(new KeyPath(realIndex));
		}

		public KeyPath Derive(string path)
		{
			return Derive(new KeyPath(path));
		}

		public KeyPath Derive(uint index)
		{
			return Derive(new KeyPath(index));
		}

		public KeyPath Derive(KeyPath derivation)
		{
			return new KeyPath(
				_Indexes
				.Concat(derivation._Indexes)
				.ToArray());
		}

		public KeyPath Parent
		{
			get
			{
				if(_Indexes.Length == 0)
					return null;
				return new KeyPath(_Indexes.Take(_Indexes.Length - 1).ToArray());
			}
		}

		public KeyPath Increment()
		{
			if(_Indexes.Length == 0)
				return null;
			var indices = _Indexes.ToArray();
			indices[indices.Length - 1]++;
			return new KeyPath(indices);
		}

		public override bool Equals(object obj)
		{
			KeyPath item = obj as KeyPath;
			if(item == null)
				return false;
			return ToString().Equals(item.ToString());
		}
		public static bool operator ==(KeyPath a, KeyPath b)
		{
			if(ReferenceEquals(a, b))
				return true;
			if(((object)a == null) || ((object)b == null))
				return false;
			return a.ToString() == b.ToString();
		}

		public static bool operator !=(KeyPath a, KeyPath b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return ToString().GetHashCode();
		}

		string _Path;
		public override string ToString()
		{
			return _Path ?? (_Path = string.Join("/", _Indexes.Select(ToString).ToArray()));
		}

		private static string ToString(uint i)
		{
			var hardened = (i & 0x80000000u) != 0;
			var nonhardened = (i & ~0x80000000u);
			return hardened ? nonhardened + "'" : nonhardened.ToString(CultureInfo.InvariantCulture);
		}

		public bool IsHardened
		{
			get
			{
				if(_Indexes.Length == 0)
					throw new InvalidOperationException("No index found in this KeyPath");
				return (_Indexes[_Indexes.Length - 1] & 0x80000000u) != 0;
			}
		}
	}
}
