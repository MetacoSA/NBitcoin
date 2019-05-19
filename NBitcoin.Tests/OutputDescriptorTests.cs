using System;
using System.Collections.Generic;
using FsCheck;
using FsCheck.Xunit;
using NBitcoin.Scripting;
using NBitcoin.Scripting.Parser;
using NBitcoin.Tests.Generators;
using Xunit;

namespace NBitcoin.Tests
{
	public class OutputDescriptorTests
	{
		public OutputDescriptorTests()
		{
			Arb.Register<OutputDescriptorGenerator>();
		}


		[Property]
		[Trait("PropertyTest", "BidirectionalConversion")]
		public void DescriptorShouldConvertToStringBidirectionally(OutputDescriptor desc)
		{
			var afterConversion = OutputDescriptor.Parse(desc.ToString());
			Assert.Equal(desc, afterConversion);
			Assert.Equal(desc.ToString(), afterConversion.ToString());
		}


		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void OutputDescriptorParserTests()
		{
			var testVectors = new string[] {
				"addr(2N7nD1pG3kK3DYaP34jQKbxB3JnEfMbVea7)",
				"pk(0279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798)",
				"pkh(02c6047f9441ed7d6d3045406e95c07cd85c778e4b8cef3ca7abac09b95c709ee5)",
				"wpkh(02f9308a019258c31049344f85f89d5229b531c845836f99b08601f113bce036f9)",
				"sh(wpkh(03fff97bd5755eeea420453a14355235d382f6472f8568a18b2f057a1460297556))",
				"combo(0279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798)",
				"sh(wsh(pkh(02e493dbf1c10d80f3581e4904930b1404cc6c13900ee0758474fa94abe8c4cd13)))",
				"multi(1,022f8bde4d1a07209355b4a7250a5c5128e88b84bddc619ab7cba8d569b240efe4,025cbdf0646e5db4eaa398f365f2ea7a0e3d419b7e0330e39ce92bddedcac4f9bc)",
				"sh(multi(2,022f01e5e15cca351daff3843fb70f3c2f0a1bdd05e5af888a67784ef3e10a2a01,03acd484e2f0c7f65309ad178a9f559abde09796974c57e714c35f110dfc27ccbe))",
				"wsh(multi(2,03a0434d9e47f3c86235477c7b1ae6ae5d3442d49b1943c2b752a68e2a47e247c7,03774ae7f858a9411e5ef4246b70c65aac5649980be5c17891bbec17895da008cb,03d01115d548e7561b15c38f004d734633687cf4419620095bc5b0f47070afe85a))",
				"sh(wsh(multi(1,03f28773c2d975288bc7d1d205c3748651b075fbc6610e58cddeeddf8f19405aa8,03499fdf9e895e719cfd64e67f07d38e3226aa7b63678949e6e49b241a60e823e4,02d7924d4f7d43ea965a465ae3095ff41131e5946f3c85f79e44adbcf8e27e080e)))",
				"pk(xpub661MyMwAqRbcFtXgS5sYJABqqG9YLmC4Q1Rdap9gSE8NqtwybGhePY2gZ29ESFjqJoCu1Rupje8YtGqsefD265TMg7usUDFdp6W1EGMcet8)",
				"pkh(xpub68Gmy5EdvgibQVfPdqkBBCHxA5htiqg55crXYuXoQRKfDBFA1WEjWgP6LHhwBZeNK1VTsfTFUHCdrfp1bgwQ9xv5ski8PX9rL2dZXvgGDnw/1'/2)",
				"pkh([d34db33f/44'/0'/0']xpub6ERApfZwUNrhLCkDtcHTcxd75RbzS1ed54G1LkBUHQVHQKqhMkhgbmJbZRkrgZw4koxb5JaHWkY4ALHY2grBGRjaDMzQLcgJvLJuZZvRcEL/1/*)",
				"wsh(multi(1,xpub661MyMwAqRbcFW31YEwpkMuc5THy2PSt5bDMsktWQcFF8syAmRUapSCGu8ED9W6oDMSgv6Zz8idoc4a6mr8BDzTJY47LJhkJ8UB7WEGuduB/1/0/*,xpub69H7F5d8KSRgmmdJg2KhpAK8SR3DjMwAdkxj3ZuxV27CprR9LgpeyGmXUbC6wb7ERfvrnKZjXoUmmDznezpbZb7ap6r1D3tgFxHmwMkQTPH/0/0/*))",
				// same with above except that is has hardend derivation
				"wsh(multi(1,xpub661MyMwAqRbcFW31YEwpkMuc5THy2PSt5bDMsktWQcFF8syAmRUapSCGu8ED9W6oDMSgv6Zz8idoc4a6mr8BDzTJY47LJhkJ8UB7WEGuduB/1/0/*,xpub69H7F5d8KSRgmmdJg2KhpAK8SR3DjMwAdkxj3ZuxV27CprR9LgpeyGmXUbC6wb7ERfvrnKZjXoUmmDznezpbZb7ap6r1D3tgFxHmwMkQTPH/0/0/*'))"
			};
			foreach (var i in testVectors)
			{
				var od = OutputDescriptor.Parse(i);
				Assert.Equal(od, OutputDescriptor.Parse(od.ToString()));
			}
		}


		[Fact]
		[Trait("Core", "Core")]
		public void DescriptorTests()
		{
			DescriptorTestCore(
				"combo(L4rK1yDtCWekvXuE6oXD9jCYfFNV2cWRpVuPLBcCU2z8TrisoyY1)",
				"combo(03a34b99f22c790c4e36b2b3c2c35a36db06226e41c692fc82b8b56ac1c540c5bd)",
				SIGNABLE,
				new string []
					{
					"2103a34b99f22c790c4e36b2b3c2c35a36db06226e41c692fc82b8b56ac1c540c5bdac",
					"76a9149a1c78a507689f6f54b847ad1cef1e614ee23f1e88ac",
					"00149a1c78a507689f6f54b847ad1cef1e614ee23f1e",
					"a91484ab21b1b2fd065d4504ff693d832434b6108d7b87"
					} );
		}

		const int DEFAULT = 0;
		const int RANGE = 1; // Expected to be ranged descriptor
		const int HARDEND = 2; // derivation needs accesses to private keys
		const int UNSOLVABLE = 4; // This descriptor is not expected to be solvable
		const int SIGNABLE = 8; // We can sign with this descriptor (this is not true when actual BIP32 derivation is used, as that's not integrated in our signing code)
		System.Random Seed = new System.Random();

		private void DescriptorTestCore(string pub, string priv, int flag, string[] scripts, KeyPath paths = null)
		{
			var keysPriv = new FlatSigningRepository();
			var keysPub = new FlatSigningRepository();
			paths = paths ?? KeyPath.Empty;

			Assert.True(OutputDescriptor.TryParse(MaybeUseHInsteadOfApostrophe(pub), out var parsePub));
			Assert.True(OutputDescriptor.TryParse(MaybeUseHInsteadOfApostrophe(priv), out var parsePriv));

			// 2. Check both will serialize back to the public version.
			string pub1 = parsePriv.ToString();
			string pub2 = parsePub.ToString();
			// Assert.True(EqualDescriptorStr(pub, pub1));
			// Assert.True(EqualDescriptorStr(pub, pub2));

			// string priv1;
			// Assert.True(parsePub.TryGetPrivateString(null, out var res));
		}

		private bool EqualDescriptorStr(string a, string b)
		{
			// May be need to ignore checksum specifically.
			return a == b;
		}

		private string MaybeUseHInsteadOfApostrophe(string input)
		{
			if (Seed.NextDouble() < 0.5)
			{
				input = input.Replace('\'', 'h');
				// changing apostrophe will breaks checksum, so delete it.
				if (input.Length > 9 && input[input.Length - 9] == '#')
					input = input.Substring(0, input.Length - 9);
			}
			return input;
		}
		private void Check(string prv, string pub, int flags, string[] script, HashSet<uint[]> paths = null)
		{
			OutputDescriptor.Parse(prv);
			OutputDescriptor.Parse(pub);
		}

	}
}