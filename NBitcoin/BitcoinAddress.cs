using NBitcoin.BouncyCastle.Math;
using NBitcoin.BouncyCastle.Math.EC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

		public override TxDestination Hash
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
	/// Base58 representation of a pubkey hash and base class for the representation of a script hash
	/// </summary>
	public class BitcoinAddress : Base58Data, IDestination
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

		public BitcoinAddress(KeyId keyId, Network network)
			: base(keyId.ToBytes(), network)
		{
		}

		protected BitcoinAddress(TxDestination dest, Network network)
			: base(dest.ToBytes(), network)
		{
		}

		protected override bool IsValid
		{
			get
			{
				return vchData.Length <= 20;
			}
		}

		public virtual TxDestination Hash
		{
			get
			{
				return new KeyId(vchData);
			}
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


		public BitcoinScriptAddress GetScriptAddress()
		{
			if(this is BitcoinScriptAddress)
				return (BitcoinScriptAddress)this;
			var redeem = PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(this);
			return new BitcoinScriptAddress(redeem.Hash, Network);
		}



		protected virtual Script GeneratePaymentScript()
		{
			return PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey((KeyId)this.Hash);
		}

		public bool VerifyMessage(string message, string signature)
		{
			var key = PubKey.RecoverFromMessage(message, signature);
			return key.Hash == Hash;
		}

		public override Base58Type Type
		{
			get
			{
				return Base58Type.PUBKEY_ADDRESS;
			}
		}

		public static BitcoinAddress Create(TxDestination id, Network network)
		{
			if(id == null)
				throw new ArgumentNullException("id");
			if(network == null)
				throw new ArgumentNullException("network");
			if(id is KeyId)
				return new BitcoinAddress(id, network);
			else if(id is ScriptId)
				return new BitcoinScriptAddress((ScriptId)id, network);
			else
				throw new NotSupportedException();
		}

		public BitcoinColoredAddress ToColoredAddress()
		{
			return new BitcoinColoredAddress(this);
		}
	}
}
