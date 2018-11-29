using System;

namespace NBitcoin.Protocol
{

	/// <summary>
	/// Represents the p2p message payload used for sharing a block's compact filter.
	/// </summary>
	[Payload("cfilter")]
	public class CompactFilterPayload : Payload
	{
		private byte _FilterType = 0;
		private byte[] _FilterBytes;
		private VarInt _NumFilterBytes = new VarInt(0);
		private uint256 _BlockHash = new uint256();

		/// <summary>
		/// Gets the Filter type for which headers are requested  
		/// </summary>
		public FilterType FilterType
		{
			get => (FilterType)_FilterType;
			internal set => _FilterType = (byte)value;
		}
		/// <summary>
		/// Gets the serialized compact filter for this block 
		/// </summary>
		public byte[] FilterBytes => _FilterBytes;


		/// <summary>
		/// Gets block hash of the Bitcoin block for which the filter is being returned  
		/// </summary>
		public uint256 BlockHash => _BlockHash;
		public CompactFilterPayload(FilterType filterType, uint256 blockhash, byte[] filterBytes)
		{
			if (filterType != FilterType.Basic /*&& filterType != FilterType.Extended*/) //Extended filters removed
				throw new ArgumentException($"'{filterType}' is not a valid value. Try with Basic.", nameof(filterType));
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

		#region IBitcoinSerializable Members

		public new void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _FilterType);
			stream.ReadWrite(ref _BlockHash);
			stream.ReadWrite(ref _FilterBytes);
		}

		#endregion

		public override void ReadWriteCore(BitcoinStream stream)
		{
			stream.ReadWrite(ref _FilterType);

			stream.ReadWrite(ref _BlockHash);

			stream.ReadWrite(ref _NumFilterBytes);

			_FilterBytes = new byte[_NumFilterBytes.ToLong()];

			stream.ReadWrite(ref _FilterBytes);
		}

		public override string ToString()
		{
			return $"Cfilter type: {this.FilterType}| Block hash: {this.BlockHash}| cfilter bytes omitted.";
		}
	}
}
