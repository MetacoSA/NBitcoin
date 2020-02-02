#nullable enable
using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class BitcoinWitPubKeyAddress : BitcoinAddress, IBech32Data, IPubkeyHashUsable
	{
		public BitcoinWitPubKeyAddress(string bech32, Network expectedNetwork)
				: base(Validate(bech32, expectedNetwork), expectedNetwork)
		{
			if (expectedNetwork.GetBech32Encoder(Bech32Type.WITNESS_PUBKEY_ADDRESS, false) is Bech32Encoder encoder)
			{
				byte witVersion;
				var decoded = encoder.Decode(bech32, out witVersion);
				_Hash = new WitKeyId(decoded);
			}
			else
				throw expectedNetwork.Bech32NotSupported(Bech32Type.WITNESS_PUBKEY_ADDRESS);
		}

		private static string Validate(string bech32, Network expectedNetwork)
		{
			if (bech32 == null)
				throw new ArgumentNullException(nameof(bech32));
			if (expectedNetwork == null)
				throw new ArgumentNullException(nameof(expectedNetwork));

			if (expectedNetwork.GetBech32Encoder(Bech32Type.WITNESS_PUBKEY_ADDRESS, false) is Bech32Encoder encoder)
			{
				try
				{
					byte witVersion;
					var data = encoder.Decode(bech32, out witVersion);
					if (data.Length == 20 && witVersion == 0)
					{
						return bech32;
					}
				}
				catch (Bech32FormatException) { throw; }
				catch (FormatException) { }
			}
			throw new FormatException("Invalid BitcoinWitPubKeyAddress");
		}

		public BitcoinWitPubKeyAddress(WitKeyId segwitKeyId, Network network) :
			base(NotNull(segwitKeyId) ?? Network.CreateBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, segwitKeyId.ToBytes(), 0, network), network)
		{
			_Hash = segwitKeyId;
		}

		private static string? NotNull(WitKeyId segwitKeyId)
		{
			if (segwitKeyId == null)
				throw new ArgumentNullException(nameof(segwitKeyId));
			return null;
		}

		public bool VerifyMessage(string message, string signature)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));
			if (signature == null)
				throw new ArgumentNullException(nameof(signature));
			var key = PubKey.RecoverFromMessage(message, signature);
			return key.WitHash == Hash;
		}

		public bool VerifyMessage(byte[] message, byte[] signature)
		{
			var key = PubKey.RecoverFromMessage(message, signature);
			return key.WitHash == Hash;
		}

		WitKeyId _Hash;
		public WitKeyId Hash
		{
			get
			{
				return _Hash;
			}
		}


		protected override Script GeneratePaymentScript()
		{
			return PayToWitTemplate.Instance.GenerateScriptPubKey(OpcodeType.OP_0, Hash._DestBytes);
		}

		public Bech32Type Type
		{
			get
			{
				return Bech32Type.WITNESS_PUBKEY_ADDRESS;
			}
		}
	}

	public class BitcoinWitScriptAddress : BitcoinAddress, IBech32Data, IPubkeyHashUsable
	{
		public BitcoinWitScriptAddress(string bech32, Network expectedNetwork)
				: base(Validate(bech32, expectedNetwork), expectedNetwork)
		{
			if (expectedNetwork.GetBech32Encoder(Bech32Type.WITNESS_SCRIPT_ADDRESS, false) is Bech32Encoder encoder)
			{
				byte witVersion;
				var decoded = encoder.Decode(bech32, out witVersion);
				_Hash = new WitScriptId(decoded);
			}
			else
				throw expectedNetwork.Bech32NotSupported(Bech32Type.WITNESS_SCRIPT_ADDRESS);
		}

		private static string Validate(string bech32, Network expectedNetwork)
		{
			if (bech32 == null)
				throw new ArgumentNullException(nameof(bech32));
			if (expectedNetwork == null)
				throw new ArgumentNullException(nameof(expectedNetwork));

			if (expectedNetwork.GetBech32Encoder(Bech32Type.WITNESS_SCRIPT_ADDRESS, false) is Bech32Encoder encoder)
			{
				try
				{
					byte witVersion;
					var data = encoder.Decode(bech32, out witVersion);
					if (data.Length == 32 && witVersion == 0)
					{
						return bech32;
					}
				}
				catch (Bech32FormatException) { throw; }
				catch (FormatException) { }
			}
			throw new FormatException("Invalid BitcoinWitScriptAddress");
		}

		public BitcoinWitScriptAddress(WitScriptId segwitScriptId, Network network)
			: base(NotNull(segwitScriptId) ?? Network.CreateBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, segwitScriptId.ToBytes(), 0, network), network)
		{
			_Hash = segwitScriptId;
		}


		private static string? NotNull(WitScriptId segwitScriptId)
		{
			if (segwitScriptId == null)
				throw new ArgumentNullException(nameof(segwitScriptId));
			return null;
		}

		public bool VerifyMessage(string message, string signature)
		{
			var key = PubKey.RecoverFromMessage(message, signature);
			return key.WitHash.ScriptPubKey.WitHash == Hash;
		}

		public bool VerifyMessage(byte[] message, byte[] signature)
		{
			var key = PubKey.RecoverFromMessage(message, signature);
			return key.WitHash.ScriptPubKey.WitHash == Hash;
		}


		WitScriptId _Hash;
		public WitScriptId Hash
		{
			get
			{
				return _Hash;
			}
		}

		protected override Script GeneratePaymentScript()
		{
			return PayToWitTemplate.Instance.GenerateScriptPubKey(OpcodeType.OP_0, Hash._DestBytes);
		}

		public Bech32Type Type
		{
			get
			{
				return Bech32Type.WITNESS_SCRIPT_ADDRESS;
			}
		}
	}
}
#nullable disable
