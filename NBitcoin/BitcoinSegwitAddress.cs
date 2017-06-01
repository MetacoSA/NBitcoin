using System.Linq;

namespace NBitcoin
{
	public class BitcoinWitPubKeyAddress : BitcoinAddress
	{

		public BitcoinWitPubKeyAddress(string base58, Network expectedNetwork = null)
			: base(base58, expectedNetwork)
		{
		}

		public BitcoinWitPubKeyAddress(WitKeyId segwitKeyId, Network network)
			: base(new[] { (byte)OpcodeType.OP_0, (byte)0x00 }.Concat(segwitKeyId.ToBytes(true)).ToArray(), network)
		{
		}


		public WitKeyId Hash
		{
			get
			{
				return new WitKeyId(vchData.SafeSubarray(2, 20));
			}
		}

		protected override bool IsValid
		{
			get
			{
				return vchData.Length == 1 + 1 + 20 && vchData[1] == 0 && PayToWitTemplate.ValidSegwitVersion(vchData[0]);
			}
		}

		protected override Script GeneratePaymentScript()
		{
			return PayToWitTemplate.Instance.GenerateScriptPubKey((OpcodeType)vchData[0], vchData.SafeSubarray(2, 20));
		}

		public override Base58Type Type
		{
			get
			{
				return Base58Type.WITNESS_P2WPKH;
			}
		}
	}

	public class BitcoinWitScriptAddress : BitcoinAddress
	{
		public BitcoinWitScriptAddress(string base58, Network expectedNetwork = null)
	: base(base58, expectedNetwork)
		{
		}

		public BitcoinWitScriptAddress(WitScriptId segwitKeyId, Network network)
	: base(new[] { (byte)OpcodeType.OP_0, (byte)0x00 }.Concat(segwitKeyId.ToBytes(true)).ToArray(), network)
		{
		}


		public WitScriptId Hash
		{
			get
			{
				return new WitScriptId(vchData.SafeSubarray(2, 32));
			}
		}

		protected override bool IsValid
		{
			get
			{
				return vchData.Length == 1 + 1 + 32 && vchData[1] == 0 && PayToWitTemplate.ValidSegwitVersion(vchData[0]);
			}
		}

		protected override Script GeneratePaymentScript()
		{
			return PayToWitTemplate.Instance.GenerateScriptPubKey((OpcodeType)vchData[0], vchData.SafeSubarray(2, 32));
		}

		public override Base58Type Type
		{
			get
			{
				return Base58Type.WITNESS_P2WSH;
			}
		}
	}
}
