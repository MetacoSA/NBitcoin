using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin
{
	/// <summary>
	/// KeyPath with the fingerprint of the root it should derive from
	/// </summary>
	public class RootedKeyPath
	{
		public static RootedKeyPath Parse(string str)
		{
			if (!TryParse(str, out var result))
				throw new FormatException("Invalid Rooted KeyPath");
			return result;
		}

		public static bool TryParse(string str, out RootedKeyPath result)
		{
			if (str == null)
				throw new ArgumentNullException(nameof(str));
			result = null;
			var separator = str.IndexOf('/');
			if (separator == -1)
			{
				if (!HDFingerprint.TryParse(str, out var fp))
					return false;
				result = new RootedKeyPath(fp, KeyPath.Empty);
			}
			else
			{
				if (!HDFingerprint.TryParse(str.Substring(0, separator), out var fp))
					return false;
				if (!NBitcoin.KeyPath.TryParse(str.Substring(separator + 1), out var keyPath))
					return false;
				result = new RootedKeyPath(fp, keyPath);
			}
			return true;
		}
		public RootedKeyPath(HDFingerprint masterFingerprint, KeyPath keyPath)
		{
			if (keyPath == null)
				throw new ArgumentNullException(nameof(keyPath));
			_KeyPath = keyPath;
			_MasterFingerprint = masterFingerprint;
		}
		public RootedKeyPath(IHDKey masterKey, KeyPath keyPath)
		{
			if (masterKey == null)
				throw new ArgumentNullException(nameof(masterKey));
			if (keyPath == null)
				throw new ArgumentNullException(nameof(keyPath));
			_KeyPath = keyPath;
			_MasterFingerprint = masterKey.GetPublicKey().GetHDFingerPrint();
		}
		private readonly KeyPath _KeyPath;
		public KeyPath KeyPath
		{
			get
			{
				return _KeyPath;
			}
		}

		private readonly HDFingerprint _MasterFingerprint;
		public HDFingerprint MasterFingerprint
		{
			get
			{
				return _MasterFingerprint;
			}
		}

		public RootedKeyPath Derive(KeyPath keyPath)
		{
			if (keyPath == null)
				throw new ArgumentNullException(nameof(keyPath));
			return new RootedKeyPath(MasterFingerprint, KeyPath.Derive(keyPath));
		}
		public RootedKeyPath Derive(uint index)
		{
			return new RootedKeyPath(MasterFingerprint, KeyPath.Derive(index));
		}

		/// <summary>
		/// Returns the longest hardened keypath from the root.
		/// For example, if the keypath is "49'/0'/0'/1/23", then the account key path is "49'/0'/0'"
		/// </summary>
		/// <returns>Return the account key path</returns>
		public RootedKeyPath GetAccountKeyPath()
		{
			return new RootedKeyPath(MasterFingerprint, KeyPath.GetAccountKeyPath());
		}

		public override string ToString() => $"{MasterFingerprint}/{KeyPath}";

		/// <summary>
		/// Mostly works same with `ToString()`, but if the `KeyPath` is empty, it just returns master finger print
		/// without `/` in the suffix
		/// </summary>
		/// <returns></returns>
		public string ToStringWithEmptyKeyPathAware() => KeyPath == KeyPath.Empty ? MasterFingerprint.ToString() : ToString();
		public override bool Equals(object obj)
		{
			RootedKeyPath item = obj as RootedKeyPath;
			if (item == null)
				return false;
			return ToString().Equals(item.ToString());
		}
		public static bool operator ==(RootedKeyPath a, RootedKeyPath b)
		{
			if (System.Object.ReferenceEquals(a, b))
				return true;
			if (((object)a == null) || ((object)b == null))
				return false;
			return a.ToString() == b.ToString();
		}

		public static bool operator !=(RootedKeyPath a, RootedKeyPath b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return ToString().GetHashCode();
		}
	}
}
