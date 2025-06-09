#if !NO_RECORDS
#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.WalletPolicies
{
	public class DerivationCache : ConcurrentDictionary<(IHDKey, int), Lazy<IHDKey?>>
	{
	}
	public class DeriveParameters
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="intent">Whether this is a deposit or change address</param>
		/// <param name="index">The address index</param>
		public DeriveParameters(AddressIntent intent, int index)
		{
			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index), "index should be positive");
			Intent = intent;
			AddressIndexes = new int[] { index };
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="intent">Whether this is a deposit or change address</param>
		/// <param name="indexes">The addresses to derive</param>
		public DeriveParameters(AddressIntent intent, int[]? indexes)
		{
			Intent = intent;
			AddressIndexes = indexes ?? Array.Empty<int>();
			foreach (var idx in AddressIndexes)
				if (idx < 0)
					throw new ArgumentOutOfRangeException(nameof(indexes), "indexes should be positive");
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="intent">Whether this is a deposit or change address</param>
		/// <param name="startIndex">The first address to start generating</param>
		/// <param name="count">The number of addresses to generate</param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public DeriveParameters(AddressIntent intent, int startIndex, int count)
		{
			if (startIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(startIndex), "startIndex should be positive");
			if (startIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(count), "count should be positive");
			Intent = intent;
			AddressIndexes = Enumerable.Range(startIndex, count).ToArray();
		}
		public AddressIntent Intent { get; }
		public int[] AddressIndexes { get; set; }
		public DerivationCache? DervivationCache { get; set; }
	}
}
#endif
