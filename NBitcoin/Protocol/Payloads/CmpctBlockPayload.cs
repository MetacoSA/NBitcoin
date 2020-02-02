using NBitcoin.Crypto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	[Payload("cmpctblock")]
	public class CmpctBlockPayload : Payload
	{
		public CmpctBlockPayload()
		{

		}
		public CmpctBlockPayload(Block block)
		{
			_Header = block.Header;
			_Nonce = RandomUtils.GetUInt64();
			UpdateShortTxIDSelector();
			PrefilledTransactions.Add(new PrefilledTransaction()
			{
				Index = 0,
				Transaction = block.Transactions[0]
			});
			foreach (var tx in block.Transactions.Skip(1))
			{
				ShortIds.Add(GetShortID(tx.GetHash()));
			}
		}
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
				if (value != null)
					UpdateShortTxIDSelector();
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
				UpdateShortTxIDSelector();
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
			if (!stream.Serializing)
			{
				ulong i = 0;
				ulong shottxidsCount = 0;
				while (_ShortIds.Count < shorttxids_size)
				{
					shottxidsCount = Math.Min(1000UL + (ulong)shottxidsCount, (ulong)shorttxids_size);
					for (; i < shottxidsCount; i++)
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
				for (var i = 0; i < _ShortIds.Count; i++)
				{
					uint lsb = (uint)(_ShortIds[i] & 0xffffffff);
					ushort msb = (ushort)((_ShortIds[i] >> 32) & 0xffff);
					stream.ReadWrite(ref lsb);
					stream.ReadWrite(ref msb);
				}
			}

			ulong txn_size = (ulong)PrefilledTransactions.Count;
			stream.ReadWriteAsVarInt(ref txn_size);

			if (!stream.Serializing)
			{
				ulong i = 0;
				ulong indicesCount = 0;
				while ((ulong)PrefilledTransactions.Count < txn_size)
				{
					indicesCount = Math.Min(1000UL + (ulong)indicesCount, (ulong)txn_size);
					for (; i < indicesCount; i++)
					{
						ulong index = 0;
						stream.ReadWriteAsVarInt(ref index);
						if (index > Int32.MaxValue)
							throw new FormatException("indexes overflowed 32-bits");
						Transaction tx = null;
						stream.ReadWrite(ref tx);
						PrefilledTransactions.Add(new PrefilledTransaction()
						{
							Index = (int)index,
							Transaction = tx
						});
					}
				}

				int offset = 0;
				for (var ii = 0; ii < PrefilledTransactions.Count; ii++)
				{
					if ((ulong)(PrefilledTransactions[ii].Index) + (ulong)(offset) > Int32.MaxValue)
						throw new FormatException("indexes overflowed 31-bits");
					PrefilledTransactions[ii].Index = PrefilledTransactions[ii].Index + offset;
					offset = PrefilledTransactions[ii].Index + 1;
				}
			}
			else
			{
				for (var i = 0; i < PrefilledTransactions.Count; i++)
				{
					uint index = checked((uint)(PrefilledTransactions[i].Index - (i == 0 ? 0 : (PrefilledTransactions[i - 1].Index + 1))));
					stream.ReadWriteAsVarInt(ref index);
					Transaction tx = PrefilledTransactions[i].Transaction;
					stream.ReadWrite(ref tx);
				}
			}

			if (!stream.Serializing)
				UpdateShortTxIDSelector();
		}

		void UpdateShortTxIDSelector()
		{
			MemoryStream ms = new MemoryStream();
			BitcoinStream stream = new BitcoinStream(ms, true);
			stream.ReadWrite(ref _Header);
			stream.ReadWrite(ref _Nonce);
			uint256 shorttxidhash = new uint256(Hashes.SHA256(ms.ToArrayEfficient()));
			_ShortTxidk0 = Hashes.SipHasher.GetULong(shorttxidhash, 0);
			_ShortTxidk1 = Hashes.SipHasher.GetULong(shorttxidhash, 1);
		}

		public ulong AddTransactionShortId(Transaction tx)
		{
			return AddTransactionShortId(tx.GetHash());
		}

		public ulong AddTransactionShortId(uint256 txId)
		{
			var id = GetShortID(txId);
			ShortIds.Add(id);
			return id;
		}

		public ulong GetShortID(uint256 txId)
		{
			return Hashes.SipHash(_ShortTxidk0, _ShortTxidk1, txId) & 0xffffffffffffL;
		}
	}

	public class PrefilledTransaction
	{

		public Transaction Transaction
		{
			get;
			set;
		}

		public int Index
		{
			get;
			set;
		}
	}
}
