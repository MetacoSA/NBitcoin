using NBitcoin.Crypto;
using Org.BouncyCastle.Math;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	

	public class StealthSpendKey
	{
		private readonly StealthPayment _Payment;
		public StealthPayment Payment
		{
			get
			{
				return _Payment;
			}
		}
		private readonly KeyId _ID;
		public KeyId ID
		{
			get
			{
				return _ID;
			}
		}
		public StealthSpendKey(KeyId id, StealthPayment payment)
		{
			_ID = id;
			_Payment = payment;
		}

		public BitcoinAddress GetAddress(Network network)
		{
			return new BitcoinAddress(ID, network);
		}
	}

	public class StealthPayment
	{
		public StealthPayment(int sigCount, PubKey[] spendPubKeys, Key privateKey, PubKey publicKey, StealthMetadata metadata)
		{
			Metadata = metadata;
			if(sigCount == 1 && spendPubKeys.Length == 1)
			{
				var template = new PayToPubkeyHashTemplate();
				SpendableScript = template.GenerateScriptPubKey(spendPubKeys[0].Uncover(privateKey, publicKey).ID);
			}
			else
			{
				var template = new PayToMultiSigTemplate();
				SpendableScript = template.GenerateScriptPubKey(sigCount, spendPubKeys.Select(p => p.Uncover(privateKey, publicKey)).ToArray());
			}
			ParseSpendable();
		}





		public StealthSpendKey[] SpendKeys
		{
			get;
			private set;
		}
		public BitcoinAddress[] GetAddresses(Network network)
		{
			return SpendKeys.Select(k => k.GetAddress(network)).ToArray();
		}

		public StealthPayment(Script spendable, StealthMetadata metadata)
		{
			Metadata = metadata;
			SpendableScript = spendable;
			ParseSpendable();
		}

		private void ParseSpendable()
		{
			List<KeyId> pubkeys = new List<KeyId>();

			var payToHash = new PayToPubkeyHashTemplate();
			var keyId = payToHash.ExtractScriptPubKeyParameters(SpendableScript);
			if(keyId != null)
			{
				SpendKeys = new StealthSpendKey[] { new StealthSpendKey(keyId, this) };
			}
			else
			{
				var payToMultiSig = new PayToMultiSigTemplate();
				var para = payToMultiSig.ExtractScriptPubKeyParameters(SpendableScript);
				if(para == null)
					throw new ArgumentException("Invalid stealth spendable output script", "spendable");
				SpendKeys = para.PubKeys.Select(k => new StealthSpendKey(k.ID, this)).ToArray();
			}
		}

		public StealthMetadata Metadata
		{
			get;
			private set;
		}
		public Script SpendableScript
		{
			get;
			private set;
		}

		public void AddToTransaction(Transaction transaction, Money value)
		{
			transaction.Outputs.Add(new TxOut(0, Metadata.Script));
			transaction.Outputs.Add(new TxOut(value,SpendableScript));
		}
	}
}
