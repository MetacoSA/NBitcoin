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
}
