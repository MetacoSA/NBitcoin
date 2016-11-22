namespace nStratis.BouncyCastle.math.field
{
	internal interface IExtensionField
		: IFiniteField
	{
		IFiniteField Subfield
		{
			get;
		}

		int Degree
		{
			get;
		}
	}
}
