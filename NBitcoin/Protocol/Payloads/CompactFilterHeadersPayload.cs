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

namespace NBitcoin.Protocol
{
	[Payload("cfheaders")]
	public class CompactFilterHeadersPayload: Payload, IBitcoinSerializable
	{
		private byte _filterType = 0;
		private uint256 _stopHash = new uint256();
		private uint256 _previousFilterHeader = new uint256();
		private VarInt _filterHashesLength = new VarInt(0);
		private uint256[] _filterHashes = { };

		public CompactFilterHeadersPayload(FilterType filterType, uint256 stopHash, uint256 previousFilterHeader, byte[] filterHashes)
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
			_filterHashes = GetHashes(filterHashes);
		}

		public CompactFilterHeadersPayload() { }

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
			stream.ReadWrite(ref _filterHashesLength);
			var _tempfilterHeaders = new byte[_filterHashesLength.ToLong() * 32];

			stream.ReadWrite(ref _tempfilterHeaders);

			_filterHashes = GetHashes(_tempfilterHeaders);
		}

		private uint256[] GetHashes(byte[] filterHeaders)
		{
			int bytesToTake = 32;
			int bytesTaken = 0;
			List<uint256> temp = new List<uint256>();

			List<BlockHeader> blockHeaders = new List<BlockHeader>();

			for (; bytesTaken < filterHeaders.Length; bytesTaken += bytesToTake)
			{
				var subArray = filterHeaders.SafeSubarray(bytesTaken, bytesToTake);

				temp.Add(new uint256(subArray));
			}

			return temp.ToArray();
		}


		public uint256 StopHash => _stopHash;
		public uint256 PreviousFilterHeader => _previousFilterHeader;
		public uint256[] FilterHashes => _filterHashes;



		public override string ToString()
		{
			return $"Cheaders type: {this.FilterType}| # of headers: {this._filterHashesLength.ToLong()}| Stop Hash: {this.StopHash}| Previous Filter Header Hash: {this.PreviousFilterHeader}";
		}
	}
}
#endif
