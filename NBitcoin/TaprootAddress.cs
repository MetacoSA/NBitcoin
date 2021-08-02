#nullable enable
using NBitcoin.DataEncoders;
#if HAS_SPAN
using NBitcoin.Secp256k1;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class TaprootAddress : BitcoinAddress, IBech32Data
	{
		public static new TaprootAddress Create(string bech32, Network expectedNetwork)
		{
			if (bech32 is null)
				throw new ArgumentNullException(nameof(bech32));
			if (expectedNetwork is null)
				throw new ArgumentNullException(nameof(expectedNetwork));
			bech32 = bech32.ToLowerInvariant();
			if (expectedNetwork.GetBech32Encoder(Bech32Type.TAPROOT_ADDRESS, false) is Bech32Encoder encoder)
			{
				var decoded = encoder.Decode(bech32, out var v);
#if HAS_SPAN
				if (v == 1 && ECXOnlyPubKey.TryCreate(decoded, out var k))
					return new TaprootAddress(bech32, new TaprootPubKey(k), expectedNetwork);
				else
					throw new FormatException("Invalid TaprootAddress");
#else
				if (v == 1 && decoded.Length == 32)
					return new TaprootAddress(bech32, new TaprootPubKey(decoded), expectedNetwork);
				else
					throw new FormatException("Invalid TaprootAddress");
#endif
			}
			else
				throw expectedNetwork.Bech32NotSupported(Bech32Type.TAPROOT_ADDRESS);
		}
		internal TaprootAddress(string str, TaprootPubKey key, Network network) : base(str, network)
		{
			_PubKey = key;
		}
		internal TaprootAddress(string str, byte[] key, Network network) : base(str, network)
		{
#if HAS_SPAN
			if (ECXOnlyPubKey.TryCreate(key, out var k))
				_PubKey = new TaprootPubKey(k);
			else
				throw new FormatException("Invalid TaprootAddress");
#else
			_PubKey = new TaprootPubKey(key);
#endif
		}

		public TaprootAddress(TaprootPubKey pubKey, Network network) :
			base(NotNull(pubKey) ?? Network.CreateBech32(Bech32Type.TAPROOT_ADDRESS, pubKey.ToBytes(), 1, network), network)
		{
			_PubKey = pubKey;
		}

		private static string? NotNull(TaprootPubKey? pubKey)
		{
			if (pubKey is null)
				throw new ArgumentNullException(nameof(pubKey));
			return null;
		}

		TaprootPubKey _PubKey;
		public TaprootPubKey PubKey
		{
			get
			{
				return _PubKey;
			}
		}


		protected override Script GeneratePaymentScript()
		{
			return _PubKey.ScriptPubKey;
		}

		public Bech32Type Type
		{
			get
			{
				return Bech32Type.TAPROOT_ADDRESS;
			}
		}
	}
}
#nullable disable
