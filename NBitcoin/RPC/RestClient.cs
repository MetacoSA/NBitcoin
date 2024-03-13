#if !NOJSONNET
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin.Protocol;
using NBitcoin.Protocol.Payloads;
using Newtonsoft.Json.Linq;
using System.Runtime.ExceptionServices;
using System.Net.Http;

namespace NBitcoin.RPC
{
	public enum RestResponseFormat
	{
		Bin,
		Hex,
		Json
	}

	/// <summary>
	/// Client class for the unauthenticated REST Interface
	/// </summary>
	public class RestClient : IBlockRepository
	{
		private readonly Uri _address;
		private readonly Network _network;


		/// <summary>
		/// Gets the <see cref="Network"/> instance for the client.
		/// </summary>
		public Network Network
		{
			get
			{
				return _network;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RestClient"/> class.
		/// </summary>
		/// <param name="address">The rest API endpoint</param>
		/// <exception cref="System.ArgumentNullException">Null rest API endpoint</exception>
		/// <exception cref="System.ArgumentException">Invalid value for RestResponseFormat</exception>
		public RestClient(Uri address)
			: this(address, Network.Main)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RestClient"/> class.
		/// </summary>
		/// <param name="address">The rest API endpoint</param>
		/// <param name="network">The network to operate with</param>
		/// <exception cref="System.ArgumentNullException">Null rest API endpoint</exception>
		/// <exception cref="System.ArgumentException">Invalid value for RestResponseFormat</exception>
		public RestClient(Uri address, Network network)
		{
			if (address == null)
				throw new ArgumentNullException(nameof(address));
			if (network == null)
				throw new ArgumentNullException(nameof(network));
			_address = address;
			_network = network;
		}

		/// <summary>
		/// Gets the block.
		/// </summary>
		/// <param name="blockId">The block identifier.</param>
		/// <returns>Given a block hash (id) returns the requested block object.</returns>
		/// <exception cref="System.ArgumentNullException">blockId cannot be null.</exception>
		public async Task<Block> GetBlockAsync(uint256 blockId, CancellationToken cancellationToken = default)
		{
			if (blockId == null)
				throw new ArgumentNullException(nameof(blockId));

			var result = await SendRequestAsync("block", RestResponseFormat.Bin, blockId.ToString()).ConfigureAwait(false);
			return Block.Load(result, Network);
		}
		/// <summary>
		/// Gets the block.
		/// </summary>
		/// <param name="blockId">The block identifier.</param>
		/// <returns>Given a block hash (id) returns the requested block object.</returns>
		/// <exception cref="System.ArgumentNullException">blockId cannot be null.</exception>
		public Block GetBlock(uint256 blockId)
		{
			return GetBlockAsync(blockId).GetAwaiter().GetResult();
		}

		/// <summary>
		/// Gets a transaction.
		/// </summary>
		/// <param name="txId">The transaction identifier.</param>
		/// <returns>Given a transaction hash (id) returns the requested transaction object.</returns>
		/// <exception cref="System.ArgumentNullException">txId cannot be null</exception>
		public async Task<Transaction> GetTransactionAsync(uint256 txId)
		{
			if (txId == null)
				throw new ArgumentNullException(nameof(txId));

			var result = await SendRequestAsync("tx", RestResponseFormat.Bin, txId.ToString()).ConfigureAwait(false);

			var tx = Network.Consensus.ConsensusFactory.CreateTransaction();
			tx.ReadWrite(result, Network);
			return tx;
		}
		/// <summary>
		/// Gets a transaction.
		/// </summary>
		/// <param name="txId">The transaction identifier.</param>
		/// <returns>Given a transaction hash (id) returns the requested transaction object.</returns>
		/// <exception cref="System.ArgumentNullException">txId cannot be null</exception>
		public Transaction GetTransaction(uint256 txId)
		{
			return GetTransactionAsync(txId).GetAwaiter().GetResult();
		}

		/// <summary>
		/// Gets blocks headers.
		/// </summary>
		/// <param name="blockId">The initial block identifier.</param>
		/// <param name="count">how many headers to get.</param>
		/// <returns>Given a block hash (blockId) returns as much block headers as specified.</returns>
		/// <exception cref="System.ArgumentNullException">blockId cannot be null</exception>
		/// <exception cref="System.ArgumentOutOfRangeException">count must be greater or equal to one.</exception>
		public async Task<IEnumerable<BlockHeader>> GetBlockHeadersAsync(uint256 blockId, int count)
		{
			if (blockId == null)
				throw new ArgumentNullException(nameof(blockId));
			if (count < 1)
				throw new ArgumentOutOfRangeException("count", "count must be greater or equal to one.");

			var result = await SendRequestAsync("headers", RestResponseFormat.Bin, count.ToString(CultureInfo.InvariantCulture), blockId.ToString()).ConfigureAwait(false);
			const int hexSize = (BlockHeader.Size);
			return Enumerable
				.Range(0, result.Length / hexSize)
				.Select(i => new BlockHeader(result.SafeSubarray(i * hexSize, hexSize), Network));
		}

		/// <summary>
		/// Gets blocks headers.
		/// </summary>
		/// <param name="blockId">The initial block identifier.</param>
		/// <param name="count">how many headers to get.</param>
		/// <returns>Given a block hash (blockId) returns as much block headers as specified.</returns>
		/// <exception cref="System.ArgumentNullException">blockId cannot be null</exception>
		/// <exception cref="System.ArgumentOutOfRangeException">count must be greater or equal to one.</exception>
		public IEnumerable<BlockHeader> GetBlockHeaders(uint256 blockId, int count)
		{
			return GetBlockHeadersAsync(blockId, count).GetAwaiter().GetResult();
		}

		/// <summary>
		/// Gets the chain information.
		/// </summary>
		/// <returns></returns>
		public async Task<ChainInfo> GetChainInfoAsync()
		{
			var result = await SendRequestAsync("chaininfo", RestResponseFormat.Json).ConfigureAwait(false);
			var o = JObject.Parse(Encoding.UTF8.GetString(result, 0, result.Length));
			return new ChainInfo
			{
				Chain = (string)o["chain"],
				BestBlockHash = uint256.Parse((string)o["bestblockhash"]),
				Blocks = (int)o["blocks"],
				ChainWork = uint256.Parse((string)o["chainwork"]),
				Difficulty = (int)o["difficulty"],
				Headers = (int)o["headers"],
				VerificationProgress = (decimal)o["verificationprogress"],
				IsPruned = (bool)o["pruned"]
			};
		}

		/// <summary>
		/// Gets unspect outputs.
		/// </summary>
		/// <param name="outPoints">The out points identifiers (TxIn-N).</param>
		/// <param name="checkMempool">if set to <c>true</c> [check mempool].</param>
		/// <returns>The unspent transaction outputs (UTXO) for the given outPoints.</returns>
		/// <exception cref="System.ArgumentNullException">outPoints cannot be null.</exception>
		public async Task<UTxOutputs> GetUnspentOutputsAsync(IEnumerable<OutPoint> outPoints, bool checkMempool)
		{
			if (outPoints == null)
				throw new ArgumentNullException(nameof(outPoints));
			var ids = from op in outPoints
					  select op.ToString();
			var result = await SendRequestAsync("getutxos" + (checkMempool ? "/checkmempool" : ""), RestResponseFormat.Bin, ids.ToArray()).ConfigureAwait(false);
			var mem = new MemoryStream(result);

			var utxos = new UTxOutputs();
			var stream = new BitcoinStream(mem, false);
			stream.ReadWrite(utxos);
			return utxos;
		}

		public async Task<byte[]> SendRequestAsync(string resource, RestResponseFormat format, params string[] parms)
		{
			var request = BuildHttpRequest(resource, format, parms);
			using (var response = await GetWebResponse(request).ConfigureAwait(false))
			{
				var buffer = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
				return buffer;
			}
		}

		#region Private methods
		private static Lazy<HttpClient> _Shared = new Lazy<HttpClient>(() => new HttpClient() { Timeout = System.Threading.Timeout.InfiniteTimeSpan });
		HttpClient _HttpClient;
		public HttpClient HttpClient
		{
			get
			{
				return _HttpClient ?? _Shared.Value;
			}
			set
			{
				_HttpClient = value;
			}
		}
		private HttpRequestMessage BuildHttpRequest(string resource, RestResponseFormat format, params string[] parms)
		{
			var hasParams = parms != null && parms.Length > 0;
			var uriBuilder = new UriBuilder(_address);
			uriBuilder.Path = "rest/" + resource + (hasParams ? "/" : "") + string.Join("/", parms) + "." + format.ToString().ToLowerInvariant();
			return new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);
		}

		private bool IsPlainText(HttpResponseMessage httpResponse)
		{
			return httpResponse.Content?.Headers?.ContentType?.MediaType?.Equals("text/plain", StringComparison.Ordinal) is true;
		}

		private async Task<HttpResponseMessage> GetWebResponse(HttpRequestMessage request)
		{
			var response = await HttpClient.SendAsync(request).ConfigureAwait(false);
			if (!response.IsSuccessStatusCode)
			{
				using (response)
				{
					if (!IsPlainText(response))
						response.EnsureSuccessStatusCode();
					var details = (await response.Content.ReadAsStringAsync().ConfigureAwait(false)).Trim();
					throw new RestApiException(details, response.StatusCode);
				}
			}
			return response;
		}
		#endregion
	}

	public class RestApiException : Exception
	{
		public RestApiException(string message, HttpStatusCode httpStatusCode)
			: base(message)
		{
			HttpStatusCode = httpStatusCode;
		}
		public HttpStatusCode HttpStatusCode { get; }
	}

	public class ChainInfo
	{
		public string Chain
		{
			get;
			internal set;
		}
		public int Blocks
		{
			get;
			internal set;
		}
		public int Headers
		{
			get;
			internal set;
		}
		public uint256 BestBlockHash
		{
			get;
			internal set;
		}
		public int Difficulty
		{
			get;
			internal set;
		}
		public decimal VerificationProgress
		{
			get;
			internal set;
		}
		public uint256 ChainWork
		{
			get;
			internal set;
		}
		public bool IsPruned
		{
			get;
			internal set;
		}
	}
}
#endif
