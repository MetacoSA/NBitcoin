using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Scanning
{
	public class FolderScanStatePersister : ScanStatePersister
	{
		
		private readonly DirectoryInfo _Folder;
		public DirectoryInfo Folder
		{
			get
			{
				return _Folder;
			}
		}
		public FolderScanStatePersister(string folder)
		{
			if(!Directory.Exists(folder))
				Directory.CreateDirectory(folder);
			_Folder = new DirectoryInfo(folder);
		}
		protected override ObjectStream<T> CreateObjectStream<T>()
		{
			var filePath = Path.Combine(Folder.FullName, typeof(T).Name);
			var fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
			return new StreamObjectStream<T>(fs);
		}
	}
}
