using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.SPV
{
	public class BlockInformation
	{
		public BlockHeader Header
		{
			get;
			set;
		}
		public int Height
		{
			get;
			set;
		}
		public int Confirmations
		{
			get;
			set;
		}
	}
	public class WalletTransaction
	{
		public WalletTransaction()
		{

		}

		public Coin[] ReceivedCoins
		{
			get;
			set;
		}
		public Coin[] SpentCoins
		{
			get;
			set;
		}
		public BlockInformation BlockInformation
		{
			get;
			set;
		}
		public MerkleBlock Proof
		{
			get;
			set;
		}

		public DateTimeOffset UnconfirmedSeen
		{
			get;
			set;
		}

		public DateTimeOffset AddedDate
		{
			get;
			set;
		}

		public Transaction Transaction
		{
			get;
			set;
		}

		public Money Balance
		{
			get
			{
				return (ReceivedCoins.Select(s => s.Amount).Sum() - SpentCoins.Select(s => s.Amount).Sum());
			}
		}

		public override string ToString()
		{
			return Balance.ToString() + " (" +
						(BlockInformation == null ? "unconfirmed)" : BlockInformation.Confirmations + " confirmations)");
		}
	}
}
