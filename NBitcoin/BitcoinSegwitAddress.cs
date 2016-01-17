using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class BitcoinSegwitPubKeyAddress : BitcoinAddress
	{

		public BitcoinSegwitPubKeyAddress(string base58, Network expectedNetwork = null)
			: base(base58, expectedNetwork)
		{
		}

		public BitcoinSegwitPubKeyAddress(SegwitKeyId segwitKeyId, Network network)
			: base(new[] { (byte)OpcodeType.OP_0, (byte)0x00 }.Concat(segwitKeyId.ToBytes(true)).ToArray(), network)
		{
		}


		public SegwitKeyId Hash
		{
			get
			{
				return new SegwitKeyId(vchData.SafeSubarray(2, 20));
			}
		}

		protected override bool IsValid
		{
			get
			{
				return vchData.Length == 1 + 1 + 20 && vchData[1] == 0 && PayToSegwitTemplate.ValidSegwitVersion(vchData[0]);
			}
		}

		protected override Script GeneratePaymentScript()
		{
			return PayToSegwitTemplate.Instance.GenerateScriptPubKey((OpcodeType)vchData[0], vchData.SafeSubarray(2, 20));
		}

		public override Base58Type Type
		{
			get
			{
				return Base58Type.WITNESS_P2WPKH;
			}
		}
	}
}
