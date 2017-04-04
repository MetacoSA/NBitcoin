using System.Diagnostics;
using HashLib;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using Xunit;
using Hashes = NBitcoin.Crypto.Hashes;

namespace NBitcoin.Tests
{
	using Hashes = Hashes;

    public class pos_hash_tests
	{

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
			Assert.Equal(hasher.Finalize(), 0x726fdb47dd0e0e31UL);
			byte[] t0 = new byte[]{ 0 };
			hasher.Write(t0);
			Assert.Equal(hasher.Finalize(), 0x74f839c593dc67fdUL);
			byte[] t1 = new byte[] { 1, 2, 3, 4, 5, 6, 7 };
			hasher.Write(t1);
			Assert.Equal(hasher.Finalize(), 0x93f5f5799a932462UL);
			hasher.Write(0x0F0E0D0C0B0A0908UL);
			Assert.Equal(hasher.Finalize(), 0x3f2acc7f57c29bdbUL);
			byte[] t2 = new byte[] { 16, 17 };
			hasher.Write(t2);
			Assert.Equal(hasher.Finalize(), 0x4bc1b3f0968dd39cUL);
			byte[] t3 = new byte[] { 18, 19, 20, 21, 22, 23, 24, 25, 26 };
			hasher.Write(t3);
			Assert.Equal(hasher.Finalize(), 0x2f2e6163076bcfadUL);
			byte[] t4 = new byte[]{ 27, 28, 29, 30, 31 };
			hasher.Write(t4);
			Assert.Equal(hasher.Finalize(), 0x7127512f72f27cceUL);
			hasher.Write(0x2726252423222120UL);
			Assert.Equal(hasher.Finalize(), 0x0e3ea96b5304a7d0UL);
			hasher.Write(0x2F2E2D2C2B2A2928UL);
			Assert.Equal(hasher.Finalize(), 0xe612a3cb9ecba951UL);

			Assert.Equal(Hashes.SipHash(0x0706050403020100UL, 0x0F0E0D0C0B0A0908UL, new uint256("1f1e1d1c1b1a191817161514131211100f0e0d0c0b0a09080706050403020100")), 0x7127512f72f27cceUL);

			// Check test vectors from spec, one byte at a time
			Hashes.SipHasher hasher2 = new Hashes.SipHasher(0x0706050403020100UL, 0x0F0E0D0C0B0A0908UL);
			for(byte x = 0; x < siphash_4_2_testvec.Length; ++x)
			{
				Assert.Equal(hasher2.Finalize(), siphash_4_2_testvec[x]);
				hasher2.Write(new byte[] { x });
			}
			// Check test vectors from spec, eight bytes at a time
			Hashes.SipHasher hasher3 = new Hashes.SipHasher(0x0706050403020100UL, 0x0F0E0D0C0B0A0908UL);
			for(var x = 0; x < siphash_4_2_testvec.Length; x += 8)
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
			Assert.Equal(uint256.Parse("0x0000066e91e46e5a264d42c89e1204963b2ee6be230b443e9159020539d972af"), Network.StratisMain.GetGenesis().GetHash());
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


        //==================X13Hash======================
        //===============================================

        // The genesis block header and the output of every hash algo in the x13 set
        // genesis header	- 01000000000000000000000000000000000000000000000000000000000000000000000018157f44917c2514c1f339346200f8b27d8ffaae9d8205bfae51030bc26ba265b88ba557ffff0f1eddf21b00
        // blake	  		- 042733333794f07574f6ca059eef16bacbfc5d563e5342d64fded94c6f6fbd139db7ebe1d48b962156391383ccb7f6064fe4583c64df954e5418b9a08908a082
        // bmw 	     		- b2e1d72db8a3807d6d929a0e1349250cae0e99475d94bd869d0163755574a89078e08f604ff32833585dc45d28a69c0b269abb3fcd5c4ee09afc8ca32fa7e40d
        // groest 			- 317024467e25cb6f1014f1b7a98c63b2ccc925b05a72180b0cdf23f42fabe653ddf51d11ce471dca48282b22261bbc7f5a729189c52554443a635889c7d47db6
        // skein 	 		- a4d126f16372bd2df3e22bc95f61e696a72a1bee32e62ca90fedc24e94dbdf314446dc00a5e6bc2907d73c7210e6cb780be00b49b26b7a6f2db29249f2bd884b
        // jh 		 		- c295dd0155177a9104a80ec27b245600f0de17db4aee4a16a1cf386db29b6a8e5ea74c32bb6c317f388f6585d4b338e53959399e75fcaa16045a4094da19cb6d
        // keccak 	 		- c4f7a14f01cab51c317b7b0064932004ac72a85d8686a9165e1f8b8a968113cd7a3398554ef1c92a3c296c192f9314a2365bc0f7775d4e478787055a9b2ce897
        // luffa 	 		- 8bc3589bea395cdd461226ccbea9cfa463edc5d556ff8c60f8053502135781747ae56b521ced7208fcf6c30dc6f9169b51f5452021b6951fa3d8240f3972d740
        // cubehash 		- 50ddc199803de46305083d0852bc4005fc473ed05ec56347ae65e9875c0571da7375bb227678805e7ef868015bd4bf714bae038937538dd7819cc58b6d03ca7b
        // shavit 			- 0bb309f45b7ec5b115a3318f0b2f0e431c8e415a3d6848087e7905e4e47c52874b79947e4bdee71668d1b1487716da57ac1f8d87e149ce1eee9080d6cc2827df
        // simd 			- 921ca1f5fc388ff8217e5bc787acb7e5b462063c12dca18b56b8bff0791d5c338b6604b74cd2c77ed7ac3a5a3843deb27e82f077c71a11a7308fc90864a0bd89
        // echo 			- ad8f8a4b105ffb83bb7546da799e29caa5bc9f2d0b584bdbf7d3275c65bdaae849e277187321d7d323e827c901530f6073bb967a198f3e3ba52c3a01716a442b
        // hamsi 			- 73ed6f3bd1805c003de63ae11f76630d35602c1a1b9504ba3f42233176425213622c9c630c830175b4f8a81f633e8bb98c663e142bcc88b0baaa7dd9e73a6907
        // fugue 			- af72d939050259913e440b23bee62e3b9604129ec8424d265a6ee4916e060000e51eead6ded2b584283ac0e04c1ea582e1a757245b5e8c408520216139e17848


        [Fact]
        [Trait("UnitTest", "UnitTest")]
        public void hashX13Genesis()
        {
            // genesis uses an x13 hash
            var genesisHeader = Encoders.Hex.DecodeData("01000000000000000000000000000000000000000000000000000000000000000000000018157f44917c2514c1f339346200f8b27d8ffaae9d8205bfae51030bc26ba265b88ba557ffff0f1eddf21b00");
            var genesisHash = HashX13.Instance.Hash(genesisHeader);
            Assert.Equal(genesisHash, uint256.Parse("0x0000066e91e46e5a264d42c89e1204963b2ee6be230b443e9159020539d972af"));
        }

	    [Fact]
	    [Trait("UnitTest", "UnitTest")]
	    public void hashBlake()
	    {
	        var paramIn = "01000000000000000000000000000000000000000000000000000000000000000000000018157f44917c2514c1f339346200f8b27d8ffaae9d8205bfae51030bc26ba265b88ba557ffff0f1eddf21b00";
	        var paramOut = "042733333794f07574f6ca059eef16bacbfc5d563e5342d64fded94c6f6fbd139db7ebe1d48b962156391383ccb7f6064fe4583c64df954e5418b9a08908a082";
	        var result = HashFactory.Crypto.SHA3.CreateBlake512().ComputeBytes(Encoders.Hex.DecodeData(paramIn));
	        Assert.Equal(paramOut, Encoders.Hex.EncodeData(result.GetBytes()));
	    }

        [Fact]
        [Trait("UnitTest", "UnitTest")]
        public void hashBlueMidnightWish()
        {
            var paramIn = "042733333794f07574f6ca059eef16bacbfc5d563e5342d64fded94c6f6fbd139db7ebe1d48b962156391383ccb7f6064fe4583c64df954e5418b9a08908a082";
            var paramOut = "b2e1d72db8a3807d6d929a0e1349250cae0e99475d94bd869d0163755574a89078e08f604ff32833585dc45d28a69c0b269abb3fcd5c4ee09afc8ca32fa7e40d";
            var result = HashFactory.Crypto.SHA3.CreateBlueMidnightWish512().ComputeBytes(Encoders.Hex.DecodeData(paramIn));
            Assert.Equal(paramOut, Encoders.Hex.EncodeData(result.GetBytes()));
        }

        [Fact]
        [Trait("UnitTest", "UnitTest")]
        public void hashGroestl()
        {
            var paramIn = "b2e1d72db8a3807d6d929a0e1349250cae0e99475d94bd869d0163755574a89078e08f604ff32833585dc45d28a69c0b269abb3fcd5c4ee09afc8ca32fa7e40d";
            var paramOut = "317024467e25cb6f1014f1b7a98c63b2ccc925b05a72180b0cdf23f42fabe653ddf51d11ce471dca48282b22261bbc7f5a729189c52554443a635889c7d47db6";
            var result = HashFactory.Crypto.SHA3.CreateGroestl512().ComputeBytes(Encoders.Hex.DecodeData(paramIn));
            Assert.Equal(paramOut, Encoders.Hex.EncodeData(result.GetBytes()));
        }

        //[Fact]
        //[Trait("UnitTest", "UnitTest")]
        //public void hashSkein()
        //{
        //    var paramIn = "317024467e25cb6f1014f1b7a98c63b2ccc925b05a72180b0cdf23f42fabe653ddf51d11ce471dca48282b22261bbc7f5a729189c52554443a635889c7d47db6";
        //    var paramOut = "a4d126f16372bd2df3e22bc95f61e696a72a1bee32e62ca90fedc24e94dbdf314446dc00a5e6bc2907d73c7210e6cb780be00b49b26b7a6f2db29249f2bd884b";
        //    var result = HashFactory.Crypto.SHA3.CreateSkein512().ComputeBytes(Encoders.Hex.DecodeData(paramIn));
        //    Assert.Equal(paramOut, Encoders.Hex.EncodeData(result.GetBytes()));
        //}

        [Fact]
        [Trait("UnitTest", "UnitTest")]
        public void hashSkeinCustom()
        {
            var paramIn = "317024467e25cb6f1014f1b7a98c63b2ccc925b05a72180b0cdf23f42fabe653ddf51d11ce471dca48282b22261bbc7f5a729189c52554443a635889c7d47db6";
            var paramOut = "a4d126f16372bd2df3e22bc95f61e696a72a1bee32e62ca90fedc24e94dbdf314446dc00a5e6bc2907d73c7210e6cb780be00b49b26b7a6f2db29249f2bd884b";
            var result = HashFactory.Crypto.SHA3.CreateSkein512_Custom().ComputeBytes(Encoders.Hex.DecodeData(paramIn));
            Assert.Equal(paramOut, Encoders.Hex.EncodeData(result.GetBytes()));
        }

        [Fact]
        [Trait("UnitTest", "UnitTest")]
        public void hashJH()
        {
            var paramIn = "a4d126f16372bd2df3e22bc95f61e696a72a1bee32e62ca90fedc24e94dbdf314446dc00a5e6bc2907d73c7210e6cb780be00b49b26b7a6f2db29249f2bd884b";
            var paramOut = "c295dd0155177a9104a80ec27b245600f0de17db4aee4a16a1cf386db29b6a8e5ea74c32bb6c317f388f6585d4b338e53959399e75fcaa16045a4094da19cb6d";
            var result = HashFactory.Crypto.SHA3.CreateJH512().ComputeBytes(Encoders.Hex.DecodeData(paramIn));
            Assert.Equal(paramOut, Encoders.Hex.EncodeData(result.GetBytes()));
        }

        [Fact]
        [Trait("UnitTest", "UnitTest")]
        public void hashKeccak()
        {
            var paramIn = "c295dd0155177a9104a80ec27b245600f0de17db4aee4a16a1cf386db29b6a8e5ea74c32bb6c317f388f6585d4b338e53959399e75fcaa16045a4094da19cb6d";
            var paramOut = "c4f7a14f01cab51c317b7b0064932004ac72a85d8686a9165e1f8b8a968113cd7a3398554ef1c92a3c296c192f9314a2365bc0f7775d4e478787055a9b2ce897";
            var result = HashFactory.Crypto.SHA3.CreateKeccak512().ComputeBytes(Encoders.Hex.DecodeData(paramIn));
            Assert.Equal(paramOut, Encoders.Hex.EncodeData(result.GetBytes()));
        }

        [Fact]
        [Trait("UnitTest", "UnitTest")]
        public void hashLuffa()
        {
            var paramIn = "c4f7a14f01cab51c317b7b0064932004ac72a85d8686a9165e1f8b8a968113cd7a3398554ef1c92a3c296c192f9314a2365bc0f7775d4e478787055a9b2ce897";
            var paramOut = "8bc3589bea395cdd461226ccbea9cfa463edc5d556ff8c60f8053502135781747ae56b521ced7208fcf6c30dc6f9169b51f5452021b6951fa3d8240f3972d740";
            var result = HashFactory.Crypto.SHA3.CreateLuffa512().ComputeBytes(Encoders.Hex.DecodeData(paramIn));
            Assert.Equal(paramOut, Encoders.Hex.EncodeData(result.GetBytes()));
        }

        [Fact]
        [Trait("UnitTest", "UnitTest")]
        public void hashCubeHash()
        {
            var paramIn = "8bc3589bea395cdd461226ccbea9cfa463edc5d556ff8c60f8053502135781747ae56b521ced7208fcf6c30dc6f9169b51f5452021b6951fa3d8240f3972d740";
            var paramOut = "50ddc199803de46305083d0852bc4005fc473ed05ec56347ae65e9875c0571da7375bb227678805e7ef868015bd4bf714bae038937538dd7819cc58b6d03ca7b";
            var result = HashFactory.Crypto.SHA3.CreateCubeHash512().ComputeBytes(Encoders.Hex.DecodeData(paramIn));
            Assert.Equal(paramOut, Encoders.Hex.EncodeData(result.GetBytes()));
        }

        //[Fact]
        //[Trait("UnitTest", "UnitTest")]
        //public void hashSHAvite3()
        //{
        //    var paramIn = "50ddc199803de46305083d0852bc4005fc473ed05ec56347ae65e9875c0571da7375bb227678805e7ef868015bd4bf714bae038937538dd7819cc58b6d03ca7b";
        //    var paramOut = "0bb309f45b7ec5b115a3318f0b2f0e431c8e415a3d6848087e7905e4e47c52874b79947e4bdee71668d1b1487716da57ac1f8d87e149ce1eee9080d6cc2827df";
        //    var result = HashFactory.Crypto.SHA3.CreateSHAvite3_512().ComputeBytes(Encoders.Hex.DecodeData(paramIn));
        //    Assert.Equal(paramOut, Encoders.Hex.EncodeData(result.GetBytes()));
        //}

        [Fact]
        [Trait("UnitTest", "UnitTest")]
        public void hashSHAvite3Custom()
        {
            var paramIn = "50ddc199803de46305083d0852bc4005fc473ed05ec56347ae65e9875c0571da7375bb227678805e7ef868015bd4bf714bae038937538dd7819cc58b6d03ca7b";
            var paramOut = "0bb309f45b7ec5b115a3318f0b2f0e431c8e415a3d6848087e7905e4e47c52874b79947e4bdee71668d1b1487716da57ac1f8d87e149ce1eee9080d6cc2827df";
            var result = HashFactory.Crypto.SHA3.CreateSHAvite3_512_Custom().ComputeBytes(Encoders.Hex.DecodeData(paramIn));
            Assert.Equal(paramOut, Encoders.Hex.EncodeData(result.GetBytes()));
        }

        [Fact]
        [Trait("UnitTest", "UnitTest")]
        public void hashSIMD()
        {
            var paramIn = "0bb309f45b7ec5b115a3318f0b2f0e431c8e415a3d6848087e7905e4e47c52874b79947e4bdee71668d1b1487716da57ac1f8d87e149ce1eee9080d6cc2827df";
            var paramOut = "921ca1f5fc388ff8217e5bc787acb7e5b462063c12dca18b56b8bff0791d5c338b6604b74cd2c77ed7ac3a5a3843deb27e82f077c71a11a7308fc90864a0bd89";
            var result = HashFactory.Crypto.SHA3.CreateSIMD512().ComputeBytes(Encoders.Hex.DecodeData(paramIn));
            Assert.Equal(paramOut, Encoders.Hex.EncodeData(result.GetBytes()));
        }

        [Fact]
        [Trait("UnitTest", "UnitTest")]
        public void hashEcho()
        {
            var paramIn = "921ca1f5fc388ff8217e5bc787acb7e5b462063c12dca18b56b8bff0791d5c338b6604b74cd2c77ed7ac3a5a3843deb27e82f077c71a11a7308fc90864a0bd89";
            var paramOut = "ad8f8a4b105ffb83bb7546da799e29caa5bc9f2d0b584bdbf7d3275c65bdaae849e277187321d7d323e827c901530f6073bb967a198f3e3ba52c3a01716a442b";
            var result = HashFactory.Crypto.SHA3.CreateEcho512().ComputeBytes(Encoders.Hex.DecodeData(paramIn));
            Assert.Equal(paramOut, Encoders.Hex.EncodeData(result.GetBytes()));
        }

        [Fact]
        [Trait("UnitTest", "UnitTest")]
        public void hashHamsi()
        {
            var paramIn = "ad8f8a4b105ffb83bb7546da799e29caa5bc9f2d0b584bdbf7d3275c65bdaae849e277187321d7d323e827c901530f6073bb967a198f3e3ba52c3a01716a442b";
            var paramOut = "73ed6f3bd1805c003de63ae11f76630d35602c1a1b9504ba3f42233176425213622c9c630c830175b4f8a81f633e8bb98c663e142bcc88b0baaa7dd9e73a6907";
            var result = HashFactory.Crypto.SHA3.CreateHamsi512().ComputeBytes(Encoders.Hex.DecodeData(paramIn));
            Assert.Equal(paramOut, Encoders.Hex.EncodeData(result.GetBytes()));
        }

        [Fact]
        [Trait("UnitTest", "UnitTest")]
        public void hashFugue()
        {
            var paramIn = "73ed6f3bd1805c003de63ae11f76630d35602c1a1b9504ba3f42233176425213622c9c630c830175b4f8a81f633e8bb98c663e142bcc88b0baaa7dd9e73a6907";
            var paramOut = "af72d939050259913e440b23bee62e3b9604129ec8424d265a6ee4916e060000e51eead6ded2b584283ac0e04c1ea582e1a757245b5e8c408520216139e17848";
            var result = HashFactory.Crypto.SHA3.CreateFugue512().ComputeBytes(Encoders.Hex.DecodeData(paramIn));
            Assert.Equal(paramOut, Encoders.Hex.EncodeData(result.GetBytes()));
        }
    }
}
