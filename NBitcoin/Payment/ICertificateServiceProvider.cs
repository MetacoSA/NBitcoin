namespace NBitcoin.Payment
{
	public interface ICertificateServiceProvider
	{
		IChainChecker GetChainChecker();
		ISignatureChecker GetSignatureChecker();
		ISigner GetSigner();
	}
}
