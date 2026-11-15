using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NBitcoin.BouncyCastle.Crypto.Digests;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using Newtonsoft.Json.Linq;
using Xunit;

namespace NBitcoin.Tests
{
	public class WycheproofTests
	{
		private static readonly string[] EcdsaFiles =
		{
			"ecdsa_secp256k1_sha256_p1363_test.json",
			"ecdsa_secp256k1_sha256_test.json",
			"ecdsa_secp256k1_sha3_256_test.json",
			"ecdsa_secp256k1_sha3_512_test.json",
			"ecdsa_secp256k1_sha512_p1363_test.json",
			"ecdsa_secp256k1_sha512_test.json",
			"ecdsa_secp256k1_shake128_p1363_test.json",
			"ecdsa_secp256k1_shake128_test.json",
			"ecdsa_secp256k1_shake256_p1363_test.json",
			"ecdsa_secp256k1_shake256_test.json"
		};

		private static readonly string[] HmacFiles =
		{
			"hmac_sha256_test.json",
			"hmac_sha512_test.json"
		};

		private static readonly string[] Pbkdf2Files =
		{
			"pbkdf2_hmacsha1_test.json",
			"pbkdf2_hmacsha256_test.json",
			"pbkdf2_hmacsha384_test.json",
			"pbkdf2_hmacsha512_test.json"
		};

		private static readonly HashSet<string> LaxDerFlags = new HashSet<string>(StringComparer.Ordinal)
		{
			"BerEncodedSignature",
			"InvalidEncoding",
			"MissingZero",
			"ModifiedSignature"
		};

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void EcdsaSecp256k1()
		{
			foreach (string file in EcdsaFiles)
			{
				JObject vector = ReadVector(file);
				foreach (JObject group in vector["testGroups"]!.Children<JObject>())
				{
					bool p1363 = group.Value<string>("type") == "EcdsaP1363Verify";
					string hashName = group.Value<string>("sha")!;
					byte[] point = Hex(group["publicKey"]!["uncompressed"]!);
					var publicKey = new PubKey(point);

					foreach (JObject test in group["tests"]!.Children<JObject>())
					{
						string context = file + ": tcId " + test.Value<int>("tcId");
						byte[] digest = Hash(hashName, Hex(test["msg"]!));
						byte[] signatureBytes = Hex(test["sig"]!);
						bool actual = false;

						try
						{
							ECDSASignature signature;
							if (p1363)
							{
								if (!ECDSASignature.TryParseFromCompact(signatureBytes, out signature!))
								{
									AssertResult(test, false, context);
									continue;
								}
							}
							else
							{
								signature = ECDSASignature.FromDER(signatureBytes);
							}

							actual = publicKey.Verify(new uint256(digest.Take(32).ToArray()), signature);
						}
						catch (FormatException)
						{
							actual = false;
						}
						catch (ArgumentException)
						{
							actual = false;
						}
						catch (CryptographicException)
						{
							actual = false;
						}

						if (!p1363 && actual && test["flags"]!.Values<string>().Any(flag => LaxDerFlags.Contains(flag))) continue; // NBitcoin intentionally accepts historical lax DER encodings.
						AssertResult(test, actual, context);
					}
				}
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void HmacSha256AndSha512()
		{
			foreach (string file in HmacFiles)
			{
				JObject vector = ReadVector(file);
				string algorithm = vector.Value<string>("algorithm")!;
				foreach (JObject group in vector["testGroups"]!.Children<JObject>())
				{
					foreach (JObject test in group["tests"]!.Children<JObject>())
					{
						string context = file + ": tcId " + test.Value<int>("tcId");
						byte[] mac = algorithm == "HMACSHA256"
							? Hashes.HMACSHA256(Hex(test["key"]!), Hex(test["msg"]!))
							: Hashes.HMACSHA512(Hex(test["key"]!), Hex(test["msg"]!));
						byte[] tag = Hex(test["tag"]!);
						bool actual = tag.Length <= mac.Length && tag.SequenceEqual(mac.Take(tag.Length));
						AssertResult(test, actual, context);
					}
				}
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void Pbkdf2HmacSha1Sha256Sha384AndSha512()
		{
			foreach (string file in Pbkdf2Files)
			{
				JObject vector = ReadVector(file);
				string algorithm = vector.Value<string>("algorithm")!;
				foreach (JObject group in vector["testGroups"]!.Children<JObject>())
				{
					foreach (JObject test in group["tests"]!.Children<JObject>())
					{
						string context = file + ": tcId " + test.Value<int>("tcId");
						byte[] password = Hex(test["password"]!);
						byte[] salt = Hex(test["salt"]!);
						using (KeyedHashAlgorithm hmac = CreatePbkdf2Hmac(algorithm, password))
						{
#pragma warning disable CS0618 // Keep coverage for NBitcoin's public PBKDF2 wrapper.
							byte[] derived = Pbkdf2.ComputeDerivedKey(
								hmac,
								salt,
								test.Value<int>("iterationCount"),
								test.Value<int>("dkLen"));
#pragma warning restore CS0618
							Assert.True(derived.SequenceEqual(Hex(test["dk"]!)), context);
						}
					}
				}
			}
		}

		private static JObject ReadVector(string file)
		{
			string path = Path.Combine(AppContext.BaseDirectory, "data", "wycheproof", file);
			return JObject.Parse(File.ReadAllText(path));
		}

		private static byte[] Hex(JToken value)
		{
			return Encoders.Hex.DecodeData(value.Value<string>()!);
		}

		private static void AssertResult(JObject test, bool actual, string context)
		{
			switch (test.Value<string>("result"))
			{
				case "valid":
					Assert.True(actual, context);
					break;
				case "invalid":
					Assert.False(actual, context);
					break;
				case "acceptable":
					break;
				default:
					throw new InvalidDataException(context + ": unknown result");
			}
		}

		private static byte[] Hash(string algorithm, byte[] message)
		{
			switch (algorithm)
			{
				case "SHA-256":
					using (SHA256 hash = SHA256.Create()) return hash.ComputeHash(message);
				case "SHA-512":
					using (SHA512 hash = SHA512.Create()) return hash.ComputeHash(message);
				case "SHA3-256":
#if NO_BC
					byte[] sha3_256 = SHA3_256.HashData(message);
					Assert.True(sha3_256.SequenceEqual(Sha3(message, 256)), "SHA3-256 digest mismatch");
					return sha3_256;
#else
					return Sha3(message, 256);
#endif
				case "SHA3-512":
#if NO_BC
					byte[] sha3_512 = SHA3_512.HashData(message);
					Assert.True(sha3_512.SequenceEqual(Sha3(message, 512)), "SHA3-512 digest mismatch");
					return sha3_512;
#else
					return Sha3(message, 512);
#endif
				case "SHAKE128":
#if NO_BC
					byte[] shake128 = Shake128.HashData(message, 32);
					Assert.True(shake128.SequenceEqual(Shake(message, 128)), "SHAKE128 digest mismatch");
					return shake128;
#else
					return Shake(message, 128);
#endif
				case "SHAKE256":
#if NO_BC
					byte[] shake256 = Shake256.HashData(message, 32);
					Assert.True(shake256.SequenceEqual(Shake(message, 256)), "SHAKE256 digest mismatch");
					return shake256;
#else
					return Shake(message, 256);
#endif
				default:
					throw new NotSupportedException(algorithm);
			}
		}

		private static byte[] Sha3(byte[] message, int bitLength)
		{
			var digest = new Sha3Digest(bitLength);
			digest.BlockUpdate(message, 0, message.Length);
			byte[] output = new byte[digest.GetDigestSize()];
			digest.DoFinal(output, 0);
			return output;
		}

		private static byte[] Shake(byte[] message, int strength)
		{
			var digest = new WycheproofShakeDigest(strength);
			digest.BlockUpdate(message, 0, message.Length);
			byte[] output = new byte[32];
			digest.FinishShake(output);
			return output;
		}

		private sealed class WycheproofShakeDigest : KeccakDigest
		{
			public WycheproofShakeDigest(int strength)
				: base(strength)
			{
				fixedOutputLength = 256;
			}

			public void FinishShake(byte[] output)
			{
				DoFinal(output, 0, 0x0F, 4);
			}
		}

		private static KeyedHashAlgorithm CreatePbkdf2Hmac(string algorithm, byte[] key)
		{
			switch (algorithm)
			{
				case "PBKDF2-HMACSHA1": return new HMACSHA1(key);
				case "PBKDF2-HMACSHA256": return new HMACSHA256(key);
				case "PBKDF2-HMACSHA384": return new HMACSHA384(key);
				case "PBKDF2-HMACSHA512": return new HMACSHA512(key);
				default: throw new NotSupportedException(algorithm);
			}
		}
	}
}
