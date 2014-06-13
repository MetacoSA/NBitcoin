using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Scanning
{
	public class ScriptHashScanner : Scanner
	{
		public ScriptHashScanner(ScriptId scriptId)
		{
			_ScriptId = scriptId;
		}
		PayToScriptHashTemplate template = new PayToScriptHashTemplate();
		private readonly ScriptId _ScriptId;
		public ScriptId ScriptId
		{
			get
			{
				return _ScriptId;
			}
		}

		public override void GetScannedPushData(List<byte[]> searchedPushData)
		{
			searchedPushData.Add(ScriptId.ToBytes());
		}
		public override Coins ScanCoins(Transaction tx, int height)
		{
			return new Coins(tx, MatchScriptHash, height);
		}
		public bool MatchScriptHash(TxOut output)
		{
			ScriptId id = template.ExtractScriptPubKeyParameters(output.ScriptPubKey);
			return (id == ScriptId);
		}

		protected override IEnumerable<TxIn> FindSpentCore(IEnumerable<Transaction> transactions)
		{
			return
				transactions
				.SelectMany(t => t.Inputs)
				.Select(i => new
				{
					TxIn = i,
					Parameters = template.ExtractScriptSigParameters(i.ScriptSig)
				})
				.Where(r => r.Parameters != null && r.Parameters.Script.ID == ScriptId)
				.Select(r=>r.TxIn);
		}
	}
}
