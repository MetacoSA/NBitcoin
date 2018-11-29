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
		private byte _FilterType = 0;
		private uint256 _StopHash = new uint256();
		private uint256 _PreviousFilterHeader = new uint256();
		private VarInt _FilterHashesLength = new VarInt(0);
		private uint256[] _FilterHashes = { };

		public CompactFilterHeadersPayload(FilterType filterType, uint256 stopHash, uint256 previousFilterHeader, byte[] filterHashes)
		{
			if (filterType != FilterType.Basic /*&& filterType != FilterType.Extended*/) //Extended filters removed
				throw new ArgumentException($"'{filterType}' is not a valid value. Try with Basic.", nameof(filterType));
			if (stopHash == null)
				throw new ArgumentException(nameof(stopHash));
			if (previousFilterHeader == null)
				throw new ArgumentException(nameof(previousFilterHeader));
			if (filterHashes == null)
				throw new ArgumentException(nameof(filterHashes));


			FilterType = filterType;
			_StopHash = stopHash;
			_PreviousFilterHeader = previousFilterHeader;
			_FilterHashes = GetHashes(filterHashes);
		}

		public CompactFilterHeadersPayload() { }

		public FilterType FilterType
		{
			get => (FilterType)_FilterType;
			internal set => _FilterType = (byte)value;
		}

		public new void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _FilterType);
			stream.ReadWrite(ref _StopHash);
			stream.ReadWrite(ref _PreviousFilterHeader);
			stream.ReadWrite(ref _FilterHashesLength);
			//var _tempfilterHeaders = new byte[_filterHashesLength.ToLong() * 32];

			if (stream.Serializing)
			{
				//turn the uint256[] into a byte array to write back into the bitcoin stream
				List<byte> _tempfilterHeaders = new List<byte>();
				byte[] _tempFilterHeaderBytes = new byte[_FilterHashesLength.ToLong() * 32]; //Init byte array to hold list after conversion

				foreach (var hash in _FilterHashes)
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
				var _tempfilterHeaders = new byte[_FilterHashesLength.ToLong() * 32];

				//Write filters to temp variable
				stream.ReadWrite(ref _tempfilterHeaders);

				//Convert the byte[] into "readable" uint256 hashes
				_FilterHashes = GetHashes(_tempfilterHeaders);
			}
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


		public uint256 StopHash => _StopHash;
		public uint256 PreviousFilterHeader => _PreviousFilterHeader;
		public uint256[] FilterHashes => _FilterHashes;



		public override string ToString()
		{
			return $"Cheaders type: {this.FilterType}| # of headers: {this._FilterHashesLength.ToLong()}| Stop Hash: {this.StopHash}| Previous Filter Header Hash: {this.PreviousFilterHeader}";
		}
	}
}
