﻿using Newtonsoft.Json.Linq;
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
		public ColoredEntry(int index, Asset asset)
		{
			if(asset == null)
				throw new ArgumentNullException("asset");
			Index = index;
			Asset = asset;
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

		public override string ToString()
		{
			if(Asset == null)
				return "[" + Index + "]";
			else
				return "[" + Index + "] " + Asset;
		}
	}
	public class ColoredTransaction : IBitcoinSerializable
	{
		public static ColoredTransaction FetchColors(Transaction tx, IColoredTransactionRepository repo)
		{
			return FetchColors(null, tx, repo);
		}
		public static ColoredTransaction FetchColors(uint256 txId, IColoredTransactionRepository repo)
		{
			if(repo == null)
				throw new ArgumentNullException("repo");
			repo = EnsureCachedRepository(repo);
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

			ColoredTransaction lastColored = null;
			//The following code is to prevent recursion of FetchColors that would fire a StackOverflow if the origin of traded asset were deep in the transaction dependency tree
			repo = EnsureCachedRepository(repo);
			HashSet<uint256> invalidColored = new HashSet<uint256>();
			Stack<Tuple<uint256, Transaction>> ancestors = new Stack<Tuple<uint256, Transaction>>();
			ancestors.Push(Tuple.Create(txId, tx));
			while(ancestors.Count != 0)
			{
				var peek = ancestors.Peek();
				txId = peek.Item1;
				tx = peek.Item2;
				bool isComplete = true;
				if(!tx.HasValidColoredMarker() && ancestors.Count != 1)
				{
					invalidColored.Add(txId);
					ancestors.Pop();
					continue;
				}

				for(int i = 0 ; i < tx.Inputs.Count ; i++)
				{
					var txin = tx.Inputs[i];
					if(repo.Get(txin.PrevOut.Hash) == null && !invalidColored.Contains(txin.PrevOut.Hash))
					{
						var prevTx = repo.Transactions.Get(txin.PrevOut.Hash);
						if(prevTx == null)
							throw new TransactionNotFoundException("Transaction " + txin.PrevOut.Hash + " not found in transaction repository", txId);
						ancestors.Push(Tuple.Create(txin.PrevOut.Hash, prevTx));
						isComplete = false;
					}
				}
				if(isComplete)
				{
					lastColored = FetchColorsWithAncestorsSolved(txId, tx, repo);
					ancestors.Pop();
				}
			}

			return lastColored;
		}

		private static IColoredTransactionRepository EnsureCachedRepository(IColoredTransactionRepository repo)
		{
			if(repo is CachedColoredTransactionRepository)
				return repo;
			repo = new CachedColoredTransactionRepository(repo);
			return repo;
		}

		private static ColoredTransaction FetchColorsWithAncestorsSolved(uint256 txId, Transaction tx, IColoredTransactionRepository repo)
		{
			ColoredTransaction colored = new ColoredTransaction();

			Queue<ColoredEntry> previousAssetQueue = new Queue<ColoredEntry>();
			for(int i = 0 ; i < tx.Inputs.Count ; i++)
			{
				var txin = tx.Inputs[i];
				var prevColored = repo.Get(txin.PrevOut.Hash);
				if(prevColored == null)
					continue;
				var prevAsset = prevColored.GetColoredEntry(txin.PrevOut.N);
				if(prevAsset != null)
				{
					var input = new ColoredEntry()
					{
						Index = i,
						Asset = prevAsset.Asset
					};
					previousAssetQueue.Enqueue(input);
					colored.Inputs.Add(input);
				}
			}

			int markerPos = 0;
			var marker = ColorMarker.Get(tx, out markerPos);
			if(marker == null)
			{
				repo.Put(txId, colored);
				return colored;
			}
			colored.Marker = marker;
			if(!marker.HasValidQuantitiesCount(tx))
			{
				repo.Put(txId, colored);
				return colored;
			}

			AssetId issuedAsset = null;
			for(int i = 0 ; i < markerPos ; i++)
			{
				var entry = new ColoredEntry();
				entry.Index = i;
				entry.Asset.Quantity = i >= marker.Quantities.Length ? 0 : marker.Quantities[i];
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
					issuedAsset = prev.Outputs[(int)txIn.PrevOut.N].ScriptPubKey.ID.ToAssetId();
				}
				entry.Asset.Id = issuedAsset;
				colored.Issuances.Add(entry);
			}

			ulong used = 0;
			for(int i = markerPos + 1 ; i < tx.Outputs.Count ; i++)
			{
				var entry = new ColoredEntry();
				entry.Index = i;
				//If there are less items in the  asset quantity list  than the number of colorable outputs (all the outputs except the marker output), the outputs in excess receive an asset quantity of zero.
				entry.Asset.Quantity = (i - 1) >= marker.Quantities.Length ? 0 : marker.Quantities[i - 1];
				if(entry.Asset.Quantity == 0)
					continue;

				//If there are less asset units in the input sequence than in the output sequence, the transaction is considered invalid and all outputs are uncolored. 
				if(previousAssetQueue.Count == 0)
				{
					colored.Transfers.Clear();
					colored.Issuances.Clear();
					repo.Put(txId, colored);
					return colored;
				}
				entry.Asset.Id = previousAssetQueue.Peek().Asset.Id;
				var remaining = entry.Asset.Quantity;
				while(remaining != 0)
				{
					if(previousAssetQueue.Count == 0 || previousAssetQueue.Peek().Asset.Id != entry.Asset.Id)
					{
						colored.Transfers.Clear();
						colored.Issuances.Clear();
						repo.Put(txId, colored);
						return colored;
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

		ColorMarker _Marker;
		public ColorMarker Marker
		{
			get
			{
				return _Marker;
			}
			set
			{
				_Marker = value;
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
			if(stream.Serializing)
			{
				if(_Marker != null)
					stream.ReadWrite(ref _Marker);
				else
					stream.ReadWrite(new Script());
			}
			else
			{
				Script script = new Script();
				stream.ReadWrite(ref script);
				if(script.Length != 0)
				{
					_Marker = new ColorMarker(script);
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

		public override string ToString()
		{
			return ToString(Network.Main);
		}

		public string ToString(Network network)
		{
			JObject obj = new JObject();
			var inputs = new JArray();
			obj.Add(new JProperty("inputs", inputs));
			foreach(var input in Inputs)
			{
				WriteEntry(network, inputs, input);
			}

			var issuances = new JArray();
			obj.Add(new JProperty("issuances", issuances));
			foreach(var issuance in Issuances)
			{
				WriteEntry(network, issuances, issuance);
			}

			var transfers = new JArray();
			obj.Add(new JProperty("transfers", transfers));
			foreach(var transfer in Transfers)
			{
				WriteEntry(network, transfers, transfer);
			}

			var destructions = new JArray();
			obj.Add(new JProperty("destructions", destructions));
			foreach(var destuction in GetDestroyedAssets())
			{
				JProperty asset = new JProperty("asset", destuction.Id.GetWif(network).ToString());
				JProperty quantity = new JProperty("quantity", destuction.Quantity);
				inputs.Add(new JObject(asset, quantity));
			}

			return obj.ToString(Newtonsoft.Json.Formatting.Indented);
		}

		private static void WriteEntry(Network network, JArray inputs, ColoredEntry entry)
		{
			JProperty index = new JProperty("index", entry.Index);
			JProperty asset = new JProperty("asset", entry.Asset.Id.GetWif(network).ToString());
			JProperty quantity = new JProperty("quantity", entry.Asset.Quantity);
			inputs.Add(new JObject(index, asset, quantity));
		}

		//00000000000000001c7a19e8ef62d815d84a473f543de77f23b8342fc26812a9 at 299220 Monday, May 5, 2014 3:47:37 PM first block
		public static readonly DateTimeOffset FirstColoredDate = new DateTimeOffset(2014, 05, 4, 0, 0, 0, TimeSpan.Zero);
	}
}
