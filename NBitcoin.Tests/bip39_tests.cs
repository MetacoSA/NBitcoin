using NBitcoin.DataEncoders;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class bip39_tests
	{

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void EngTest()
		{
			var test = JObject.Parse(File.ReadAllText("data/bip39_vectors.json"));

			foreach(var language in test.Properties())
			{
				var lang = GetList(language.Name);
				foreach(var langTest in ((JArray)language.Value).OfType<JArray>().Take(2))
				{
					var entropy = Encoders.Hex.DecodeData(langTest[0].ToString());
					string mnemonic = langTest[1].ToString();
					string seed = langTest[2].ToString();
					var bip39 = new BIP39(mnemonic, "TREZOR", lang);
					Assert.Equal(seed, Encoders.Hex.EncodeData(bip39.SeedBytes));

					var bip392 = new BIP39("TREZOR", lang, entropy);
					Assert.Equal(seed, Encoders.Hex.EncodeData(bip392.SeedBytes));
				}
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void JapTest()
		{
			var test = JArray.Parse(File.ReadAllText("data/bip39_JP.json", Encoding.UTF32));

			foreach(var unitTest in test.OfType<JObject>())
			{
				var entropy = Encoders.Hex.DecodeData(unitTest["entropy"].ToString());
				string mnemonic = unitTest["mnemonic"].ToString();
				string seed = unitTest["seed"].ToString();
				string passphrase = unitTest["passphrase"].ToString();
				var bip39 = new BIP39(mnemonic, passphrase, Wordlist.Japanese);
				Assert.Equal(seed, Encoders.Hex.EncodeData(bip39.SeedBytes));
				var bip32 = unitTest["bip32_xprv"].ToString();
				var bip32Actual = bip39.ExtKey.ToString(Network.Main);
				Assert.Equal(bip32, bip32Actual.ToString());

				var bip392 = new BIP39(passphrase, Wordlist.Japanese, entropy);
				bip32Actual = bip392.ExtKey.ToString(Network.Main);
				Assert.Equal(bip32, bip32Actual.ToString());

				var bip393 = bip39.ExtKey.EncryptToMnemonic(passphrase, Wordlist.Japanese, entropy);
				bip32Actual = bip393.ExtKey.ToString(Network.Main);
				Assert.Equal(bip32, bip32Actual.ToString());
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void TestKnownEnglish()
		{
			Assert.Equal(Language.English, BIP39.AutoDetectLanguageOfWords(new string[] { "abandon", "abandon", "abandon", "abandon", "abandon", "abandon", "abandon", "abandon", "abandon", "abandon", "abandon", "about" }));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void TestKnownJapenese()
		{
			Assert.Equal(Language.Japanese, BIP39.AutoDetectLanguageOfWords(new string[] { "あいこくしん", "あいさつ", "あいだ", "あおぞら", "あかちゃん", "あきる", "あけがた", "あける", "あこがれる", "あさい", "あさひ", "あしあと", "あじわう", "あずかる", "あずき", "あそぶ", "あたえる", "あたためる", "あたりまえ", "あたる", "あつい", "あつかう", "あっしゅく", "あつまり", "あつめる", "あてな", "あてはまる", "あひる", "あぶら", "あぶる", "あふれる", "あまい", "あまど", "あまやかす", "あまり", "あみもの", "あめりか" }));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void TestKnownSpanish()
		{
			Assert.Equal(Language.Spanish, BIP39.AutoDetectLanguageOfWords(new string[] { "yoga", "yogur", "zafiro", "zanja", "zapato", "zarza", "zona", "zorro", "zumo", "zurdo" }));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void TestKnownChineseSimplified()
		{
			Assert.Equal(Language.ChineseSimplified, BIP39.AutoDetectLanguageOfWords(new string[] { "的", "一", "是", "在", "不", "了", "有", "和", "人", "这" }));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void TestKnownChineseTraditional()
		{
			Assert.Equal(Language.ChineseTraditional, BIP39.AutoDetectLanguageOfWords(new string[] { "的", "一", "是", "在", "不", "了", "有", "和", "載" }));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void TestKnownUnknown()
		{
			Assert.Equal(Language.Unknown, BIP39.AutoDetectLanguageOfWords(new string[] { "gffgfg", "khjkjk", "kjkkj" }));
		}


		private Wordlist GetList(string lang)
		{
			if(lang == "english")
				return Wordlist.English;
			throw new NotSupportedException(lang);
		}
	}
}
