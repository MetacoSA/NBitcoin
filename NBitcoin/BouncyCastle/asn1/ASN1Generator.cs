using System.IO;

namespace NBitcoin.BouncyCastle.Asn1
{
	internal abstract class Asn1Generator
	{
		private Stream _out;

		protected Asn1Generator(
			Stream outStream)
		{
			_out = outStream;
		}

		protected Stream Out
		{
			get
			{
				return _out;
			}
		}

		public abstract void AddObject(Asn1Encodable obj);

		public abstract Stream GetRawOutputStream();

		public abstract void Close();
	}
}
