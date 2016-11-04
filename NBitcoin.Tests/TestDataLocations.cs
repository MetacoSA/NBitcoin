using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Tests
{
	using System.Net;

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
	}
}
