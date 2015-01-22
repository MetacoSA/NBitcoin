using System;
using System.Globalization;
using System.Linq;

namespace NBitcoin
{
	public class KeyPath
	{
		public KeyPath(string path)
		{
			_indexes =
				path
				.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(Parse)
				.ToArray();

		}

		private static uint Parse(string i)
		{
			bool hardened = i.EndsWith("'");
			var nonhardened = hardened ? i.Substring(0, i.Length - 1) : i;
			var index = uint.Parse(nonhardened);
			return hardened ? index | 0x80000000u : index;
		}

		public KeyPath(params uint[] indexes)
		{
			_indexes = indexes;
		}

	    readonly uint[] _indexes;

		public uint this[int index]
		{
			get
			{
				return _indexes[index];
			}
		}

		public uint[] Indexes
		{
			get
			{
				return _indexes;
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

		public KeyPath Derive(uint index)
		{
			return Derive(new KeyPath(index));
		}

		public KeyPath Derive(KeyPath derivation)
		{
			return new KeyPath(
				_indexes
				.Concat(derivation._indexes)
				.ToArray());
		}

		public override bool Equals(object obj)
		{
			KeyPath item = obj as KeyPath;
			return item != null && ToString().Equals(item.ToString());
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

		string _path;

		public override string ToString()
		{
		    return _path ?? (_path = string.Join("/", _indexes.Select(ToString).ToArray()));
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
				if(_indexes.Length == 0)
					throw new InvalidOperationException("No indice found in this KeyPath");
				return (_indexes[_indexes.Length - 1] & 0x80000000u) != 0;
			}
		}
	}
}
