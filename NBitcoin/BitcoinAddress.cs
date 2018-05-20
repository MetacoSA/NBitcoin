using NBitcoin.DataEncoders;
using System;
using System.Linq;

namespace NBitcoin
{
	/// <summary>
	/// Base58 representaiton of a script hash
	/// </summary>
	public class BitcoinScriptAddress : BitcoinAddress, IBase58Data
	{
		public BitcoinScriptAddress(string base58, Network expectedNetwork)
			: base(Validate(base58, ref expectedNetwork), expectedNetwork)
		{
			var decoded = Encoders.Base58Check.DecodeData(base58);
			_Hash = new ScriptId(new uint160(decoded.Skip(expectedNetwork.GetVersionBytes(Base58Type.SCRIPT_ADDRESS, true).Length).ToArray()));
		}

		public BitcoinScriptAddress(string str, ScriptId id, Network expectedNetwork = null)
			: base(str, expectedNetwork)
		{
			if(id == null)
				throw new ArgumentNullException(nameof(id));
			_Hash = id;
		}

		private static string Validate(string base58, ref Network expectedNetwork)
		{
			if(base58 == null)
				throw new ArgumentNullException(nameof(base58));
			var networks = expectedNetwork == null ? Network.GetNetworks() : new[] { expectedNetwork };
			var data = Encoders.Base58Check.DecodeData(base58);
			foreach(var network in networks)
			{
				var versionBytes = network.GetVersionBytes(Base58Type.SCRIPT_ADDRESS, false);
				if(versionBytes != null && data.StartWith(versionBytes))
				{
					if(data.Length == versionBytes.Length + 20)
					{
						expectedNetwork = network;
						return base58;
					}
				}
			}
			throw new FormatException("Invalid BitcoinScriptAddress");
		}

		public BitcoinScriptAddress(ScriptId scriptId, Network network)
			: base(NotNull(scriptId) ?? Network.CreateBase58(Base58Type.SCRIPT_ADDRESS, scriptId.ToBytes(), network), network)
		{
			_Hash = scriptId;
		}

		private static string NotNull(ScriptId scriptId)
		{
			if(scriptId == null)
				throw new ArgumentNullException(nameof(scriptId));
			return null;
		}

		ScriptId _Hash;
		public ScriptId Hash
		{
			get
			{
				return _Hash;
			}
		}

		public Base58Type Type
		{
			get
			{
				return Base58Type.SCRIPT_ADDRESS;
			}
		}

		protected override Script GeneratePaymentScript()
		{
			return PayToScriptHashTemplate.Instance.GenerateScriptPubKey((ScriptId)Hash);
		}
	}

	/// <summary>
	/// Base58 representation of a bitcoin address
	/// </summary>
	public abstract class BitcoinAddress : IDestination, IBitcoinString
	{
		/// <summary>
		/// Detect whether the input base58 is a pubkey hash or a script hash
		/// </summary>
		/// <param name="str">The string to parse</param>
		/// <param name="expectedNetwork">The expected network to which it belongs</param>
		/// <returns>A BitcoinAddress or BitcoinScriptAddress</returns>
		/// <exception cref="System.FormatException">Invalid format</exception>
		public static BitcoinAddress Create(string str, Network expectedNetwork)
		{
			if(str == null)
				throw new ArgumentNullException(nameof(str));
			return Network.Parse<BitcoinAddress>(str, expectedNetwork);
		}

		/// <summary>
		/// Detect whether the input base58 is a pubkey hash or a script hash
		/// </summary>
		/// <param name="str">The string to parse</param>
		/// <returns>A BitcoinAddress or BitcoinScriptAddress</returns>
		/// <exception cref="System.FormatException">Invalid format</exception>
		[Obsolete("Use BitcoinCreate(string, Network) instead")]
		public static BitcoinAddress Create(string str)
		{
			if(str == null)
				throw new ArgumentNullException(nameof(str));
			return Network.Parse<BitcoinAddress>(str, null);
		}

		internal protected BitcoinAddress(string str, Network network)
		{
			if(network == null)
				throw new ArgumentNullException(nameof(network));
			if(str == null)
				throw new ArgumentNullException(nameof(str));
			_Str = str;
			_Network = network;
		}

		string _Str;		

		Script _ScriptPubKey;
		public Script ScriptPubKey
		{
			get
			{
				if(_ScriptPubKey == null)
				{
					_ScriptPubKey = GeneratePaymentScript();
				}
				return _ScriptPubKey;
			}
		}

		protected abstract Script GeneratePaymentScript();

		public BitcoinScriptAddress GetScriptAddress()
		{
			var bitcoinScriptAddress = this as BitcoinScriptAddress;
			if(bitcoinScriptAddress != null)
				return bitcoinScriptAddress;

			return new BitcoinScriptAddress(this.ScriptPubKey.Hash, Network);
		}

		public BitcoinColoredAddress ToColoredAddress()
		{
			return new BitcoinColoredAddress(this);
		}


		private readonly Network _Network;
		public Network Network
		{
			get
			{
				return _Network;
			}
		}

		public override string ToString()
		{
			return _Str;
		}


		public override bool Equals(object obj)
		{
			BitcoinAddress item = obj as BitcoinAddress;
			if(item == null)
				return false;
			return _Str.Equals(item._Str);
		}
		public static bool operator ==(BitcoinAddress a, BitcoinAddress b)
		{
			if(System.Object.ReferenceEquals(a, b))
				return true;
			if(((object)a == null) || ((object)b == null))
				return false;
			return a._Str == b._Str;
		}

		public static bool operator !=(BitcoinAddress a, BitcoinAddress b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return _Str.GetHashCode();
		}
	}
}
