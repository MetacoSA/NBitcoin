#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NBitcoin.Protocol
{

	[Payload("cfcheckpt")]
	public class CompactFilterCheckPointPayload : Payload, IBitcoinSerializable
	{
		private byte _FilterType = 0;
		private uint256 _StopHash = new uint256();
		private uint256[] _FilterHeaders = { };
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
			_StopHash = stopHash;
			_FilterHeaders = GetHashes(filterHeaders);
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
			get => (FilterType)_FilterType;
			internal set => _FilterType = (byte)value;
		}
		public uint256 StopHash { get => _StopHash; set => _StopHash = value; }
		public uint256[] FilterHeaders { get => _FilterHeaders; set => _FilterHeaders = value; }
		public VarInt FilterHeadersLength { get => _FilterHeadersLength; set => _FilterHeadersLength = value; }

		public new void ReadWrite(BitcoinStream stream)
		{
			var length = stream.Inner.Length;

			stream.ReadWrite(ref _FilterType);
			stream.ReadWrite(ref _StopHash);
			stream.ReadWrite(ref _FilterHeadersLength);

			//when serializing(Writing) we have to fill the tempfilterHeaders
			if (stream.Serializing)
			{
				//turn the uint256[] into a byte array to write back into the bitcoin stream
				List<byte> _tempfilterHeaders = new List<byte>();
				byte[] _tempFilterHeaderBytes = new byte[_FilterHeadersLength.ToLong() * 32]; //Init byte array to hold list after conversion

				foreach (var hash in _FilterHeaders)
				{
					foreach (var bytee in hash.ToBytes())
					{
						_tempfilterHeaders.Add(bytee);

					}
				}
				//Write bytes
				_tempFilterHeaderBytes = _tempfilterHeaders.ToArray();
				stream.ReadWrite(ref _tempFilterHeaderBytes);
			}


			if (!stream.Serializing)
			{
				//instantiate a byte[] to hold the incoming hashes
				var _tempfilterHeaders = new byte[_FilterHeadersLength.ToLong() * 32];

				//Write filters to temp variable
				stream.ReadWrite(ref _tempfilterHeaders);

				//Convert the byte[] into "readable" uint256 hashes
				_FilterHeaders = GetHashes(_tempfilterHeaders);
			}
		}


		public override string ToString()
		{
			return $"cfcheckpt - filter type: {this._FilterType}| Stop Hash {this.StopHash}| # of checkpoints: {this._FilterHeadersLength.ToLong()}";
		}
	}
}
#endif
