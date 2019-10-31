using System;

namespace NBitcoin.Scripting.Miniscript
{
	public interface IMiniscriptKeyHash
	{
		uint160 ToHash160();
		string ToHex();
		bool TryParse(string str);
	}

	/// <summary>
	/// Interface to abstract a PubKey in Miniscript
	/// When we are dealing with Miniscript directly without output descriptor, pubkey is directly hex
	/// encoded. But when we are parsing Output Descriptor, we may want to parse a xpub (or other similar encodings)
	/// So we must not hold pubkey directly.
	/// </summary>
	public interface IMiniscriptKey<TPKh> where TPKh : IMiniscriptKeyHash
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

		bool TryParse(string str);

		bool Equals(IMiniscriptKey<TPKh> other);
	}

}
