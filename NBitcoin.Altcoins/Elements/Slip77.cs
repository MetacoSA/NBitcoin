using NBitcoin.Crypto;

namespace NBitcoin.Altcoins.Elements
{
	/// <summary>
	/// SLIP-0077 Implementation: Deterministic blinding key derivation for Confidential Transactions
	/// https://github.com/satoshilabs/slips/blob/master/slip-0077.md
	/// </summary>
	public static class Slip77
	{
		public static Key DeriveSlip77BlindingKey(this Slip21Node masterBlindingKey, Script script)
		{
			return new Key(Hashes.HMACSHA256(masterBlindingKey.Key.ToBytes(), script.ToBytes()));
		}

		public static Slip21Node GetSlip77Node(this Slip21Node node)
		{
			return node.DeriveChild("SLIP-0077");
		}
	}
}
