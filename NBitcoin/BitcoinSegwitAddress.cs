﻿using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class BitcoinWitPubKeyAddress : BitcoinAddress, IBech32Data
	{
		public BitcoinWitPubKeyAddress(string bech32, Network expectedNetwork = null)
				: base(Validate(bech32, ref expectedNetwork), expectedNetwork)
		{
			var encoder = expectedNetwork.GetBech32Encoder(Bech32Type.WITNESS_PUBKEY_ADDRESS, true);
			byte witVersion;
			var decoded = encoder.Decode(bech32, out witVersion);
			_Hash = new WitKeyId(decoded);
		}

		private static string Validate(string bech32, ref Network expectedNetwork)
		{
			if(bech32 == null)
				throw new ArgumentNullException("bech32");
			var networks = expectedNetwork == null ? Network.GetNetworks() : new[] { expectedNetwork };
			foreach(var network in networks)
			{
				var encoder = network.GetBech32Encoder(Bech32Type.WITNESS_PUBKEY_ADDRESS, false);
				if(encoder == null)
					continue;
				try
				{
					byte witVersion;
					var data = encoder.Decode(bech32, out witVersion);
					if(data.Length == 20 && witVersion == 0)
					{
						expectedNetwork = network;
						return bech32;
					}
				}
				catch(Bech32FormatException) { throw; }
				catch(FormatException) { continue; }
			}
			throw new FormatException("Invalid BitcoinWitPubKeyAddress");
		}

		public BitcoinWitPubKeyAddress(WitKeyId segwitKeyId, Network network) :
			base(NotNull(segwitKeyId) ?? Network.CreateBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, segwitKeyId.ToBytes(), 0, network), network)
		{
			_Hash = segwitKeyId;
		}

		private static string NotNull(WitKeyId segwitKeyId)
		{
			if(segwitKeyId == null)
				throw new ArgumentNullException("segwitKeyId");
			return null;
		}

		public bool VerifyMessage(string message, string signature)
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

	public class BitcoinWitScriptAddress : BitcoinAddress, IBech32Data
	{
		public BitcoinWitScriptAddress(string bech32, Network expectedNetwork = null)
				: base(Validate(bech32, ref expectedNetwork), expectedNetwork)
		{
			var encoder = expectedNetwork.GetBech32Encoder(Bech32Type.WITNESS_SCRIPT_ADDRESS, true);
			byte witVersion;
			var decoded = encoder.Decode(bech32, out witVersion);
			_Hash = new WitScriptId(decoded);
		}

		private static string Validate(string bech32, ref Network expectedNetwork)
		{
			if(bech32 == null)
				throw new ArgumentNullException("bech32");
			var networks = expectedNetwork == null ? Network.GetNetworks() : new[] { expectedNetwork };
			foreach(var network in networks)
			{
				var encoder = network.GetBech32Encoder(Bech32Type.WITNESS_SCRIPT_ADDRESS, false);
				if(encoder == null)
					continue;
				try
				{
					byte witVersion;
					var data = encoder.Decode(bech32, out witVersion);
					if(data.Length == 32 && witVersion == 0)
					{
						expectedNetwork = network;
						return bech32;
					}
				}
				catch(Bech32FormatException) { throw; }
				catch(FormatException) { continue; }
			}
			throw new FormatException("Invalid BitcoinWitScriptAddress");
		}

		public BitcoinWitScriptAddress(WitScriptId segwitScriptId, Network network)
	: base(NotNull(segwitScriptId) ?? Network.CreateBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, segwitScriptId.ToBytes(), 0, network), network)
		{
			_Hash = segwitScriptId;
		}


		private static string NotNull(WitScriptId segwitScriptId)
		{
			if(segwitScriptId == null)
				throw new ArgumentNullException("segwitScriptId");
			return null;
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
