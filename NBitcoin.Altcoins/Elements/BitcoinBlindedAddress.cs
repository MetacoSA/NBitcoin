using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Altcoins.Elements
{
	public class BitcoinBlindedAddress : BitcoinAddress
	{
		public BitcoinBlindedAddress(string base58, Network network)
			: base(base58, network)
		{
			var prefix = network.GetVersionBytes(Base58Type.BLINDED_ADDRESS, true);
			var vchData = Encoders.Base58Check.DecodeData(base58);
			var version = network.GetVersionBytes(Base58Type.PUBKEY_ADDRESS, false);
			bool p2pkh = true;
			if (version == null || !StartWith(prefix.Length, vchData, version))
			{
				p2pkh = false;
				version = network.GetVersionBytes(Base58Type.SCRIPT_ADDRESS, false);
				if (version == null || !StartWith(prefix.Length, vchData, version))
				{
					throw new FormatException("Invalid Bitcoin Blinded Address");
				}
			}

			if (vchData.Length != prefix.Length + version.Length + 33 + 20)
				throw new FormatException("Invalid Bitcoin Blinded Address");
			var blinding = vchData.SafeSubarray(prefix.Length + version.Length, 33);
			if (PubKey.Check(blinding, true))
			{
				_BlindingKey = new PubKey(blinding);

				var hash = vchData.SafeSubarray(prefix.Length + version.Length + 33, 20);
				_UnblindedAddress =
					p2pkh ? (BitcoinAddress)new BitcoinPubKeyAddress(new KeyId(hash), network)
						  : new BitcoinScriptAddress(new ScriptId(hash), network);
			}
			else
			{
				throw new FormatException("Invalid Bitcoin Blinded Address");
			}
		}

		public BitcoinBlindedAddress(PubKey blindingKey, BitcoinAddress address)
			: base(GetBase58(blindingKey, address), address.Network)
		{
			_BlindingKey = blindingKey;
			_UnblindedAddress = address;
		}

		private static string GetBase58(PubKey blindingKey, BitcoinAddress address)
		{
			if (address == null)
				throw new ArgumentNullException(nameof(address));
			if (blindingKey == null)
				throw new ArgumentNullException(nameof(blindingKey));

			if (address is BitcoinBlindedAddress ba)
				address = ba.UnblindedAddress;
			if(!(address is IBase58Data))
				throw new ArgumentException("Unsupported address");

			var network = address.Network;
			var keyId = address.ScriptPubKey.GetDestination();
			if (keyId == null)
				throw new ArgumentException("The passed address can't be reduced to a hash");
			var bytes = address.Network.GetVersionBytes(Base58Type.BLINDED_ADDRESS, true).Concat(network.GetVersionBytes(((IBase58Data)address).Type, true), blindingKey.ToBytes(), keyId.ToBytes());
			return Encoders.Base58Check.EncodeData(bytes);
		}

		private static bool StartWith(int aoffset, byte[] a, byte[] b)
		{
			if (a.Length - aoffset < b.Length)
				return false;
			for (int i = 0; i < b.Length; i++)
			{
				if (a[aoffset + i] != b[i])
					return false;
			}
			return true;
		}

		BitcoinAddress _UnblindedAddress;
		public BitcoinAddress UnblindedAddress
		{
			get
			{
				return _UnblindedAddress;
			}
		}

		PubKey _BlindingKey;
		public PubKey BlindingKey
		{
			get
			{
				return _BlindingKey;
			}
		}

		protected override Script GeneratePaymentScript()
		{
			return _UnblindedAddress.ScriptPubKey;
		}
	}
}