namespace NBitcoin.Payment
{
	public interface IChainChecker
	{
		bool VerifyChain(byte[] certificate, byte[][] additionalCertificates);
	}
}
