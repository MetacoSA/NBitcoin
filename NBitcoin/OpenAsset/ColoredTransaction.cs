using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.OpenAsset
{
	public class ColoredEntry : IBitcoinSerializable
	{
		public ColoredEntry()
		{

		}
		uint _Index;
		public int Index
		{
			get
			{
				return (int)_Index;
			}
			set
			{
				_Index = (uint)value;
			}
		}
		Asset _Asset = new Asset();
		public Asset Asset
		{
			get
			{
				return _Asset;
			}
			set
			{
				_Asset = value;
			}
		}
		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWriteAsVarInt(ref _Index);
			stream.ReadWrite(ref _Asset);
		}

		#endregion
	}
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

			ColoredTransaction colored = new ColoredTransaction();

			Queue<ColoredEntry> previousAssetQueue = new Queue<ColoredEntry>();
			for(int i = 0 ; i < tx.Inputs.Count ; i++)
			{
				var txin = tx.Inputs[i];
				var prevColored = repo.Get(txin.PrevOut.Hash);
				if(prevColored == null)
				{
					var prevTx = repo.Transactions.Get(txin.PrevOut.Hash);
					if(prevTx == null)
						throw new TransactionNotFoundException("Transaction " + txin.PrevOut.Hash + " not found in transaction repository", txId);
					if(!prevTx.HasColoredMarker())
					{
						continue;
					}
					prevColored = FetchColors(txin.PrevOut.Hash, prevTx, repo);
				}

				var prevAsset = prevColored.GetColoredEntry(txin.PrevOut.N);
				if(prevAsset != null)
				{
					previousAssetQueue.Enqueue(prevAsset);
					colored.Inputs.Add(new ColoredEntry()
					{
						Index = i,
						Asset = prevAsset.Asset
					});
				}
			}

			int markerPos = 0;
			colored.TxId = txId;
			var payload = OpenAssetPayload.Get(tx, out markerPos);
			if(payload == null)
			{
				repo.Put(txId, colored);
				return colored;
			}

			colored.Payload = payload;
			ScriptId issuedAsset = null;
			for(int i = 0 ; i < markerPos ; i++)
			{
				var entry = new ColoredEntry();
				entry.Index = i;
				entry.Asset.Quantity = i >= payload.Quantities.Length ? 0 : payload.Quantities[i];
				if(entry.Asset.Quantity == 0)
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
				entry.Asset.Id = issuedAsset;
				colored.Issuances.Add(entry);
			}

			//If there are more items in the  asset quantity list  than the number of colorable outputs, the transaction is deemed invalid, and all outputs are uncolored.
			if(payload.Quantities.Length > tx.Outputs.Count - 1)
			{
				repo.Put(txId, colored);
				return colored;
			}

			ulong used = 0;
			for(int i = markerPos + 1 ; i < tx.Outputs.Count ; i++)
			{
				var entry = new ColoredEntry();
				entry.Index = i;
				//If there are less items in the  asset quantity list  than the number of colorable outputs (all the outputs except the marker output), the outputs in excess receive an asset quantity of zero.
				entry.Asset.Quantity = (i - 1) >= payload.Quantities.Length ? 0 : payload.Quantities[i - 1];
				if(entry.Asset.Quantity == 0)
					continue;

				//If there are less asset units in the input sequence than in the output sequence, the transaction is considered invalid and all outputs are uncolored. 
				if(previousAssetQueue.Count == 0)
				{
					colored.Transfers.Clear();
					colored.Issuances.Clear();
					break;
				}
				entry.Asset.Id = previousAssetQueue.Peek().Asset.Id;
				var remaining = entry.Asset.Quantity;
				while(remaining != 0)
				{
					if(previousAssetQueue.Count == 0 || previousAssetQueue.Peek().Asset.Id != entry.Asset.Id)
					{
						colored.Transfers.Clear();
						colored.Issuances.Clear();
						break;
					}
					var assertPart = Math.Min(previousAssetQueue.Peek().Asset.Quantity - used, remaining);
					remaining = remaining - assertPart;
					used += assertPart;
					if(used == previousAssetQueue.Peek().Asset.Quantity)
					{
						previousAssetQueue.Dequeue();
						used = 0;
					}
				}
				if(remaining != 0)
					break;
				colored.Transfers.Add(entry);
			}
			repo.Put(txId, colored);
			return colored;
		}

		public ColoredEntry GetColoredEntry(uint n)
		{
			return Issuances
				.Concat(Transfers)
				.FirstOrDefault(i => i.Index == n);
		}
		public ColoredTransaction()
		{
			Issuances = new List<ColoredEntry>();
			Transfers = new List<ColoredEntry>();
			Inputs = new List<ColoredEntry>();
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

		List<ColoredEntry> _Issuances;
		public List<ColoredEntry> Issuances
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

		List<ColoredEntry> _Transfers;
		public List<ColoredEntry> Transfers
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

		public Asset[] GetDestroyedAssets()
		{
			var burned = Inputs
				.GroupBy(i => i.Asset.Id)
				.Select(g => new
				{
					Id = g.Key,
					Quantity = g.Aggregate(BigInteger.Zero, (a, o) => a + o.Asset.Quantity)
				});

			var transfered =
				Transfers
				.GroupBy(i => i.Asset.Id)
				.Select(g => new
				{
					Id = g.Key,
					Quantity = -g.Aggregate(BigInteger.Zero, (a, o) => a + o.Asset.Quantity)
				});

			return burned.Concat(transfered)
				.GroupBy(o => o.Id)
				.Select(g => new Asset()
				{
					Id = g.Key,
					Quantity = (ulong)g.Aggregate(BigInteger.Zero, (a, o) => a + o.Quantity)
				})
				.Where(a => a.Quantity != 0)
				.ToArray();
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _TxId);
			if(stream.Serializing)
			{
				if(_Payload != null)
					stream.ReadWrite(ref _Payload);
				else
					stream.ReadWrite(new Script());
			}
			else
			{
				Script script = new Script();
				stream.ReadWrite(ref script);
				if(script.Length != 0)
				{
					_Payload = new OpenAssetPayload(script);
				}
				else
				{
				}
			}
			stream.ReadWrite(ref _Inputs);
			stream.ReadWrite(ref _Issuances);
			stream.ReadWrite(ref _Transfers);
		}

		#endregion

		List<ColoredEntry> _Inputs;
		public List<ColoredEntry> Inputs
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
	}
}
