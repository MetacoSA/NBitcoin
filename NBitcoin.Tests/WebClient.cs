#if NOWEBCLIENT
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Tests
{
	public class WebClient
	{
		public void DownloadFile(string url, string file)
		{
			HttpClient client = new HttpClient();
			var bytes = client.GetByteArrayAsync(url).GetAwaiter().GetResult();
			File.WriteAllBytes(file, bytes);
		}
	}
}
#endif