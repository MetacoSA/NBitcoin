using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NBitcoin.DataEncoders;
using Newtonsoft.Json.Linq;

namespace NBitcoin.REST
{
	public enum RestResponseFormat
	{
		Bin,
		Hex,
		Json
	}

	public class RestClient : IBlockRepository
	{
		private readonly Uri _address;
		private readonly RestResponseFormat _format;

		public RestClient(Uri address)
			: this(address, RestResponseFormat.Bin)
		{}

		public RestClient(Uri address, RestResponseFormat format)
		{
			if (address == null)
				throw new ArgumentNullException("address");

			var typeOfRestResponseFormat = typeof(RestResponseFormat);
			if (!Enum.IsDefined(typeOfRestResponseFormat, format))
			{
				throw new ArgumentException("Invalid value for RestResponseFormat");
			}

			_address = address;
			_format = format;
		}


		public async Task<Block> GetBlockAsync(uint256 blockId)
		{
			if (blockId == null) throw new ArgumentNullException("blockId");
			
			var result = await SendRequestAsync("block", _format, blockId.ToString());
			return new Block(result);
		}

		public async Task<Transaction> GetTransactionAsync(uint256 txId)
		{
			if (txId == null) throw new ArgumentNullException("txId");

			var result = await SendRequestAsync("tx", _format, txId.ToString());
			return new Transaction(result); 
		}

		public async Task<IEnumerable<BlockHeader>> GetBlockHeadersAsync(uint256 blockId, int count)
		{
			if (blockId == null) throw new ArgumentNullException("blockId");
			if(count < 1) throw new ArgumentOutOfRangeException("count", "count must be greater or equal to one.");

			var result = await SendRequestAsync("headers", _format, count.ToString(), blockId.ToString());
			const int hexSize = (BlockHeader.Size);
			return Enumerable
				.Range(0, result.Length / hexSize)
				.Select(i => new BlockHeader(result.SafeSubarray(i * hexSize, hexSize)));	
		}

		public async Task<ChainInfo> GetChainInfoAsync()
		{
			var result = await SendRequestAsync("chaininfo", RestResponseFormat.Json);
			var o = JObject.Parse(Encoding.UTF8.GetString(result));
			return new ChainInfo {
				Chain = (string) o["chain"],
				BestBlockHash = uint256.Parse((string) o["bestblockhash"]),
				Blocks = (int) o["blocks"],
				ChainWork = uint256.Parse((string) o["chainwork"]),
				Difficulty = (int) o["difficulty"],
				Headers = (int) o["headers"],
				VerificationProgress = (decimal) o["verificationprogress"],
				IsPruned = (bool) o["pruned"]
			};
		}

		public async Task<Unspent> GetUnspectOutputsAsync(IEnumerable<OutPoint> outPoints, bool checkMempool)
		{
			if (outPoints == null) throw new ArgumentNullException("outPoints");
			var ids = from op in outPoints select op.ToString();
			var result = await SendRequestAsync("getutxos" + (checkMempool ? "/checkmempool" : ""), _format, ids.ToArray() );
			var mem = new MemoryStream(result);
			var utxos = new Unspent(); 
			using (var reader = new BinaryReader(mem, Encoding.UTF8, true))
			{
				utxos.ChainHeight = reader.ReadInt32();
				utxos.ChainTipHash = new uint256 (reader.ReadBytes(32));
				utxos.Bitmap = new BitArray(reader.ReadBytes((outPoints.Count()/8) + 1));
			}
			var outCount = 0;
			for (var i = 0; i < utxos.Bitmap.Length; i++)
			{
				if(utxos.Bitmap[i]) outCount++;
			}
			var b = new BitcoinStream(mem, false);
			var outs = new List<TxOut>();
			for (int i = 0; i < outCount; i++)
			{
				var txOut = new TxOut();
				b.ReadWrite(ref txOut);
				outs.Add(txOut);
			}
			utxos.Outpust = outs.ToArray();
			return utxos;
		}

		private async Task<byte[]> SendRequestAsync(string resource, RestResponseFormat format, params string[] parms)
		{
			var request = BuildHttpRequest(resource, format, parms);
            using(var response = await GetWebResponse(request).ConfigureAwait(false))
            {
                var stream = response.GetResponseStream();
	            var memoryStream = new MemoryStream();
				stream.CopyTo(memoryStream);

                response.Close();
				return memoryStream.ToArray();
			}
		}

		private WebRequest BuildHttpRequest(string resource, RestResponseFormat format, params string[] parms)
		{
			var hasParams = parms != null && parms.Length > 0;
			var uriBuilder = new UriBuilder(_address);
			uriBuilder.Path = "rest/" + resource + (hasParams ? "/" : "") + string.Join("/", parms) + "." + format.ToString().ToLowerInvariant();

			var request = WebRequest.CreateHttp(uriBuilder.Uri);
			request.Method = "GET";
            request.KeepAlive = false;
			return request;
		}

		private static async Task<WebResponse> GetWebResponse(WebRequest request)
		{
			WebResponse response;
			try
			{
				response = await request.GetResponseAsync();
			}
			catch (WebException ex)
			{
				// "WebException status: {0}", ex.Status);

				// Even if the request "failed" we need to continue reading the response from the router
				response = ex.Response as HttpWebResponse;

				if (response == null)
					throw;
			}
			return response;
		}
	}

	public class Unspent
	{
		public int ChainHeight { get; internal set; }
		public uint256 ChainTipHash { get; internal set; }
		public BitArray Bitmap { get; internal set; }
		public TxOut[] Outpust { get; internal set; }
	}

	public class ChainInfo
	{
		public string Chain { get; internal set; }
		public int Blocks { get; internal set; }
		public int Headers { get; internal set; }
		public uint256 BestBlockHash { get; internal set; }
		public int Difficulty { get; internal set; }
		public decimal VerificationProgress { get; internal set; }
		public uint256 ChainWork { get; internal set; }
		public bool IsPruned { get; internal set; }
	}
}
