using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Scanning
{
	public class PubKeyHashScanner : Scanner
	{
		public PubKeyHashScanner(KeyId pubKeyHash)
		{
			if(pubKeyHash == null)
				throw new ArgumentNullException("pubKeyHash");
			_PubKeyHash = pubKeyHash;
		}
		PayToPubkeyHashTemplate template = new PayToPubkeyHashTemplate();
		private readonly KeyId _PubKeyHash;
		public KeyId KeyId
		{
			get
			{
				return _PubKeyHash;
			}
		}

		public override void GetScannedPushData(List<byte[]> searchedPushData)
		{
			searchedPushData.Add(KeyId.ToBytes());
		}


		public override Coins ScanCoins(Transaction tx, int height)
		{
			var hash = tx.GetHash();
			return new Coins(tx, MatchPubKeyHash, height);
		}

		public bool MatchPubKeyHash(TxOut output)
		{
			var id = template.ExtractScriptPubKeyParameters(output.ScriptPubKey);
			return (id == KeyId);
		}
	}
}
