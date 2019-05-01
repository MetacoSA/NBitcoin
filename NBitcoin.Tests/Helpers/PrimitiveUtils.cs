
namespace NBitcoin.Tests.Helpers
{
	internal static class PrimitiveUtils
	{
		internal static Coin RandomCoin(Money amount, Script scriptPubKey, bool p2sh)
		{
			var outpoint = RandOutpoint();
			if(!p2sh)
				return new Coin(outpoint, new TxOut(amount, scriptPubKey));
			return new ScriptCoin(outpoint, new TxOut(amount, scriptPubKey.Hash), scriptPubKey);
		}
		internal static Coin RandomCoin(Money amount, Key receiver)
		{
			return RandomCoin(amount, receiver.PubKey.GetAddress(Network.Main));
		}
		internal static Coin RandomCoin(Money amount, IDestination receiver)
		{
			var outpoint = RandOutpoint();
			return new Coin(outpoint, new TxOut(amount, receiver));
		}

		internal static OutPoint RandOutpoint()
		{
			return new OutPoint(Rand(), 0);
		}
		internal static uint256 Rand()
		{
			return new uint256(RandomUtils.GetBytes(32));
		}
	}
}
