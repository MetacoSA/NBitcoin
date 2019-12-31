using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
#if !NOHTTPCLIENT
using System.Net.Http;
using System.Net.Http.Headers;
#endif
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.NBitcoin;
using System.Runtime.ExceptionServices;

namespace NBitcoin.Payment
{
	/// <summary>
	/// https://github.com/bitcoin/bips/blob/master/bip-0021.mediawiki
	/// </summary>
	public class BitcoinUrlBuilder
	{
		public BitcoinUrlBuilder()
		{

		}
		public BitcoinUrlBuilder(Uri uri, Network network)
			: this(uri.AbsoluteUri, network)
		{
			if (uri == null)
				throw new ArgumentNullException(nameof(uri));
		}

		public BitcoinUrlBuilder(string uri, Network network)
		{
			if (uri == null)
				throw new ArgumentNullException(nameof(uri));
			if (!uri.StartsWith("bitcoin:", StringComparison.OrdinalIgnoreCase))
				throw new FormatException("Invalid scheme");
			if (network == null)
				throw new ArgumentNullException(nameof(network));
			uri = uri.Remove(0, "bitcoin:".Length);
			if (uri.StartsWith("//"))
				uri = uri.Remove(0, 2);

			var paramStart = uri.IndexOf('?');
			string address = null;
			if (paramStart == -1)
				address = uri;
			else
			{
				address = uri.Substring(0, paramStart);
				uri = uri.Remove(0, 1); //remove ?
			}
			if (address != String.Empty)
			{
				Address = Network.Parse<BitcoinAddress>(address, network);
			}
			uri = uri.Remove(0, address.Length);

			Dictionary<string, string> parameters;
			try
			{
				parameters = UriHelper.DecodeQueryParameters(uri);
			}
			catch (ArgumentException)
			{
				throw new FormatException("A URI parameter is duplicated");
			}
			if (parameters.ContainsKey("amount"))
			{
				Amount = Money.Parse(parameters["amount"]);
				parameters.Remove("amount");
			}
			if (parameters.ContainsKey("label"))
			{
				Label = parameters["label"];
				parameters.Remove("label");
			}
			if (parameters.ContainsKey("message"))
			{
				Message = parameters["message"];
				parameters.Remove("message");
			}
#pragma warning disable CS0618 // Type or member is obsolete
			if (parameters.ContainsKey("r"))
			{
				PaymentRequestUrl = new Uri(parameters["r"], UriKind.Absolute);
				parameters.Remove("r");
			}
#pragma warning restore CS0618 // Type or member is obsolete
			_UnknowParameters = parameters;
			var reqParam = parameters.Keys.FirstOrDefault(k => k.StartsWith("req-", StringComparison.OrdinalIgnoreCase));
			if (reqParam != null)
				throw new FormatException("Non compatible required parameter " + reqParam);
		}

		private readonly Dictionary<string, string> _UnknowParameters = new Dictionary<string, string>();
		public Dictionary<string, string> UnknowParameters
		{
			get
			{
				return _UnknowParameters;
			}
		}
#if !NOHTTPCLIENT
		[Obsolete("BIP70 is obsolete")]
		public PaymentRequest GetPaymentRequest()
		{
			if (PaymentRequestUrl == null)
				throw new InvalidOperationException("No PaymentRequestUrl specified");

			return GetPaymentRequestAsync().GetAwaiter().GetResult();
		}
		[Obsolete("BIP70 is obsolete")]
		public async Task<PaymentRequest> GetPaymentRequestAsync(HttpClient httpClient = null)
		{
			if (PaymentRequestUrl == null)
				throw new InvalidOperationException("No PaymentRequestUrl specified");

			bool own = false;
			if (httpClient == null)
			{
				httpClient = new HttpClient();
				own = true;
			}
			try
			{
				HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, PaymentRequestUrl);
				req.Headers.Clear();
				req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(PaymentRequest.MediaType));

				var result = await httpClient.SendAsync(req).ConfigureAwait(false);
				if (!result.IsSuccessStatusCode)
					throw new WebException(result.StatusCode + "(" + (int)result.StatusCode + ")");
				if (result.Content.Headers.ContentType == null || !result.Content.Headers.ContentType.MediaType.Equals(PaymentRequest.MediaType, StringComparison.OrdinalIgnoreCase))
				{
					throw new WebException("Invalid contenttype received, expecting " + PaymentRequest.MediaType + ", but got " + result.Content.Headers.ContentType);
				}
				var stream = await result.Content.ReadAsStreamAsync().ConfigureAwait(false);
				return PaymentRequest.Load(stream);
			}
			finally
			{
				if (own)
					httpClient.Dispose();
			}
		}
#endif
		[Obsolete("BIP70 is obsolete")]
		public Uri PaymentRequestUrl
		{
			get;
			set;
		}

		public BitcoinAddress Address
		{
			get;
			set;
		}
		public Money Amount
		{
			get;
			set;
		}
		public string Label
		{
			get;
			set;
		}
		public string Message
		{
			get;
			set;
		}
		public Uri Uri
		{
			get
			{
				Dictionary<string, string> parameters = new Dictionary<string, string>();
				StringBuilder builder = new StringBuilder();
				builder.Append("bitcoin:");
				if (Address != null)
				{
					builder.Append(Address.ToString());
				}

				if (Amount != null)
				{
					parameters.Add("amount", Amount.ToString(false, true));
				}
				if (Label != null)
				{
					parameters.Add("label", Label.ToString());
				}
				if (Message != null)
				{
					parameters.Add("message", Message.ToString());
				}
#pragma warning disable CS0618 // Type or member is obsolete
				if (PaymentRequestUrl != null)
				{
					parameters.Add("r", PaymentRequestUrl.ToString());
				}
#pragma warning restore CS0618 // Type or member is obsolete

				foreach (var kv in UnknowParameters)
				{
					parameters.Add(kv.Key, kv.Value);
				}

				WriteParameters(parameters, builder);

				return new System.Uri(builder.ToString(), UriKind.Absolute);
			}
		}

		private static void WriteParameters(Dictionary<string, string> parameters, StringBuilder builder)
		{
			bool first = true;
			foreach (var parameter in parameters)
			{
				if (first)
				{
					first = false;
					builder.Append("?");
				}
				else
					builder.Append("&");
				builder.Append(parameter.Key);
				builder.Append("=");
				builder.Append(System.Web.NBitcoin.HttpUtility.UrlEncode(parameter.Value));
			}
		}

		public override string ToString()
		{
			return Uri.AbsoluteUri;
		}
	}
}
