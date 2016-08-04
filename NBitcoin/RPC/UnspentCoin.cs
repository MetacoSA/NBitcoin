using NBitcoin.DataEncoders;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.RPC
{
	public class UnspentCoin
	{
		internal UnspentCoin(JObject unspent)
		{
			OutPoint = new OutPoint(uint256.Parse((string)unspent["txid"]), (uint)unspent["vout"]);
			Address = Network.CreateFromBase58Data<BitcoinAddress>((string)unspent["address"]);
			Account = (string)unspent["account"];
			ScriptPubKey = new Script(Encoders.Hex.DecodeData((string)unspent["scriptPubKey"]));
			var amount = (decimal)unspent["amount"];
			Amount = new Money((long)(amount * Money.COIN));
			Confirmations = (uint)unspent["confirmations"];
		}

		public OutPoint OutPoint
		{
			get;
			private set;
		}

		public BitcoinAddress Address
		{
			get;
			private set;
		}
		public string Account
		{
			get;
			private set;
		}
		public Script ScriptPubKey
		{
			get;
			private set;
		}
		public uint Confirmations
		{
			get;
			private set;
		}

		public Money Amount
		{
			get;
			private set;
		}

		public Coin AsCoin()
		{
			return new Coin(OutPoint, new TxOut(Amount, ScriptPubKey));
		}
	}
}
