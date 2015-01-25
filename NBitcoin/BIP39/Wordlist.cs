using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class Wordlist
	{
		static Wordlist()
		{
			WordlistSource = new GithubWordlistSource();
		}
		private static Wordlist _Japanese;
		public static Wordlist Japanese
		{
			get
			{
				if(_Japanese == null)
					_Japanese = LoadWordList(Language.Japanese).Result;
				return _Japanese;
			}
		}

		private static Wordlist _ChineseSimplified;
		public static Wordlist ChineseSimplified
		{
			get
			{
				if(_ChineseSimplified == null)
					_ChineseSimplified = LoadWordList(Language.ChineseSimplified).Result;
				return _ChineseSimplified;
			}
		}

		private static Wordlist _ChineseTraditional;
		public static Wordlist ChineseTraditional
		{
			get
			{
				if(_ChineseTraditional == null)
					_ChineseTraditional = LoadWordList(Language.ChineseTraditional).Result;
				return _ChineseTraditional;
			}
		}

		private static Wordlist _Spanish;
		public static Wordlist Spanish
		{
			get
			{
				if(_Spanish == null)
					_Spanish = LoadWordList(Language.Spanish).Result;
				return _Spanish;
			}
		}

		private static Wordlist _English;
		public static Wordlist English
		{
			get
			{
				if(_English == null)
					_English = LoadWordList(Language.English).Result;
				return _English;
			}
		}


		public static Task<Wordlist> LoadWordList(Language language)
		{
			string name = GetLanguageFileName(language);
			return LoadWordList(name);
		}

		private static string GetLanguageFileName(Language language)
		{
			string name = null;
			switch(language)
			{
				case Language.ChineseTraditional:
					name = "chinese_traditional";
					break;
				case Language.ChineseSimplified:
					name = "chinese_simplified";
					break;
				case Language.English:
					name = "english";
					break;
				case Language.Japanese:
					name = "japanese";
					break;
				case Language.Spanish:
					name = "spanish";
					break;
				default:
					throw new NotSupportedException(language.ToString());
			}
			return name;
		}

		static Dictionary<string, Wordlist> _LoadedLists = new Dictionary<string, Wordlist>();
		public static async Task<Wordlist> LoadWordList(string name)
		{
			if(name == null)
				throw new ArgumentNullException("name");
			Wordlist result = null;
			lock(_LoadedLists)
			{
				_LoadedLists.TryGetValue(name, out result);
			}
			if(result != null)
				return await Task.FromResult<Wordlist>(result).ConfigureAwait(false);
			

			if(WordlistSource == null)
				throw new InvalidOperationException("Wordlist.WordlistSource is not set, impossible to fetch word list.");
			result = await WordlistSource.Load(name).ConfigureAwait(false);
			if(result != null)
				lock(_LoadedLists)
				{
					_LoadedLists.AddOrReplace(name, result);
				}
			return result;
		}

		public static IWordlistSource WordlistSource
		{
			get;
			set;
		}

		private String[] _words;

		/// <summary>
		/// Constructor used by inheritence only
		/// </summary>
		/// <param name="words">The words to be used in the wordlist</param>
		public Wordlist(String[] words)
		{
			_words = words
						.Select(w=>w.Normalize(NormalizationForm.FormKD))
						.ToArray();
		}

		/// <summary>
		/// Method to determine if word exists in word list, great for auto language detection
		/// </summary>
		/// <param name="word">The word to check for existence</param>
		/// <returns>Exists (true/false)</returns>
		public bool WordExists(string word, out int index)
		{
			word = word.Normalize(NormalizationForm.FormKD);
			if(_words.Contains(word))
			{
				index = Array.IndexOf(_words, word);
				return true;
			}

			//index -1 means word is not in wordlist
			index = -1;
			return false;
		}

		/// <summary>
		/// Returns a string containing the word at the specified index of the wordlist
		/// </summary>
		/// <param name="index">Index of word to return</param>
		/// <returns>Word</returns>
		public string GetWordAtIndex(int index)
		{
			return _words[index];
		}

		/// <summary>
		/// The number of all the words in the wordlist
		/// </summary>
		public int WordCount
		{
			get
			{
				return _words.Length;
			}
		}
	}
}
