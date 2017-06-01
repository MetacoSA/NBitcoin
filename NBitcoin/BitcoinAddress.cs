using System;
using NBitcoin.OpenAsset;

namespace NBitcoin
{
	/// <summary>
	/// Base58 representaiton of a script hash
	/// </summary>
	public class BitcoinScriptAddress : BitcoinAddress
	{
		public BitcoinScriptAddress(string address, Network expectedNetwork)
			: base(address, expectedNetwork)
		{
		}

		public BitcoinScriptAddress(ScriptId scriptId, Network network)
			: base(scriptId, network)
		{
		}

		protected override bool IsValid
		{
			get
			{
				return vchData.Length == 20;
			}
		}

		public ScriptId Hash
		{
			get
			{
				return new ScriptId(vchData);
			}
		}

		public override Base58Type Type
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
	public abstract class BitcoinAddress : Base58Data, IDestination
	{
		/// <summary>
		/// Detect whether the input base58 is a pubkey hash or a script hash
		/// </summary>
		/// <param name="base58">The Base58 string to parse</param>
		/// <param name="expectedNetwork">The expected network to which it belongs</param>
		/// <returns>A BitcoinAddress or BitcoinScriptAddress</returns>
		/// <exception cref="System.FormatException">Invalid format</exception>
		public static BitcoinAddress Create(string base58, Network expectedNetwork = null)
		{
			if(base58 == null)
				throw new ArgumentNullException("base58");
			return Network.CreateFromBase58Data<BitcoinAddress>(base58, expectedNetwork);
		}

		public BitcoinAddress(string base58, Network expectedNetwork = null)
			: base(base58, expectedNetwork)
		{
		}

		public BitcoinAddress(TxDestination id, Network network)
			: base(id.ToBytes(), network)
		{
		}

		public BitcoinAddress(byte[] rawBytes, Network network)
			: base(rawBytes, network)
		{
		}

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
	}
}
