#nullable enable
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin
{
	public class PSBTHDKeyMatch<T> : PSBTHDKeyMatch where T : PSBTCoin
	{
		internal PSBTHDKeyMatch(T psbtCoin, IHDKey accountKey, KeyPath addressKeyPath, KeyValuePair<IPubKey, RootedKeyPath> kv)
			: base(psbtCoin, accountKey, addressKeyPath, kv)
		{
			if (psbtCoin == null)
				throw new ArgumentNullException(nameof(psbtCoin));
			_Coin = psbtCoin;
		}

		private readonly T _Coin;
		public new T Coin
		{
			get
			{
				return _Coin;
			}
		}
	}

	public class PSBTHDKeyMatch
	{
		internal PSBTHDKeyMatch(PSBTCoin psbtCoin, IHDKey accountKey, KeyPath addressKeyPath, KeyValuePair<IPubKey, RootedKeyPath> kv)
		{
			if (psbtCoin == null)
				throw new ArgumentNullException(nameof(psbtCoin));
			if (addressKeyPath == null)
				throw new ArgumentNullException(nameof(addressKeyPath));
			if (accountKey == null)
				throw new ArgumentNullException(nameof(accountKey));
			_Coin = psbtCoin;
			_KeyPath = kv.Value;
			_PubKey = kv.Key;
			_AddressKeyPath = addressKeyPath;
			_AccountKey = accountKey;
		}

		private readonly PSBTCoin _Coin;
		public PSBTCoin Coin
		{
			get
			{
				return _Coin;
			}
		}


		private readonly IPubKey _PubKey;
		public IPubKey PubKey
		{
			get
			{
				return _PubKey;
			}
		}


		private readonly KeyPath _AddressKeyPath;
		/// <summary>
		/// KeyPath relative to the accountKey to PubKey
		/// </summary>
		public KeyPath AddressKeyPath
		{
			get
			{
				return _AddressKeyPath;
			}
		}


		private readonly IHDKey _AccountKey;
		public IHDKey AccountKey
		{
			get
			{
				return _AccountKey;
			}
		}

		private readonly RootedKeyPath _KeyPath;
		public RootedKeyPath RootedKeyPath
		{
			get
			{
				return _KeyPath;
			}
		}
	}

	public class PSBTCoinList<T> : IReadOnlyList<T> where T : PSBTCoin
	{
		/// <summary>
		/// Filter the coins which contains the <paramref name="accountKey"/> and <paramref name="accountKeyPath"/> in the HDKeys and derive
		/// the same scriptPubKeys as <paramref name="accountHDScriptPubKey"/>.
		/// </summary>
		/// <param name="accountHDScriptPubKey">The hdScriptPubKey used to generate addresses</param>
		/// <param name="accountKey">The account key that will be used to sign (ie. 49'/0'/0')</param>
		/// <param name="accountKeyPath">The account key path</param>
		/// <returns>Inputs with HD keys matching masterFingerprint and account key</returns>
		public IEnumerable<T> CoinsFor(IHDScriptPubKey accountHDScriptPubKey, IHDKey accountKey, RootedKeyPath? accountKeyPath = null)
		{
			return GetPSBTCoins(accountHDScriptPubKey, accountKey, accountKeyPath);
		}

		/// <summary>
		/// Filter the keys which contains the <paramref name="accountKey"/> and <paramref name="accountKeyPath"/> in the HDKeys and whose input/output 
		/// the same scriptPubKeys as <paramref name="accountHDScriptPubKey"/>.
		/// </summary>
		/// <param name="accountHDScriptPubKey">The hdScriptPubKey used to generate addresses</param>
		/// <param name="accountKey">The account key that will be used to sign (ie. 49'/0'/0')</param>
		/// <param name="accountKeyPath">The account key path</param>
		/// <returns>HD Keys matching master root key</returns>
		public IEnumerable<PSBTHDKeyMatch<T>> HDKeysFor(IHDScriptPubKey accountHDScriptPubKey, IHDKey accountKey, RootedKeyPath? accountKeyPath = null)
		{
			return GetHDKeys(accountHDScriptPubKey, accountKey, accountKeyPath);
		}

		/// <summary>
		/// Filter the keys which contains the <paramref name="accountKey"/> and <paramref name="accountKeyPath"/>.
		/// </summary>
		/// <param name="accountKey">The account key that will be used to sign (ie. 49'/0'/0')</param>
		/// <param name="accountKeyPath">The account key path</param>
		/// <returns>HD Keys matching master root key</returns>
		public IEnumerable<PSBTHDKeyMatch<T>> HDKeysFor(IHDKey accountKey, RootedKeyPath? accountKeyPath = null)
		{
			return GetHDKeys(null, accountKey, accountKeyPath);
		}

		internal IEnumerable<T> GetPSBTCoins(IHDScriptPubKey accountHDScriptPubKey, IHDKey accountKey, RootedKeyPath? accountKeyPath = null)
		{
			return GetHDKeys(accountHDScriptPubKey, accountKey, accountKeyPath)
							.Select(c => c.Coin)
							.Distinct();
		}

		internal IEnumerable<PSBTHDKeyMatch<T>> GetHDKeys(IHDScriptPubKey? hdScriptPubKey, IHDKey accountKey, RootedKeyPath? accountKeyPath = null)
		{
			if (accountKey == null)
				throw new ArgumentNullException(nameof(accountKey));
			accountKey = accountKey.AsHDKeyCache();
			hdScriptPubKey = hdScriptPubKey?.AsHDKeyCache();
			var accountFingerprint = accountKey.GetPublicKey().GetHDFingerPrint();
			foreach (var c in this)
			{
				foreach (var match in c.HDKeysFor(hdScriptPubKey, accountKey, accountKeyPath, accountFingerprint))
				{
					yield return (PSBTHDKeyMatch<T>)match;
				}
			}
		}
		protected List<T> _Inner = new List<T>();

		public int Count => _Inner.Count;

		public T this[int index] => _Inner[index];

		public IEnumerator<T> GetEnumerator()
		{
			return _Inner.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _Inner.GetEnumerator();
		}
	}
}
#nullable disable
