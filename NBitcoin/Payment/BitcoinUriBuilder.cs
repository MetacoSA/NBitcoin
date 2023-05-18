﻿#nullable enable
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
	public class BitcoinUriBuilder
	{
		public BitcoinUriBuilder(Network network)
		{
			Network = network;
			scheme = network.UriScheme;
		}
		public BitcoinUriBuilder(Uri uri, Network network)
			: this(uri.AbsoluteUri, network)
		{
			if (uri == null)
				throw new ArgumentNullException(nameof(uri));
		}

		public Network Network { get; }
		string scheme;

		public BitcoinUriBuilder(string uri, Network network)
		{
			if (uri == null)
				throw new ArgumentNullException(nameof(uri));
			if (network == null)
				throw new ArgumentNullException(nameof(network));
            
            var parsedUri = new Uri(uri, UriKind.Absolute);
            scheme =
                parsedUri.Scheme.Equals(network.UriScheme, StringComparison.OrdinalIgnoreCase) ? network.UriScheme :
                parsedUri.Scheme.Equals("bitcoin", StringComparison.OrdinalIgnoreCase) ? "bitcoin" :
                throw new FormatException("Invalid scheme");

			Network = network;

			if (parsedUri.AbsolutePath is { Length: > 0 } address)
            {
				Address = Network.Parse<BitcoinAddress>(address, network);
			}

			Dictionary<string, string> parameters;
			try
			{
                parameters = UriHelper.DecodeQueryParameters(parsedUri.GetComponents(UriComponents.Query, UriFormat.UriEscaped));
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
			_UnknownParameters = parameters;
			var reqParam = parameters.Keys.FirstOrDefault(k => k.StartsWith("req-", StringComparison.OrdinalIgnoreCase));
			if (reqParam != null)
				throw new FormatException("Non compatible required parameter " + reqParam);
		}

		private readonly Dictionary<string, string> _UnknownParameters = new Dictionary<string, string>();
		public IReadOnlyDictionary<string, string> UnknownParameters
		{
			get
			{
				return _UnknownParameters;
			}
		}
		[Obsolete("Use UnknownParameters property")]
		public Dictionary<string, string> UnknowParameters
		{
			get
			{
				return _UnknownParameters;
			}
		}

		public BitcoinAddress? Address
		{
			get;
			set;
		}
		public Money? Amount
		{
			get;
			set;
		}
		public string? Label
		{
			get;
			set;
		}
		public string? Message
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
				builder.Append($"{scheme}:");
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

				foreach (var kv in UnknownParameters)
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
