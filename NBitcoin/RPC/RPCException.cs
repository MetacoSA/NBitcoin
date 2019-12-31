#if !NOJSONNET
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.RPC
{
	/// <summary>
	/// RPC error code thrown by the <see cref="RPCClient"/>
	/// </summary>
	public enum RPCErrorCode
	{
		//! Standard JSON-RPC 2.0 errors

		/// <summary>
		/// RPC_INVALID_REQUEST is internally mapped to HTTP_BAD_REQUEST (400).
		/// It should not be used for application-layer errors. 
		/// </summary>
		RPC_INVALID_REQUEST = -32600,

		/// <summary>
		/// RPC_METHOD_NOT_FOUND is internally mapped to HTTP_NOT_FOUND (404).
		/// It should not be used for application-layer errors.
		/// </summary>
		RPC_METHOD_NOT_FOUND = -32601,
		RPC_INVALID_PARAMS = -32602,

		/// <summary>
		/// RPC_INTERNAL_ERROR should only be used for genuine errors in bitcoind
		/// (for example datadir corruption).
		/// </summary>
		RPC_INTERNAL_ERROR = -32603,
		RPC_PARSE_ERROR = -32700,

		//! General application defined errors

		/// <summary>
		/// std::exception thrown in command handling
		/// </summary>
		RPC_MISC_ERROR = -1,
		/// <summary>
		/// Server is in safe mode, and command is not allowed in safe mode
		/// </summary>
		RPC_FORBIDDEN_BY_SAFE_MODE = -2,
		/// <summary>
		/// Unexpected type was passed as parameter
		/// </summary>
		RPC_TYPE_ERROR = -3,

		/// <summary>
		/// Invalid address or key
		/// </summary>
		RPC_INVALID_ADDRESS_OR_KEY = -5,
		/// <summary>
		/// Ran out of memory during operation
		/// </summary>
		RPC_OUT_OF_MEMORY = -7,
		/// <summary>
		/// Invalid, missing or duplicate parameter
		/// </summary>
		RPC_INVALID_PARAMETER = -8,
		/// <summary>
		/// Database error
		/// </summary>
		RPC_DATABASE_ERROR = -20,
		/// <summary>
		/// Error parsing or validating structure in raw format
		/// </summary>
		RPC_DESERIALIZATION_ERROR = -22,
		/// <summary>
		/// General error during transaction or block submission
		/// </summary>
		RPC_VERIFY_ERROR = -25,
		/// <summary>
		/// Transaction or block was rejected by network rules
		/// </summary>
		RPC_VERIFY_REJECTED = -26,
		/// <summary>
		/// Transaction already in chain
		/// </summary>
		RPC_VERIFY_ALREADY_IN_CHAIN = -27,
		/// <summary>
		/// Client still warming up
		/// </summary>
		RPC_IN_WARMUP = -28,
		/// <summary>
		/// RPC method is deprecated
		/// </summary>
		RPC_METHOD_DEPRECATED = -32,

		//! Aliases for backward compatibility
		RPC_TRANSACTION_ERROR = RPC_VERIFY_ERROR,
		RPC_TRANSACTION_REJECTED = RPC_VERIFY_REJECTED,
		RPC_TRANSACTION_ALREADY_IN_CHAIN = RPC_VERIFY_ALREADY_IN_CHAIN,

		//! P2P client errors
		/// <summary>
		/// Bitcoin is not connected
		/// </summary>
		RPC_CLIENT_NOT_CONNECTED = -9,
		/// <summary>
		/// Still downloading initial blocks
		/// </summary>
		RPC_CLIENT_IN_INITIAL_DOWNLOAD = -10,
		/// <summary>
		/// Node is already added
		/// </summary>
		RPC_CLIENT_NODE_ALREADY_ADDED = -23,
		/// <summary>
		/// Node has not been added before
		/// </summary>
		RPC_CLIENT_NODE_NOT_ADDED = -24,
		/// <summary>
		/// Node to disconnect not found in connected nodes
		/// </summary>
		RPC_CLIENT_NODE_NOT_CONNECTED = -29,
		/// <summary>
		/// Invalid IP/Subnet
		/// </summary>
		RPC_CLIENT_INVALID_IP_OR_SUBNET = -30,
		/// <summary>
		/// No valid connection manager instance found
		/// </summary>
		RPC_CLIENT_P2P_DISABLED = -31,

		//! Wallet errors
		/// <summary>
		/// Unspecified problem with wallet (key not found etc.)
		/// </summary>
		RPC_WALLET_ERROR = -4,
		/// <summary>
		/// Not enough funds in wallet or account
		/// </summary>
		RPC_WALLET_INSUFFICIENT_FUNDS = -6,
		/// <summary>
		/// Invalid account name
		/// </summary>
		RPC_WALLET_INVALID_ACCOUNT_NAME = -11,
		/// <summary>
		/// Keypool ran out, call keypoolrefill first
		/// </summary>
		RPC_WALLET_KEYPOOL_RAN_OUT = -12,
		/// <summary>
		/// Enter the wallet passphrase with walletpassphrase first
		/// </summary>
		RPC_WALLET_UNLOCK_NEEDED = -13,
		/// <summary>
		/// The wallet passphrase entered was incorrect
		/// </summary>
		RPC_WALLET_PASSPHRASE_INCORRECT = -14,
		/// <summary>
		/// Command given in wrong wallet encryption state (encrypting an encrypted wallet etc.)
		/// </summary>
		RPC_WALLET_WRONG_ENC_STATE = -15,
		/// <summary>
		/// Failed to encrypt the wallet
		/// </summary>
		RPC_WALLET_ENCRYPTION_FAILED = -16,
		/// <summary>
		/// Wallet is already unlocked
		/// </summary>
		RPC_WALLET_ALREADY_UNLOCKED = -17,
		/// <summary>
		/// Invalid wallet specified
		/// </summary>
		RPC_WALLET_NOT_FOUND = -18,
		/// <summary>
		/// No wallet specified (error when there are multiple wallets loaded)
		/// </summary>
		RPC_WALLET_NOT_SPECIFIED = -19
	}



	public class RPCException : Exception
	{
		public RPCException(RPCErrorCode code, string message, RPCResponse result)
			: base(String.IsNullOrEmpty(message) ? FindMessage(code) : message)
		{
			_RPCCode = code;
			_RPCCodeMessage = FindMessage(code);
			_RPCResult = result;
		}

		private readonly RPCResponse _RPCResult;
		public RPCResponse RPCResult
		{
			get
			{
				return _RPCResult;
			}
		}

		private static string FindMessage(RPCErrorCode code)
		{
			switch (code)
			{
				case RPCErrorCode.RPC_MISC_ERROR:
					return "std::exception thrown in command handling";
				case RPCErrorCode.RPC_FORBIDDEN_BY_SAFE_MODE:
					return "Server is in safe mode, and command is not allowed in safe mode";
				case RPCErrorCode.RPC_TYPE_ERROR:
					return "Unexpected type was passed as parameter";
				case RPCErrorCode.RPC_INVALID_ADDRESS_OR_KEY:
					return "Invalid address or key";
				case RPCErrorCode.RPC_OUT_OF_MEMORY:
					return "Ran out of memory during operation";
				case RPCErrorCode.RPC_INVALID_PARAMETER:
					return "Invalid, missing or duplicate parameter";
				case RPCErrorCode.RPC_DATABASE_ERROR:
					return "Database error";
				case RPCErrorCode.RPC_DESERIALIZATION_ERROR:
					return "Error parsing or validating structure in raw format";
				case RPCErrorCode.RPC_TRANSACTION_ERROR:
					return "General error during transaction submission";
				case RPCErrorCode.RPC_TRANSACTION_REJECTED:
					return "Transaction was rejected by network rules";
				case RPCErrorCode.RPC_TRANSACTION_ALREADY_IN_CHAIN:
					return "Transaction already in chain";
				default:
					return code.ToString();
			}
		}

		private readonly RPCErrorCode _RPCCode;
		public RPCErrorCode RPCCode
		{
			get
			{
				return _RPCCode;
			}
		}

		private readonly string _RPCCodeMessage;
		public string RPCCodeMessage
		{
			get
			{
				return _RPCCodeMessage;
			}
		}
	}
}
#endif