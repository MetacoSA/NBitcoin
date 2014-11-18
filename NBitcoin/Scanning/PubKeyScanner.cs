using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Scanning
{
	public class PubKeyScanner : Scanner
	{
		public PubKeyScanner(PubKey pubKey)
		{
			_PubKey = pubKey;
		}
		private readonly PubKey _PubKey;
		public PubKey PubKey
		{
			get
			{
				return _PubKey;
			}
		}

		public override void GetScannedPushData(List<byte[]> searchedPushData)
		{
			searchedPushData.Add(PubKey.ToBytes());
		}
		public override Coins ScanCoins(uint256 txId, Transaction tx, int height)
		{
			return new Coins(tx, MatchScriptHash, height);
		}
		public bool MatchScriptHash(TxOut output)
		{
			var key = PayToPubkeyTemplate.Instance.ExtractScriptPubKeyParameters(output.ScriptPubKey);
			return key != null && (key.ID == PubKey.ID);
		}

		public override IEnumerable<TxIn> FindSpent(IEnumerable<Transaction> transactions)
		{
			return new TxIn[0]; //Impossible to know without pubkey
		}
	}
}
