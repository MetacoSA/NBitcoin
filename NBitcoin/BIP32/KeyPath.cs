using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class KeyPath
	{
		public KeyPath(string path)
		{
			_Indexes =
				path
				.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(c => uint.Parse(c))
				.ToArray();

		}

		public KeyPath(params uint[] indexes)
		{
			_Indexes = indexes;
		}
		uint[] _Indexes;
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
				return _Indexes;
			}
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

		public override bool Equals(object obj)
		{
			KeyPath item = obj as KeyPath;
			if(item == null)
				return false;
			return ToString().Equals(item.ToString());
		}
		public static bool operator ==(KeyPath a, KeyPath b)
		{
			if(System.Object.ReferenceEquals(a, b))
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
			if(_Path == null)
			{
				_Path = string.Join("/", _Indexes);
			}
			return _Path;
		}
	}
}
