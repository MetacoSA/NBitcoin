using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.BouncyCastle.Asn1.Sec;
using NBitcoin.BouncyCastle.Asn1.X9;
using NBitcoin.BouncyCastle.Crypto.Parameters;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.BouncyCastle.Math.EC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
using NBitcoin.BouncyCastle.Asn1;

namespace NBitcoin.Tests
{
	public class DeterministicSignatureTests
	{
		static DeterministicSignatureTests()
		{
			curves = new Dictionary<string, DerObjectIdentifier>();
			curves.Add("B-571", SecObjectIdentifiers.SecT571r1);
			curves.Add("B-409", SecObjectIdentifiers.SecT409r1);
			curves.Add("B-283", SecObjectIdentifiers.SecT283r1);
			curves.Add("B-233", SecObjectIdentifiers.SecT233r1);
			curves.Add("B-163", SecObjectIdentifiers.SecT163r2);
			curves.Add("P-521", SecObjectIdentifiers.SecP521r1);
			curves.Add("P-384", SecObjectIdentifiers.SecP384r1);
			curves.Add("P-256", SecObjectIdentifiers.SecP256r1);
			curves.Add("P-224", SecObjectIdentifiers.SecP224r1);
			curves.Add("P-192", SecObjectIdentifiers.SecP192r1);
			curves.Add("K-571", SecObjectIdentifiers.SecT571k1);
			curves.Add("K-409", SecObjectIdentifiers.SecT409k1);
			curves.Add("K-283", SecObjectIdentifiers.SecT283k1);
			curves.Add("K-233", SecObjectIdentifiers.SecT233k1);
			curves.Add("K-163", SecObjectIdentifiers.SecT163k1);

		}

		static Dictionary<string, DerObjectIdentifier> curves;
		public DeterministicSignatureTests()
		{


		}

		class DeterministicSigTest
		{

			public BigInteger K
			{
				get;
				set;
			}

			public BigInteger S
			{
				get;
				set;
			}

			public BigInteger R
			{
				get;
				set;
			}

			public string Message
			{
				get;
				set;
			}

			public string Hash
			{
				get;
				set;
			}

			public ECPrivateKeyParameters Key
			{
				get;
				set;
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void IETFDetailedExample()
		{
			ECPrivateKeyParameters key = ParseKey(
   @"
	curve: NIST K-163
	q = 4000000000000000000020108A2E0CC0D99F8A5EF
   x = 09A4D6792295A7F730FC3F2B49CBC0F62E862272F
   Ux = 79AEE090DB05EC252D5CB4452F356BE198A4FF96F
   Uy = 782E29634DDC9A31EF40386E896BAA18B53AFA5A3");

			DeterministicSigTest test = ParseTest(@"
   With SHA-256, message = sample:
   k = 23AF4074C90A02B3FE61D286D5C87F425E6BDD81B
   r = 113A63990598A3828C407C0F4D2438D990DF99A7F
   s = 1313A2E03F5412DDB296A22E2C455335545672D9F");

			TestSig(key, test);
		}

		private void TestSig(ECPrivateKeyParameters key, DeterministicSigTest test)
		{
			var dsa = new DeterministicECDSA(GetHash(test.Hash));
			dsa.setPrivateKey(key);
			dsa.update(Encoding.UTF8.GetBytes(test.Message));
			var result = dsa.sign();

			var signature = ECDSASignature.FromDER(result);
			Assert.Equal(test.S, signature.S);
			Assert.Equal(test.R, signature.R);
		}

		private Func<BouncyCastle.Crypto.IDigest> GetHash(string hash)
		{
			if(hash.Equals("SHA-256", StringComparison.OrdinalIgnoreCase))
				return () => new NBitcoin.BouncyCastle.Crypto.Digests.Sha256Digest();

			if(hash.Equals("SHA-1", StringComparison.OrdinalIgnoreCase))
				return () => new NBitcoin.BouncyCastle.Crypto.Digests.Sha1Digest();

			if(hash.Equals("SHA-224", StringComparison.OrdinalIgnoreCase))
				return () => new NBitcoin.BouncyCastle.Crypto.Digests.Sha224Digest();

			if(hash.Equals("SHA-384", StringComparison.OrdinalIgnoreCase))
				return () => new NBitcoin.BouncyCastle.Crypto.Digests.Sha384Digest();

			if(hash.Equals("SHA-512", StringComparison.OrdinalIgnoreCase))
				return () => new NBitcoin.BouncyCastle.Crypto.Digests.Sha512Digest();

			throw new NotImplementedException();
		}

		private void TestSig(DeterministicSigTest test)
		{
			TestSig(test.Key, test);
		}


		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void DeterministicSignatureTestVectors()
		{
			foreach(var test in ParseTestsDump(File.ReadAllText("data/determiniticECDSA.txt")))
			{
				TestSig(test);
			}
		}

		private IEnumerable<DeterministicSigTest> ParseTestsDump(string testDump)
		{
			foreach(var curveTest in testDump.Split(new string[] { "Key pair:" }, StringSplitOptions.RemoveEmptyEntries))
			{
				var tests = curveTest.Split(new string[] { "Signatures:" }, StringSplitOptions.RemoveEmptyEntries);
				if(tests.Length == 1)
					continue;
				if(tests.Length != 2)
					throw new Exception("Test bug");
				var key = tests[0];
				var signatures = tests[1];
				var privateKey = ParseKey(key);
				foreach(var test in ParseTests(signatures))
				{
					test.Key = privateKey;
					yield return test;
				}

			}
		}

		private IEnumerable<DeterministicSigTest> ParseTests(string tests)
		{
			foreach(var test in tests.Split(new string[] { "With " }, StringSplitOptions.RemoveEmptyEntries))
			{
				var result = ParseTest("With " + test);
				if(result != null)
					yield return result;
			}
		}

		private DeterministicSigTest ParseTest(string data)
		{
			var match = Regex.Match(data, "With (.*?), message = \"?(.*?)\"?:");
			if(!match.Success)
				return null;
			data = data.Replace(match.Value, "");

			Dictionary<string, string> values = ToDictionnary(data);

			return new DeterministicSigTest()
			{
				Message = match.Groups[2].Value,
				Hash = match.Groups[1].Value,
				K = new BigInteger(values["k"], 16),
				R = new BigInteger(values["r"], 16),
				S = new BigInteger(values["s"], 16),
			};
		}

		private static Dictionary<string, string> ToDictionnary(string data)
		{
			Dictionary<string, string> values = new Dictionary<string, string>();

			var lines = data.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
			string previous = null;
			foreach(var line in lines)
			{
				var kv = line.Replace("\t", "")
							  .Replace(" ", "")
							  .Split(new string[] { ":", "=" }, StringSplitOptions.RemoveEmptyEntries);
				if(kv.Length != 2)
				{
					if(kv.Length == 1 && previous != null)
					{
						values[previous] = values[previous] + kv[0];
					}
					continue;
				}
				previous = kv[0];
				values.Add(kv[0], kv[1]);
			}
			return values;
		}

		private ECPrivateKeyParameters ParseKey(string data)
		{
			Dictionary<string, string> values = ToDictionnary(data);

			var curveName = values["curve"].Replace("NIST", "");
			var curve = SecNamedCurves.GetByOid(curves[curveName]);
			var domain = new ECDomainParameters(curve.Curve, curve.G, new BigInteger(values["q"], 16), curve.H);
			Assert.Equal(domain.N, curve.N);

			var key = new ECPrivateKeyParameters(new BigInteger(values["x"], 16), domain);

			ECPoint pub = curve.G.Multiply(key.D);

			Assert.Equal(pub.Normalize().XCoord.ToBigInteger(), new BigInteger(values["Ux"], 16));
			Assert.Equal(pub.Normalize().YCoord.ToBigInteger(), new BigInteger(values["Uy"], 16));

			return key;
		}

		[Fact]
		public void BlindingSignature()
		{
			// Test with known values 
			var requester = new ECDSABlinding.Requester();
			var r = new Key(Encoders.Hex.DecodeData("31E151628AED2A6ABF7155809CF4F3C762E7160F38B4DA56B784D9045190CFA0"));
			var key = new Key(Encoders.Hex.DecodeData("B7E151628AED2A6ABF7158809CF4F3C762E7160F38B4DA56A784D9045190CFEF"));
			var signer = new ECDSABlinding.Signer(key, r);

			var message = new uint256(Encoders.Hex.DecodeData("243F6A8885A308D313198A2E03707344A4093822299F31D0082EFA98EC4E6C89"), false);
			var blindedMessage = requester.BlindMessage(message, r.PubKey, key.PubKey);

			var blindSignature = signer.Sign(blindedMessage);
			var unblindedSignature = requester.UnblindSignature(blindSignature);

			Assert.True( ECDSABlinding.VerifySignature(message, unblindedSignature, key.PubKey) );
			Assert.False(ECDSABlinding.VerifySignature(uint256.One, unblindedSignature, key.PubKey) );
			Assert.False(ECDSABlinding.VerifySignature(uint256.One, unblindedSignature, key.PubKey) );
			Assert.False(ECDSABlinding.VerifySignature(
				message, 
				new BlindSignature(unblindedSignature.C, BigInteger.Zero.Subtract(unblindedSignature.S)), 
				key.PubKey) );
			Assert.False(ECDSABlinding.VerifySignature(
				message, 
				new BlindSignature(BigInteger.Zero.Subtract(unblindedSignature.C), unblindedSignature.S), 
				key.PubKey) );
			Assert.False(ECDSABlinding.VerifySignature(
				message, 
				new BlindSignature(BigInteger.Zero.Subtract(unblindedSignature.C), unblindedSignature.S), 
				new Key().PubKey) );

			// Test with unknown values 
			requester = new ECDSABlinding.Requester();
			signer = new ECDSABlinding.Signer(new Key(), new Key());

			message = Hashes.Hash256(Encoders.ASCII.DecodeData("Hello world!"));
			blindedMessage = requester.BlindMessage(message, signer.R.PubKey, signer.Key.PubKey);

			blindSignature = signer.Sign(blindedMessage);
			unblindedSignature = requester.UnblindSignature(blindSignature);

			Assert.True( ECDSABlinding.VerifySignature(message, unblindedSignature, signer.Key.PubKey) );
			Assert.False(ECDSABlinding.VerifySignature(uint256.One, unblindedSignature, signer.Key.PubKey) );
			Assert.False(ECDSABlinding.VerifySignature(uint256.One, unblindedSignature, signer.Key.PubKey) );
			Assert.False(ECDSABlinding.VerifySignature(
				message, 
				new BlindSignature(BigInteger.Zero, unblindedSignature.S), 
				signer.Key.PubKey) );
			Assert.False(ECDSABlinding.VerifySignature(
				message, 
				new BlindSignature(unblindedSignature.C, BigInteger.Zero), 
				signer.Key.PubKey) );
			Assert.False(ECDSABlinding.VerifySignature(
				message, 
				new BlindSignature(BigInteger.One, unblindedSignature.S), 
				signer.Key.PubKey) );
			Assert.False(ECDSABlinding.VerifySignature(
				message, 
				new BlindSignature(unblindedSignature.C, BigInteger.One), 
				signer.Key.PubKey) );
			Assert.False(ECDSABlinding.VerifySignature(
				message, 
				new BlindSignature(BigInteger.One, BigInteger.One), 
				signer.Key.PubKey) );
			Assert.False(ECDSABlinding.VerifySignature(
				message, 
				new BlindSignature(unblindedSignature.C, BigInteger.Zero.Subtract(unblindedSignature.S)), 
				signer.Key.PubKey) );
			Assert.False(ECDSABlinding.VerifySignature(
				message, 
				new BlindSignature(BigInteger.Zero.Subtract(unblindedSignature.C), unblindedSignature.S), 
				signer.Key.PubKey) );
			Assert.False(ECDSABlinding.VerifySignature(
				message, 
				new BlindSignature(BigInteger.Zero.Subtract(unblindedSignature.C), unblindedSignature.S), 
				new Key().PubKey) );
		}
	}
}
