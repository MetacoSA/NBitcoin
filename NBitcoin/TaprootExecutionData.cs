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
			InputIndex = inputIndex;
		}
		public TaprootExecutionData(int inputIndex, uint256 tapleaf)
		{
			InputIndex = inputIndex;
			HashVersion = HashVersion.Tapscript;
			TapleafHash = tapleaf;
		}
		public int InputIndex { get; set; }
		public byte[] Annex { get; set; }

		SigHash _SigHash = SigHash.Default;
		public SigHash SigHash
		{
			get
			{
				return _SigHash;
			}
			set
			{
				if (!((byte)value <= 0x03 || ((byte)value >= 0x81 && (byte)value <= 0x83)))
					throw new ArgumentException("Invalid SigHash", nameof(value));
				_SigHash = value;
			}
		}
		public HashVersion HashVersion { get; } = HashVersion.Taproot;
		public uint256 TapleafHash { get; set; }
		public uint CodeseparatorPosition { get; set; } = 0xffffffff;
	}
}
