using System;

namespace NBitcoin.Scripting.Miniscript
{
	public interface IMiniscriptKeyHash
	{
		uint160 ToHash160();
		string ToHex();
	}

	/// <summary>
	/// Interface to abstract a PubKey in Miniscript
	/// When we are dealing with Miniscript directly without output descriptor, pubkey is directly hex
	/// encoded. But when we are parsing Output Descriptor, we may want to parse a xpub (or other similar encodings)
	/// So we must not hold pubkey directly.
	/// </summary>
	public interface IMiniscriptKey<TPKh>
		where TPKh : IMiniscriptKeyHash
	{
		TPKh ToPubKeyHash();

		/// <summary>
		/// Converts an object to public key
		/// </summary>
		/// <returns></returns>
		PubKey ToPublicKey();

		/// <summary>
		/// Computes the size of a public key when serialized in a script,
		/// including the length bytes
		/// </summary>
		/// <returns></returns>
		int SerializedLength();

		string ToHex();

		bool Equals(IMiniscriptKey<TPKh> other);
	}

	public class MiniscriptStringKeyHash : IMiniscriptKeyHash
	{
		public uint160 ToHash160()
		{
			throw new NotImplementedException();
		}

		public string ToHex()
		{
			throw new NotImplementedException();
		}

		public static MiniscriptStringKeyHash Parse(string str)
		{
			throw new NotImplementedException();
		}
	}
	public class MiniscriptStringKey : IMiniscriptKey<MiniscriptStringKeyHash>
	{
		public MiniscriptStringKeyHash ToPubKeyHash()
		{
			throw new NotImplementedException();
		}

		public PubKey ToPublicKey()
		{
			throw new NotImplementedException();
		}

		public int SerializedLength()
		{
			throw new NotImplementedException();
		}

		public string ToHex()
		{
			throw new NotImplementedException();
		}

		public bool Equals(IMiniscriptKey<MiniscriptStringKeyHash> other)
		{
			throw new NotImplementedException();
		}

		public static MiniscriptStringKey Parse(string str)
		{
			throw new NotImplementedException();
		}
	}
	public static class MiniscriptKeyParser<T, TPKh>
		where TPKh : class, IMiniscriptKeyHash, new()
		where T : class, IMiniscriptKey<TPKh>, new()
	{
		public static T TryParse(string str)
		{
			var t = new T();
			if (t is PubKey)
			{
				return new PubKey(str) as T;
			}

			if (t is MiniscriptStringKey)
			{
				return MiniscriptStringKey.Parse(str) as T;
			}
			throw new NotSupportedException();
		}

		public static TPKh TryParseHash(string str)
		{
			var t = new TPKh();
			if (t is uint160)
				return uint160.Parse(str) as TPKh;
			if (t is MiniscriptStringKeyHash)
				return MiniscriptStringKeyHash.Parse(str) as TPKh;
			throw new NotSupportedException();
		}
	}

}
