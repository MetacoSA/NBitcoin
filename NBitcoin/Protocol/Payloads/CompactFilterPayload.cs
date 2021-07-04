using System;
using System.Collections.Generic;

namespace NBitcoin.Protocol
{
	/// <summary>
	/// Represents the p2p message payload used for sharing a block's compact filter.
	/// </summary>
	[Payload("cfilter")]
	public class CompactFilterPayload : Payload
	{
		private byte _FilterType = (byte)FilterType.Basic;
		private byte[] _FilterBytes;
		private uint256 _BlockHash = new uint256();

		/// <summary>
		/// Gets the Filter type for which headers are requested.
		/// </summary>
		public FilterType FilterType
		{
			get => (FilterType)_FilterType;
			internal set => _FilterType = (byte)value;
		}

		/// <summary>
		/// Gets the serialized compact filter for this block.
		/// </summary>
		public byte[] FilterBytes => _FilterBytes;

		/// <summary>
		/// Gets block hash of the Bitcoin block for which the filter is being returned.
		/// </summary>
		public uint256 BlockHash => _BlockHash;

		public CompactFilterPayload(FilterType filterType, uint256 blockhash, byte[] filterBytes)
		{
			if (filterType != FilterType.Basic)
				throw new ArgumentException($"'{filterType}' is not a valid value.", nameof(filterType));
			if (blockhash == null)
				throw new ArgumentNullException(nameof(blockhash));
			if (filterBytes == null)
				throw new ArgumentNullException(nameof(filterBytes));

			FilterType = filterType;
			_BlockHash = blockhash;
			_FilterBytes = filterBytes;
		}

		public CompactFilterPayload()
		{
		}

		public new void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _FilterType);
			stream.ReadWrite(ref _BlockHash);
			stream.ReadWrite(ref _FilterBytes);
		}
	}

	[Payload("cfcheckpt")]
	public class CompactFilterCheckPointPayload : Payload
	{
		protected byte _FilterType = (byte)FilterType.Basic;
		protected uint256 _StopHash = uint256.Zero;
		protected List<uint256> _FilterHeaders = new List<uint256>();

		public CompactFilterCheckPointPayload()
		{
		}

		public CompactFilterCheckPointPayload(FilterType filterType, uint256 stopHash, List<uint256> filterHeaders)
		{
			if (filterType != FilterType.Basic)
				throw new ArgumentException(nameof(filterType));
			if (stopHash == null)
				throw new ArgumentException(nameof(stopHash));
			if (filterHeaders == null)
				throw new ArgumentException(nameof(filterHeaders));

			FilterType = filterType;
			_StopHash = stopHash;
			_FilterHeaders = filterHeaders;
		}

		public FilterType FilterType
		{
			get => (FilterType)_FilterType;
			internal set => _FilterType = (byte)value;
		}

		public uint256 StopHash 
		{ 
			get => _StopHash; 
			set => _StopHash = value;
		}
		
		public List<uint256> FilterHeaders 
		{ 
			get => _FilterHeaders; 
			set => _FilterHeaders = value;
		}

		public override void ReadWriteCore(BitcoinStream stream)
		{
			stream.ReadWrite(ref _FilterType);
			stream.ReadWrite(ref _StopHash);
			stream.ReadWrite(ref _FilterHeaders);
		}
	}

	[Payload("cfheaders")]
	public class CompactFilterHeadersPayload: CompactFilterCheckPointPayload
	{
		private uint256 _PreviousFilterHeader = uint256.Zero;

		public override void ReadWriteCore(BitcoinStream stream)
		{
			stream.ReadWrite(ref _FilterType);
			stream.ReadWrite(ref _StopHash);
			stream.ReadWrite(ref _PreviousFilterHeader);
			stream.ReadWrite(ref _FilterHeaders);
		}

		public uint256 PreviousFilterHeader 
		{ 
			get => _PreviousFilterHeader; 
			set => _PreviousFilterHeader = value;
		}
	}
}
