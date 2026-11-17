using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Security.Cryptography;
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
			"ecdsa_secp256k1_sha512_p1363_test.json",
			"ecdsa_secp256k1_sha512_test.json"
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

		// NBitcoin parses each case to the r and s of valid tcId 7 in the same file.
		private static readonly Dictionary<string, HashSet<int>> LaxDerCases = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal)
		{
			{ "ecdsa_secp256k1_sha256_test.json", new HashSet<int>
				{
					6, // Legacy: ASN encoding of r misses leading 0
					8, // length of sequence [r, s] uses long form encoding
					9, // length of sequence [r, s] contains a leading 0
					10, // length of sequence [r, s] uses 70 instead of 69
					11, // length of sequence [r, s] uses 68 instead of 69
					12, // uint32 overflow in length of sequence [r, s]
					13, // uint64 overflow in length of sequence [r, s]
					14, // length of sequence [r, s] = 2**31 - 1
					15, // length of sequence [r, s] = 2**31
					16, // length of sequence [r, s] = 2**32 - 1
					17, // length of sequence [r, s] = 2**40 - 1
					18, // length of sequence [r, s] = 2**64 - 1
					20, // replaced sequence [r, s] by an indefinite length tag without termination
					23, // appending 0's to sequence [r, s]
					25, // appending unused 0's to sequence [r, s]
					26, // appending null value to sequence [r, s]
					47, // sequence [r, s] of size 4166 to check for overflows
					48, // indefinite length
					49, // indefinite length with truncated delimiter
					50, // indefinite length with additional element
					51, // indefinite length with truncated element
					52, // indefinite length with garbage
					53, // indefinite length with nonempty EOC
					55, // append empty sequence
					56, // append zero
					57, // append garbage with high tag number
					58, // append null with explicit tag
					59, // append null with implicit tag
					62, // repeating element in sequence
					67, // length of r uses long form encoding
					68, // length of r contains a leading 0
					84, // prepending 0's to r
					113, // length of s uses long form encoding
					114, // length of s contains a leading 0
					127, // prepending 0's to s
				}
			},
			{ "ecdsa_secp256k1_sha512_test.json", new HashSet<int>
				{
					6, // Legacy: ASN encoding of s misses leading 0
					8, // length of sequence [r, s] uses long form encoding
					9, // length of sequence [r, s] contains a leading 0
					10, // length of sequence [r, s] uses 70 instead of 69
					11, // length of sequence [r, s] uses 68 instead of 69
					12, // uint32 overflow in length of sequence [r, s]
					13, // uint64 overflow in length of sequence [r, s]
					14, // length of sequence [r, s] = 2**31 - 1
					15, // length of sequence [r, s] = 2**31
					16, // length of sequence [r, s] = 2**32 - 1
					17, // length of sequence [r, s] = 2**40 - 1
					18, // length of sequence [r, s] = 2**64 - 1
					20, // replaced sequence [r, s] by an indefinite length tag without termination
					23, // appending 0's to sequence [r, s]
					25, // appending unused 0's to sequence [r, s]
					26, // appending null value to sequence [r, s]
					47, // sequence [r, s] of size 4166 to check for overflows
					48, // indefinite length
					49, // indefinite length with truncated delimiter
					50, // indefinite length with additional element
					51, // indefinite length with truncated element
					52, // indefinite length with garbage
					53, // indefinite length with nonempty EOC
					55, // append empty sequence
					56, // append zero
					57, // append garbage with high tag number
					58, // append null with explicit tag
					59, // append null with implicit tag
					62, // repeating element in sequence
					67, // length of r uses long form encoding
					68, // length of r contains a leading 0
					84, // prepending 0's to r
					114, // length of s uses long form encoding
					115, // length of s contains a leading 0
					128, // prepending 0's to s
				}
			},
		};

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public async Task EcdsaSecp256k1()
		{
			foreach (string file in EcdsaFiles)
			{
				JObject vector = await ReadVectorAsync(file);
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

						if (!p1363 && LaxDerCases.TryGetValue(file, out HashSet<int> exempted) && exempted.Contains(test.Value<int>("tcId")))
						{
							// These encodings are intentionally accepted by Bitcoin Core's lax parser.
							Assert.True(actual, context);
							continue;
						}
						AssertResult(test, actual, context);
					}
				}
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public async Task HmacSha256AndSha512()
		{
			foreach (string file in HmacFiles)
			{
				JObject vector = await ReadVectorAsync(file);
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
		public async Task Pbkdf2HmacSha1Sha256Sha384AndSha512()
		{
			foreach (string file in Pbkdf2Files)
			{
				JObject vector = await ReadVectorAsync(file);
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
			{ "ecdsa_secp256k1_sha512_p1363_test.json", "7e12bee4519587d2b31fe7a03a617fad771f352b505edb41d063c35a4023dbc7" },
			{ "ecdsa_secp256k1_sha512_test.json", "b614f432e4110bcdd3d792180a195e5ba33da7704db3d1298d42312cc64acd40" },
			{ "hmac_sha256_test.json", "2d201cfa61d1bf95e6f5d07d96634b4a348b31e8eaa277ad7c8d09677b7a743f" },
			{ "hmac_sha512_test.json", "b6c90477bdb4a6fc8ee3d1f7b2c0b69a8dfffab34718abaa6cabd71cc2ba1207" },
			{ "pbkdf2_hmacsha1_test.json", "5ef5307e119bd118da50ad276426ed3baade2d093e93b643a1d70be0bf0b861a" },
			{ "pbkdf2_hmacsha256_test.json", "1bf37af2cefe40c829ee9ecebb3505bb6424be8824bc97aa3e1c2076e860d192" },
			{ "pbkdf2_hmacsha384_test.json", "892613e78d38f1b81df77f1321a59a9fbf6d5c5d1c702126b9f709d7fd242483" },
			{ "pbkdf2_hmacsha512_test.json", "6a764a448b7a283478340d58f5dc20455524e8589a935fb0d66fb851e997b1c8" },
		};

		private static readonly HttpClient VectorClient = new HttpClient();

		private static string VectorPath(string file)
		{
			return Path.Combine(AppContext.BaseDirectory, "wycheproof", file);
		}

		private static async Task EnsureDownloadedAsync(string file)
		{
			string path = VectorPath(file);
			if (File.Exists(path)) return;
			byte[] bytes = await VectorClient.GetByteArrayAsync(VectorUrl + file);
			VerifyVectorHash(file, bytes);
			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
			try
			{
				using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true))
				{
					await stream.WriteAsync(bytes, 0, bytes.Length);
				}
				try { File.Move(temporary, path); }
				catch (IOException) when (File.Exists(path)) { }
			}
			finally { if (File.Exists(temporary)) File.Delete(temporary); }
		}

		private static async Task<JObject> ReadVectorAsync(string file)
		{
			// Keep discovery free of network I/O. An unavailable uncached vector fails
			// the test rather than silently losing cryptographic regression coverage.
			await EnsureDownloadedAsync(file);
			byte[] bytes;
			using (var stream = new FileStream(VectorPath(file), FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
			using (var buffer = new MemoryStream())
			{
				await stream.CopyToAsync(buffer);
				bytes = buffer.ToArray();
			}
			VerifyVectorHash(file, bytes);
			return JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes));
		}

		private static void VerifyVectorHash(string file, byte[] bytes)
		{
			using (SHA256 hash = SHA256.Create())
			{
				string actual = BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
				Assert.True(actual == VectorHashes[file], file + ": SHA-256 mismatch: expected " + VectorHashes[file] + ", got " + actual);
			}
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
				default:
					throw new NotSupportedException(algorithm);
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
