#if !NOJSONNET
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
		internal UnspentCoin(JObject unspent, Network network)
		{
			OutPoint = new OutPoint(uint256.Parse((string)unspent["txid"]), (uint)unspent["vout"]);
			var address = (string)unspent["address"];
			if (address != null)
				Address = network.Parse<BitcoinAddress>(address);
			Account = (string)unspent["account"];
			ScriptPubKey = new Script(Encoders.Hex.DecodeData((string)unspent["scriptPubKey"]));
			var redeemScriptHex = (string)unspent["redeemScript"];
			if (redeemScriptHex != null)
			{
				RedeemScript = new Script(Encoders.Hex.DecodeData(redeemScriptHex));
			}
			var amount = (decimal)unspent["amount"];
			Amount = new Money((long)(amount * Money.COIN));
			Confirmations = (uint)unspent["confirmations"];

			// Added in Bitcoin Core 0.10.0
			if (unspent["spendable"] != null)
			{
				IsSpendable = (bool)unspent["spendable"];
			}
			else
			{
				// Default to True for earlier versions, i.e. if not present
				IsSpendable = true;
			}
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

		public Script RedeemScript
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
			var coin = new Coin(OutPoint, new TxOut(Amount, ScriptPubKey));
			if (RedeemScript != null)
				coin = coin.ToScriptCoin(RedeemScript);
			return coin;
		}

		public bool IsSpendable
		{
			get;
			private set;
		}
	}
}
#endif