#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class CompactSignature
	{
		public CompactSignature(int recoveryId, byte[] sig64)
		{
			if (sig64 is null)
				throw new ArgumentNullException(nameof(sig64));
			if (sig64.Length is not 64)
				throw new ArgumentException("sig64 should be 64 bytes", nameof(sig64));
			RecoveryId = recoveryId;
			Signature = sig64;
		}

		/// <summary>
		/// 
		/// </summary>
		public int RecoveryId { get; }

		/// <summary>
		/// The signature of 64 bytes
		/// </summary>
		public byte[] Signature { get; }

		public PubKey RecoverPubKey(uint256 hash)
		{
			return PubKey.RecoverCompact(hash, this);
		}
	}
}
