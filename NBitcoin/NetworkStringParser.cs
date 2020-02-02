using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{

	/// <summary>
	/// This class provide a hook for additional string format in altcoin network
	/// </summary>
	public class NetworkStringParser
	{
		/// <summary>
		/// Try to parse a string
		/// </summary>
		/// <param name="str">The string to parse</param>
		/// <param name="result">The result</param>
		/// <returns>True if it was possible to parse the string</returns>
		public bool TryParse<T>(string str, Network network, out T result) where T : IBitcoinString
		{
			var returned = TryParse(str, network, typeof(T), out var result2);
			result = returned? (T) result2 : default;
			return returned;
		}

		public virtual bool TryParse(string str, Network network, Type targetType, out IBitcoinString result)
		{
			result = null;
			return false;
		}


		public virtual Base58CheckEncoder GetBase58CheckEncoder()
		{
			return (Base58CheckEncoder)Encoders.Base58Check;
		}

		public virtual BitcoinPubKeyAddress CreateP2PKH(KeyId keyId, Network network)
		{
			return new BitcoinPubKeyAddress(keyId, network);
		}

		public virtual BitcoinScriptAddress CreateP2SH(ScriptId scriptId, Network network)
		{
			return new BitcoinScriptAddress(scriptId, network);
		}

		public virtual BitcoinAddress CreateP2WPKH(WitKeyId witKeyId, Network network)
		{
			return new BitcoinWitPubKeyAddress(witKeyId, network);
		}

		public virtual BitcoinAddress CreateP2WSH(WitScriptId scriptId, Network network)
		{
			return new BitcoinWitScriptAddress(scriptId, network);
		}
	}
}
