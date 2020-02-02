#if !NOHTTPCLIENT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	[Obsolete()]
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
			if (network == null)
				throw new ArgumentNullException(nameof(network));
			_BaseUri = new Uri("http://" + (network == Network.Main ? "" : "t") + "api.qbit.ninja/");
			_Network = network;
		}


		private readonly Network _Network;
		public Network Network
		{
			get
			{
				return _Network;
			}
		}

		public QBitNinjaTransactionRepository(Uri baseUri, Network network)
			: this(baseUri?.AbsoluteUri, network)
		{

		}

		public QBitNinjaTransactionRepository(string baseUri, Network network)
		{
			if (baseUri == null)
				throw new ArgumentNullException(nameof(baseUri));
			if (network == null)
				throw new ArgumentNullException(nameof(network));
			if (!baseUri.EndsWith("/"))
				baseUri += "/";
			_BaseUri = new Uri(baseUri, UriKind.Absolute);
			_Network = network;
		}



		#region ITransactionRepository Members

		public async Task<Transaction> GetAsync(uint256 txId)
		{
			using (HttpClient client = new HttpClient())
			{
				var tx = await client.GetAsync(BaseUri.AbsoluteUri + "transactions/" + txId + "?format=raw").ConfigureAwait(false);
				if (tx.StatusCode == System.Net.HttpStatusCode.NotFound)
					return null;
				tx.EnsureSuccessStatusCode();
				var bytes = await tx.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
				var txx = _Network.Consensus.ConsensusFactory.CreateTransaction();
				txx.ReadWrite(bytes, _Network);
				return txx;
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