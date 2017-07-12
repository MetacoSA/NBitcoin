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

			// The default value is 100,000 milliseconds (100 seconds).
			// That's long enough to download Bitcoin.
			client.Timeout = TimeSpan.FromMinutes(5);

			// Changed .GetAwaiter().GetResult() to .Result
			// https://stackoverflow.com/questions/17284517/is-task-result-the-same-as-getawaiter-getresult
			var bytes = client.GetByteArrayAsync(url).Result;
			File.WriteAllBytes(file, bytes);
		}
	}
}
#endif