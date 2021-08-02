#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class TaprootExecutionData
	{
		public TaprootExecutionData(int inputIndex)
		{
			if (inputIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(inputIndex));
			InputIndex = inputIndex;
		}
		public TaprootExecutionData(int inputIndex, uint256? tapleaf)
		{
			if (inputIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(inputIndex));
			InputIndex = inputIndex;
			HashVersion = tapleaf is null ? HashVersion.Taproot : HashVersion.Tapscript;
			TapleafHash = tapleaf;
		}
		public int InputIndex { get; set; }
		public uint256? AnnexHash { get; set; }

		TaprootSigHash _SigHash = TaprootSigHash.Default;
		public static bool IsValidSigHash(byte value)
		{
			return (value <= 0x03 || (value >= 0x81 && value <= 0x83));
		}
		public TaprootSigHash SigHash
		{
			get
			{
				return _SigHash;
			}
			set
			{
				if (!IsValidSigHash((byte)value))
					throw new ArgumentException("Invalid SigHash", nameof(value));
				_SigHash = value;
			}
		}
		public HashVersion HashVersion { get; } = HashVersion.Taproot;
		public uint256? TapleafHash { get; }
		public uint CodeseparatorPosition { get; set; } = 0xffffffff;
	}
}
