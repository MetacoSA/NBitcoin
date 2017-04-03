using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{

	/// <summary>
	/// Block headers received after a getheaders messages
	/// </summary>
	[Payload("headers")]
	public class HeadersPayload : Payload
	{
		class BlockHeaderWithTxCount : IBitcoinSerializable
		{
			public BlockHeaderWithTxCount()
			{

			}
			public BlockHeaderWithTxCount(BlockHeader header)
			{
				_Header = header;
			}
			internal BlockHeader _Header;
			#region IBitcoinSerializable Members

			public void ReadWrite(BitcoinStream stream)
			{
				stream.ReadWrite(ref _Header);
				VarInt txCount = new VarInt(0);
				stream.ReadWrite(ref txCount);

				// stratis adds an additional byte to the end of a header need to investigate why
				if (Transaction.TimeStamp)
					stream.ReadWrite(ref txCount);

			}

			#endregion
		}
		List<BlockHeader> headers = new List<BlockHeader>();

		public HeadersPayload()
		{

		}

		public HeadersPayload(params BlockHeader[] headers)
		{
			Headers.AddRange(headers);
		}

		public List<BlockHeader> Headers
		{
			get
			{
				return headers;
			}
		}

		public override void ReadWriteCore(BitcoinStream stream)
		{
			if(stream.Serializing)
			{
				var heardersOff = headers.Select(h => new BlockHeaderWithTxCount(h)).ToList();
				stream.ReadWrite(ref heardersOff);
			}
			else
			{
				headers.Clear();
				List<BlockHeaderWithTxCount> headersOff = new List<BlockHeaderWithTxCount>();
				stream.ReadWrite(ref headersOff);
				headers.AddRange(headersOff.Select(h => h._Header));
			}
		}
	}
}
