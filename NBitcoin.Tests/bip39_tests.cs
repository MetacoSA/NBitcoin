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
					string mnemonicStr = langTest[1].ToString();
					string seed = langTest[2].ToString();
					var mnemonic = new Mnemonic(mnemonicStr, lang);
					Assert.Equal(seed, Encoders.Hex.EncodeData(mnemonic.DeriveSeed("TREZOR")));

					mnemonic = new Mnemonic(lang, entropy);
					Assert.Equal(seed, Encoders.Hex.EncodeData(mnemonic.DeriveSeed("TREZOR")));
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
				string mnemonicStr = unitTest["mnemonic"].ToString();
				string seed = unitTest["seed"].ToString();
				string passphrase = unitTest["passphrase"].ToString();
				var mnemonic = new Mnemonic(mnemonicStr, Wordlist.Japanese);
				Assert.Equal(seed, Encoders.Hex.EncodeData(mnemonic.DeriveSeed(passphrase)));
				var bip32 = unitTest["bip32_xprv"].ToString();
				var bip32Actual = mnemonic.DeriveExtKey(passphrase).ToString(Network.Main);
				Assert.Equal(bip32, bip32Actual.ToString());

				mnemonic = new Mnemonic(Wordlist.Japanese, entropy);
				bip32Actual = mnemonic.DeriveExtKey(passphrase).ToString(Network.Main);
				Assert.Equal(bip32, bip32Actual.ToString());
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void TestKnownEnglish()
		{
			Assert.Equal(Language.English, Wordlist.AutoDetectLanguage(new string[] { "abandon", "abandon", "abandon", "abandon", "abandon", "abandon", "abandon", "abandon", "abandon", "abandon", "abandon", "about" }));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void TestKnownJapenese()
		{
			Assert.Equal(Language.Japanese, Wordlist.AutoDetectLanguage(new string[] { "あいこくしん", "あいさつ", "あいだ", "あおぞら", "あかちゃん", "あきる", "あけがた", "あける", "あこがれる", "あさい", "あさひ", "あしあと", "あじわう", "あずかる", "あずき", "あそぶ", "あたえる", "あたためる", "あたりまえ", "あたる", "あつい", "あつかう", "あっしゅく", "あつまり", "あつめる", "あてな", "あてはまる", "あひる", "あぶら", "あぶる", "あふれる", "あまい", "あまど", "あまやかす", "あまり", "あみもの", "あめりか" }));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void TestKnownSpanish()
		{
			Assert.Equal(Language.Spanish, Wordlist.AutoDetectLanguage(new string[] { "yoga", "yogur", "zafiro", "zanja", "zapato", "zarza", "zona", "zorro", "zumo", "zurdo" }));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void TestKnownChineseSimplified()
		{
			Assert.Equal(Language.ChineseSimplified, Wordlist.AutoDetectLanguage(new string[] { "的", "一", "是", "在", "不", "了", "有", "和", "人", "这" }));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void TestKnownChineseTraditional()
		{
			Assert.Equal(Language.ChineseTraditional, Wordlist.AutoDetectLanguage(new string[] { "的", "一", "是", "在", "不", "了", "有", "和", "載" }));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void TestKnownUnknown()
		{
			Assert.Equal(Language.Unknown, Wordlist.AutoDetectLanguage(new string[] { "gffgfg", "khjkjk", "kjkkj" }));
		}


		private Wordlist GetList(string lang)
		{
			if(lang == "english")
				return Wordlist.English;
			throw new NotSupportedException(lang);
		}
	}
}
