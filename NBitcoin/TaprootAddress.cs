﻿#nullable enable
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
		public TaprootAddress(string bech32, Network expectedNetwork)
				: base(Validate(bech32, expectedNetwork), expectedNetwork)
		{
			if (expectedNetwork.GetBech32Encoder(Bech32Type.TAPROOT_ADDRESS, false) is Bech32Encoder encoder)
			{
				var decoded = encoder.Decode(bech32, out _);
#if HAS_SPAN
				if (ECXOnlyPubKey.TryCreate(decoded, out var k))
					_PubKey = new TaprootPubKey(k);
				else
					throw new FormatException("Invalid TaprootAddress");
#else
				_PubKey = new TaprootPubKey(decoded);
#endif
			}
			else
				throw expectedNetwork.Bech32NotSupported(Bech32Type.TAPROOT_ADDRESS);
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

		private static string Validate(string bech32, Network expectedNetwork)
		{
			if (bech32 == null)
				throw new ArgumentNullException(nameof(bech32));
			if (expectedNetwork == null)
				throw new ArgumentNullException(nameof(expectedNetwork));

			if (expectedNetwork.GetBech32Encoder(Bech32Type.TAPROOT_ADDRESS, false) is Bech32Encoder encoder)
			{
				try
				{
					byte witVersion;
					var data = encoder.Decode(bech32, out witVersion);
					if (data.Length == 32 && witVersion == 1)
					{
						return bech32;
					}
				}
				catch (Bech32FormatException) { throw; }
				catch (FormatException) { }
			}
			else
			{
				throw expectedNetwork.Bech32NotSupported(Bech32Type.TAPROOT_ADDRESS);
			}
			throw new FormatException("Invalid TaprootAddress");
		}

		public TaprootAddress(TaprootPubKey pubKey, Network network) :
			base(NotNull(pubKey) ?? Network.CreateBech32(Bech32Type.TAPROOT_ADDRESS, pubKey.ToBytes(), 1, network), network)
		{
			_PubKey = pubKey;
		}

		private static string? NotNull(TaprootPubKey pubKey)
		{
			if (pubKey == null)
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
