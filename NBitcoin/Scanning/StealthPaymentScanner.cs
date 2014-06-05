using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Scanning
{
	public class StealthPaymentScanner : Scanner
	{
		public StealthPaymentScanner(BitField prefix, PubKey[] spendKeys, Key scan)
		{
			_Scan = scan;
			_Prefix = prefix;
			_SpendKeys = spendKeys.ToArray();
		}
		public StealthPaymentScanner(BitcoinStealthAddress address, Key scan)
			: this(address.Prefix, address.SpendPubKeys, scan)
		{

		}
		private readonly Key _Scan;
		public Key Scan
		{
			get
			{
				return _Scan;
			}
		}
		private readonly PubKey[] _SpendKeys;
		public PubKey[] SpendKeys
		{
			get
			{
				return _SpendKeys;
			}
		}
		private readonly BitField _Prefix;
		public BitField Prefix
		{
			get
			{
				return _Prefix;
			}
		}


		public override Coins ScanCoins(Transaction tx, int height)
		{
			var payments = StealthPayment.GetPayments(tx, SpendKeys, Prefix, Scan);
			return new Coins(tx, txout => Match(txout, payments), height);
		}

		private bool Match(TxOut txout, StealthPayment[] payments)
		{
			return payments.Any(p=>p.SpendableScript.Same(txout.ScriptPubKey) && !txout.IsDust);
		}
	}
}
