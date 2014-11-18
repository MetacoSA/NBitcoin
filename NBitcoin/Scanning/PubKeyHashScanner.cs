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
		public PubKeyHashScanner(BitcoinAddress address)
			: this((KeyId)address.ID)
		{

		}
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


		public override Coins ScanCoins(uint256 txId, Transaction tx, int height)
		{
			return new Coins(tx, MatchPubKeyHash, height);
		}

		public bool MatchPubKeyHash(TxOut output)
		{
			var id = PayToPubkeyHashTemplate.ExtractScriptPubKeyParameters(output.ScriptPubKey);
			return (id == KeyId);
		}

		public override IEnumerable<TxIn> FindSpent(IEnumerable<Transaction> transactions)
		{
			return
				transactions
				.SelectMany(t => t.Inputs)
				.Select(i => new
				{
					TxIn = i,
					Parameters = PayToPubkeyHashTemplate.ExtractScriptSigParameters(i.ScriptSig)
				})
				.Where(r => r.Parameters != null && r.Parameters.PublicKey.ID == KeyId)
				.Select(r => r.TxIn);
		}
	}
}
