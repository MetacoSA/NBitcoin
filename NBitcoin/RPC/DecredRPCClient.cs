using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;


namespace NBitcoin.RPC
{
    public class DecredRPCClient : RPCClient
	{
		public DecredRPCClient(string authenticationString, Uri address, Network network = null) : base(authenticationString, address, network)
		{
			// TODO: Correctly handle self signed certificates.
			var handler = new HttpClientHandler();
			handler.ClientCertificateOptions = ClientCertificateOption.Manual;
			handler.ServerCertificateCustomValidationCallback =
			    (httpRequestMessage, cert, cetChain, policyErrors) =>
			{
			    return true;
			};
			var client = new HttpClient(handler);
			this.HttpClient = client;
		}

		public override RPCClient PrepareBatch()
		{
			var auth = $"{CredentialString.UserPassword.UserName}:{CredentialString.UserPassword.Password}";
			return new DecredRPCClient(auth, Address, Network)
			{
				BatchedRequests = new ConcurrentQueue<Tuple<RPCRequest, TaskCompletionSource<RPCResponse>>>(),
				Capabilities = Capabilities,
				HttpClient = HttpClient,
				AllowBatchFallback = AllowBatchFallback
			};
		}
		public override RPCClient Clone()
		{
			var auth = $"{CredentialString.UserPassword.UserName}:{CredentialString.UserPassword.Password}";
			return new DecredRPCClient(auth, Address, Network)
			{
				BatchedRequests = BatchedRequests,
				Capabilities = Capabilities,
				HttpClient = HttpClient,
				AllowBatchFallback = AllowBatchFallback
			};
		}

#nullable enable
		public override async Task<uint256> SendToAddressAsync(
			BitcoinAddress address,
			Money amount,
			SendToAddressParameters? parameters,
			CancellationToken cancellationToken = default
			)
		{
			if (address is null)
				throw new ArgumentNullException(nameof(address));
			if (amount is null)
				throw new ArgumentNullException(nameof(amount));

			// Maximum compatiblity
			if (parameters is null)
			{
				List<object> list = new List<object>();
				list.Add(address.ToString());
				// Dcrwallet will not accept this amount argument as a string.
				list.Add(amount.ToUnit(MoneyUnit.BTC));
				var resp = await SendCommandAsync(RPCOperations.sendtoaddress, cancellationToken, list.ToArray()).ConfigureAwait(false);
				return uint256.Parse(resp.Result.ToString());
			}
			else
			{
				var list = new Dictionary<string, object>();
				list.Add("address", address.ToString());
				list.Add("amount", amount.ToString());
				if (parameters.Comment is string)
					list.Add("comment", parameters.Comment);
				if (parameters.CommentTo is string)
					list.Add("comment_to", parameters.CommentTo);
				if (parameters.SubstractFeeFromAmount is bool v)
					list.Add("subtractfeefromamount", v);
				if (parameters.Replaceable is bool v1)
					list.Add("replaceable", v1);
				if (parameters.ConfTarget is int v2)
					list.Add("conf_target", v2);
				if (parameters.EstimateMode is EstimateSmartFeeMode v3)
					list.Add("estimate_mode", v3 == EstimateSmartFeeMode.Conservative ? "conservative" : "economical");
				if (parameters.FeeRate is FeeRate v4)
					list.Add("fee_rate", v4.SatoshiPerByte.ToString(CultureInfo.InvariantCulture));
				var resp = await SendCommandWithNamedArgsAsync("sendtoaddress", list, cancellationToken).ConfigureAwait(false);
				return uint256.Parse(resp.Result.ToString());
			}

		}

		public override async Task<RPCClient> CreateWalletAsync(string walletNameOrPath, CreateWalletOptions? options = null, CancellationToken cancellationToken = default)
		{
			// Decred node cannot create a wallet.
			if (walletNameOrPath is null)
				throw new ArgumentNullException(nameof(walletNameOrPath));

			int port = 0;
			if (options?.Port != null)
			{
				port = (int)options.Port;
			}

			return SetDecredWalletContext(walletNameOrPath, port);
		}

		public RPCClient SetDecredWalletContext(string? walletName, int port)
		{
			RPCCredentialString credentialString;;

			if (BatchedRequests is null)
			{
				credentialString = RPCCredentialString.Parse(CredentialString.ToString());
			}
			else
			{
				if (string.IsNullOrEmpty(CredentialString.WalletName))
				{
					credentialString = CredentialString;
				}
				else
				{
					throw new InvalidOperationException("Batch RPC client already has a wallet assigned.");
				}
			}
			credentialString.WalletName = walletName;

			var auth = $"{CredentialString.UserPassword.UserName}:{CredentialString.UserPassword.Password}";
			var builder = new UriBuilder(Address);
			builder.Port = port;
			return new DecredRPCClient(auth, builder.Uri, Network)
			{
				BatchedRequests = BatchedRequests,
				Capabilities = Capabilities,
				HttpClient = HttpClient,
				AllowBatchFallback = AllowBatchFallback
			};
		}

	}


}
