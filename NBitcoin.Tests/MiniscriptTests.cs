using System;
using System.Linq;
using NBitcoin.DataEncoders;
using NBitcoin.Scripting.Miniscript;
using NBitcoin.Scripting.Miniscript.Policy;
using NBitcoin.Scripting.Parser;
using Xunit;

namespace NBitcoin.Tests
{
	public class MiniscriptTests
	{
		public static HexEncoder Hex = new DataEncoders.HexEncoder();

		public static Key[] PrivKeys = (new []
			{
				"4141414141414141414141414141414141414141414141414141414141414141",
				"4242424242424242424242424242424242424242424242424242424242424242",
				"4343434343434343434343434343434343434343434343434343434343434343",
				"4444444444444444444444444444444444444444444444444444444444444444",
				"4545454545454545454545454545454545454545454545454545454545454545",
			}).Select(str => new Key(Hex.DecodeData(str))).ToArray();

		public static PubKey[] PubKeys = PrivKeys.Select(k => k.PubKey).ToArray();

		public static TheoryData<string, string, bool, bool, bool, uint, uint> MSAttributeTestCase =>
			new TheoryData<string, string, bool, bool, bool, uint, uint>
			{
				{
					"lltvln:after(1231488000)",
					"6300676300676300670400046749b1926869516868",
					true,
					true,
					false,
					12,
					3
				},
				{
					"uuj:and_v(v:thresh_m(2,03d01115d548e7561b15c38f004d734633687cf4419620095bc5b0f47070afe85a,025601570cb47f238d2b0286db4a990fa0f3ba28d1a319f5e7cf55c2a2444da7cc),after(1231488000))",
					"6363829263522103d01115d548e7561b15c38f004d734633687cf4419620095bc5b0f47070afe85a21025601570cb47f238d2b0286db4a990fa0f3ba28d1a319f5e7cf55c2a2444da7cc52af0400046749b168670068670068",
					true, true, true, 14, 5
				},
				{
					"or_b(un:thresh_m(2,03daed4f2be3a8bf278e70132fb0beb7522f570e144bf615c07e996d443dee8729,024ce119c96e2fa357200b559b2f7dd5a5f02d5290aff74b03f3e471b273211c97),al:older(16))",
					"63522103daed4f2be3a8bf278e70132fb0beb7522f570e144bf615c07e996d443dee872921024ce119c96e2fa357200b559b2f7dd5a5f02d5290aff74b03f3e471b273211c9752ae926700686b63006760b2686c9b",
					true, false, false, 14, 5
				},
				{
					"j:and_v(vdv:after(1567547623),older(2016))",
					"829263766304e7e06e5db169686902e007b268",
					true,
					true,
					false,
					11,
					1
				},
				{
					"t:and_v(vu:hash256(131772552c01444cd81360818376a040b7c3b2b7b0a53550ee3edde216cec61b),v:sha256(ec4916dd28fc4c10d78e287ca5d9cc51ee1ae73cbfde08c6b37324cbfaac8bc5))",
					"6382012088aa20131772552c01444cd81360818376a040b7c3b2b7b0a53550ee3edde216cec61b876700686982012088a820ec4916dd28fc4c10d78e287ca5d9cc51ee1ae73cbfde08c6b37324cbfaac8bc58851",
					true, true, false, 12, 3
				},
				{
					"t:andor(thresh_m(3,02d7924d4f7d43ea965a465ae3095ff41131e5946f3c85f79e44adbcf8e27e080e,03fff97bd5755eeea420453a14355235d382f6472f8568a18b2f057a1460297556,02e493dbf1c10d80f3581e4904930b1404cc6c13900ee0758474fa94abe8c4cd13),v:older(4194305),v:sha256(9267d3dbed802941483f1afa2a6bc68de5f653128aca9bf1461c5d0a3ad36ed2))",
					"532102d7924d4f7d43ea965a465ae3095ff41131e5946f3c85f79e44adbcf8e27e080e2103fff97bd5755eeea420453a14355235d382f6472f8568a18b2f057a14602975562102e493dbf1c10d80f3581e4904930b1404cc6c13900ee0758474fa94abe8c4cd1353ae6482012088a8209267d3dbed802941483f1afa2a6bc68de5f653128aca9bf1461c5d0a3ad36ed2886703010040b2696851",
					true, true, false, 13, 5
				},
				{
					"or_d(thresh_m(1,02f9308a019258c31049344f85f89d5229b531c845836f99b08601f113bce036f9),or_b(thresh_m(3,022f01e5e15cca351daff3843fb70f3c2f0a1bdd05e5af888a67784ef3e10a2a01,032fa2104d6b38d11b0230010559879124e42ab8dfeff5ff29dc9cdadd4ecacc3f,03d01115d548e7561b15c38f004d734633687cf4419620095bc5b0f47070afe85a),su:after(500000)))",
					"512102f9308a019258c31049344f85f89d5229b531c845836f99b08601f113bce036f951ae73645321022f01e5e15cca351daff3843fb70f3c2f0a1bdd05e5af888a67784ef3e10a2a0121032fa2104d6b38d11b0230010559879124e42ab8dfeff5ff29dc9cdadd4ecacc3f2103d01115d548e7561b15c38f004d734633687cf4419620095bc5b0f47070afe85a53ae7c630320a107b16700689b68",
					true, true, false, 15, 7
				},
				{
					"or_d(sha256(38df1c1f64a24a77b23393bca50dff872e31edc4f3b5aa3b90ad0b82f4f089b6),and_n(un:after(499999999),older(4194305)))",
					"82012088a82038df1c1f64a24a77b23393bca50dff872e31edc4f3b5aa3b90ad0b82f4f089b68773646304ff64cd1db19267006864006703010040b26868",
					true, false, false, 16, 1
				},
				{
					"and_v(or_i(v:thresh_m(2,02c6047f9441ed7d6d3045406e95c07cd85c778e4b8cef3ca7abac09b95c709ee5,03774ae7f858a9411e5ef4246b70c65aac5649980be5c17891bbec17895da008cb),v:thresh_m(2,03e60fce93b59e9ec53011aabc21c23e97b2a31369b87a5ae9c44ee89e2a6dec0a,025cbdf0646e5db4eaa398f365f2ea7a0e3d419b7e0330e39ce92bddedcac4f9bc)),sha256(d1ec675902ef1633427ca360b290b0b3045a0d9058ddb5e648b4c3c3224c5c68))",
					"63522102c6047f9441ed7d6d3045406e95c07cd85c778e4b8cef3ca7abac09b95c709ee52103774ae7f858a9411e5ef4246b70c65aac5649980be5c17891bbec17895da008cb52af67522103e60fce93b59e9ec53011aabc21c23e97b2a31369b87a5ae9c44ee89e2a6dec0a21025cbdf0646e5db4eaa398f365f2ea7a0e3d419b7e0330e39ce92bddedcac4f9bc52af6882012088a820d1ec675902ef1633427ca360b290b0b3045a0d9058ddb5e648b4c3c3224c5c6887",
					true, true, true, 11, 5
				},
				{
					"j:and_b(thresh_m(2,0279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798,024ce119c96e2fa357200b559b2f7dd5a5f02d5290aff74b03f3e471b273211c97),s:or_i(older(1),older(4252898)))",
					"82926352210279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f8179821024ce119c96e2fa357200b559b2f7dd5a5f02d5290aff74b03f3e471b273211c9752ae7c6351b26703e2e440b2689a68",
					true, false, true, 14, 4
				},
				{
					"and_b(older(16),s:or_d(sha256(e38990d0c7fc009880a9c07c23842e886c6bbdc964ce6bdd5817ad357335ee6f),n:after(1567547623)))",
					"60b27c82012088a820e38990d0c7fc009880a9c07c23842e886c6bbdc964ce6bdd5817ad357335ee6f87736404e7e06e5db192689a",
					true, false, false, 12, 1
				},
				{
					"j:and_v(v:hash160(20195b5a3d650c17f0f29f91c33f8f6335193d07),or_d(sha256(96de8fc8c256fa1e1556d41af431cace7dca68707c78dd88c3acab8b17164c47),older(16)))",
					"82926382012088a91420195b5a3d650c17f0f29f91c33f8f6335193d078882012088a82096de8fc8c256fa1e1556d41af431cace7dca68707c78dd88c3acab8b17164c4787736460b26868",
					true, false, false, 16, 2
				},
				{
					"and_b(hash256(32ba476771d01e37807990ead8719f08af494723de1d228f2c2c07cc0aa40bac),a:and_b(hash256(131772552c01444cd81360818376a040b7c3b2b7b0a53550ee3edde216cec61b),a:older(1)))",
					"82012088aa2032ba476771d01e37807990ead8719f08af494723de1d228f2c2c07cc0aa40bac876b82012088aa20131772552c01444cd81360818376a040b7c3b2b7b0a53550ee3edde216cec61b876b51b26c9a6c9a",
					true, true, false, 15, 2
				},
				{
					"thresh(2,thresh_m(2,03a0434d9e47f3c86235477c7b1ae6ae5d3442d49b1943c2b752a68e2a47e247c7,036d2b085e9e382ed10b69fc311a03f8641ccfff21574de0927513a49d9a688a00),a:thresh_m(1,036d2b085e9e382ed10b69fc311a03f8641ccfff21574de0927513a49d9a688a00),ac:pk(022f01e5e15cca351daff3843fb70f3c2f0a1bdd05e5af888a67784ef3e10a2a01))",
					"522103a0434d9e47f3c86235477c7b1ae6ae5d3442d49b1943c2b752a68e2a47e247c721036d2b085e9e382ed10b69fc311a03f8641ccfff21574de0927513a49d9a688a0052ae6b5121036d2b085e9e382ed10b69fc311a03f8641ccfff21574de0927513a49d9a688a0051ae6c936b21022f01e5e15cca351daff3843fb70f3c2f0a1bdd05e5af888a67784ef3e10a2a01ac6c935287",
					true, true, true, 13, 6
				},
				{
					"and_n(sha256(d1ec675902ef1633427ca360b290b0b3045a0d9058ddb5e648b4c3c3224c5c68),t:or_i(v:older(4252898),v:older(144)))",
					"82012088a820d1ec675902ef1633427ca360b290b0b3045a0d9058ddb5e648b4c3c3224c5c68876400676303e2e440b26967029000b269685168",
					true, false, false, 14, 2
				},
				{
					"or_d(d:and_v(v:older(4252898),v:older(4252898)),sha256(38df1c1f64a24a77b23393bca50dff872e31edc4f3b5aa3b90ad0b82f4f089b6))",
					"766303e2e440b26903e2e440b26968736482012088a82038df1c1f64a24a77b23393bca50dff872e31edc4f3b5aa3b90ad0b82f4f089b68768",
					true, false, false, 14, 2
				},
				{
					"c:and_v(or_c(sha256(9267d3dbed802941483f1afa2a6bc68de5f653128aca9bf1461c5d0a3ad36ed2),v:thresh_m(1,02c44d12c7065d812e8acf28d7cbb19f9011ecd9e9fdf281b0e6a3b5e87d22e7db)),pk(03acd484e2f0c7f65309ad178a9f559abde09796974c57e714c35f110dfc27ccbe))",
					"82012088a8209267d3dbed802941483f1afa2a6bc68de5f653128aca9bf1461c5d0a3ad36ed28764512102c44d12c7065d812e8acf28d7cbb19f9011ecd9e9fdf281b0e6a3b5e87d22e7db51af682103acd484e2f0c7f65309ad178a9f559abde09796974c57e714c35f110dfc27ccbeac",
					true, false, true, 8, 2
				},
				{
					"c:and_v(or_c(thresh_m(2,036d2b085e9e382ed10b69fc311a03f8641ccfff21574de0927513a49d9a688a00,02352bbf4a4cdd12564f93fa332ce333301d9ad40271f8107181340aef25be59d5),v:ripemd160(1b0f3c404d12075c68c938f9f60ebea4f74941a0)),pk(03fff97bd5755eeea420453a14355235d382f6472f8568a18b2f057a1460297556))",
					"5221036d2b085e9e382ed10b69fc311a03f8641ccfff21574de0927513a49d9a688a002102352bbf4a4cdd12564f93fa332ce333301d9ad40271f8107181340aef25be59d552ae6482012088a6141b0f3c404d12075c68c938f9f60ebea4f74941a088682103fff97bd5755eeea420453a14355235d382f6472f8568a18b2f057a1460297556ac",
					true, true, true, 10, 5
				},
				{
					"and_v(andor(hash256(8a35d9ca92a48eaade6f53a64985e9e2afeb74dcf8acb4c3721e0dc7e4294b25),v:hash256(939894f70e6c3a25da75da0cc2071b4076d9b006563cf635986ada2e93c0d735),v:older(50000)),after(499999999))",
					"82012088aa208a35d9ca92a48eaade6f53a64985e9e2afeb74dcf8acb4c3721e0dc7e4294b2587640350c300b2696782012088aa20939894f70e6c3a25da75da0cc2071b4076d9b006563cf635986ada2e93c0d735886804ff64cd1db1",
					true, false, false, 14, 2
				},
				{
					"andor(hash256(5f8d30e655a7ba0d7596bb3ddfb1d2d20390d23b1845000e1e118b3be1b3f040),j:and_v(v:hash160(3a2bff0da9d96868e66abc4427bea4691cf61ccd),older(4194305)),ripemd160(44d90e2d3714c8663b632fcf0f9d5f22192cc4c8))",
					"82012088aa205f8d30e655a7ba0d7596bb3ddfb1d2d20390d23b1845000e1e118b3be1b3f040876482012088a61444d90e2d3714c8663b632fcf0f9d5f22192cc4c8876782926382012088a9143a2bff0da9d96868e66abc4427bea4691cf61ccd8803010040b26868",
					true, false, false, 20, 2
				},
				{
					"or_i(c:and_v(v:after(500000),pk(02c6047f9441ed7d6d3045406e95c07cd85c778e4b8cef3ca7abac09b95c709ee5)),sha256(d9147961436944f43cd99d28b2bbddbf452ef872b30c8279e255e7daafc7f946))",
					"630320a107b1692102c6047f9441ed7d6d3045406e95c07cd85c778e4b8cef3ca7abac09b95c709ee5ac6782012088a820d9147961436944f43cd99d28b2bbddbf452ef872b30c8279e255e7daafc7f9468768",
					true, true, false, 10, 2
				},
				{
					"thresh(2,c:pk_h(5dedfbf9ea599dd4e3ca6a80b333c472fd0b3f69),s:sha256(e38990d0c7fc009880a9c07c23842e886c6bbdc964ce6bdd5817ad357335ee6f),a:hash160(dd69735817e0e3f6f826a9238dc2e291184f0131))",
					"76a9145dedfbf9ea599dd4e3ca6a80b333c472fd0b3f6988ac7c82012088a820e38990d0c7fc009880a9c07c23842e886c6bbdc964ce6bdd5817ad357335ee6f87936b82012088a914dd69735817e0e3f6f826a9238dc2e291184f0131876c935287",
					true, false, false, 18, 4
				},
				{
					"and_n(sha256(9267d3dbed802941483f1afa2a6bc68de5f653128aca9bf1461c5d0a3ad36ed2),uc:and_v(v:older(144),pk(03fe72c435413d33d48ac09c9161ba8b09683215439d62b7940502bda8b202e6ce)))",
					"82012088a8209267d3dbed802941483f1afa2a6bc68de5f653128aca9bf1461c5d0a3ad36ed28764006763029000b2692103fe72c435413d33d48ac09c9161ba8b09683215439d62b7940502bda8b202e6ceac67006868",
					true, false, true, 13, 3
				},
				{
					"and_n(c:pk(03daed4f2be3a8bf278e70132fb0beb7522f570e144bf615c07e996d443dee8729),and_b(l:older(4252898),a:older(16)))",
					"2103daed4f2be3a8bf278e70132fb0beb7522f570e144bf615c07e996d443dee8729ac64006763006703e2e440b2686b60b26c9a68",
					true, true, true, 12, 2
				},
				{
					"c:or_i(and_v(v:older(16),pk_h(9fc5dbe5efdce10374a4dd4053c93af540211718)),pk_h(2fbd32c8dd59ee7c17e66cb6ebea7e9846c3040f))",
					"6360b26976a9149fc5dbe5efdce10374a4dd4053c93af540211718886776a9142fbd32c8dd59ee7c17e66cb6ebea7e9846c3040f8868ac",
					true, true, true, 12, 3
				},
				{
					"or_d(c:pk_h(c42e7ef92fdb603af844d064faad95db9bcdfd3d),andor(c:pk(024ce119c96e2fa357200b559b2f7dd5a5f02d5290aff74b03f3e471b273211c97),older(2016),after(1567547623)))",
					"76a914c42e7ef92fdb603af844d064faad95db9bcdfd3d88ac736421024ce119c96e2fa357200b559b2f7dd5a5f02d5290aff74b03f3e471b273211c97ac6404e7e06e5db16702e007b26868",
					true, true, false, 13, 3
				},
				{
					"c:andor(ripemd160(6ad07d21fd5dfc646f0b30577045ce201616b9ba),pk_h(9fc5dbe5efdce10374a4dd4053c93af540211718),and_v(v:hash256(8a35d9ca92a48eaade6f53a64985e9e2afeb74dcf8acb4c3721e0dc7e4294b25),pk_h(dd100be7d9aea5721158ebde6d6a1fd8fff93bb1)))",
					"82012088a6146ad07d21fd5dfc646f0b30577045ce201616b9ba876482012088aa208a35d9ca92a48eaade6f53a64985e9e2afeb74dcf8acb4c3721e0dc7e4294b258876a914dd100be7d9aea5721158ebde6d6a1fd8fff93bb1886776a9149fc5dbe5efdce10374a4dd4053c93af5402117188868ac",
					true, false, true, 18, 3
				},
				{
					"c:andor(u:ripemd160(6ad07d21fd5dfc646f0b30577045ce201616b9ba),pk_h(20d637c1a6404d2227f3561fdbaff5a680dba648),or_i(pk_h(9652d86bedf43ad264362e6e6eba6eb764508127),pk_h(751e76e8199196d454941c45d1b3a323f1433bd6)))",
					"6382012088a6146ad07d21fd5dfc646f0b30577045ce201616b9ba87670068646376a9149652d86bedf43ad264362e6e6eba6eb764508127886776a914751e76e8199196d454941c45d1b3a323f1433bd688686776a91420d637c1a6404d2227f3561fdbaff5a680dba6488868ac",
					true, false, true, 23, 4
				},
				{
					"c:or_i(andor(c:pk_h(fcd35ddacad9f2d5be5e464639441c6065e6955d),pk_h(9652d86bedf43ad264362e6e6eba6eb764508127),pk_h(06afd46bcdfd22ef94ac122aa11f241244a37ecc)),pk(02d7924d4f7d43ea965a465ae3095ff41131e5946f3c85f79e44adbcf8e27e080e))",
					"6376a914fcd35ddacad9f2d5be5e464639441c6065e6955d88ac6476a91406afd46bcdfd22ef94ac122aa11f241244a37ecc886776a9149652d86bedf43ad264362e6e6eba6eb7645081278868672102d7924d4f7d43ea965a465ae3095ff41131e5946f3c85f79e44adbcf8e27e080e68ac",
					true, true, true, 17, 5
				},
			};

		[Theory]
		[Trait("Core", "Core")]
		[MemberData(nameof(MSAttributeTestCase))]
		public void MiniscriptAttributesTest(
			string msStr, string expectedHex, bool valid, bool nonMalleable,
			bool needSig, uint ops, uint _stack)
		{
			if (valid)
			{
				// var ms = Miniscript<PubKey, uint160>.Parse(msStr);
				// Assert.Equal(ms.ToScript().ToHex(), expectedHex);
				// Assert.Equal(ms.Type.Malleability.NonMalleable, nonMalleable);
				// Assert.Equal(ms.Type.Malleability.Safe, needSig);
				// Assert.Equal(ms.Ext.OpsCountSat, ops);
			}
			else
			{
				Assert.Throws<ParsingException>(() => Miniscript<PubKey, uint160>.Parse(msStr));
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void PolicyParserTest()
		{
			ConcretePolicy<MiniscriptStringKey, MiniscriptStringKeyHash>.Parse("pk(foo)");

			Assert.Throws<FormatException>(() => ConcretePolicy<PubKey, uint160>.Parse("pk(foo)"));
			var msRealPK = ConcretePolicy<PubKey, uint160>.Parse($"pk({PubKeys[0]})");
			Assert.True(msRealPK.AssertValid());

			var strOr = $"or(99@pk({PubKeys[0]}),pk({PubKeys[1]}))";
			var orRes = ConcretePolicy<PubKey, uint160>.Parse(strOr);
			Assert.True(orRes.AssertValid());

			var strNestedOr = $"or(after(3),{strOr})";
			var nestedOrRes = ConcretePolicy<PubKey, uint160>.Parse(strNestedOr);
			Assert.True(nestedOrRes.AssertValid());

			var strAnd = $"and(older(3),{strNestedOr})";
			var andRes = ConcretePolicy<PubKey, uint160>.Parse(strAnd);
			Assert.True(andRes.AssertValid());

			var hash256 = Crypto.Hashes.Hash256(PubKeys[2].ToBytes()).ToString();
			var hash160 = Crypto.Hashes.Hash160(PubKeys[2].ToBytes()).ToString();
			var strThresh = $"thresh(2,hash256({hash256}),{strAnd},hash160({hash160}))";
			var threshRes = ConcretePolicy<PubKey,uint160>.Parse(strThresh);
			Assert.True(threshRes.AssertValid());

		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void ConcretePolicyUnitTest()
		{
			var testCase1 = "and(sha256(a12b09447802e01b4fd9b7f226b08e18538765ef2a5e3cc6a435139d7bb69dee),and(hash256(f0ce5893c7a52f6a9e7c06e41f53ddbbf62ab492cdb4ad69f620f84acf57a6a3),hash160(e21d51db6c8f9079fb9d2471fbd9639ed2630d33)))";
			var concreteP1_1 = ConcretePolicy<PubKey, uint160>.Parse(testCase1);
			var concreteP1_2 = ConcretePolicy<PubKey, uint160>.Parse(testCase1);
			Assert.Equal(concreteP1_1, concreteP1_2);

			var testCase2 = "and(ripemd160(a8cb8a3e2c696b51f3178711ef2a11f20554b107),pk(024aa5f11820cdf29e8aa46ec1f97def0538e12e5f9dd9a7693294570551f8ad9a))";
			var concreteP2_1 = ConcretePolicy<PubKey, uint160>.Parse(testCase2);
			var concreteP2_2 = ConcretePolicy<PubKey, uint160>.Parse(testCase2);
			Assert.Equal(concreteP2_1.GetHashCode(), concreteP2_2.GetHashCode());
		}
	}
}
