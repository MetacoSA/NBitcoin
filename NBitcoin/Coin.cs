using NBitcoin.OpenAsset;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public interface IColoredCoin : ICoin
	{
		Coin Bearer
		{
			get;
		}
	}
	public interface ICoin
	{
		OutPoint Outpoint
		{
			get;
		}
		Script ScriptPubKey
		{
			get;
		}
		Money Amount
		{
			get;
		}
	}

	public class IssuanceCoin : IColoredCoin
	{
		public IssuanceCoin()
		{

		}
		public IssuanceCoin(Coin bearer)
		{
			Bearer = bearer;
		}

		public IssuanceCoin(OutPoint outpoint, TxOut txout)
		{
			Bearer = new Coin(outpoint, txout);
		}


		public ScriptId AssetId
		{
			get
			{
				return Bearer.TxOut.ScriptPubKey.ID;
			}
		}

	

		#region ICoin Members


		public Money Amount
		{
			get
			{
				return Bearer.TxOut.Value;
			}
			set
			{
				Bearer.TxOut.Value = value;
			}
		}

		#endregion

		public Script ScriptPubKey
		{
			get
			{
				return Bearer.TxOut.ScriptPubKey;
			}
		}

		#region IColoredCoin Members


		public Coin Bearer
		{
			get;
			set;
		}

		
		public OutPoint Outpoint
		{
			get
			{
				return Bearer.Outpoint;
			}
		}

		#endregion
	}

	public class ColoredCoin : IColoredCoin
	{
		public ColoredCoin(Asset asset, Coin bearer)
		{
			Asset = asset;
			Bearer = bearer;
		}

		public Asset Asset
		{
			get;
			set;
		}

		public Coin Bearer
		{
			get;
			set;
		}
		#region ICoin Members

		public OutPoint Outpoint
		{
			get
			{
				return Bearer.Outpoint;
			}
		}

		public Script ScriptPubKey
		{
			get
			{
				return Bearer.ScriptPubKey;
			}
		}

		public Money Amount
		{
			get
			{
				return Asset.Quantity;
			}
		}

		#endregion

		public static IEnumerable<ColoredCoin> Find(Transaction tx, ColoredTransaction colored)
		{
			return Find(null, tx, colored);
		}
		public static IEnumerable<ColoredCoin> Find(uint256 txId, Transaction tx, ColoredTransaction colored)
		{
			if(txId == null)
				txId = tx.GetHash();
			foreach(var entry in colored.Issuances.Concat(colored.Transfers))
			{
				var txout = tx.Outputs[entry.Index];
				yield return new ColoredCoin(entry.Asset, new Coin(new OutPoint(txId, entry.Index), txout));
			}
		}

		public static IEnumerable<ColoredCoin> Find(Transaction tx, NoSqlColoredTransactionRepository repo)
		{
			return Find(null, tx, repo);
		}
		public static IEnumerable<ColoredCoin> Find(uint256 txId, Transaction tx, NoSqlColoredTransactionRepository repo)
		{
			if(txId == null)
				txId = tx.GetHash();
			var colored = tx.GetColoredTransaction(repo);
			return Find(txId, tx, colored);
		}
	}
	public class Coin : ICoin
	{
		public Coin()
		{

		}
		public Coin(OutPoint outpoint, TxOut txOut)
		{
			Outpoint = outpoint;
			TxOut = txOut;
		}
		public OutPoint Outpoint
		{
			get;
			set;
		}
		public TxOut TxOut
		{
			get;
			set;
		}

		#region ICoin Members


		public Money Amount
		{
			get
			{
				return TxOut.Value;
			}
			set
			{
				TxOut.Value = value;
			}
		}

		#endregion

		public Script ScriptPubKey
		{
			get
			{
				return TxOut.ScriptPubKey;
			}
		}
	}

	public class ScriptCoin : Coin
	{
		public ScriptCoin()
		{

		}
		public ScriptCoin(OutPoint outpoint, TxOut txOut, Script redeem)
			: base(outpoint, txOut)
		{
			Redeem = redeem;
		}
		public Script Redeem
		{
			get;
			set;
		}
	}

	public class StealthCoin : Coin
	{
		public StealthCoin()
		{
		}
		public StealthCoin(OutPoint outpoint, TxOut txOut, StealthMetadata stealthMetadata, BitcoinStealthAddress address)
			: base(outpoint, txOut)
		{
			StealthMetadata = stealthMetadata;
			Address = address;
		}
		public StealthMetadata StealthMetadata
		{
			get;
			set;
		}

		public BitcoinStealthAddress Address
		{
			get;
			set;
		}

		public static StealthCoin Find(Transaction tx, BitcoinStealthAddress address, Key scan)
		{
			var payment = address.GetPayments(tx, scan).FirstOrDefault();
			if(payment == null)
				return null;
			var txId = tx.GetHash();
			var txout = tx.Outputs.First(o => o.ScriptPubKey == payment.SpendableScript);
			return new StealthCoin(new OutPoint(txId, tx.Outputs.IndexOf(txout)), txout, payment.Metadata, address);
		}

		public StealthPayment GetPayment()
		{
			return new StealthPayment(TxOut.ScriptPubKey, StealthMetadata);
		}
	}
}
