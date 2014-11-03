using NBitcoin.Stealth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Scanning
{
	public class StealthPaymentScanner : Scanner
	{

		public StealthPaymentScanner(BitcoinStealthAddress address, Key scan)
		{
			if(address == null)
				throw new ArgumentNullException("address");
			if(scan == null)
				throw new ArgumentNullException("scan");
			_Address = address;
			_Scan = scan;
		}

		private readonly BitcoinStealthAddress _Address;
		public BitcoinStealthAddress Address
		{
			get
			{
				return _Address;
			}
		}
		private readonly Key _Scan;
		public Key Scan
		{
			get
			{
				return _Scan;
			}
		}

		public override Coins ScanCoins(uint256 txId, Transaction tx, int height)
		{
			var payments = StealthPayment.GetPayments(tx, Address, Scan);
			return new Coins(tx, txout => Match(txout, payments), height);
		}

		private bool Match(TxOut txout, StealthPayment[] payments)
		{
			return payments.Any(p => p.ScriptPubKey == txout.ScriptPubKey && !txout.IsDust);
		}

		public override IEnumerable<TxIn> FindSpent(IEnumerable<Transaction> transactions)
		{
			return new TxIn[0]; //Impossible to know withtout the initial payment
		}
	}
}
