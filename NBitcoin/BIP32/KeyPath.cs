#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
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
			return new KeyPath(path);
		}

		/// <summary>
		/// Try Parse a KeyPath
		/// </summary>
		/// <param name="path">The KeyPath formated like 10/0/2'/3</param>
		/// <param name="keyPath">The successfully parsed Key path</param>
		/// <returns>True if the string is parsed successfully; otherwise false</returns>
		public static bool TryParse(string path, out KeyPath? keyPath)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));
			bool isValid = true;
			int count = 0;
			var indices =
				path
				.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
				.Where(p => p != "m")
				.Select(p =>
				{
					isValid &= TryParseCore(p, out var i);
					count++;
					if (count > 255)
						isValid = false;
					return i;
				})
				.Where(_ => isValid)
				.ToArray();
			if (!isValid)
			{
				keyPath = null;
				return false;
			}
			keyPath = new KeyPath(indices);
			return true;
		}

		public KeyPath(string path)
		{
			int count = 0;
			_Indexes =
				path
				.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
				.Where(p => p != "m")
				.Select(p =>
				{
					if (!TryParseCore(p, out var i))
						throw new FormatException("KeyPath uncorrectly formatted");
					count++;
					if (count > 255)
						throw new FormatException("KeyPath uncorrectly formatted");
					return i;
				})
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
			for (int i = 0; i < depth; i++)
			{
				result[i] = Utils.ToUInt32(data, i * 4, true);
			}

			return new KeyPath(result);
		}

		public byte[] ToBytes() =>
			Indexes.Count() == 0 ? new byte[0] : Indexes.Select(i => Utils.ToBytes(i, true)).Aggregate((a, b) => a.Concat(b)).ToArray();

		private static bool TryParseCore(string i, out uint index)
		{
			if (i.Length == 0)
			{
				index = 0;
				return false;
			}
			bool hardened = i[i.Length - 1] == '\'' || i[i.Length - 1] == 'h';
			var nonhardened = hardened ? i.Substring(0, i.Length - 1) : i;
			if (!uint.TryParse(nonhardened, out index))
				return false;

			// when parsing, number equals or greater than 0x80000000 (= 2147483648) should not be allowed.
			if (index >= 0x80000000u)
			{
				index = 0;
				return false;
			}
			if (hardened)
			{
				index = index | 0x80000000u;
				return true;
			}
			else
			{
				return true;
			}
		}

		static KeyPath _Empty = new KeyPath(new uint[0]);

		public static KeyPath Empty
		{
			get
			{
				return _Empty;
			}
		}

		public KeyPath(params uint[] indexes)
		{
			if (indexes.Length > 255)
				throw new ArgumentException(paramName: nameof(indexes), message: "A KeyPath should have at most 255 indices");
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

		public int Length
		{
			get
			{
				return _Indexes.Length;
			}
		}

		public KeyPath Derive(int index, bool hardened)
		{
			if (index < 0)
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

		public KeyPath? Parent
		{
			get
			{
				if (_Indexes.Length == 0)
					return null;
				return new KeyPath(_Indexes.Take(_Indexes.Length - 1).ToArray());
			}
		}

		public KeyPath Increment()
		{
			if (_Indexes.Length == 0)
				throw new InvalidOperationException("Cannot increment an empty keypath");
			var indices = _Indexes.ToArray();
			indices[indices.Length - 1]++;
			return new KeyPath(indices);
		}

		public override bool Equals(object? obj)
		{
			if (obj is KeyPath k)
				return StructuralComparisons.StructuralEqualityComparer.Equals(_Indexes, k._Indexes);
			return false;
		}
		public static bool operator ==(KeyPath? a, KeyPath? b)
		{
			if (a is KeyPath && b is KeyPath)
				return StructuralComparisons.StructuralEqualityComparer.Equals(a._Indexes, b._Indexes);
			return a is null && b is null;
		}

		public static KeyPath? operator +(KeyPath? a, KeyPath? b)
		{
			if (a is KeyPath && b is KeyPath)
				return a.Derive(b);
			if (a is null && !(b is null))
				return b;
			if (b is null && !(a is null))
				return a;
			return null;
		}

		public static bool operator !=(KeyPath a, KeyPath b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return ToString().GetHashCode();
		}

		string? _Path;
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

		/// <summary>
		/// True if the last index in the path is hardened
		/// </summary>
		public bool IsHardened
		{
			get
			{
				if (_Indexes.Length == 0)
					throw new InvalidOperationException("No index found in this KeyPath");
				return (_Indexes[_Indexes.Length - 1] & 0x80000000u) != 0;
			}
		}

		/// <summary>
		/// Returns the longest non-hardened keypath to the leaf.
		/// For example, if the keypath is "49'/0'/0'/1/23", then the address key path is "1/23"
		/// </summary>
		/// <returns>Return the address key path</returns>
		public KeyPath GetAddressKeyPath()
		{
			List<uint> indexes = new List<uint>();
			for (int i = Indexes.Length - 1; i >= 0; i--)
			{
				if (Indexes[i] >= 0x80000000U)
					break;
				indexes.Insert(0, Indexes[i]);
			}
			return new KeyPath(indexes.ToArray());
		}

		/// <summary>
		/// Returns the longest hardened keypath from the root.
		/// For example, if the keypath is "49'/0'/0'/1/23", then the account key path is "49'/0'/0'"
		/// </summary>
		/// <returns>Return the account key path</returns>
		public KeyPath GetAccountKeyPath()
		{
			List<uint> indexes = new List<uint>();
			for (int i = 0; i < Indexes.Length; i++)
			{
				if (Indexes[i] < 0x80000000U)
					break;
				indexes.Add(Indexes[i]);
			}
			return new KeyPath(indexes.ToArray());
		}

		/// <summary>
		/// True if at least one index in the path is hardened
		/// </summary>
		public bool IsHardenedPath
		{
			get
			{
				return _Indexes.Any(i => (i & 0x80000000u) != 0);
			}
		}

		public RootedKeyPath ToRootedKeyPath(IHDKey masterKey)
		{
			if (masterKey == null)
				throw new ArgumentNullException(nameof(masterKey));
			return ToRootedKeyPath(masterKey.GetPublicKey().GetHDFingerPrint());
		}

		public RootedKeyPath ToRootedKeyPath(HDFingerprint masterFingerprint)
		{
			return new RootedKeyPath(masterFingerprint, this);
		}
	}
}
#nullable disable
