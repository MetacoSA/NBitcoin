using System;

namespace HashLib
{
    public static class HashFactory
    {
        public static class Hash32
        {
            public static IHash CreateAP()
            {
                return new HashLib.Hash32.AP();
            }
            
            public static IHash CreateBernstein()
            {
                return new HashLib.Hash32.Bernstein();
            }
            
            public static IHash CreateBernstein1()
            {
                return new HashLib.Hash32.Bernstein1();
            }
            
            public static IHash CreateBKDR()
            {
                return new HashLib.Hash32.BKDR();
            }
            
            public static IHash CreateDEK()
            {
                return new HashLib.Hash32.DEK();
            }
            
            public static IHash CreateDJB()
            {
                return new HashLib.Hash32.DJB();
            }
            
            public static IHash CreateDotNet()
            {
                return new HashLib.Hash32.DotNet();
            }
            
            public static IHash CreateELF()
            {
                return new HashLib.Hash32.ELF();
            }
            
            public static IHash CreateFNV()
            {
                return new HashLib.Hash32.FNV();
            }
            
            public static IHash CreateFNV1a()
            {
                return new HashLib.Hash32.FNV1a();
            }
            
            public static IHash CreateJenkins3()
            {
                return new HashLib.Hash32.Jenkins3();
            }
            
            public static IHash CreateJS()
            {
                return new HashLib.Hash32.JS();
            }

            public static IHashWithKey CreateMurmur2()
            {
                return new HashLib.Hash32.Murmur2();
            }

            public static IHashWithKey CreateMurmur3()
            {
                return new HashLib.Hash32.Murmur3();
            }
            
            public static IHash CreateOneAtTime()
            {
                return new HashLib.Hash32.OneAtTime();
            }
            
            public static IHash CreatePJW()
            {
                return new HashLib.Hash32.PJW();
            }
            
            public static IHash CreateRotating()
            {
                return new HashLib.Hash32.Rotating();
            }
            
            public static IHash CreateRS()
            {
                return new HashLib.Hash32.RS();
            }
            
            public static IHash CreateSDBM()
            {
                return new HashLib.Hash32.SDBM();
            }
            
            public static IHash CreateShiftAndXor()
            {
                return new HashLib.Hash32.ShiftAndXor();
            }

            public static IHash CreateSuperFast()
            {
                return new HashLib.Hash32.SuperFast();
            }
        }

        public static class Checksum
        {
            /// <summary>
            /// IEEE 802.3, polynomial = 0xEDB88320
            /// </summary>
            /// <returns></returns>
            public static IHash CreateCRC32_IEEE()
            {
                return new HashLib.Checksum.CRC32_IEEE();
            }

            /// <summary>
            /// Castagnoli, polynomial = 0x82F63B78
            /// </summary>
            /// <returns></returns>
            public static IHash CreateCRC32_CASTAGNOLI()
            {
                return new HashLib.Checksum.CRC32_CASTAGNOLI();
            }

            /// <summary>
            /// Koopman, polynomial = 0xEB31D82E
            /// </summary>
            /// <returns></returns>
            public static IHash CreateCRC32_KOOPMAN()
            {
                return new HashLib.Checksum.CRC32_KOOPMAN();
            }

            /// <summary>
            /// Q, polynomial = 0xD5828281
            /// </summary>
            /// <returns></returns>
            public static IHash CreateCRC32_Q()
            {
                return new HashLib.Checksum.CRC32_Q();
            }

            public static IHash CreateCRC32(uint a_polynomial, uint a_initial_value = uint.MaxValue, uint a_final_xor = uint.MaxValue)
            {
                return new HashLib.Checksum.CRC32(a_polynomial, a_initial_value, a_final_xor);
            }

            public static IHash CreateAdler32()
            {
                return new HashLib.Checksum.Adler32();
            }

            /// <summary>
            /// ECMA 182, polynomial = 0xD800000000000000
            /// </summary>
            /// <returns></returns>
            public static IHash CreateCRC64_ISO()
            {
                return new HashLib.Checksum.CRC64_ISO();
            }

            /// <summary>
            /// ISO, polynomial = 0xC96C5795D7870F42
            /// </summary>
            /// <returns></returns>
            public static IHash CreateCRC64_ECMA()
            {
                return new HashLib.Checksum.CRC64_ECMA();
            }

            public static IHash CreateCRC64(ulong a_polynomial, ulong a_initial_value = ulong.MaxValue, ulong a_final_xor = ulong.MaxValue)
            {
                return new HashLib.Checksum.CRC64(a_polynomial, a_initial_value, a_final_xor);
            }
        }

        public static class Hash64
        {
            public static IHash CreateFNV1a()
            {
                return new HashLib.Hash64.FNV1a64();
            }

            public static IHash CreateFNV()
            {
                return new HashLib.Hash64.FNV64();
            }

            public static IHashWithKey CreateMurmur2()
            {
                return new HashLib.Hash64.Murmur2_64();
            }

            public static IHashWithKey CreateSipHash()
            {
                return new HashLib.Hash64.SipHash();
            }
        }

        public static class Hash128
        {
            public static IHashWithKey CreateMurmur3_128()
            {
                return new HashLib.Hash128.Murmur3_128();
            }
        }

        public static class Crypto
        {
            public static class SHA3
            {
                public static IHash CreateJH224()
                {
                    return new HashLib.Crypto.SHA3.JH224();
                }

                public static IHash CreateJH256()
                {
                    return new HashLib.Crypto.SHA3.JH256();
                }

                public static IHash CreateJH384()
                {
                    return new HashLib.Crypto.SHA3.JH384();
                }

                public static IHash CreateJH512()
                {
                    return new HashLib.Crypto.SHA3.JH512();
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="a_hash_size">224, 256, 384, 512</param>
                /// <returns></returns>
                public static IHash CreateJH(HashLib.HashSize a_hash_size)
                {
                    switch (a_hash_size)
                    {
                        case HashLib.HashSize.HashSize224: return CreateJH224();
                        case HashLib.HashSize.HashSize256: return CreateJH256();
                        case HashLib.HashSize.HashSize384: return CreateJH384();
                        case HashLib.HashSize.HashSize512: return CreateJH512();
                        default: throw new ArgumentException();
                    }
                }

                public static IHash CreateBlake224()
                {
                    return new HashLib.Crypto.SHA3.Blake224();
                }

                public static IHash CreateBlake256()
                {
                    return new HashLib.Crypto.SHA3.Blake256();
                }

                public static IHash CreateBlake384()
                {
                    return new HashLib.Crypto.SHA3.Blake384();
                }

                public static IHash CreateBlake512()
                {
                    return new HashLib.Crypto.SHA3.Blake512();
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="a_hash_size">224, 256, 384, 512</param>
                /// <returns></returns>
                public static IHash CreateBlake(HashLib.HashSize a_hash_size)
                {
                    switch (a_hash_size)
                    {
                        case HashLib.HashSize.HashSize224: return CreateBlake224();
                        case HashLib.HashSize.HashSize256: return CreateBlake256();
                        case HashLib.HashSize.HashSize384: return CreateBlake384();
                        case HashLib.HashSize.HashSize512: return CreateBlake512();
                        default: throw new ArgumentException();
                    }
                    
                }

                public static IHash CreateBlueMidnightWish224()
                {
                    return new HashLib.Crypto.SHA3.BlueMidnightWish224();
                }

                public static IHash CreateBlueMidnightWish256()
                {
                    return new HashLib.Crypto.SHA3.BlueMidnightWish256();
                }

                public static IHash CreateBlueMidnightWish384()
                {
                    return new HashLib.Crypto.SHA3.BlueMidnightWish384();
                }

                public static IHash CreateBlueMidnightWish512()
                {
                    return new HashLib.Crypto.SHA3.BlueMidnightWish512();
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="a_hash_size">224, 256, 384, 512</param>
                /// <returns></returns>
                public static IHash CreateBlueMidnightWish(HashLib.HashSize a_hash_size)
                {
                    switch (a_hash_size)
                    {
                        case HashLib.HashSize.HashSize224: return CreateBlueMidnightWish224();
                        case HashLib.HashSize.HashSize256: return CreateBlueMidnightWish256();
                        case HashLib.HashSize.HashSize384: return CreateBlueMidnightWish384();
                        case HashLib.HashSize.HashSize512: return CreateBlueMidnightWish512();
                        default: throw new ArgumentException();
                    }
                }

                public static IHash CreateCubeHash224()
                {
                    return new HashLib.Crypto.SHA3.CubeHash224();
                }

                public static IHash CreateCubeHash256()
                {
                    return new HashLib.Crypto.SHA3.CubeHash256();
                }

                public static IHash CreateCubeHash384()
                {
                    return new HashLib.Crypto.SHA3.CubeHash384();
                }

                public static IHash CreateCubeHash512()
                {
                    return new HashLib.Crypto.SHA3.CubeHash512();
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="a_hash_size">224, 256, 384, 512</param>
                /// <returns></returns>
                public static IHash CreateCubeHash(HashLib.HashSize a_hash_size)
                {
                    switch (a_hash_size)
                    {
                        case HashLib.HashSize.HashSize224: return CreateCubeHash224();
                        case HashLib.HashSize.HashSize256: return CreateCubeHash256();
                        case HashLib.HashSize.HashSize384: return CreateCubeHash384();
                        case HashLib.HashSize.HashSize512: return CreateCubeHash512();
                        default: throw new ArgumentException();
                    }
                }

                public static IHash CreateEcho224()
                {
                    return new HashLib.Crypto.SHA3.Echo224();
                }

                public static IHash CreateEcho256()
                {
                    return new HashLib.Crypto.SHA3.Echo256();
                }

                public static IHash CreateEcho384()
                {
                    return new HashLib.Crypto.SHA3.Echo384();
                }

                public static IHash CreateEcho512()
                {
                    return new HashLib.Crypto.SHA3.Echo512();
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="a_hash_size">224, 256, 384, 512</param>
                /// <returns></returns>
                public static IHash CreateEcho(HashLib.HashSize a_hash_size)
                {
                    switch (a_hash_size)
                    {
                        case HashLib.HashSize.HashSize224: return CreateEcho224();
                        case HashLib.HashSize.HashSize256: return CreateEcho256();
                        case HashLib.HashSize.HashSize384: return CreateEcho384();
                        case HashLib.HashSize.HashSize512: return CreateEcho512();
                        default: throw new ArgumentException();
                    }
                }

                public static IHash CreateFugue224()
                {
                    return new HashLib.Crypto.SHA3.Fugue224();
                }

                public static IHash CreateFugue256()
                {
                    return new HashLib.Crypto.SHA3.Fugue256();
                }

                public static IHash CreateFugue384()
                {
                    return new HashLib.Crypto.SHA3.Fugue384();
                }

                public static IHash CreateFugue512()
                {
                    return new HashLib.Crypto.SHA3.Fugue512();
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="a_hash_size">224, 256, 384, 512</param>
                /// <returns></returns>
                public static IHash CreateFugue(HashLib.HashSize a_hash_size)
                {
                    switch (a_hash_size)
                    {
                        case HashLib.HashSize.HashSize224: return CreateFugue224();
                        case HashLib.HashSize.HashSize256: return CreateFugue256();
                        case HashLib.HashSize.HashSize384: return CreateFugue384();
                        case HashLib.HashSize.HashSize512: return CreateFugue512();
                        default: throw new ArgumentException();
                    }
                }

                public static IHash CreateGroestl224()
                {
                    return new HashLib.Crypto.SHA3.Groestl224();
                }

                public static IHash CreateGroestl256()
                {
                    return new HashLib.Crypto.SHA3.Groestl256();
                }

                public static IHash CreateGroestl384()
                {
                    return new HashLib.Crypto.SHA3.Groestl384();
                }

                public static IHash CreateGroestl512()
                {
                    return new HashLib.Crypto.SHA3.Groestl512();
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="a_hash_size">224, 256, 384, 512</param>
                /// <returns></returns>
                public static IHash CreateGroestl(HashLib.HashSize a_hash_size)
                {
                    switch (a_hash_size)
                    {
                        case HashLib.HashSize.HashSize224: return CreateGroestl224();
                        case HashLib.HashSize.HashSize256: return CreateGroestl256();
                        case HashLib.HashSize.HashSize384: return CreateGroestl384();
                        case HashLib.HashSize.HashSize512: return CreateGroestl512();
                        default: throw new ArgumentException();
                    }
                }

                public static IHash CreateHamsi224()
                {
                    return new HashLib.Crypto.SHA3.Hamsi224();
                }

                public static IHash CreateHamsi256()
                {
                    return new HashLib.Crypto.SHA3.Hamsi256();
                }

                public static IHash CreateHamsi384()
                {
                    return new HashLib.Crypto.SHA3.Hamsi384();
                }

                public static IHash CreateHamsi512()
                {
                    return new HashLib.Crypto.SHA3.Hamsi512();
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="a_hash_size">224, 256, 384, 512</param>
                /// <returns></returns>
                public static IHash CreateHamsi(HashLib.HashSize a_hash_size)
                {
                    switch (a_hash_size)
                    {
                        case HashLib.HashSize.HashSize224: return CreateHamsi224();
                        case HashLib.HashSize.HashSize256: return CreateHamsi256();
                        case HashLib.HashSize.HashSize384: return CreateHamsi384();
                        case HashLib.HashSize.HashSize512: return CreateHamsi512();
                        default: throw new ArgumentException();
                    }
                }

                public static IHash CreateKeccak224()
                {
                    return new HashLib.Crypto.SHA3.Keccak224();
                }

                public static IHash CreateKeccak256()
                {
                    return new HashLib.Crypto.SHA3.Keccak256();
                }

                public static IHash CreateKeccak384()
                {
                    return new HashLib.Crypto.SHA3.Keccak384();
                }

                public static IHash CreateKeccak512()
                {
                    return new HashLib.Crypto.SHA3.Keccak512();
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="a_hash_size">224, 256, 384, 512</param>
                /// <returns></returns>
                public static IHash CreateKeccak(HashLib.HashSize a_hash_size)
                {
                    switch (a_hash_size)
                    {
                        case HashLib.HashSize.HashSize224: return CreateKeccak224();
                        case HashLib.HashSize.HashSize256: return CreateKeccak256();
                        case HashLib.HashSize.HashSize384: return CreateKeccak384();
                        case HashLib.HashSize.HashSize512: return CreateKeccak512();
                        default: throw new ArgumentException();
                    }
                }

                public static IHash CreateLuffa224()
                {
                    return new HashLib.Crypto.SHA3.Luffa224();
                }

                public static IHash CreateLuffa256()
                {
                    return new HashLib.Crypto.SHA3.Luffa256();
                }

                public static IHash CreateLuffa384()
                {
                    return new HashLib.Crypto.SHA3.Luffa384();
                }

                public static IHash CreateLuffa512()
                {
                    return new HashLib.Crypto.SHA3.Luffa512();
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="a_hash_size">224, 256, 384, 512</param>
                /// <returns></returns>
                public static IHash CreateLuffa(HashLib.HashSize a_hash_size)
                {
                    switch (a_hash_size)
                    {
                        case HashLib.HashSize.HashSize224: return CreateLuffa224();
                        case HashLib.HashSize.HashSize256: return CreateLuffa256();
                        case HashLib.HashSize.HashSize384: return CreateLuffa384();
                        case HashLib.HashSize.HashSize512: return CreateLuffa512();
                        default: throw new ArgumentException();
                    }
                }

                public static IHash CreateShabal224()
                {
                    return new HashLib.Crypto.SHA3.Shabal224();
                }

                public static IHash CreateShabal256()
                {
                    return new HashLib.Crypto.SHA3.Shabal256();
                }

                public static IHash CreateShabal384()
                {
                    return new HashLib.Crypto.SHA3.Shabal384();
                }

                public static IHash CreateShabal512()
                {
                    return new HashLib.Crypto.SHA3.Shabal512();
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="a_hash_size">224, 256, 384, 512</param>
                /// <returns></returns>
                public static IHash CreateShabal(HashLib.HashSize a_hash_size)
                {
                    switch (a_hash_size)
                    {
                        case HashLib.HashSize.HashSize224: return CreateShabal224();
                        case HashLib.HashSize.HashSize256: return CreateShabal256();
                        case HashLib.HashSize.HashSize384: return CreateShabal384();
                        case HashLib.HashSize.HashSize512: return CreateShabal512();
                        default: throw new ArgumentException();
                    }
                }

                public static IHash CreateSHAvite3_224()
                {
                    return new HashLib.Crypto.SHA3.SHAvite3_224();
                }

                public static IHash CreateSHAvite3_256()
                {
                    return new HashLib.Crypto.SHA3.SHAvite3_256();
                }

                public static IHash CreateSHAvite3_384()
                {
                    return new HashLib.Crypto.SHA3.SHAvite3_384();
                }

                public static IHash CreateSHAvite3_512()
                {
                    return new HashLib.Crypto.SHA3.SHAvite3_512();
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="a_hash_size">224, 256, 384, 512</param>
                /// <returns></returns>
                public static IHash CreateSHAvite3(HashLib.HashSize a_hash_size)
                {
                    switch (a_hash_size)
                    {
                        case HashLib.HashSize.HashSize224: return CreateSHAvite3_224();
                        case HashLib.HashSize.HashSize256: return CreateSHAvite3_256();
                        case HashLib.HashSize.HashSize384: return CreateSHAvite3_384();
                        case HashLib.HashSize.HashSize512: return CreateSHAvite3_512();
                        default: throw new ArgumentException();
                    }
                }

                public static IHash CreateSIMD224()
                {
                    return new HashLib.Crypto.SHA3.SIMD224();
                }

                public static IHash CreateSIMD256()
                {
                    return new HashLib.Crypto.SHA3.SIMD256();
                }

                public static IHash CreateSIMD384()
                {
                    return new HashLib.Crypto.SHA3.SIMD384();
                }

                public static IHash CreateSIMD512()
                {
                    return new HashLib.Crypto.SHA3.SIMD512();
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="a_hash_size">224, 256, 384, 512</param>
                /// <returns></returns>
                public static IHash CreateSIMD(HashLib.HashSize a_hash_size)
                {
                    switch (a_hash_size)
                    {
                        case HashLib.HashSize.HashSize224: return CreateSIMD224();
                        case HashLib.HashSize.HashSize256: return CreateSIMD256();
                        case HashLib.HashSize.HashSize384: return CreateSIMD384();
                        case HashLib.HashSize.HashSize512: return CreateSIMD512();
                        default: throw new ArgumentException();
                    }
                }

                public static IHash CreateSkein224()
                {
                    return new HashLib.Crypto.SHA3.Skein224();
                }

                public static IHash CreateSkein256()
                {
                    return new HashLib.Crypto.SHA3.Skein256();
                }

                public static IHash CreateSkein384()
                {
                    return new HashLib.Crypto.SHA3.Skein384();
                }

                public static IHash CreateSkein512()
                {
                    return new HashLib.Crypto.SHA3.Skein512();
                }
                public static IHash CreateSkein512Custom()
                {
                    return new HashLib.Crypto.SHA3.Custom.Skein512();
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="a_hash_size">224, 256, 384, 512</param>
                /// <returns></returns>
                public static IHash CreateSkein(HashLib.HashSize a_hash_size)
                {
                    switch (a_hash_size)
                    {
                        case HashLib.HashSize.HashSize224: return CreateSkein224();
                        case HashLib.HashSize.HashSize256: return CreateSkein256();
                        case HashLib.HashSize.HashSize384: return CreateSkein384();
                        case HashLib.HashSize.HashSize512: return CreateSkein512();
                        default: throw new ArgumentException();
                    }
                }
            }

            public static class BuildIn
            {
                public static IHash CreateMD5CryptoServiceProvider()
                {
                    return new HashLib.Crypto.BuildIn.MD5CryptoServiceProvider();
                }

                public static IHash CreateRIPEMD160Managed()
                {
                    return new HashLib.Crypto.BuildIn.RIPEMD160Managed();
                }

                public static IHash CreateSHA1Cng()
                {
                    return new HashLib.Crypto.BuildIn.SHA1Cng();
                }

                public static IHash CreateSHA1CryptoServiceProvider()
                {
                    return new HashLib.Crypto.BuildIn.SHA1CryptoServiceProvider();
                }

                public static IHash CreateSHA1Managed()
                {
                    return new HashLib.Crypto.BuildIn.SHA1Managed();
                }

                public static IHash CreateSHA256Cng()
                {
                    return new HashLib.Crypto.BuildIn.SHA256Cng();
                }

                public static IHash CreateSHA256CryptoServiceProvider()
                {
                    return new HashLib.Crypto.BuildIn.SHA256CryptoServiceProvider();
                }

                public static IHash CreateSHA256Managed()
                {
                    return new HashLib.Crypto.BuildIn.SHA256Managed();
                }

                public static IHash CreateSHA384Cng()
                {
                    return new HashLib.Crypto.BuildIn.SHA384Cng();
                }

                public static IHash CreateSHA384CryptoServiceProvider()
                {
                    return new HashLib.Crypto.BuildIn.SHA384CryptoServiceProvider();
                }

                public static IHash CreateSHA384Managed()
                {
                    return new HashLib.Crypto.BuildIn.SHA384Managed();
                }

                public static IHash CreateSHA512Cng()
                {
                    return new HashLib.Crypto.BuildIn.SHA512Cng();
                }

                public static IHash CreateSHA512CryptoServiceProvider()
                {
                    return new HashLib.Crypto.BuildIn.SHA512CryptoServiceProvider();
                }

                public static IHash CreateSHA512Managed()
                {
                    return new HashLib.Crypto.BuildIn.SHA512Managed();
                }
            }

            public static IHash CreateGost()
            {
                return new HashLib.Crypto.Gost();
            }

            public static IHash CreateGrindahl256()
            {
                return new HashLib.Crypto.Grindahl256();
            }

            public static IHash CreateGrindahl512()
            {
                return new HashLib.Crypto.Grindahl512();
            }

            public static IHash CreateHAS160()
            {
                return new HashLib.Crypto.HAS160();
            }

            public static IHash CreateHaval_3_128()
            {
                return new HashLib.Crypto.Haval_3_128();
            }

            public static IHash CreateHaval_4_128()
            {
                return new HashLib.Crypto.Haval_4_128();
            }

            public static IHash CreateHaval_5_128()
            {
                return new HashLib.Crypto.Haval_5_128();
            }

            public static IHash CreateHaval_3_160()
            {
                return new HashLib.Crypto.Haval_3_160();
            }

            public static IHash CreateHaval_4_160()
            {
                return new HashLib.Crypto.Haval_4_160();
            }

            public static IHash CreateHaval_5_160()
            {
                return new HashLib.Crypto.Haval_5_160();
            }

            public static IHash CreateHaval_3_192()
            {
                return new HashLib.Crypto.Haval_3_192();
            }

            public static IHash CreateHaval_4_192()
            {
                return new HashLib.Crypto.Haval_4_192();
            }

            public static IHash CreateHaval_5_192()
            {
                return new HashLib.Crypto.Haval_5_192();
            }

            public static IHash CreateHaval_3_224()
            {
                return new HashLib.Crypto.Haval_3_224();
            }

            public static IHash CreateHaval_4_224()
            {
                return new HashLib.Crypto.Haval_4_224();
            }

            public static IHash CreateHaval_5_224()
            {
                return new HashLib.Crypto.Haval_5_224();
            }

            public static IHash CreateHaval_3_256()
            {
                return new HashLib.Crypto.Haval_3_256();
            }

            public static IHash CreateHaval_4_256()
            {
                return new HashLib.Crypto.Haval_4_256();
            }

            public static IHash CreateHaval_5_256()
            {
                return new HashLib.Crypto.Haval_5_256();
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="a_rounds">3, 4, 5</param>
            /// <param name="a_hash_size">128, 160, 192, 224, 256</param>
            /// <returns></returns>
            public static IHash CreateHaval(HashRounds a_rounds, HashLib.HashSize a_hash_size)
            {
                switch (a_rounds)
                {
                    case HashRounds.Rounds3:

                        switch (a_hash_size)
                        {
                            case HashLib.HashSize.HashSize128: return CreateHaval_3_128();
                            case HashLib.HashSize.HashSize160: return CreateHaval_3_160();
                            case HashLib.HashSize.HashSize192: return CreateHaval_3_192();
                            case HashLib.HashSize.HashSize224: return CreateHaval_3_224();
                            case HashLib.HashSize.HashSize256: return CreateHaval_3_256();
                            default: throw new ArgumentException();
                        }

                    case HashRounds.Rounds4:

                        switch (a_hash_size)
                        {
                            case HashLib.HashSize.HashSize128: return CreateHaval_4_128();
                            case HashLib.HashSize.HashSize160: return CreateHaval_4_160();
                            case HashLib.HashSize.HashSize192: return CreateHaval_4_192();
                            case HashLib.HashSize.HashSize224: return CreateHaval_4_224();
                            case HashLib.HashSize.HashSize256: return CreateHaval_4_256();
                            default: throw new ArgumentException();
                        }

                    case HashRounds.Rounds5:

                        switch (a_hash_size)
                        {
                            case HashLib.HashSize.HashSize128: return CreateHaval_5_128();
                            case HashLib.HashSize.HashSize160: return CreateHaval_5_160();
                            case HashLib.HashSize.HashSize192: return CreateHaval_5_192();
                            case HashLib.HashSize.HashSize224: return CreateHaval_5_224();
                            case HashLib.HashSize.HashSize256: return CreateHaval_5_256();
                            default: throw new ArgumentException();
                        }

                    default: throw new ArgumentException();
                }
            }

            public static IHash CreateMD2()
            {
                return new HashLib.Crypto.MD2();
            }

            public static IHash CreateMD4()
            {
                return new HashLib.Crypto.MD4();
            }

            public static IHash CreateMD5()
            {
                return new HashLib.Crypto.MD5();
            }

            public static IHash CreatePanama()
            {
                return new HashLib.Crypto.Panama();
            }

            public static IHash CreateRadioGatun32()
            {
                return new HashLib.Crypto.RadioGatun32();
            }

            public static IHash CreateRadioGatun64()
            {
                return new HashLib.Crypto.RadioGatun64();
            }

            public static IHash CreateRIPEMD()
            {
                return new HashLib.Crypto.RIPEMD();
            }

            public static IHash CreateRIPEMD128()
            {
                return new HashLib.Crypto.RIPEMD128();
            }

            public static IHash CreateRIPEMD160()
            {
                return new HashLib.Crypto.RIPEMD160();
            }

            public static IHash CreateRIPEMD256()
            {
                return new HashLib.Crypto.RIPEMD256();
            }

            public static IHash CreateRIPEMD320()
            {
                return new HashLib.Crypto.RIPEMD320();
            }

            public static IHash CreateSHA0()
            {
                return new HashLib.Crypto.SHA0();
            }

            public static IHash CreateSHA1()
            {
                return new HashLib.Crypto.SHA1();
            }

            public static IHash CreateSHA224()
            {
                return new HashLib.Crypto.SHA224();
            }

            public static IHash CreateSHA256()
            {
                return new HashLib.Crypto.SHA256();
            }

            public static IHash CreateSHA384()
            {
                return new HashLib.Crypto.SHA384();
            }

            public static IHash CreateSHA512()
            {
                return new HashLib.Crypto.SHA512();
            }

            public static IHash CreateSnefru_4_128()
            {
                return new HashLib.Crypto.Snefru_4_128();
            }

            public static IHash CreateSnefru_4_256()
            {
                return new HashLib.Crypto.Snefru_4_256();
            }

            public static IHash CreateSnefru_8_128()
            {
                return new HashLib.Crypto.Snefru_8_128();
            }

            public static IHash CreateSnefru_8_256()
            {
                return new HashLib.Crypto.Snefru_8_256();
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="a_rounds">4, 8</param>
            /// <param name="a_hash_size">128, 256</param>
            /// <returns></returns>
            public static IHash CreateSnefru(HashRounds a_rounds, HashLib.HashSize a_hash_size)
            {
                switch (a_rounds)
                {
                    case HashRounds.Rounds4:

                        switch (a_hash_size)
                        {
                            case HashLib.HashSize.HashSize128: return CreateSnefru_4_128();
                            case HashLib.HashSize.HashSize256: return CreateSnefru_4_256();
                            default: throw new ArgumentException();
                        }

                    case HashRounds.Rounds8:

                        switch (a_hash_size)
                        {
                            case HashLib.HashSize.HashSize128: return CreateSnefru_8_128();
                            case HashLib.HashSize.HashSize256: return CreateSnefru_8_256();
                            default: throw new ArgumentException();
                        }

                    default: throw new ArgumentException();
                }
            }

            public static IHash CreateTiger_3_192()
            {
                return new HashLib.Crypto.Tiger_3_192();
            }

            public static IHash CreateTiger_4_192()
            {
                return new HashLib.Crypto.Tiger_4_192();
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="a_rounds">3, 4</param>
            /// <returns></returns>
            public static IHash CreateTiger(HashRounds a_rounds)
            {
                switch (a_rounds)
                {
                    case HashRounds.Rounds3: return CreateTiger_3_192();
                    case HashRounds.Rounds4: return CreateTiger_4_192();
                    default: throw new ArgumentException();
                }
            }

            public static IHash CreateTiger2()
            {
                return new HashLib.Crypto.Tiger2();
            }

            public static IHash CreateWhirlpool()
            {
                return new HashLib.Crypto.Whirlpool();
            }
        }

        public static class HMAC
        {
            public static IHMAC CreateHMAC(IHash a_hash)
            {
                if (a_hash is IHMAC)
                {
                    return (IHMAC)a_hash;
                }
                else if (a_hash is IHasHMACBuildIn)
                {
                    IHasHMACBuildIn h = (IHasHMACBuildIn)a_hash;
                    return new HMACBuildInAdapter(h.GetBuildHMAC(), h.BlockSize);
                }
                else
                {
                    return new HMACNotBuildInAdapter(a_hash);
                }
            }
        }

        public static class Wrappers
        {
            public static System.Security.Cryptography.HashAlgorithm HashToHashAlgorithm(IHash a_hash)
            {
                return new HashAlgorithmWrapper(a_hash);
            }

            public static IHash HashAlgorithmToHash(System.Security.Cryptography.HashAlgorithm a_hash, 
                int a_block_size = -1)
            {
                return new HashCryptoBuildIn(a_hash, a_block_size);
            }
        }
    }
}
