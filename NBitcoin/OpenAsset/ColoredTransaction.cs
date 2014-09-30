using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.OpenAsset
{
	public class ColoredTransaction : IBitcoinSerializable
	{
		public static ColoredTransaction FetchColors(Transaction tx, IColoredTransactionRepository repo)
		{
			return FetchColors(null, tx, repo);
		}
		public static ColoredTransaction FetchColors(uint256 txId, IColoredTransactionRepository repo)
		{
			var colored = repo.Get(txId);
			if(colored != null)
				return colored;
			var tx = repo.Transactions.Get(txId);
			if(tx == null)
				throw new TransactionNotFoundException("Transaction " + txId + " not found in transaction repository", txId);
			return FetchColors(txId, tx, repo);
		}
		public static ColoredTransaction FetchColors(uint256 txId, Transaction tx, IColoredTransactionRepository repo)
		{
			txId = txId ?? tx.GetHash();
			var result = repo.Get(txId);
			if(result != null)
				return result;
			int markerPos = 0;
			ColoredTransaction colored = new ColoredTransaction();
			colored.TxId = txId;
			var payload = OpenAssetPayload.Get(tx, out markerPos);
			if(payload == null)
				return colored;

			colored.Payload = payload;
			ScriptId issuedAsset = null;
			for(int i = 0 ; i < markerPos ; i++)
			{
				var asset = new Asset();
				asset.Index = i;
				asset.Quantity = i >= payload.Quantities.Length ? 0 : payload.Quantities[i];
				if(asset.Quantity == 0)
					continue;

				if(issuedAsset == null)
				{
					var txIn = tx.Inputs.FirstOrDefault();
					if(txIn == null)
						continue;
					var prev = repo.Transactions.Get(txIn.PrevOut.Hash);
					if(prev == null)
						throw new TransactionNotFoundException("This open asset transaction is issuing assets, but it needs a parent transaction in the TransactionRepository to know the address of the issued asset (missing : " + txIn.PrevOut.Hash + ")", txIn.PrevOut.Hash);
					issuedAsset = prev.Outputs[(int)txIn.PrevOut.N].ScriptPubKey.ID;
				}
				asset.AssetId = issuedAsset;
				colored.Issuances.Add(asset);
			}


			Queue<Asset> spentAssets = null;
			uint used = 0;
			for(int i = markerPos + 1 ; i < tx.Outputs.Count ; i++)
			{
				var asset = new Asset();
				asset.Index = i;
				asset.Quantity = (i - 1) >= payload.Quantities.Length ? 0 : payload.Quantities[i - 1];
				if(asset.Quantity == 0)
					continue;

				if(spentAssets == null)
				{
					spentAssets = new Queue<Asset>();
					foreach(var txin in tx.Inputs)
					{
						var prevColored = FetchColors(txin.PrevOut.Hash, repo);
						if(prevColored != null)
						{
							var prevAsset = prevColored.GetAsset(txin.PrevOut.N);
							if(prevAsset != null)
							{
								spentAssets.Enqueue(prevAsset);
								colored.Inputs.Add(prevAsset);
							}
						}
					}
				}
				if(spentAssets.Count == 0)
				{
					//Invalid
					break;
				}
				asset.AssetId = spentAssets.Peek().AssetId;
				var remaining = asset.Quantity;
				while(remaining != 0)
				{
					if(spentAssets.Count == 0 || spentAssets.Peek().AssetId != asset.AssetId)
					{
						//Invalid
						break;
					}
					var assertPart = Math.Min(spentAssets.Peek().Quantity - used, remaining);
					remaining = remaining - assertPart;
					if(used == spentAssets.Peek().Quantity)
					{
						spentAssets.Dequeue();
						used = 0;
					}
				}
				colored.Transfers.Add(asset);
			}
			repo.Put(txId, colored);
			return colored;
		}

		public Asset GetAsset(uint n)
		{
			return Issuances
				.Concat(Transfers)
				.FirstOrDefault(i => i.Index == n);
		}
		public ColoredTransaction()
		{
			Issuances = new List<Asset>();
			Transfers = new List<Asset>();
			Inputs = new List<Asset>();
		}
		uint256 _TxId;
		public uint256 TxId
		{
			get
			{
				return _TxId;
			}
			set
			{
				_TxId = value;
			}
		}

		OpenAssetPayload _Payload;
		public OpenAssetPayload Payload
		{
			get
			{
				return _Payload;
			}
			set
			{
				_Payload = value;
			}
		}

		List<Asset> _Inputs;
		public List<Asset> Inputs
		{
			get
			{
				return _Inputs;
			}
			set
			{
				_Inputs = value;
			}
		}
		List<Asset> _Issuances;
		public List<Asset> Issuances
		{
			get
			{
				return _Issuances;
			}
			set
			{
				_Issuances = value;
			}
		}

		List<Asset> _Transfers;
		public List<Asset> Transfers
		{
			get
			{
				return _Transfers;
			}
			set
			{
				_Transfers = value;
			}
		}


		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _TxId);
			stream.ReadWrite(ref _Inputs);
			stream.ReadWrite(ref _Issuances);
			stream.ReadWrite(ref _Transfers);
		}

		#endregion
	}
}
