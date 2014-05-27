using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

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
		public BitcoinUrlBuilder(Uri uri)
			: this(uri.ToString())
		{

		}
		public BitcoinUrlBuilder(string uri)
		{
			if(!uri.StartsWith("bitcoin:", StringComparison.InvariantCultureIgnoreCase))
				throw new FormatException("Invalid scheme");
			uri = uri.Remove(0, "bitcoin:".Length);
			if(uri.StartsWith("//"))
				uri = uri.Remove(0, 2);

			var paramStart = uri.IndexOf('?');
			string address = null;
			if(paramStart == -1)
				address = uri;
			else
			{
				address = uri.Substring(0, paramStart);
				uri = uri.Remove(0, 1); //remove ?
			}
			if(address != String.Empty)
			{
				Address = Network.CreateFromBase58Data<BitcoinAddress>(address);
			}
			uri = uri.Remove(0, address.Length);

			var parameters = UriHelper.DecodeQueryParameters(uri);
			if(parameters.ContainsKey("amount"))
			{
				Amount = Money.Parse(parameters["amount"]);
			}
			if(parameters.ContainsKey("label"))
			{
				Label = parameters["label"];
			}
			if(parameters.ContainsKey("message"))
			{
				Message = parameters["message"];
			}
			if(parameters.ContainsKey("r"))
			{
				PaymentRequestUrl = new Uri(parameters["r"], UriKind.Absolute);
			}

			var reqParam = parameters.Keys.FirstOrDefault(k => k.StartsWith("req-", StringComparison.InvariantCultureIgnoreCase));
			if(reqParam != null)
				throw new FormatException("Non compatible required parameter " + reqParam);
		}

		/// <summary>
		/// https://github.com/bitcoin/bips/blob/master/bip-0072.mediawiki
		/// </summary>
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
				if(Address != null)
				{
					builder.Append(Address.ToString());
				}

				if(Amount != null)
				{
					parameters.Add("amount", Amount.ToString());
				}
				if(Label != null)
				{
					parameters.Add("label", Label.ToString());
				}
				if(Message != null)
				{
					parameters.Add("message", Message.ToString());
				}
				if(PaymentRequestUrl != null)
				{
					parameters.Add("r", PaymentRequestUrl.ToString());
				}

				WriteParameters(parameters, builder);

				return new System.Uri(builder.ToString(), UriKind.Absolute);
			}
		}

		private static void WriteParameters(Dictionary<string, string> parameters, StringBuilder builder)
		{
			bool first = true;
			foreach(var parameter in parameters)
			{
				if(first)
				{
					first = false;
					builder.Append("?");
				}
				else
					builder.Append("&");
				builder.Append(parameter.Key);
				builder.Append("=");
				builder.Append(HttpUtility.UrlEncode(parameter.Value));
			}
		}

		public override string ToString()
		{
			return Uri.ToString();
		}
	}
}
