using System;

namespace NBitcoin.Scripting.Miniscript
{
	public interface IMiniscriptKeyHash
	{
		uint160 ToHash160();
	}

	/// <summary>
	/// Interface to abstract PubKey in Miniscript
	/// </summary>
	public interface IMiniscriptKey
	{
		IMiniscriptKeyHash MiniscriptKeyHash { get; }

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

		bool TryParse(string str);

		bool Equals(IMiniscriptKey other);
	}

}
