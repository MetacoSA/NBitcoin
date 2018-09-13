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
	public enum FilterType : byte
	{
		Basic = (0x00),
		//Extended = (0x01) Decison to remove extended filter due to increased filter size when doing recursive parsing.
		//https://lists.linuxfoundation.org/pipermail/bitcoin-dev/2018-May/016037.html
	}

	/// <summary>
	/// Represents the p2p message payload used for requesting a range of compact filter/headers.
	/// </summary>
	public abstract class CompactFiltersQueryPayload : Payload
	{
		private byte _filterType;
		private uint _startHeight;
		private uint256 _stopHash;
		/// <summary>
		/// Gets the Filter type for which headers are requested  
		/// </summary>
		public FilterType FilterType
		{
			get => (FilterType)_filterType;
			internal set => _filterType = (byte)value;
		}
		/// <summary>
		/// Gets the height of the first block in the requested range
		/// </summary>
		public uint StartHeight => _startHeight;
		/// <summary>
		/// Gets the hash of the last block in the requested range 
		/// </summary>
		public uint256 StopHash => _stopHash;
		protected CompactFiltersQueryPayload(FilterType filterType, uint startHeight, uint256 stopHash)
		{
			if (filterType != FilterType.Basic /*&& filterType != FilterType.Extended*/) //Extended Filter removed
				throw new ArgumentException($"'{filterType}' is not a valid value. Try with Basic.", nameof(filterType));
			if (stopHash == null)
				throw new ArgumentNullException(nameof(stopHash));

			FilterType = filterType;
			_startHeight = startHeight;
			_stopHash = stopHash;
		}

		#region IBitcoinSerializable Members
		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _filterType);
			stream.ReadWrite(ref _startHeight);
			stream.ReadWrite(ref _stopHash);
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
	}

	[Payload("getcfcheckpt")]
	public class GetCompactFilterCheckPointPayload : Payload
	{
		private byte _filterType;
		private uint256 _stopHash;

		public GetCompactFilterCheckPointPayload(FilterType filterType, uint256 stopHash)
		{

		}


		public FilterType FilterType
		{
			get => (FilterType)_filterType;
			internal set => _filterType = (byte)value;
		}
		/// <summary>
		/// Gets the height of the first block in the requested range
		/// </summary>
	
			

		/// <summary>
		/// Gets the hash of the last block in the requested range 
		/// </summary>
		public uint256 StopHash => _stopHash;
	}



}
#endif