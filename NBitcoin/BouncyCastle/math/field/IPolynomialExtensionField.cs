namespace nStratis.BouncyCastle.math.field
{
	internal interface IPolynomialExtensionField
		: IExtensionField
	{
		IPolynomial MinimalPolynomial
		{
			get;
		}
	}
}
