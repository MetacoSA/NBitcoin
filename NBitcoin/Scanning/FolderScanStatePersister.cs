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
		class FileObjectStream<T> : ObjectStream<T> where T : class, IBitcoinSerializable, new()
		{
			public FileObjectStream(FileStream fs)
			{
				if(fs == null)
					throw new ArgumentNullException("fs");
				_FileStream = fs;
			}
			private readonly FileStream _FileStream;
			public FileStream FileStream
			{
				get
				{
					return _FileStream;
				}
			}
			public override void Rewind()
			{
				_FileStream.Position = 0;
			}

			protected override void WriteNextCore(T obj)
			{
				obj.ReadWrite(_FileStream, true);
				_FileStream.Flush();
			}

			protected override T ReadNextCore()
			{
				if(EOF)
					return null;
				var obj = new T();
				obj.ReadWrite(_FileStream, false);
				return obj;
			}

			public override bool EOF
			{
				get
				{
					return _FileStream.Position == _FileStream.Length;
				}
			}

			internal void Close()
			{
				_FileStream.Close();
			}
		}
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
			return new FileObjectStream<T>(fs);
		}

		public override void Init(int startHeight)
		{
			var filePath = Path.Combine(Folder.FullName, "Height");
			if(File.Exists(filePath))
				throw new InvalidOperationException("Folder scan state persister already initialized");
			File.WriteAllText(filePath, startHeight.ToString());
		}

		public override int GetStartHeight()
		{
			var filePath = Path.Combine(Folder.FullName, "Height");
			if(!File.Exists(filePath))
				throw new InvalidOperationException("Folder scan state persister not initialized");
			return int.Parse(File.ReadAllText(filePath));
		}

		public override void CloseStream<T>(ObjectStream<T> stream)
		{
			((FileObjectStream<T>)stream).Close();
		}
	}
}
