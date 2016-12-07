using System.IO;
using System.Net;

#if NOWEBCLIENT
using nStratis.Tests;
#endif
namespace nStratis.Tests
{
	public class TestDataLocations
	{
		public static string BlockFolderLocation
		{
			get
			{
				EnsureDownloaded(@"download\blocks\blk0001.dat", "https://onedrive.live.com/download.aspx?cid=3E5405DC8E6A9F4F&resid=3E5405DC8E6A9F4F%21120&canary=WEXg5NdVyhofKGNJlW0V0e8AbKxmTjJ1yP47KsA8hyU%3D8&ithint=%2Edat");
				return @"download\blocks";
			}
		}

		public static string Block0001Location
		{
			get
			{
				EnsureDownloaded(@"download\blocks\blk0001.dat", "https://onedrive.live.com/download.aspx?cid=3E5405DC8E6A9F4F&resid=3E5405DC8E6A9F4F%21120&canary=WEXg5NdVyhofKGNJlW0V0e8AbKxmTjJ1yP47KsA8hyU%3D8&ithint=%2Edat");
				return @"download\blocks\blk0001.dat";
			}
		}

		public static string BlockHeadersLocation
		{
			get
			{
				EnsureDownloaded(@"download\blocks\Headers.dat", "https://onedrive.live.com/download.aspx?cid=3E5405DC8E6A9F4F&resid=3E5405DC8E6A9F4F%21123&canary=WEXg5NdVyhofKGNJlW0V0e8AbKxmTjJ1yP47KsA8hyU%3D8&ithint=%2Edat");
				return @"download\blocks\Headers.dat";
			}
		}

		private static void EnsureDownloaded(string file, string url)
		{
			if (File.Exists(file))
				return;

			if (!Directory.Exists(Path.GetDirectoryName(file)))
				Directory.CreateDirectory(Path.GetDirectoryName(file));

			WebClient client = new WebClient();
			client.DownloadFile(url, file);
		}

		public static string DataBlockFolder(string file)
		{
			var p = Path.DirectorySeparatorChar;
			var folder = DataFolder("blocks");
			return $@"{folder}\{file}".Replace('\\', p);
		}

		public static string DataFolder(string file)
		{
			var p = Path.DirectorySeparatorChar;
			var current = Directory.GetCurrentDirectory();
			if(Directory.Exists($@"{current}\data".Replace('\\', p)))
			{
				return $@"{current}\data\{file}".Replace('\\', p);
			}

			if (Directory.Exists($@"{current}\bin\Debug\netcoreapp1.0\data".Replace('\\', p)))
			{
				return $@"{current}\bin\Debug\netcoreapp1.0\data\{file}".Replace('\\', p);
			}

			if (Directory.Exists($@"{current}\bin\Debug\netcoreapp1.1\data".Replace('\\', p)))
			{
				return $@"{current}\bin\Debug\netcoreapp1.1\data\{file}".Replace('\\', p);
			}

			throw new DirectoryNotFoundException();
		}
	}
}
