﻿using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class BitcoinScriptAddress : BitcoinAddress
	{

		public BitcoinScriptAddress(string address, Network network)
			: base(address, network)
		{
		}

		public BitcoinScriptAddress(ScriptId scriptId, Network network)
			: base(scriptId, network)
		{
		}

		public override TxDestination ID
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
			return new PayToScriptHashTemplate().GenerateScriptPubKey((ScriptId)ID);
		}
	}
	public class BitcoinAddress : Base58Data
	{
		public BitcoinAddress(string base58, Network network)
			: base(base58, network)
		{
		}

		public BitcoinAddress(byte[] rawBytes, Network network)
			: base(rawBytes, network)
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

		public virtual TxDestination ID
		{
			get
			{
				return new KeyId(vchData);
			}
		}

		Script _PaymentScript;
		public Script PaymentScript
		{
			get
			{
				if(_PaymentScript == null)
				{
					_PaymentScript = GeneratePaymentScript();
				}
				return _PaymentScript;
			}
		}

		protected virtual Script GeneratePaymentScript()
		{
			return new PayToPubkeyHashTemplate().GenerateScriptPubKey((KeyId)this.ID);
		}

		public bool VerifyMessage(string message, string signature)
		{
			var key = PubKey.RecoverFromMessage(message, signature);
			return key.ID == ID;
		}

		public override Base58Type Type
		{
			get
			{
				return Base58Type.PUBKEY_ADDRESS;
			}
		}
	}
}
