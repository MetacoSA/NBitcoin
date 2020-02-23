using System;
using System.Collections;
using System.Collections.Generic;

namespace NBitcoin.Scripting.Miniscript
{
	/// <summary>
	/// Trait describing a lookup table for signatures, hash preimages, etc.
	/// Every method has a default implementation that simply returns `false`
	/// on every query. Users are expected to override the methods that they
	/// have data for.
	/// </summary>
	/// <typeparam name="TPk"></typeparam>
	/// <typeparam name="TPKh"></typeparam>
	public abstract class ISatisfier<TPk, TPKh>
	where TPk : IMiniscriptKey<TPKh>
	where TPKh : IMiniscriptKeyHash
	{
		public virtual bool TryGetSignature(TPk pubkey, out TransactionSignature txSig)
		{
			txSig = default;
			return false;
		}
		public virtual bool TryGetPubKey(TPKh pubkeyHash, out TPk pubkey)
		{
			pubkey = default;
			return false;
		}
		public virtual bool TryGetPubkeyAndSig(TPKh pubkeyHash, out TPk pubkey, out TransactionSignature txSig)
		{
			txSig = default;
			pubkey = default;
			return false;
		}
		public virtual bool TryGetSha256Preimage(uint256 hash, out uint256 preimage)
		{
			preimage = default;
			return false;
		}
		public virtual bool TryGetHash256Preimage(uint256 hash, out uint256 preimage)
		{
			preimage = default;
			return false;
		}
		public virtual bool TryGetRipemd160Preimage(uint160 hash, out uint256 preimage)
		{
			preimage = default;
			return false;
		}
		public virtual bool TryGetHash160Preimage(uint160 hash, out uint256 preimage)
		{
			preimage = default;
			return false;
		}

		public virtual bool IsOlderThan(uint currentTime) => false;
		public virtual bool IsAfterThan(uint currentTime) => false;
	}

	public class InMemorySatisfier<TPk, TPKh> : ISatisfier<TPk, TPKh>, IDictionary<TPk, Tuple<TPk, TransactionSignature>>
	where TPk : IMiniscriptKey<TPKh>
	where TPKh : IMiniscriptKeyHash
	{
		public override bool TryGetSignature(TPk pubkey, out TransactionSignature txSig)
		{
			throw new NotImplementedException();
		}

		public override bool TryGetPubKey(TPKh pubkeyHash, out TPk pubkey)
		{
			throw new NotImplementedException();
		}

		public override bool TryGetPubkeyAndSig(TPKh pubkeyHash, out TPk pubkey, out TransactionSignature txSig)
		{
			throw new NotImplementedException();
		}

		public override bool TryGetSha256Preimage(uint256 hash, out uint256 preimage)
		{
			throw new NotImplementedException();
		}

		public override bool TryGetHash256Preimage(uint256 hash, out uint256 preimage)
		{
			throw new NotImplementedException();
		}

		public override bool TryGetRipemd160Preimage(uint160 hash, out uint256 preimage)
		{
			throw new NotImplementedException();
		}

		public override bool TryGetHash160Preimage(uint160 hash, out uint256 preimage)
		{
			throw new NotImplementedException();
		}

		public override bool IsOlderThan(uint currentTime)
		{
			throw new NotImplementedException();
		}

		public override bool IsAfterThan(uint currentTime)
		{
			throw new NotImplementedException();
		}

		public IEnumerator<KeyValuePair<TPk, Tuple<TPk, TransactionSignature>>> GetEnumerator()
		{
			throw new NotImplementedException();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Add(KeyValuePair<TPk, Tuple<TPk, TransactionSignature>> item)
		{
			throw new NotImplementedException();
		}

		public void Clear()
		{
			throw new NotImplementedException();
		}

		public bool Contains(KeyValuePair<TPk, Tuple<TPk, TransactionSignature>> item)
		{
			throw new NotImplementedException();
		}

		public void CopyTo(KeyValuePair<TPk, Tuple<TPk, TransactionSignature>>[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}

		public bool Remove(KeyValuePair<TPk, Tuple<TPk, TransactionSignature>> item)
		{
			throw new NotImplementedException();
		}

		public int Count { get; }
		public bool IsReadOnly { get; }
		public void Add(TPk key, Tuple<TPk, TransactionSignature> value)
		{
			throw new NotImplementedException();
		}

		public bool ContainsKey(TPk key)
		{
			throw new NotImplementedException();
		}

		public bool Remove(TPk key)
		{
			throw new NotImplementedException();
		}

		public bool TryGetValue(TPk key, out Tuple<TPk, TransactionSignature> value)
		{
			throw new NotImplementedException();
		}

		public Tuple<TPk, TransactionSignature> this[TPk key]
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		public ICollection<TPk> Keys { get; }
		public ICollection<Tuple<TPk, TransactionSignature>> Values { get; }
	}
}
