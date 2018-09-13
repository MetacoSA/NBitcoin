using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.Protocol.Payloads
{

	[Payload("cfcheckpt")]
	public class CompactFilterCheckPointPayload : Payload
	{
		private byte _filterType;
		private byte[] _stopHash;
		private byte[] _filterHeaders;

		public CompactFilterCheckPointPayload(FilterType filterType, byte[] stopHash, byte[] filterHeaders)
		{
			if (filterType != FilterType.Basic)
				throw new ArgumentException(nameof(filterType));
			if (stopHash == null)
				throw new ArgumentException(nameof(stopHash));
			if (filterHeaders == null)
				throw new ArgumentException(nameof(filterHeaders));

			FilterType = filterType;
			_stopHash = stopHash;
			_filterHeaders = filterHeaders;
		}

		public FilterType FilterType
		{
			get => (FilterType)_filterType;
			internal set => _filterType = (byte)value;
		}



		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _filterType);
			stream.ReadWrite(ref _stopHash);
			stream.ReadWrite(ref _filterHeaders);
		}
	}
}
