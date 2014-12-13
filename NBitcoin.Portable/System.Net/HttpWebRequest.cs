using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Net
{
	public static class HttpWebRequestExtensions
	{
		public static Task<WebResponse> GetResponseAsync(this WebRequest request)
		{
			var begin = new Func<AsyncCallback, object, IAsyncResult>(request.BeginGetResponse);
			var end = new Func<IAsyncResult, WebResponse>(request.EndGetResponse);
			return Task.Factory.FromAsync(begin, end, null);
		}

		public static Task<Stream> GetRequestStreamAsync(this WebRequest request)
		{
			var begin = new Func<AsyncCallback,object,IAsyncResult>(request.BeginGetRequestStream);
			var end = new Func<IAsyncResult,Stream>(request.EndGetRequestStream);
			return Task.Factory.FromAsync(begin, end, null);
		}
	}
}
