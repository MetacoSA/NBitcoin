using System;
using System.IO;

namespace NBitcoin.BouncyCastle.Crypto.Digests
{
	public class NullDigest : IDigest
	{
		private readonly MemoryStream bOut = new MemoryStream();

		public string AlgorithmName
		{
			get { return "NULL"; }
		}

		public int GetByteLength()
		{
			// TODO Is this okay?
			return 0;
		}

		public int GetDigestSize()
		{
			return (int) bOut.Length;
		}

		public void Update(byte b)
		{
			bOut.WriteByte(b);
		}

		public void BlockUpdate(byte[] inBytes, int inOff, int len)
		{
			bOut.Write(inBytes, inOff, len);
		}

		public int DoFinal(byte[] outBytes, int outOff)
		{
			byte[] res = bOut.ToArray();
			res.CopyTo(outBytes, outOff);
			Reset();
			return res.Length;
		}

		public void Reset()
		{
			bOut.SetLength(0);
		}
	}
}
