using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NBitcoin.Protocol;
using NBitcoin.Protocol.Payloads;
using Newtonsoft.Json.Linq;
using System.Runtime.ExceptionServices;

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
		private readonly RestResponseFormat _format;

		/// <summary>
		/// Initializes a new instance of the <see cref="RestClient"/> class.
		/// </summary>
		/// <param name="serviceEndpoint">The rest API endpoint.</param>
		public RestClient(Uri serviceEndpoint)
			: this(serviceEndpoint, RestResponseFormat.Bin)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RestClient"/> class.
		/// </summary>
		/// <param name="address">The rest API endpoint</param>
		/// <param name="format">The format (bin | hex | json).</param>
		/// <exception cref="System.ArgumentNullException">Null rest API endpoint</exception>
		/// <exception cref="System.ArgumentException">Invalid value for RestResponseFormat</exception>
		private RestClient(Uri address, RestResponseFormat format)
		{
			if(address == null)
				throw new ArgumentNullException("address");

			var typeOfRestResponseFormat = typeof(RestResponseFormat);
			if(!Enum.IsDefined(typeOfRestResponseFormat, format))
			{
				throw new ArgumentException("Invalid value for RestResponseFormat");
			}

			_address = address;
			_format = format;
		}


		/// <summary>
		/// Gets the block.
		/// </summary>
		/// <param name="blockId">The block identifier.</param>
		/// <returns>Given a block hash (id) returns the requested block object.</returns>
		/// <exception cref="System.ArgumentNullException">blockId cannot be null.</exception>
		public async Task<Block> GetBlockAsync(uint256 blockId)
		{
			if(blockId == null)
				throw new ArgumentNullException("blockId");

			var result = await SendRequestAsync("block", _format, blockId.ToString()).ConfigureAwait(false);
			return new Block(result);
		}
		/// <summary>
		/// Gets the block.
		/// </summary>
		/// <param name="blockId">The block identifier.</param>
		/// <returns>Given a block hash (id) returns the requested block object.</returns>
		/// <exception cref="System.ArgumentNullException">blockId cannot be null.</exception>
		public Block GetBlock(uint256 blockId)
		{
			try
			{
				return GetBlockAsync(blockId).Result;
			}
			catch(AggregateException aex)
			{
				ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
				throw;
			}
		}

		/// <summary>
		/// Gets a transaction.
		/// </summary>
		/// <param name="txId">The transaction identifier.</param>
		/// <returns>Given a transaction hash (id) returns the requested transaction object.</returns>
		/// <exception cref="System.ArgumentNullException">txId cannot be null</exception>
		public async Task<Transaction> GetTransactionAsync(uint256 txId)
		{
			if(txId == null)
				throw new ArgumentNullException("txId");

			var result = await SendRequestAsync("tx", _format, txId.ToString()).ConfigureAwait(false);
			return new Transaction(result);
		}
		/// <summary>
		/// Gets a transaction.
		/// </summary>
		/// <param name="txId">The transaction identifier.</param>
		/// <returns>Given a transaction hash (id) returns the requested transaction object.</returns>
		/// <exception cref="System.ArgumentNullException">txId cannot be null</exception>
		public Transaction GetTransaction(uint256 txId)
		{
			try
			{
				return GetTransactionAsync(txId).Result;
			}
			catch(AggregateException ex)
			{
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				throw;
			}
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
			if(blockId == null)
				throw new ArgumentNullException("blockId");
			if(count < 1)
				throw new ArgumentOutOfRangeException("count", "count must be greater or equal to one.");

			var result = await SendRequestAsync("headers", _format, count.ToString(CultureInfo.InvariantCulture), blockId.ToString()).ConfigureAwait(false);
			const int hexSize = (BlockHeader.Size);
			return Enumerable
				.Range(0, result.Length / hexSize)
				.Select(i => new BlockHeader(result.SafeSubarray(i * hexSize, hexSize)));
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
			try
			{
				return GetBlockHeadersAsync(blockId, count).Result;
			}
			catch(AggregateException aex)
			{
				ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
				throw;
			}
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
			if(outPoints == null)
				throw new ArgumentNullException("outPoints");
			var ids = from op in outPoints
					  select op.ToString();
			var result = await SendRequestAsync("getutxos" + (checkMempool ? "/checkmempool" : ""), _format, ids.ToArray()).ConfigureAwait(false);
			var mem = new MemoryStream(result);

			var utxos = new UTxOutputs();
			var stream = new BitcoinStream(mem, false);
			stream.ReadWrite(utxos);
			return utxos;
		}

		#region Private methods
		private async Task<byte[]> SendRequestAsync(string resource, RestResponseFormat format, params string[] parms)
		{
			var request = BuildHttpRequest(resource, format, parms);
			using(var response = await GetWebResponse(request).ConfigureAwait(false))
			{
				var stream = response.GetResponseStream();
				var bytesToRead = (int)response.ContentLength;
				var buffer = stream.ReadBytes(bytesToRead);
				return buffer;
			}
		}

		private WebRequest BuildHttpRequest(string resource, RestResponseFormat format, params string[] parms)
		{
			var hasParams = parms != null && parms.Length > 0;
			var uriBuilder = new UriBuilder(_address);
			uriBuilder.Path = "rest/" + resource + (hasParams ? "/" : "") + string.Join("/", parms) + "." + format.ToString().ToLowerInvariant();

			var request = WebRequest.CreateHttp(uriBuilder.Uri);
			request.Method = "GET";
#if !(PORTABLE || NETCORE)
			request.KeepAlive = false;
#endif
			return request;
		}

		private static async Task<WebResponse> GetWebResponse(WebRequest request)
		{
			WebResponse response = null;
			try
			{
				response = await request.GetResponseAsync().ConfigureAwait(false);
			}
			catch(WebException ex)
			{
				// "WebException status: {0}", ex.Status);

				// Even if the request "failed" we need to continue reading the response from the router
				response = ex.Response as HttpWebResponse;

				if(response == null)
					throw;

				var stream = response.GetResponseStream();
				var bytesToRead = (int)response.ContentLength;
				var buffer = stream.ReadBytes(bytesToRead);
				response.Dispose();
				throw new RestApiException(Encoding.UTF8.GetString(buffer, 0, buffer.Length - 2), ex);
			}
			return response;
		}
		#endregion
	}

	public class RestApiException : Exception
	{
		public RestApiException(string message, WebException inner)
			: base(message, inner)
		{
		}
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
