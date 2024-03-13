#nullable enable
#if HAS_SPAN
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBitcoin.Crypto;

namespace NBitcoin
{
	public enum TapLeafVersion : byte
	{
		C0 = 0xC0,
	}
	public class TapScript
	{
		public Script Script { get; }
		public TapLeafVersion Version { get; }
		uint256? _LeafHash;
		public uint256 LeafHash => _LeafHash ??= ComputeLeafHash(Script, Version);

		internal static uint256 ComputeLeafHash(Script script, TapLeafVersion version)
		{
			var hash = new HashStream { SingleSHA256 = true };
			hash.InitializeTagged("TapLeaf");
			hash.WriteByte((byte)version);
			var bs = new BitcoinStream(hash, true);
			bs.ReadWrite(script);
			return hash.GetHash();
		}
		public TapScript(Script script, TapLeafVersion version)
		{
			if (script is null)
				throw new ArgumentNullException(nameof(script));
			this.Script = script;
			this.Version = version;
		}

		public TapScript(TapScript script)
		{
			if (script is null)
				throw new ArgumentNullException(nameof(script));
			Script = script.Script;
			Version = script.Version;
			_LeafHash = script._LeafHash;
		}

		[return: NotNullIfNotNull("script")]
		public static implicit operator Script?(TapScript? script)
		{
			if (script is null)
				return null;
			return script.Script;
		}
		public override string ToString()
		{
			return $"{(byte)Version:X2}: {Script}";
		}


		public override bool Equals(object? obj)
		{
			TapScript? item = obj as TapScript;
			if (item is null)
				return false;
			return LeafHash.Equals(item.LeafHash);
		}
		public static bool operator ==(TapScript a, TapScript b)
		{
			if (System.Object.ReferenceEquals(a, b))
				return true;
			if (a is null || b is null)
				return false;
			return a.LeafHash.Equals(b.LeafHash);
		}

		public static bool operator !=(TapScript a, TapScript b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return LeafHash.GetHashCode();
		}
	}
}
#endif
