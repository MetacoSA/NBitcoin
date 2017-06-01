#if !NOHTTPCLIENT
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class QBitNinjaTransactionRepository : ITransactionRepository
	{
		private readonly Uri _BaseUri;
		public Uri BaseUri
		{
			get
			{
				return _BaseUri;
			}
		}

		/// <summary>
		/// Use qbitninja public servers
		/// </summary>
		/// <param name="network"></param>
		public QBitNinjaTransactionRepository(Network network)
		{
			if(network == null)
				throw new ArgumentNullException("network");
			_BaseUri = new Uri("http://" + (network == Network.Main ? "" : "t") + "api.qbit.ninja/");
		}

		public QBitNinjaTransactionRepository(Uri baseUri)
			: this(baseUri.AbsoluteUri)
		{

		}

		public QBitNinjaTransactionRepository(string baseUri)
		{
			if(!baseUri.EndsWith("/"))
				baseUri += "/";
			_BaseUri = new Uri(baseUri, UriKind.Absolute);
		}



		#region ITransactionRepository Members

		public async Task<Transaction> GetAsync(uint256 txId)
		{
			using(HttpClient client = new HttpClient())
			{
				var tx = await client.GetAsync(BaseUri.AbsoluteUri + "transactions/" + txId + "?format=raw").ConfigureAwait(false);
				if(tx.StatusCode == System.Net.HttpStatusCode.NotFound)
					return null;
				tx.EnsureSuccessStatusCode();
				var bytes = await tx.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
				return new Transaction(bytes);
			}
		}

		public Task PutAsync(uint256 txId, Transaction tx)
		{
			return Task.FromResult(false);
		}

		#endregion
	}
}
#endif