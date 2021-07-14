using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBitcoin.Altcoins.HashX11;
using Xunit;
using System.IO;

namespace NBitcoin.Tests
{
	public class hash_tests
	{

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void InitiateTaggedWorks()
		{
#if HAS_SPAN
			var sha = new NBitcoin.Secp256k1.SHA256();
			sha.InitializeTagged("lol");
			sha.Write(new byte[32]);
			byte[] buff = new byte[32];
			sha.GetHash(buff);
			Assert.Equal("9185788706ad8d475d2410ce07554aeff7a212418159a8fa8ef2b3cb4a883b62", new uint256(buff).ToString());
#endif
			HashStream stream = new HashStream();
			stream.SingleSHA256 = true;
			stream.InitializeTagged("lol");
			stream.Write(new byte[32], 0, 32);
			var actual = stream.GetHash();
			Assert.Equal("9185788706ad8d475d2410ce07554aeff7a212418159a8fa8ef2b3cb4a883b62", actual.ToString());
		}


		[Fact]
		[Trait("Core", "Core")]
		public void murmurhash3()
		{
			// Test MurmurHash3 with various inputs. Of course this is retested in the
			// bloom filter tests - they would fail if MurmurHash3() had any problems -
			// but is useful for those trying to implement Bitcoin libraries as a
			// source of test data for their MurmurHash3() primitive during
			// development.
			//
			// The magic number 0xFBA4C795 comes from CBloomFilter::Hash()
			T(0x00000000, 0x00000000, "");
			T(0x6a396f08, 0xFBA4C795, "");
			T(0x81f16f39, 0xffffffff, "");

			T(0x514e28b7, 0x00000000, "00");
			T(0xea3f0b17, 0xFBA4C795, "00");
			T(0xfd6cf10d, 0x00000000, "ff");

			T(0x16c6b7ab, 0x00000000, "0011");
			T(0x8eb51c3d, 0x00000000, "001122");
			T(0xb4471bf8, 0x00000000, "00112233");
			T(0xe2301fa8, 0x00000000, "0011223344");
			T(0xfc2e4a15, 0x00000000, "001122334455");
			T(0xb074502c, 0x00000000, "00112233445566");
			T(0x8034d2a0, 0x00000000, "0011223344556677");
			T(0xb4698def, 0x00000000, "001122334455667788");
		}

		/*
   SipHash-2-4 output with
   k = 00 01 02 ...
   and
   in = (empty string)
   in = 00 (1 byte)
   in = 00 01 (2 bytes)
   in = 00 01 02 (3 bytes)
   ...
   in = 00 01 02 ... 3e (63 bytes)
   from: https://131002.net/siphash/siphash24.c
*/
		ulong[] siphash_4_2_testvec = new ulong[]{
	0x726fdb47dd0e0e31, 0x74f839c593dc67fd, 0x0d6c8009d9a94f5a, 0x85676696d7fb7e2d,
	0xcf2794e0277187b7, 0x18765564cd99a68d, 0xcbc9466e58fee3ce, 0xab0200f58b01d137,
	0x93f5f5799a932462, 0x9e0082df0ba9e4b0, 0x7a5dbbc594ddb9f3, 0xf4b32f46226bada7,
	0x751e8fbc860ee5fb, 0x14ea5627c0843d90, 0xf723ca908e7af2ee, 0xa129ca6149be45e5,
	0x3f2acc7f57c29bdb, 0x699ae9f52cbe4794, 0x4bc1b3f0968dd39c, 0xbb6dc91da77961bd,
	0xbed65cf21aa2ee98, 0xd0f2cbb02e3b67c7, 0x93536795e3a33e88, 0xa80c038ccd5ccec8,
	0xb8ad50c6f649af94, 0xbce192de8a85b8ea, 0x17d835b85bbb15f3, 0x2f2e6163076bcfad,
	0xde4daaaca71dc9a5, 0xa6a2506687956571, 0xad87a3535c49ef28, 0x32d892fad841c342,
	0x7127512f72f27cce, 0xa7f32346f95978e3, 0x12e0b01abb051238, 0x15e034d40fa197ae,
	0x314dffbe0815a3b4, 0x027990f029623981, 0xcadcd4e59ef40c4d, 0x9abfd8766a33735c,
	0x0e3ea96b5304a7d0, 0xad0c42d6fc585992, 0x187306c89bc215a9, 0xd4a60abcf3792b95,
	0xf935451de4f21df2, 0xa9538f0419755787, 0xdb9acddff56ca510, 0xd06c98cd5c0975eb,
	0xe612a3cb9ecba951, 0xc766e62cfcadaf96, 0xee64435a9752fe72, 0xa192d576b245165a,
	0x0a8787bf8ecb74b2, 0x81b3e73d20b49b6f, 0x7fa8220ba3b2ecea, 0x245731c13ca42499,
	0xb78dbfaf3a8d83bd, 0xea1ad565322a1a0b, 0x60e61c23a3795013, 0x6606d7e446282b93,
	0x6ca4ecb15c5f91e1, 0x9f626da15c9625f3, 0xe51b38608ef25f57, 0x958a324ceb064572
};

		[Fact]
		[Trait("Core", "Core")]
		public void siphash()
		{
			Hashes.SipHasher hasher = new Hashes.SipHasher(0x0706050403020100UL, 0x0F0E0D0C0B0A0908UL);
			Assert.Equal(0x726fdb47dd0e0e31UL, hasher.Finalize());
			byte[] t0 = new byte[] { 0 };
			hasher.Write(t0);
			Assert.Equal(0x74f839c593dc67fdUL, hasher.Finalize());
			byte[] t1 = new byte[] { 1, 2, 3, 4, 5, 6, 7 };
			hasher.Write(t1);
			Assert.Equal(0x93f5f5799a932462UL, hasher.Finalize());
			hasher.Write(0x0F0E0D0C0B0A0908UL);
			Assert.Equal(0x3f2acc7f57c29bdbUL, hasher.Finalize());
			byte[] t2 = new byte[] { 16, 17 };
			hasher.Write(t2);
			Assert.Equal(0x4bc1b3f0968dd39cUL, hasher.Finalize());
			byte[] t3 = new byte[] { 18, 19, 20, 21, 22, 23, 24, 25, 26 };
			hasher.Write(t3);
			Assert.Equal(0x2f2e6163076bcfadUL, hasher.Finalize());
			byte[] t4 = new byte[] { 27, 28, 29, 30, 31 };
			hasher.Write(t4);
			Assert.Equal(0x7127512f72f27cceUL, hasher.Finalize());
			hasher.Write(0x2726252423222120UL);
			Assert.Equal(0x0e3ea96b5304a7d0UL, hasher.Finalize());
			hasher.Write(0x2F2E2D2C2B2A2928UL);
			Assert.Equal(0xe612a3cb9ecba951UL, hasher.Finalize());

			Assert.Equal(0x7127512f72f27cceUL, Hashes.SipHash(0x0706050403020100UL, 0x0F0E0D0C0B0A0908UL, new uint256("1f1e1d1c1b1a191817161514131211100f0e0d0c0b0a09080706050403020100")));

			// Check test vectors from spec, one byte at a time
			Hashes.SipHasher hasher2 = new Hashes.SipHasher(0x0706050403020100UL, 0x0F0E0D0C0B0A0908UL);
			for (byte x = 0; x < siphash_4_2_testvec.Length; ++x)
			{
				Assert.Equal(hasher2.Finalize(), siphash_4_2_testvec[x]);
				hasher2.Write(new byte[] { x });
			}
			// Check test vectors from spec, eight bytes at a time
			Hashes.SipHasher hasher3 = new Hashes.SipHasher(0x0706050403020100UL, 0x0F0E0D0C0B0A0908UL);
			for (var x = 0; x < siphash_4_2_testvec.Length; x += 8)
			{
				Assert.Equal(hasher3.Finalize(), siphash_4_2_testvec[x]);
				hasher3.Write(uint64_t(x) | (uint64_t(x + 1) << 8) | (uint64_t(x + 2) << 16) | (uint64_t(x + 3) << 24) |
							 (uint64_t(x + 4) << 32) | (uint64_t(x + 5) << 40) | (uint64_t(x + 6) << 48) | (uint64_t(x + 7) << 56));
			}
		}

		private ulong uint64_t(int x)
		{
			return (ulong)x;
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void hash256()
		{
			Assert.Equal(uint256.Parse("000000000019d6689c085ae165831e934ff763ae46a2a6c172b3f1b60a8ce26f"), Network.Main.GetGenesis().GetHash());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void hash160()
		{
			var data = new byte[] { 1, 2, 3, 4 };
			var result = Hashes.Hash160(data);
			Assert.Equal("706ea1768da7f0c489bf931b362c2d26d8cbd2ec", result.ToString());
		}

		[DebuggerHidden]
		private void T(uint expected, uint seed, string data)
		{
			Assert.Equal(Hashes.MurmurHash3(seed, Encoders.Hex.DecodeData(data)), expected);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void quark()
		{
			var bytes = Encoders.Hex.DecodeData("01000000000000000000000000000000000000000000000000000000000000000000000027cc0d8f6a20e41f445b1045d1c73ba4b068ee60b5fd4aa34027cbbe5c2e161e1546db5af0ff0f1e18cb3f01");
			var hashBytes = new Quark().ComputeBytes(bytes).ToArray();

			var hash = Encoders.Hex.EncodeData(hashBytes.Reverse().ToArray());
			Assert.Equal("00000f4fb42644a07735beea3647155995ab01cf49d05fdc082c08eb673433f9", hash);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void x11()
		{
			var bytes = Encoders.Hex.DecodeData("010000000000000000000000000000000000000000000000000000000000000000000000c762a6567f3cc092f0684bb62b7e00a84890b990f07cc71a6bb58d64b98e02e0022ddb52f0ff0f1ec23fb901");

			var hashBytes = new X11().ComputeBytes(bytes).ToArray();

			var hash = Encoders.Hex.EncodeData(hashBytes.Reverse().ToArray());

			Assert.Equal("00000ffd590b1485b3caadc19b22e6379c733355108f107a430458cdf3407ab6", hash);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanCalculateMerkleRoot()
		{
			Block block = Network.Main.Consensus.ConsensusFactory.CreateBlock();
			block.ReadWrite(Encoders.Hex.DecodeData(File.ReadAllText(@"data/block169482.txt")), Network.Main);
			Assert.Equal(block.Header.HashMerkleRoot, block.GetMerkleRoot().Hash);
		}
	}
}
