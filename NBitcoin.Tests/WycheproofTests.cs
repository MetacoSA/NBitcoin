using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
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

		[WycheproofFact("ecdsa")]
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

		[WycheproofFact("hmac")]
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

		[WycheproofFact("pbkdf2")]
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

		private const string VectorUrl = "https://raw.githubusercontent.com/C2SP/wycheproof/65ca494727a52944c204441c860b039e6786b99b/testvectors_v1/";
		private static readonly Dictionary<string, string> VectorHashes = new Dictionary<string, string>
		{
			{ "ecdsa_secp256k1_sha256_p1363_test.json", "7a339efc7134fb2495cd32afdbd692e0f86427d3c24e9073f6a7d858bb8788d2" },
			{ "ecdsa_secp256k1_sha256_test.json", "43db761c0a2eae71fb0755d355d5130e28ce64a5b07846cf27e7072082597a81" },
			{ "ecdsa_secp256k1_sha3_256_test.json", "4748f02694d2391576cf28c8cb3d13149739b5602eeb079deae4577e111afbc1" },
			{ "ecdsa_secp256k1_sha3_512_test.json", "d1760ce172483843ab4003e8fd78e16e6fc003cdf7190f1fe7d34dba1842c05c" },
			{ "ecdsa_secp256k1_sha512_p1363_test.json", "7e12bee4519587d2b31fe7a03a617fad771f352b505edb41d063c35a4023dbc7" },
			{ "ecdsa_secp256k1_sha512_test.json", "b614f432e4110bcdd3d792180a195e5ba33da7704db3d1298d42312cc64acd40" },
			{ "ecdsa_secp256k1_shake128_p1363_test.json", "30ead3b9c2845717115bc75cedd90621e35b3c096b2047af91fb7811e059fb9d" },
			{ "ecdsa_secp256k1_shake128_test.json", "861bc8936ce7869155076f614eb05121acba71868c1b856bcf7258d646e95ecb" },
			{ "ecdsa_secp256k1_shake256_p1363_test.json", "24a5fa4887f8167b135f07f7da603acaf125e39994a439bda63f43a74555178b" },
			{ "ecdsa_secp256k1_shake256_test.json", "7f3e61cb13bf976a9567639b00c0f727e8807d4b69a375562f0d14ec0afe1dd0" },
			{ "hmac_sha256_test.json", "2d201cfa61d1bf95e6f5d07d96634b4a348b31e8eaa277ad7c8d09677b7a743f" },
			{ "hmac_sha512_test.json", "b6c90477bdb4a6fc8ee3d1f7b2c0b69a8dfffab34718abaa6cabd71cc2ba1207" },
			{ "pbkdf2_hmacsha1_test.json", "5ef5307e119bd118da50ad276426ed3baade2d093e93b643a1d70be0bf0b861a" },
			{ "pbkdf2_hmacsha256_test.json", "1bf37af2cefe40c829ee9ecebb3505bb6424be8824bc97aa3e1c2076e860d192" },
			{ "pbkdf2_hmacsha384_test.json", "892613e78d38f1b81df77f1321a59a9fbf6d5c5d1c702126b9f709d7fd242483" },
			{ "pbkdf2_hmacsha512_test.json", "6a764a448b7a283478340d58f5dc20455524e8589a935fb0d66fb851e997b1c8" },
		};

		// xUnit v2 cannot dynamically skip during test execution. Fetch during discovery so
		// a connection failure can be reported as a real skipped fact.
		public sealed class WycheproofFactAttribute : FactAttribute
		{
			public WycheproofFactAttribute(string family)
			{
				string[] files = family == "ecdsa" ? EcdsaFiles : family == "hmac" ? HmacFiles : Pbkdf2Files;
				try
				{
					foreach (string file in files) EnsureDownloaded(file);
				}
				catch (Exception ex) when (IsUnavailable(ex))
				{
					Skip = "Wycheproof vectors unavailable (network connection failed or timed out).";
				}
				catch (Exception)
				{
					// Let the test report HTTP, disk and other errors as failures.
				}
			}
		}

		private static bool IsUnavailable(Exception ex)
		{
			for (Exception? cause = ex; cause != null; cause = cause.InnerException)
			{
				if (cause is SocketException || cause is TaskCanceledException) return true;
				if (cause is WebException web && (web.Status == WebExceptionStatus.ConnectFailure ||
					web.Status == WebExceptionStatus.NameResolutionFailure || web.Status == WebExceptionStatus.ProxyNameResolutionFailure ||
					web.Status == WebExceptionStatus.Timeout)) return true;
			}
			return false;
		}

		private static string VectorPath(string file)
		{
			return Path.Combine(AppContext.BaseDirectory, "wycheproof", file);
		}

		private static void EnsureDownloaded(string file)
		{
			string path = VectorPath(file);
			if (File.Exists(path)) return;
			using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) })
			{
				byte[] bytes = client.GetByteArrayAsync(VectorUrl + file).GetAwaiter().GetResult();
				Directory.CreateDirectory(Path.GetDirectoryName(path)!);
				string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
				try
				{
					File.WriteAllBytes(temporary, bytes);
					try { File.Move(temporary, path); }
					catch (IOException) when (File.Exists(path)) { }
				}
				finally { if (File.Exists(temporary)) File.Delete(temporary); }
			}
		}

		private static JObject ReadVector(string file)
		{
			EnsureDownloaded(file);
			byte[] bytes = File.ReadAllBytes(VectorPath(file));
			using (SHA256 hash = SHA256.Create())
			{
				string actual = BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
				Assert.True(actual == VectorHashes[file], file + ": SHA-256 mismatch: expected " + VectorHashes[file] + ", got " + actual);
			}
			return JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes));
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
