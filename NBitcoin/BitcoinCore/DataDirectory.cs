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
	}
}
#endif