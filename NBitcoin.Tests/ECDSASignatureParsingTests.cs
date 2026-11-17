using System;
using System.IO;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using Xunit;

namespace NBitcoin.Tests
{
	public class ECDSASignatureParsingTests
	{
#if !HAS_SPAN
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void StreamConstructorLeavesFollowingDataUnread()
		{
			byte[] bytes = Encoders.Hex.DecodeData("3081ff020101020102deadbeef");
			using (var stream = new MemoryStream(bytes))
			{
				Assert.Equal(Encoders.Hex.DecodeData("3006020101020102"), new ECDSASignature(stream).ToDER());
				Assert.Equal(0xde, stream.ReadByte());
			}
		}
#endif

		[Theory]
		[Trait("UnitTest", "UnitTest")]
		[InlineData("3f1006020101020102")] // Non-minimal SEQUENCE tag
		[InlineData("30071f010101020102")] // Wrong INTEGER tag on r
		[InlineData("30071f020101020102")] // Non-minimal INTEGER tag on r
		[InlineData("30070201011f020102")] // Non-minimal INTEGER tag on s
		[InlineData("")]
		[InlineData("30")]
		[InlineData("308201")]
		[InlineData("3006020201")]
		[InlineData("3006020101020202")]
		[InlineData("300602840100000001020102")] // Overflowing length descriptor
		public void RejectsMalformedCoreLaxDer(string hex)
		{
			byte[] bytes = Encoders.Hex.DecodeData(hex);
			Assert.Throws<FormatException>(() => ECDSASignature.FromDER(bytes));
			Assert.False(ECDSASignature.IsValidDER(bytes));
#if !HAS_SPAN
			using (var stream = new MemoryStream(bytes))
				Assert.Throws<FormatException>(() => new ECDSASignature(stream));
#endif
		}

		[Theory]
		[Trait("UnitTest", "UnitTest")]
		[InlineData("3006020101020102", "3006020101020102")]
		[InlineData("3006020180020102", "300702020080020102")] // Negative ASN.1 integer treated as unsigned
		[InlineData("30080202000102020002", "3006020101020102")] // Excess padding
		[InlineData("308106020101020102", "3006020101020102")] // Long-form sequence length
		[InlineData("3081ff020101020102", "3006020101020102")] // Sequence length is ignored
		[InlineData("3080020101020102", "3006020101020102")] // Indefinite sequence length
		[InlineData("30080281010102810102", "3006020101020102")] // Long-form integer lengths
		[InlineData("3006020101020102deadbeef", "3006020101020102")] // Trailing garbage
		[InlineData("300402000200", "3006020100020100")] // Empty integers
		[InlineData("30260221010000000000000000000000000000000000000000000000000000000000000000020102", "3006020100020100")] // More than 32 bytes
		[InlineData("30250220fffffffffffffffffffffffffffffffebaaedce6af48a03bbfd25e8cd0364141020102", "3006020100020100")] // r == curve order
		[InlineData("30250201010220fffffffffffffffffffffffffffffffebaaedce6af48a03bbfd25e8cd0364141", "3006020100020100")] // s == curve order
		public void PreservesCoreLaxDerCompatibility(string hex, string canonical)
		{
			byte[] bytes = Encoders.Hex.DecodeData(hex);
			byte[] expected = Encoders.Hex.DecodeData(canonical);
			Assert.Equal(expected, ECDSASignature.FromDER(bytes).ToDER());
			Assert.True(ECDSASignature.IsValidDER(bytes));
#if !HAS_SPAN
			using (var stream = new MemoryStream(bytes))
			{
				Assert.Equal(expected, new ECDSASignature(stream).ToDER());
				Assert.True(stream.CanRead);
			}
#endif
		}
	}
}
