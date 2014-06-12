using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class CoinsView
	{
		public CoinsView(NoSqlRepository index)
		{
			if(index == null)
				throw new ArgumentNullException("index");
			_Index = index;
		}

		public CoinsView()
			: this(new InMemoryNoSqlRepository())
		{

		}
		private readonly NoSqlRepository _Index;
		public NoSqlRepository Index
		{
			get
			{
				return _Index;
			}
		}

		public Coins GetCoins(uint256 txId)
		{
			return Index.Get<Coins>(txId.ToString());
		}

		public void SetCoins(uint256 txId, Coins coins)
		{
			Index.Put(txId.ToString(), coins);
		}

		public bool HaveCoins(uint256 txId)
		{
			return GetCoins(txId) != null;
		}

		public uint256 GetBestBlock()
		{
			var block = Index.Get<uint256>("B");
			return block ?? new uint256(0);
		}

		public void SetBestBlock(uint256 blockId)
		{
			Index.Put("B", blockId);
		}

		public bool HaveInputs(Transaction tx)
		{
			if(!tx.IsCoinBase)
			{
				// first check whether information about the prevout hash is available
				for(int i = 0 ; i < tx.Inputs.Count ; i++)
				{
					OutPoint prevout = tx.Inputs[i].PrevOut;
					if(!HaveCoins(prevout.Hash))
						return false;
				}

				// then check whether the actual outputs are available
				for(int i = 0 ; i < tx.Inputs.Count ; i++)
				{
					OutPoint prevout = tx.Inputs[i].PrevOut;
					Coins coins = GetCoins(prevout.Hash);
					if(!coins.IsAvailable(prevout.N))
						return false;
				}
			}
			return true;
		}

		public TxOut GetOutputFor(TxIn input)
		{
			Coins coins = GetCoins(input.PrevOut.Hash);
			if(!coins.IsAvailable(input.PrevOut.N))
			{
				return null;
			}
			return coins.Outputs[(int)input.PrevOut.N];
		}

		public Money GetValueIn(Transaction tx)
		{
			if(tx.IsCoinBase)
				return 0;
			return tx.Inputs.Select(i => GetOutputFor(i).Value).Sum();
		}

		public CoinsView CreateCached()
		{
			return new CoinsView(new CachedNoSqlRepository(Index));
		}

		public void AddTransaction(Transaction tx, int height)
		{
			SetCoins(tx.GetHash(), new Coins(tx, height));
		}
	}
}
