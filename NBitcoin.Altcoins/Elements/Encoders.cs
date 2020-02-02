using NBitcoin.DataEncoders;

namespace NBitcoin.Altcoins.Elements
{
	public static class ElementsEncoders
	{
		public static Blech32Encoder Blech32(string hrp)
		{
			return new Blech32Encoder(hrp);
		}
		public static Blech32Encoder Blech32(byte[] hrp)
		{
			return new Blech32Encoder(hrp);
		}
	}
}
