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

	/// <summary>
	/// Represents the p2p message payload used for sharing a block's compact filter.
	/// </summary>
	[Payload("cfilter")]
	public class CompactFilterPayload : Payload
	{
		private byte _filterType;
		private byte[] _filterBytes;
		private uint256 _blockHash;

		/// <summary>
		/// Gets the Filter type for which headers are requested  
		/// </summary>
		public FilterType FilterType
		{
			get => (FilterType)_filterType;
			internal set => _filterType = (byte)value;
		}
		/// <summary>
		/// Gets the serialized compact filter for this block 
		/// </summary>
		public byte[] FilterBytes => _filterBytes;


		/// <summary>
		/// Gets block hash of the Bitcoin block for which the filter is being returned  
		/// </summary>
		public uint256 BlockHash => _blockHash;
		public CompactFilterPayload(FilterType filterType, uint256 blockhash, byte[] filterBytes)
		{
			if (filterType != FilterType.Basic /*&& filterType != FilterType.Extended*/) //Extended filters removed
				throw new ArgumentException($"'{filterType}' is not a valid value. Try with Basic or Extended.", nameof(filterType));
			if (blockhash == null)
				throw new ArgumentNullException(nameof(blockhash));
			if (filterBytes == null)
				throw new ArgumentNullException(nameof(filterBytes));


			FilterType = filterType;
			_blockHash = blockhash;
			_filterBytes = filterBytes;
		}

		#region IBitcoinSerializable Members
		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _filterType);
			stream.ReadWrite(ref _blockHash);
			stream.ReadWriteAsVarString(ref _filterBytes);
		}

		#endregion
	}
}
#endif