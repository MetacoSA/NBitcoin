namespace NBitcoin
{
	/// <summary>
	/// Base58 representation of a pubkey hash and base class for the representation of a script hash
	/// </summary>
	public class BitcoinPubKeyAddress : BitcoinAddress
	{
		public BitcoinPubKeyAddress(string base58, Network expectedNetwork = null)
			: base(base58, expectedNetwork)
		{
		}

		public BitcoinPubKeyAddress(KeyId keyId, Network network)
			: base(keyId, network)
		{
		}

		protected override bool IsValid
		{
			get
			{
				return vchData.Length == 20;
			}
		}


		public bool VerifyMessage(string message, string signature)
		{
			var key = PubKey.RecoverFromMessage(message, signature);
			return key.Hash == Hash;
		}


		public KeyId Hash
		{
			get
			{
				return new KeyId(vchData);
			}
		}


		public override Base58Type Type
		{
			get
			{
				return Base58Type.PUBKEY_ADDRESS;
			}
		}

		protected override Script GeneratePaymentScript()
		{
			return PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey((KeyId)this.Hash);
		}
	}
}
