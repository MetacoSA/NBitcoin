#if NOWEBCLIENT
using System;
using System.IO;
using System.Net.Http;

namespace NBitcoin.Tests
{
	public class WebClient
	{
		public void DownloadFile(string url, string file)
		{
			HttpClient client = new HttpClient();

			// The default value is 100,000 milliseconds (100 seconds) and
			// that's not long enough to download Bitcoin on slower connections.
			client.Timeout = TimeSpan.FromMinutes(5);

			var bytes = client.GetByteArrayAsync(url).GetAwaiter().GetResult();
			File.WriteAllBytes(file, bytes);
		}
	}
}
#endif