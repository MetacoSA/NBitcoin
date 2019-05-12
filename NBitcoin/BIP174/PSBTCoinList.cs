using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin
{
	public class PSBTHDKeyMatch<T> : PSBTHDKeyMatch where T: PSBTCoin
	{
		public PSBTHDKeyMatch(T psbtCoin, KeyValuePair<PubKey, Tuple<HDFingerprint, KeyPath>> kv)
			: base (psbtCoin, kv)
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
		public PSBTHDKeyMatch(PSBTCoin psbtCoin, KeyValuePair<PubKey, Tuple<HDFingerprint, KeyPath>> kv)
		{
			if (psbtCoin == null)
				throw new ArgumentNullException(nameof(psbtCoin));
			_Coin = psbtCoin;
			_Fingerprint = kv.Value.Item1;
			_KeyPath = kv.Value.Item2;
			_PubKey = kv.Key;
		}

		private readonly PSBTCoin _Coin;
		public PSBTCoin Coin
		{
			get
			{
				return _Coin;
			}
		}


		private readonly PubKey _PubKey;
		public PubKey PubKey
		{
			get
			{
				return _PubKey;
			}
		}

		private readonly HDFingerprint _Fingerprint;
		public HDFingerprint Fingerprint
		{
			get
			{
				return _Fingerprint;
			}
		}

		private readonly KeyPath _KeyPath;
		public KeyPath KeyPath
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
		/// Filter the coins which contains a HD Key path matching this masterFingerprint/account key
		/// </summary>
		/// <param name="masterFingerprint">The master root fingerprint</param>
		/// <param name="accountKey">The account key (ie. 49'/0'/0')</param>
		/// <returns>Inputs with HD keys matching masterFingerprint and account key</returns>
		public IEnumerable<T> CoinsFor(HDFingerprint? masterFingerprint, IHDKey accountKey)
		{
			return GetPSBTCoins(masterFingerprint, accountKey);
		}
		/// <summary>
		/// Filter the coins which contains a HD Key path matching this master root key
		/// </summary>
		/// <param name="masterKey">The master root key</param>
		/// <returns>Inputs with HD keys matching master root key</returns>
		public IEnumerable<T> CoinsFor(IHDKey masterKey)
		{
			if (masterKey == null)
				throw new ArgumentNullException(nameof(masterKey));
			return GetPSBTCoins(null, masterKey);
		}

		/// <summary>
		/// Filter the hd keys which contains a HD Key path matching this masterFingerprint/account key
		/// </summary>
		/// <param name="masterFingerprint">The master root fingerprint</param>
		/// <param name="accountKey">The account key (ie. 49'/0'/0')</param>
		/// <returns>HD Keys matching master root key</returns>
		public IEnumerable<PSBTHDKeyMatch<T>> HDKeysFor(HDFingerprint? masterFingerprint, IHDKey accountKey)
		{
			return GetHDKeys(masterFingerprint, accountKey);
		}
		/// <summary>
		/// Filter the hd keys which contains a HD Key path matching this master root key
		/// </summary>
		/// <param name="masterKey">The master root key</param>
		/// <returns>HD Keys matching master root key</returns>
		public IEnumerable<PSBTHDKeyMatch<T>> HDKeysFor(IHDKey masterKey)
		{
			if (masterKey == null)
				throw new ArgumentNullException(nameof(masterKey));
			return GetHDKeys(null, masterKey);
		}

		internal IEnumerable<T> GetPSBTCoins(HDFingerprint? masterFingerprint, IHDKey accountKey)
		{
			return GetHDKeys(masterFingerprint, accountKey)
							.Select(c => c.Coin)
							.Distinct();
		}
		internal IEnumerable<PSBTHDKeyMatch<T>> GetHDKeys(HDFingerprint? masterFingerprint, IHDKey accountKey)
		{
			if (accountKey == null)
				throw new ArgumentNullException(nameof(accountKey));
			accountKey = accountKey.AsHDKeyCache();
			var accountFingerprint = accountKey.GetPublicKey().GetHDFingerPrint();
			foreach (var c in this)
			{
				foreach (var match in c.HDKeysFor(masterFingerprint, accountKey, accountFingerprint))
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
