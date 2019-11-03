#nullable enable
using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	/// <summary>
	/// Abstracts an object able to verify messages from which it is possible to extract public key.
	/// </summary>
	public interface IPubkeyHashUsable
	{
		bool VerifyMessage(string message, string signature);

		bool VerifyMessage(byte[] message, byte[] signature);
	}

	/// <summary>
	/// Base58 representation of a pubkey hash and base class for the representation of a script hash
	/// </summary>
	public class BitcoinPubKeyAddress : BitcoinAddress, IBase58Data, IPubkeyHashUsable
	{
		public BitcoinPubKeyAddress(string base58, Network expectedNetwork)
			: base(Validate(base58, expectedNetwork), expectedNetwork)
		{
			var decoded = expectedNetwork.NetworkStringParser.GetBase58CheckEncoder().DecodeData(base58);
			if (expectedNetwork.GetVersionBytes(Base58Type.PUBKEY_ADDRESS, false) is byte[] bytes)
				_KeyId = new KeyId(new uint160(decoded.Skip(bytes.Length).ToArray()));
			else
				throw expectedNetwork.Base58NotSupported(Base58Type.PUBKEY_ADDRESS);
		}

		public BitcoinPubKeyAddress(string str, KeyId id, Network expectedNetwork)
			: base(str, expectedNetwork)
		{
			if (id == null)
				throw new ArgumentNullException(nameof(id));
			_KeyId = id;
		}

		private static string Validate(string base58, Network expectedNetwork)
		{
			if (base58 == null)
				throw new ArgumentNullException(nameof(base58));
			if (expectedNetwork == null)
				throw new ArgumentNullException(nameof(expectedNetwork));
			var data = expectedNetwork.NetworkStringParser.GetBase58CheckEncoder().DecodeData(base58);
			var versionBytes = expectedNetwork.GetVersionBytes(Base58Type.PUBKEY_ADDRESS, false);
			if (versionBytes != null && data.StartWith(versionBytes))
			{
				if (data.Length == versionBytes.Length + 20)
				{
					return base58;
				}
			}
			throw new FormatException("Invalid BitcoinPubKeyAddress");
		}



		public BitcoinPubKeyAddress(KeyId keyId, Network network) :
			base(NotNull(keyId) ?? Network.CreateBase58(Base58Type.PUBKEY_ADDRESS, keyId.ToBytes(), network), network)
		{
			_KeyId = keyId;
		}

		private static string? NotNull(KeyId keyId)
		{
			if (keyId == null)
				throw new ArgumentNullException(nameof(keyId));
			return null;
		}

		public bool VerifyMessage(string message, string signature)
		{
			var key = PubKey.RecoverFromMessage(message, signature);
			return key.Hash == Hash;
		}

		public bool VerifyMessage(byte[] message, byte[] signature)
		{
			var key = PubKey.RecoverFromMessage(message, signature);
			return key.Hash == Hash;
		}

		KeyId _KeyId;
		public KeyId Hash
		{
			get
			{
				return _KeyId;
			}
		}


		public Base58Type Type
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
#nullable disable
