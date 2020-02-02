using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	[Payload("getblocktxn")]
	public class GetBlockTxnPayload : Payload
	{

		uint256 _BlockId = uint256.Zero;
		public uint256 BlockId
		{
			get
			{
				return _BlockId;
			}
			set
			{
				_BlockId = value;
			}
		}



		private List<int> _Indices = new List<int>();
		public List<int> Indices
		{
			get
			{
				return _Indices;
			}
		}

		public override void ReadWriteCore(BitcoinStream stream)
		{
			stream.ReadWrite(ref _BlockId);
			ulong indexes_size = (ulong)_Indices.Count;
			stream.ReadWriteAsVarInt(ref indexes_size);
			if (!stream.Serializing)
			{
				ulong i = 0;
				ulong indicesCount = 0;
				while ((ulong)_Indices.Count < indexes_size)
				{
					indicesCount = Math.Min(1000UL + (ulong)indicesCount, (ulong)indexes_size);
					for (; i < indicesCount; i++)
					{
						ulong index = 0;
						stream.ReadWriteAsVarInt(ref index);
						if (index > Int32.MaxValue)
							throw new FormatException("indexes overflowed 31-bits");
						_Indices.Add((int)index);
					}
				}

				int offset = 0;
				for (var ii = 0; ii < _Indices.Count; ii++)
				{
					if ((ulong)(_Indices[ii]) + (ulong)(offset) > Int32.MaxValue)
						throw new FormatException("indexes overflowed 31-bits");
					_Indices[ii] = _Indices[ii] + offset;
					offset = _Indices[ii] + 1;
				}
			}
			else
			{
				for (var i = 0; i < _Indices.Count; i++)
				{
					int index = _Indices[i] - (i == 0 ? 0 : (_Indices[i - 1] + 1));
					stream.ReadWrite(ref index);
				}
			}
		}
	}
}
