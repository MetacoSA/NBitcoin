using NBitcoin.Crypto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol.Payloads
{
	[Payload("cmpctblock")]
	public class CmpctBlockPayload : Payload
	{
		BlockHeader _Header;
		public BlockHeader Header
		{
			get
			{
				return _Header;
			}
			set
			{
				_Header = value;
			}
		}


		ulong _Nonce;
		public ulong Nonce
		{
			get
			{
				return _Nonce;
			}
			set
			{
				_Nonce = value;
			}
		}



		private List<ulong> _ShortIds = new List<ulong>();
		public List<ulong> ShortIds
		{
			get
			{
				return _ShortIds;
			}
		}



		private List<PrefilledTransaction> _PrefilledTransactions = new List<PrefilledTransaction>();
		private ulong _ShortTxidk0;
		private ulong _ShortTxidk1;

		public List<PrefilledTransaction> PrefilledTransactions
		{
			get
			{
				return _PrefilledTransactions;
			}
		}

		public override void ReadWriteCore(BitcoinStream stream)
		{
			stream.ReadWrite(ref _Header);
			stream.ReadWrite(ref _Nonce);

			var shorttxids_size = (uint)_ShortIds.Count;
			stream.ReadWriteAsVarInt(ref shorttxids_size);
			if(!stream.Serializing)
			{
				ulong i = 0;
				ulong shottxidsCount = 0;
				while(_ShortIds.Count < shorttxids_size)
				{
					shottxidsCount = Math.Min(1000UL + (ulong)shottxidsCount, (ulong)shorttxids_size);
					for(; i < shottxidsCount; i++)
					{
						uint lsb = 0;
						ushort msb = 0;
						stream.ReadWrite(ref lsb);
						stream.ReadWrite(ref msb);
						_ShortIds.Add(((ulong)(msb) << 32) | (ulong)(lsb));
					}
				}
			}
			else
			{
				for(var i = 0; i < _ShortIds.Count; i++)
				{
					uint lsb = (uint)(_ShortIds[i] & 0xffffffff);
					ushort msb = (ushort)((_ShortIds[i] >> 32) & 0xffff);
					stream.ReadWrite(ref lsb);
					stream.ReadWrite(ref msb);
				}
			}
			stream.ReadWrite(ref _PrefilledTransactions);

			if(!stream.Serializing)
				FillShortTxIDSelector();
		}

		private void FillShortTxIDSelector()
		{
			MemoryStream ms = new MemoryStream();
			BitcoinStream stream = new BitcoinStream(ms, true);
			stream.ReadWrite(ref _Header);
			stream.ReadWrite(ref _Nonce);
			uint256 shorttxidhash = new uint256(Hashes.SHA256(ms.ToArrayEfficient()));
			_ShortTxidk0 = Hashes.SipHasher.GetULong(shorttxidhash, 0);
			_ShortTxidk1 = Hashes.SipHasher.GetULong(shorttxidhash, 1);
		}

		internal ulong GetShortID(uint256 txhash)
		{
			return Hashes.SipHash(_ShortTxidk0, _ShortTxidk1, txhash) & 0xffffffffffffL;
		}
	}

	public class PrefilledTransaction : IBitcoinSerializable
	{

		Transaction _Transaction;
		public Transaction Transaction
		{
			get
			{
				return _Transaction;
			}
			set
			{
				_Transaction = value;
			}
		}


		ushort _DiffIndex;
		public ushort DiffIndex
		{
			get
			{
				return _DiffIndex;
			}
			set
			{
				_DiffIndex = value;
			}
		}

		public void ReadWrite(BitcoinStream stream)
		{
			ulong idx = _DiffIndex;
			stream.ReadWriteAsVarInt(ref idx);
			if(idx > ushort.MaxValue)
				throw new FormatException("index overflowed 16-bits");
			_DiffIndex = (ushort)idx;
			stream.ReadWrite(ref _Transaction);
		}
	}
}
