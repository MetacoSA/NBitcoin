#if !NOSOCKET
using NBitcoin.Crypto;
using NBitcoin.Protocol.Behaviors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.Protocol.Payloads
{
	[Payload("cfheaders")]
	public class CompactFilterHeadersPayload: Payload
	{
		private byte _filterType;
		private byte[] _stopHash;
		private byte[] _previousFilterHeader;
		private byte[] _filterHashes;

		public CompactFilterHeadersPayload(FilterType filterType, byte[] stopHash, byte[] previousFilterHeader, byte[] filterHashes)
		{
			if (filterType != FilterType.Basic)
				throw new ArgumentException(nameof(filterType));
			if (stopHash == null)
				throw new ArgumentException(nameof(stopHash));
			if (previousFilterHeader == null)
				throw new ArgumentException(nameof(previousFilterHeader));
			if (filterHashes == null)
				throw new ArgumentException(nameof(filterHashes));


			FilterType = filterType;
			_stopHash = stopHash;
			_previousFilterHeader = previousFilterHeader;
			_filterHashes = filterHashes;
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
			stream.ReadWrite(ref _previousFilterHeader);
			stream.ReadWrite(ref _filterHashes);
		}


		public byte[] StopHash => _stopHash;
		public byte[] PreviousFilterHeader => _previousFilterHeader;
		public byte[] FilterHashes => _filterHashes;
	}
}
#endif
