using NBitcoin.DataEncoders;
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
				var encoder = expectedNetwork.GetBech32Encoder(Bech32Type.WITNESS_PUBKEY_ADDRESS, false);
				if(encoder == null)
					continue;
				try
				{
					byte witVersion;
					var data = encoder.Decode(bech32, out witVersion);
					if(data.Length == 20 && witVersion == 0)
					{
						return bech32;
					}
				}
				catch(FormatException) { continue; }
			}
			throw new FormatException("Invalid BitcoinWitPubKeyAddress");
		}

		public BitcoinWitPubKeyAddress(WitKeyId segwitKeyId, Network network):
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

	//public class BitcoinWitScriptAddress : BitcoinAddress
	//{
	//	public BitcoinWitScriptAddress(string base58, Network expectedNetwork = null)
	//: base(base58, expectedNetwork)
	//	{
	//	}

	//	public BitcoinWitScriptAddress(WitScriptId segwitKeyId, Network network)
	//: base(new[] { (byte)OpcodeType.OP_0, (byte)0x00 }.Concat(segwitKeyId.ToBytes(true)).ToArray(), network)
	//	{
	//	}


	//	public WitScriptId Hash
	//	{
	//		get
	//		{
	//			return new WitScriptId(vchData.SafeSubarray(2, 32));
	//		}
	//	}

	//	protected override bool IsValid
	//	{
	//		get
	//		{
	//			return vchData.Length == 1 + 1 + 32 && vchData[1] == 0 && PayToWitTemplate.ValidSegwitVersion(vchData[0]);
	//		}
	//	}

	//	protected override Script GeneratePaymentScript()
	//	{
	//		return PayToWitTemplate.Instance.GenerateScriptPubKey((OpcodeType)vchData[0], vchData.SafeSubarray(2, 32));
	//	}

	//	public override Base58Type Type
	//	{
	//		get
	//		{
	//			return Base58Type.WITNESS_P2WSH;
	//		}
	//	}
	//}
}
