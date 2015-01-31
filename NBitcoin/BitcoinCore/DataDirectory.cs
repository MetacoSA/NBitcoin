#if !NOFILEIO
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.BitcoinCore
{
	public class DataDirectory
	{
		private readonly string _Folder;
		public string Folder
		{
			get
			{
				return _Folder;
			}
		}

		private readonly Network _Network;
		public Network Network
		{
			get
			{
				return _Network;
			}
		}
		public DataDirectory(string dataFolder, Network network)
		{
			EnsureExist(dataFolder);
			this._Folder = dataFolder;
			this._Network = network;
		}

		private void EnsureExist(string folder)
		{
			if(!Directory.Exists(folder))
				Directory.CreateDirectory(folder);
		}

		public PersistantChain GetChain()
		{
			var path = Path.Combine(Folder, "chainstate");
			EnsureExist(path);
			return new PersistantChain(Network, new StreamObjectStream<ChainChange>(File.Open(Path.Combine(path, "chainchanges"), FileMode.OpenOrCreate)));
		}

		public IndexedBlockUndoStore GetIndexedBlockUndoStore()
		{
			var path = Path.Combine(Folder, "blocks");
			EnsureExist(path);
			return new IndexedBlockUndoStore(new SQLiteNoSqlRepository(Path.Combine(path, "undoindex")),
										 new BlockUndoStore(path, Network));
		}

		public IndexedBlockStore GetIndexedBlockStore()
		{
			var path = Path.Combine(Folder, "blocks");
			EnsureExist(path);
			return new IndexedBlockStore(new SQLiteNoSqlRepository(Path.Combine(path, "blockindex")), 
										 new BlockStore(path, Network));
		}

		public CoinsView GetCoinsView()
		{
			var path = Path.Combine(Folder, "coins");
			EnsureExist(path);
			return new CoinsView(new SQLiteNoSqlRepository(Path.Combine(path, "coinsIndex"))); 
		}
	}
}
#endif