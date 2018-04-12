using System;

namespace NBitcoin.Altcoins.HashX11
{
    internal static class HashFactory
    {
		internal static class Crypto
        {
			internal static class SHA3
            {
                internal static IHash CreateJH224()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.JH224();
                }

                internal static IHash CreateJH256()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.JH256();
                }

                internal static IHash CreateJH384()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.JH384();
                }

                internal static IHash CreateJH512()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.JH512();
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="a_hash_size">224, 256, 384, 512</param>
                /// <returns></returns>
                internal static IHash CreateJH(NBitcoin.Altcoins.HashX11.HashSize a_hash_size)
                {
                    switch (a_hash_size)
                    {
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize224: return CreateJH224();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize256: return CreateJH256();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize384: return CreateJH384();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize512: return CreateJH512();
                        default: throw new ArgumentException();
                    }
                }

                internal static IHash CreateBlake224()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Blake224();
                }

                internal static IHash CreateBlake256()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Blake256();
                }

                internal static IHash CreateBlake384()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Blake384();
                }

                internal static IHash CreateBlake512()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Blake512();
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="a_hash_size">224, 256, 384, 512</param>
                /// <returns></returns>
                internal static IHash CreateBlake(NBitcoin.Altcoins.HashX11.HashSize a_hash_size)
                {
                    switch (a_hash_size)
                    {
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize224: return CreateBlake224();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize256: return CreateBlake256();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize384: return CreateBlake384();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize512: return CreateBlake512();
                        default: throw new ArgumentException();
                    }
                    
                }

                internal static IHash CreateBlueMidnightWish224()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.BlueMidnightWish224();
                }

                internal static IHash CreateBlueMidnightWish256()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.BlueMidnightWish256();
                }

                internal static IHash CreateBlueMidnightWish384()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.BlueMidnightWish384();
                }

                internal static IHash CreateBlueMidnightWish512()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.BlueMidnightWish512();
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="a_hash_size">224, 256, 384, 512</param>
                /// <returns></returns>
                internal static IHash CreateBlueMidnightWish(NBitcoin.Altcoins.HashX11.HashSize a_hash_size)
                {
                    switch (a_hash_size)
                    {
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize224: return CreateBlueMidnightWish224();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize256: return CreateBlueMidnightWish256();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize384: return CreateBlueMidnightWish384();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize512: return CreateBlueMidnightWish512();
                        default: throw new ArgumentException();
                    }
                }

                internal static IHash CreateCubeHash224()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.CubeHash224();
                }

                internal static IHash CreateCubeHash256()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.CubeHash256();
                }

                internal static IHash CreateCubeHash384()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.CubeHash384();
                }

                internal static IHash CreateCubeHash512()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.CubeHash512();
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="a_hash_size">224, 256, 384, 512</param>
                /// <returns></returns>
                internal static IHash CreateCubeHash(NBitcoin.Altcoins.HashX11.HashSize a_hash_size)
                {
                    switch (a_hash_size)
                    {
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize224: return CreateCubeHash224();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize256: return CreateCubeHash256();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize384: return CreateCubeHash384();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize512: return CreateCubeHash512();
                        default: throw new ArgumentException();
                    }
                }

                internal static IHash CreateEcho224()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Echo224();
                }

                internal static IHash CreateEcho256()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Echo256();
                }

                internal static IHash CreateEcho384()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Echo384();
                }

                internal static IHash CreateEcho512()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Echo512();
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="a_hash_size">224, 256, 384, 512</param>
                /// <returns></returns>
                internal static IHash CreateEcho(NBitcoin.Altcoins.HashX11.HashSize a_hash_size)
                {
                    switch (a_hash_size)
                    {
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize224: return CreateEcho224();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize256: return CreateEcho256();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize384: return CreateEcho384();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize512: return CreateEcho512();
                        default: throw new ArgumentException();
                    }
                }

                internal static IHash CreateFugue224()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Fugue224();
                }

                internal static IHash CreateFugue256()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Fugue256();
                }

                internal static IHash CreateFugue384()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Fugue384();
                }

                internal static IHash CreateFugue512()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Fugue512();
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="a_hash_size">224, 256, 384, 512</param>
                /// <returns></returns>
                internal static IHash CreateFugue(NBitcoin.Altcoins.HashX11.HashSize a_hash_size)
                {
                    switch (a_hash_size)
                    {
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize224: return CreateFugue224();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize256: return CreateFugue256();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize384: return CreateFugue384();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize512: return CreateFugue512();
                        default: throw new ArgumentException();
                    }
                }

                internal static IHash CreateGroestl224()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Groestl224();
                }

                internal static IHash CreateGroestl256()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Groestl256();
                }

                internal static IHash CreateGroestl384()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Groestl384();
                }

                internal static IHash CreateGroestl512()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Groestl512();
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="a_hash_size">224, 256, 384, 512</param>
                /// <returns></returns>
                internal static IHash CreateGroestl(NBitcoin.Altcoins.HashX11.HashSize a_hash_size)
                {
                    switch (a_hash_size)
                    {
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize224: return CreateGroestl224();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize256: return CreateGroestl256();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize384: return CreateGroestl384();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize512: return CreateGroestl512();
                        default: throw new ArgumentException();
                    }
                }

                internal static IHash CreateHamsi224()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Hamsi224();
                }

                internal static IHash CreateHamsi256()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Hamsi256();
                }

                internal static IHash CreateHamsi384()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Hamsi384();
                }

                internal static IHash CreateHamsi512()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Hamsi512();
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="a_hash_size">224, 256, 384, 512</param>
                /// <returns></returns>
                internal static IHash CreateHamsi(NBitcoin.Altcoins.HashX11.HashSize a_hash_size)
                {
                    switch (a_hash_size)
                    {
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize224: return CreateHamsi224();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize256: return CreateHamsi256();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize384: return CreateHamsi384();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize512: return CreateHamsi512();
                        default: throw new ArgumentException();
                    }
                }

                internal static IHash CreateKeccak224()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Keccak224();
                }

                internal static IHash CreateKeccak256()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Keccak256();
                }

                internal static IHash CreateKeccak384()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Keccak384();
                }

                internal static IHash CreateKeccak512()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Keccak512();
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="a_hash_size">224, 256, 384, 512</param>
                /// <returns></returns>
                internal static IHash CreateKeccak(NBitcoin.Altcoins.HashX11.HashSize a_hash_size)
                {
                    switch (a_hash_size)
                    {
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize224: return CreateKeccak224();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize256: return CreateKeccak256();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize384: return CreateKeccak384();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize512: return CreateKeccak512();
                        default: throw new ArgumentException();
                    }
                }

                internal static IHash CreateLuffa224()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Luffa224();
                }

                internal static IHash CreateLuffa256()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Luffa256();
                }

                internal static IHash CreateLuffa384()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Luffa384();
                }

                internal static IHash CreateLuffa512()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Luffa512();
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="a_hash_size">224, 256, 384, 512</param>
                /// <returns></returns>
                internal static IHash CreateLuffa(NBitcoin.Altcoins.HashX11.HashSize a_hash_size)
                {
                    switch (a_hash_size)
                    {
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize224: return CreateLuffa224();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize256: return CreateLuffa256();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize384: return CreateLuffa384();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize512: return CreateLuffa512();
                        default: throw new ArgumentException();
                    }
                }

                internal static IHash CreateShabal224()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Shabal224();
                }

                internal static IHash CreateShabal256()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Shabal256();
                }

                internal static IHash CreateShabal384()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Shabal384();
                }

                internal static IHash CreateShabal512()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Shabal512();
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="a_hash_size">224, 256, 384, 512</param>
                /// <returns></returns>
                internal static IHash CreateShabal(NBitcoin.Altcoins.HashX11.HashSize a_hash_size)
                {
                    switch (a_hash_size)
                    {
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize224: return CreateShabal224();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize256: return CreateShabal256();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize384: return CreateShabal384();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize512: return CreateShabal512();
                        default: throw new ArgumentException();
                    }
                }

                internal static IHash CreateSHAvite3_224()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.SHAvite3_224();
                }

                internal static IHash CreateSHAvite3_256()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.SHAvite3_256();
                }

                internal static IHash CreateSHAvite3_384()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.SHAvite3_384();
                }

                internal static IHash CreateSHAvite3_512()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.SHAvite3_512();
                }

                internal static IHash CreateSHAvite3_512_Custom()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Custom.SHAvite3_512();
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="a_hash_size">224, 256, 384, 512</param>
                /// <returns></returns>
                internal static IHash CreateSHAvite3(NBitcoin.Altcoins.HashX11.HashSize a_hash_size)
                {
                    switch (a_hash_size)
                    {
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize224: return CreateSHAvite3_224();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize256: return CreateSHAvite3_256();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize384: return CreateSHAvite3_384();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize512: return CreateSHAvite3_512();
                        default: throw new ArgumentException();
                    }
                }

                internal static IHash CreateSIMD224()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.SIMD224();
                }

                internal static IHash CreateSIMD256()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.SIMD256();
                }

                internal static IHash CreateSIMD384()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.SIMD384();
                }

                internal static IHash CreateSIMD512()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.SIMD512();
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="a_hash_size">224, 256, 384, 512</param>
                /// <returns></returns>
                internal static IHash CreateSIMD(NBitcoin.Altcoins.HashX11.HashSize a_hash_size)
                {
                    switch (a_hash_size)
                    {
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize224: return CreateSIMD224();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize256: return CreateSIMD256();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize384: return CreateSIMD384();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize512: return CreateSIMD512();
                        default: throw new ArgumentException();
                    }
                }

                internal static IHash CreateSkein224()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Skein224();
                }

                internal static IHash CreateSkein256()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Skein256();
                }

                internal static IHash CreateSkein384()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Skein384();
                }

                internal static IHash CreateSkein512()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Skein512();
                }
                internal static IHash CreateSkein512_Custom()
                {
                    return new NBitcoin.Altcoins.HashX11.Crypto.SHA3.Custom.Skein512();
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="a_hash_size">224, 256, 384, 512</param>
                /// <returns></returns>
                internal static IHash CreateSkein(NBitcoin.Altcoins.HashX11.HashSize a_hash_size)
                {
                    switch (a_hash_size)
                    {
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize224: return CreateSkein224();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize256: return CreateSkein256();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize384: return CreateSkein384();
                        case NBitcoin.Altcoins.HashX11.HashSize.HashSize512: return CreateSkein512();
                        default: throw new ArgumentException();
                    }
                }
            }
        }
	}
}
