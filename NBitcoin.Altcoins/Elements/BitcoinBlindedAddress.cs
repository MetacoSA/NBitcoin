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
			var version = network.GetVersionBytes(Base58Type.PUBKEY_ADDRESS, false);
			var blech32 = network.GetBech32Encoder(Bech32Type.BLINDED_ADDRESS, false);

			if (blech32 != null && NBitcoin.DataEncoders.Encoders.ASCII
				    .DecodeData(base58.Substring(0, blech32.HumanReadablePart.Length))
				    .SequenceEqual(blech32.HumanReadablePart))
			{
					var vchData = blech32.Decode(base58, out var witnessVerion);
					bool p2pkh = !(version == null || !StartWith(prefix.Length, vchData, version));
					var script = false;
					var blinding = vchData.SafeSubarray(0, 33);
					byte[] hash;
					if (vchData.Length == 53)
					{

						hash = vchData.SafeSubarray(version.Length + 32, 20);
					}
					else
					{
						hash = vchData.SafeSubarray(version.Length + 32, vchData.Length - version.Length - 32);
						script = true;
					}
					if (PubKey.Check(blinding, true))
					{
						_BlindingKey = new PubKey(blinding);
						if (witnessVerion == 0)
						{
							_UnblindedAddress =script?  (BitcoinAddress) new BitcoinWitScriptAddress(new WitScriptId(hash), network):  new BitcoinWitPubKeyAddress(new WitKeyId(hash), network);
						}
						else if (witnessVerion > 16 || hash.Length < 2 || hash.Length > 40)
						{
							throw new FormatException("Invalid Bitcoin Blinded Address");
						}
					}
					else
					{
						throw new FormatException("Invalid Bitcoin Blinded Address");
					}

			}
			else
			{

				var vchData = NBitcoin.DataEncoders.Encoders.Base58Check.DecodeData(base58);
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
						p2pkh
							? (BitcoinAddress) new BitcoinPubKeyAddress(new KeyId(hash), network)
							: new BitcoinScriptAddress(new ScriptId(hash), network);
				}
				else
				{
					throw new FormatException("Invalid Bitcoin Blinded Address");
				}
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
			if (!(address is IBase58Data))
			{
				byte witVer;
				byte[] witProg;
				var blech32Encoder = address.Network.GetBech32Encoder(Bech32Type.BLINDED_ADDRESS, true);
				Bech32Encoder bech32Encoder = null;
				switch (address)
				{
					case BitcoinWitPubKeyAddress _:
						bech32Encoder= address.Network.GetBech32Encoder(Bech32Type.WITNESS_PUBKEY_ADDRESS, true);
						break;
					case BitcoinWitScriptAddress _:
						bech32Encoder= address.Network.GetBech32Encoder(Bech32Type.WITNESS_SCRIPT_ADDRESS, true);
						break;
					default:
							throw new ArgumentException($"no bech32 encoder for {address.GetType()} found");
				}

				witProg = bech32Encoder.Decode(address.ToString(), out witVer);
				return blech32Encoder.Encode(witVer, blindingKey.ToBytes().Concat(witProg));
			}
			else
			{


				var network = address.Network;
				var keyId = address.ScriptPubKey.GetDestination();
				if (keyId == null)
					throw new ArgumentException("The passed address can't be reduced to a hash");
				var bytes = address.Network.GetVersionBytes(Base58Type.BLINDED_ADDRESS, true).Concat(
					network.GetVersionBytes(((IBase58Data) address).Type, true), blindingKey.ToBytes(),
					keyId.ToBytes());
				return NBitcoin.DataEncoders.Encoders.Base58Check.EncodeData(bytes);
			}
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
