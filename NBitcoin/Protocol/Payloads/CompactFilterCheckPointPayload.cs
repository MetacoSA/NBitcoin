using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NBitcoin.Protocol
{

	[Payload("cfcheckpt")]
	public class CompactFilterCheckPointPayload : Payload, IBitcoinSerializable
	{
		private byte _filterType = 0;
		private uint256 _stopHash = new uint256();
		private uint256[] _filterHeaders = { };
		private VarInt _FilterHeadersLength = new VarInt(0);
		public CompactFilterCheckPointPayload()
		{

		}

		public CompactFilterCheckPointPayload(FilterType filterType, uint256 stopHash, uint filtersHeaderLength, byte[] filterHeaders)
		{
			if (filterType != FilterType.Basic /*&& filterType != FilterType.Extended*/) //Extended filters removed
				throw new ArgumentException(nameof(filterType));
			if (stopHash == null)
				throw new ArgumentException(nameof(stopHash));
			if (filterHeaders == null)
				throw new ArgumentException(nameof(filterHeaders));

			FilterType = filterType;
			_stopHash = stopHash;
			_filterHeaders = GetHashes(filterHeaders);
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

		public FilterType FilterType
		{
			get => (FilterType)_filterType;
			internal set => _filterType = (byte)value;
		}
		public uint256 StopHash { get => _stopHash; set => _stopHash = value; }
		public uint256[] FilterHeaders { get => _filterHeaders; set => _filterHeaders = value; }
		public VarInt FilterHeadersLength { get => _FilterHeadersLength; set => _FilterHeadersLength = value; }

		public void ReadWrite(BitcoinStream stream)
		{
			var length = stream.Inner.Length;

			stream.ReadWrite(ref _filterType);
			stream.ReadWrite(ref _stopHash);
			stream.ReadWrite(ref _FilterHeadersLength);
			var _tempfilterHeaders = new byte[_FilterHeadersLength.ToLong() * 32];

			stream.ReadWrite(ref _tempfilterHeaders);

			_filterHeaders = GetHashes(_tempfilterHeaders);
		}


		public override string ToString()
		{
			return $"cfcheckpt - filter type: {this._filterType}| Stop Hash {this.StopHash}| # of checkpoints: {this._FilterHeadersLength.ToLong()}";
		}
	}
}
