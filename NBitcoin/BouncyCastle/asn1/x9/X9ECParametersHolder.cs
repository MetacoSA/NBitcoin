namespace NBitcoin.BouncyCastle.asn1.x9
{
	internal abstract class X9ECParametersHolder
	{
		private X9ECParameters parameters;

		public X9ECParameters Parameters
		{
			get
			{
				lock(this)
				{
					if(parameters == null)
					{
						parameters = CreateParameters();
					}

					return parameters;
				}
			}
		}

		protected abstract X9ECParameters CreateParameters();
	}
}
