using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class Coin
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
