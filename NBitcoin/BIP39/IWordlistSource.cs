using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public interface IWordlistSource
	{
		Task<Wordlist> Load(string name);
	}

	/// <summary>
	/// Fetch wordlist on github (ex : https://raw.githubusercontent.com/bitcoin/bips/master/bip-0039/japanese.txt)
	/// </summary>
	public class GithubWordlistSource : IWordlistSource
	{

		#region IWordlistSource Members

		public async Task<Wordlist> Load(string name)
		{
			HttpClient client = new HttpClient();
			var result = await client.GetAsync("https://raw.githubusercontent.com/bitcoin/bips/master/bip-0039/" + name + ".txt").ConfigureAwait(false);
			if(result.StatusCode == System.Net.HttpStatusCode.NotFound)
				return null;
			var list = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
			return new Wordlist(list.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries),
				name == "japanese" ? '　' : ' ', name
				);
		}

		#endregion
	}
}
