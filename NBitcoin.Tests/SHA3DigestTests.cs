using System;
using System.IO;
using System.Text;
using NBitcoin.BouncyCastle.Crypto.Digests;
using NBitcoin.BouncyCastle.Utilities;
using NBitcoin.DataEncoders;
using Xunit;

namespace NBitcoin.Tests
{
	internal class MySha3Digest : Sha3Digest
	{
		internal MySha3Digest(int bitLength)
			: base(bitLength)
		{
		}

		internal int MyDoFinal(byte[] output, int outOff, byte partialByte, int partialBits)
		{
			return DoFinal(output, outOff, partialByte, partialBits);
		}
	}

	public class Sha3DigestTest
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void TestVectors()
		{
			using (var r = new StreamReader("data/SHA3TestVectors.txt"))
			{
				string line;
				while ((line = ReadLine(r)) is {})
				{
					if (line.Length != 0)
					{
						RunTestVector(ReadTestVector(r, line));
					}
				}
			}
		}

		private MySha3Digest CreateDigest(string algorithm)
		{
			if (algorithm.StartsWith("SHA3-"))
			{
				int bits = int.Parse(algorithm.Substring("SHA3-".Length));
				return new MySha3Digest(bits);
			}
			throw new ArgumentException("Unknown algorithm: " + algorithm, "algorithm");
		}

		private byte[] DecodeBinary(string block)
		{
			int bits = block.Length;
			int fullBytes = bits / 8;
			int totalBytes = (bits + 7) / 8;
			byte[] result = new byte[totalBytes];

			for (int i = 0; i < fullBytes; ++i)
			{
				string byteStr = Reverse(block.Substring(i * 8, 8));
				result[i] = Convert.ToByte(byteStr, 2);
			}

			if (totalBytes > fullBytes)
			{
				string byteStr = Reverse(block.Substring(fullBytes * 8));
				result[fullBytes] = Convert.ToByte(byteStr, 2);
			}

			return result;
		}

		private string ReadBlock(StreamReader r)
		{
			var b = new StringBuilder();
			string line;
			while ((line = ReadBlockLine(r)) != null)
			{
				b.Append(line);
			}
			return b.ToString();
		}

		private string ReadBlockLine(StreamReader r)
		{
			string line = ReadLine(r);
			if (line == null || line.Length == 0)
			{
				return null;
			}
			return line.Replace(" ", "");
		}

		private TestVector ReadTestVector(StreamReader r, string header)
		{
			string[] parts = SplitAround(header, TestVector.SAMPLE_OF);

			string algorithm = parts[0];
			int bits = int.Parse(StripFromChar(parts[1], '-'));

			SkipUntil(r, TestVector.MSG_HEADER);
			string messageBlock = ReadBlock(r);
			if (messageBlock.Length != bits)
			{
				throw new InvalidOperationException("Test vector length mismatch");
			}
			byte[] message = DecodeBinary(messageBlock);

			SkipUntil(r, TestVector.HASH_HEADER);
			byte[] hash = Encoders.Hex.DecodeData(ReadBlock(r));

			return new TestVector(algorithm, bits, message, hash);
		}

		private string ReadLine(StreamReader r)
		{
			string line = r.ReadLine();
			return line == null ? null : StripFromChar(line, '#').Trim();
		}

		private string RequireLine(StreamReader r)
		{
			string line = ReadLine(r);
			if (line == null)
			{
				throw new EndOfStreamException();
			}
			return line;
		}

		private string Reverse(string s)
		{
			char[] cs = s.ToCharArray();
			Array.Reverse(cs);
			return new string(cs);
		}

		private void RunTestVector(TestVector v)
		{
			int bits = v.Bits;
			int partialBits = bits % 8;

			MySha3Digest d = CreateDigest(v.Algorithm);
			byte[] output = new byte[d.GetDigestSize()];

			byte[] m = v.Message;
			if (partialBits == 0)
			{
				d.BlockUpdate(m, 0, m.Length);
				d.DoFinal(output, 0);
			}
			else
			{
				d.BlockUpdate(m, 0, m.Length - 1);
				d.MyDoFinal(output, 0, m[m.Length - 1], partialBits);
			}

			Assert.True(Arrays.AreEqual(v.Hash, output));
		}

		private void SkipUntil(StreamReader r, string header)
		{
			string line;
			do
			{
				line = RequireLine(r);
			}
			while (line.Length == 0);
			if (!line.Equals(header))
			{
				throw new IOException("Expected: " + header);
			}
		}

		private string[] SplitAround(string s, string separator)
		{
			int i = s.IndexOf(separator);
			if (i < 0)
				throw new InvalidOperationException();
			return new string[] { s.Substring(0, i), s.Substring(i + separator.Length) };
		}

		private string StripFromChar(string s, char c)
		{
			int i = s.IndexOf(c);
			if (i >= 0)
			{
				s = s.Substring(0, i);
			}
			return s;
		}



		internal class TestVector
		{
			internal static string SAMPLE_OF = " sample of ";
			internal static string MSG_HEADER = "Msg as bit string";
			internal static string HASH_HEADER = "Hash val is";

			private readonly string algorithm;
			private readonly int bits;
			private readonly byte[] message;
			private readonly byte[] hash;

			internal TestVector(string algorithm, int bits, byte[] message, byte[] hash)
			{
				this.algorithm = algorithm;
				this.bits = bits;
				this.message = message;
				this.hash = hash;
			}

			public string Algorithm
			{
				get { return algorithm; }
			}

			public int Bits
			{
				get { return bits; }
			}

			public byte[] Message
			{
				get { return message; }
			}

			public byte[] Hash
			{
				get { return hash; }
			}
		}
	}
}
