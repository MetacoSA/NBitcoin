namespace NBitcoin.Payment
{
	public interface ISignatureChecker
	{
		bool VerifySignature(byte[] certificate, byte[] hash, string hashOID, byte[] signature);
	}
}
