using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.RPC
{
	public class RPCCredentialString
	{
		public static RPCCredentialString Parse(string str)
		{
			RPCCredentialString r;
			if (!TryParse(str, out r))
				throw new FormatException("Invalid RPC Credential string");
			return r;
		}

		public static bool TryParse(string str, out RPCCredentialString connectionString)
		{
			connectionString = null;
			if (str == null)
				throw new ArgumentNullException(nameof(str));

			var parts = str.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
			string walletName = null;
			string server = null;
			foreach (var part in parts)
			{
				if (part == parts[parts.Length - 1])
				{
					TryParseAuth(part, out connectionString);
					break;
				}
				if (part.StartsWith("wallet=", StringComparison.OrdinalIgnoreCase))
				{
					walletName = part.Substring("wallet=".Length);
				}
				else if (part.StartsWith("server=", StringComparison.OrdinalIgnoreCase))
				{
					server = part.Substring("server=".Length);
				}
				else
					return false;
			}

			if (connectionString == null)
				return false;
			connectionString.WalletName = walletName;
			connectionString.Server = server;
			return true;
		}

		private static bool TryParseAuth(string str, out RPCCredentialString connectionString)
		{
			str = str.Trim();
			if (str.Equals("default", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(str))
			{
				connectionString = new RPCCredentialString();
				return true;
			}

			if (str.StartsWith("cookiefile=", StringComparison.OrdinalIgnoreCase))
			{
				var path = str.Substring("cookiefile=".Length);
				connectionString = new RPCCredentialString();
				connectionString.CookieFile = path;
				return true;
			}

			if (str.IndexOf(':') != -1)
			{
				var parts = str.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length >= 2)
				{
					parts[1] = string.Join(":", parts.Skip(1).ToArray());
					connectionString = new RPCCredentialString();
					connectionString.UserPassword = new NetworkCredential(parts[0], parts[1]);
					return true;
				}
			}

			connectionString = null;
			return false;
		}

		public string Server
		{
			get; set;
		}



		/// <summary>
		/// Use default connection settings of the chain
		/// </summary>
		public bool UseDefault
		{
			get
			{
				return CookieFile == null && UserPassword == null;
			}
		}

		/// <summary>
		/// Name of the wallet in multi wallet mode
		/// </summary>
		public string WalletName
		{
			get; set;
		}

		/// <summary>
		/// Path to cookie file
		/// </summary>
		public string CookieFile
		{
			get
			{
				return _CookieFile;
			}
			set
			{
				if (value != null)
					Reset();
				_CookieFile = value;
			}
		}

		private void Reset()
		{
			_CookieFile = null;
			_UsernamePassword = null;
		}

		string _CookieFile;

		/// <summary>
		/// Username and password
		/// </summary>
		public NetworkCredential UserPassword
		{
			get
			{
				return _UsernamePassword;
			}
			set
			{
				if (value != null)
					Reset();
				_UsernamePassword = value;
			}
		}
		NetworkCredential _UsernamePassword;

		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();
			if (!string.IsNullOrEmpty(WalletName))
			{
				builder.Append($"wallet={WalletName};");
			}
			if (!string.IsNullOrEmpty(Server))
			{
				builder.Append($"server={Server};");
			}
			var authPath = UseDefault ? "default" :
				   CookieFile != null ? ("cookiefile=" + CookieFile) :
				   UserPassword != null ? $"{UserPassword.UserName}:{UserPassword.Password}" :
				   "default";
			builder.Append(authPath);
			return builder.ToString();
		}
	}
}
