namespace NBitcoin.BouncyCastle.math.field
{
	internal interface IPolynomial
	{
		int Degree
		{
			get;
		}

		//BigInteger[] GetCoefficients();

		int[] GetExponentsPresent();

		//Term[] GetNonZeroTerms();
	}
}
