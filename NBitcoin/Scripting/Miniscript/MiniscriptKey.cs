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
		public readonly string Text;
		public MiniscriptStringKeyHash() {}

		public MiniscriptStringKeyHash(string text)
		{
			Text = text;
		}

		public uint160 ToHash160()
		{
			throw new NotSupportedException();
		}

		public string ToHex() => Text;

		public static MiniscriptStringKeyHash Parse(string str) => new MiniscriptStringKeyHash(str);
	}
	public class MiniscriptStringKey : IMiniscriptKey<MiniscriptStringKeyHash>
	{
		public readonly string Text;

		public MiniscriptStringKey() {}
		public MiniscriptStringKey(string text)
		{
			Text = text;
		}

		public MiniscriptStringKeyHash ToPubKeyHash() =>
			new MiniscriptStringKeyHash($"<{Text}>");

		public PubKey ToPublicKey()
		{
			throw new NotSupportedException();
		}

		public int SerializedLength()
		{
			throw new NotSupportedException();
		}

		public string ToHex() => Text;

		public bool Equals(IMiniscriptKey<MiniscriptStringKeyHash> other)
		{
			return Equals(other as MiniscriptStringKey);
		}


		public static MiniscriptStringKey Parse(string str)
			=> new MiniscriptStringKey(str);
	}
	public static class MiniscriptFragmentParser<TPk, TPKh>
		where TPk : class, IMiniscriptKey<TPKh>, new()
		where TPKh : class, IMiniscriptKeyHash, new()
	{
		public static TPk ParseKey(string str)
		{
			var t = new TPk();
			if (t is PubKey)
			{
				return new PubKey(str) as TPk;
			}

			if (t is MiniscriptStringKey)
			{
				return MiniscriptStringKey.Parse(str) as TPk;
			}
			throw new NotSupportedException();
		}

		public static TPKh ParseHash(string str)
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