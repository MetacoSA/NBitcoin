using System;

namespace NBitcoin.Protocol
{
	public enum FilterType : byte
	{
		Basic = (0x00),
	}

	/// <summary>
	/// Represents the p2p message payload used for requesting a range of compact filter/headers.
	/// </summary>
	public abstract class CompactFiltersQueryPayload : Payload
	{
		private byte _FilterType = (byte)FilterType.Basic;
		private uint _StartHeight = 0;
		private uint256 _StopHash = new uint256();

		/// <summary>
		/// Gets the Filter type for which headers are requested.
		/// </summary>
		public FilterType FilterType
		{
			get => (FilterType)_FilterType;
			internal set => _FilterType = (byte)value;
		}

		/// <summary>
		/// Gets the height of the first block in the requested range.
		/// </summary>
		public uint StartHeight => _StartHeight;
		
		/// <summary>
		/// Gets the hash of the last block in the requested range.
		/// </summary>
		public uint256 StopHash => _StopHash;
		
		protected CompactFiltersQueryPayload(FilterType filterType, uint startHeight, uint256 stopHash)
		{
			if (filterType != FilterType.Basic)
				throw new ArgumentException($"'{filterType}' is not a valid value.", nameof(filterType));
			if (stopHash == null)
				throw new ArgumentNullException(nameof(stopHash));

			FilterType = filterType;
			_StartHeight = startHeight;
			_StopHash = stopHash;
		}

		protected CompactFiltersQueryPayload() { }

		#region IBitcoinSerializable Members
		public override void ReadWriteCore(BitcoinStream stream)
		{
			stream.ReadWrite(ref _FilterType);
			stream.ReadWrite(ref _StartHeight);
			stream.ReadWrite(ref _StopHash);
		}
		#endregion
	}

	/// <summary>
	/// Represents the p2p message payload used for requesting a range of compact filter.
	/// </summary>
	[Payload("getcfilters")]
	public class GetCompactFiltersPayload : CompactFiltersQueryPayload
	{
		public GetCompactFiltersPayload(FilterType filterType, uint startHeight, uint256 stopHash)
			: base(filterType, startHeight, stopHash)
		{
		}

		public GetCompactFiltersPayload() { }
	}

	/// <summary>
	/// Represents the p2p message payload used for requesting a range of compact filter headers.
	/// </summary>
	[Payload("getcfheaders")]
	public class GetCompactFilterHeadersPayload : CompactFiltersQueryPayload
	{
		public GetCompactFilterHeadersPayload(FilterType filterType, uint startHeight, uint256 stopHash)
			: base(filterType, startHeight, stopHash)
		{
		}

		public GetCompactFilterHeadersPayload() { }
	}

	[Payload("getcfcheckpt")]
	public class GetCompactFilterCheckPointPayload : Payload
	{
		private byte _FilterType;
		private uint256 _StopHash;

		public GetCompactFilterCheckPointPayload(FilterType filterType, uint256 stopHash)
		{
			if (filterType != FilterType.Basic)
				throw new ArgumentException($"'{filterType}' is not a valid value.", nameof(filterType));
			if (stopHash == null)
				throw new ArgumentNullException(nameof(stopHash));

			FilterType = filterType;
			_StopHash = stopHash;
		}

		public GetCompactFilterCheckPointPayload() { }

		public override void ReadWriteCore(BitcoinStream stream)
		{
			stream.ReadWrite(ref _FilterType);
			stream.ReadWrite(ref _StopHash);
		}

		/// <summary>
		/// Gets the Filter type for which headers are requested.
		/// </summary>
		public FilterType FilterType
		{
			get => (FilterType)_FilterType;
			internal set => _FilterType = (byte)value;
		}

		/// <summary>
		/// Gets the hash of the last block in the requested range.
		/// </summary>
		public uint256 StopHash => _StopHash;
	}
}
