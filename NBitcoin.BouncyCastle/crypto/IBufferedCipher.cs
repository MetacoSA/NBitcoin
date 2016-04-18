using System;

namespace NBitcoin.BouncyCastle.Crypto
{
	/// <remarks>Block cipher engines are expected to conform to this interface.</remarks>
    public interface IBufferedCipher
    {
		/// <summary>The name of the algorithm this cipher implements.</summary>
		string AlgorithmName { get; }

		/// <summary>Initialise the cipher.</summary>
		/// <param name="forEncryption">If true the cipher is initialised for encryption,
		/// if false for decryption.</param>
		/// <param name="parameters">The key and other data required by the cipher.</param>
        void Init(bool forEncryption, ICipherParameters parameters);

		int GetBlockSize();

		int GetOutputSize(int inputLen);

		int GetUpdateOutputSize(int inputLen);

		byte[] ProcessByte(byte input);
		int ProcessByte(byte input, byte[] output, int outOff);

		byte[] ProcessBytes(byte[] input);
		byte[] ProcessBytes(byte[] input, int inOff, int length);
		int ProcessBytes(byte[] input, byte[] output, int outOff);
		int ProcessBytes(byte[] input, int inOff, int length, byte[] output, int outOff);

		byte[] DoFinal();
		byte[] DoFinal(byte[] input);
		byte[] DoFinal(byte[] input, int inOff, int length);
		int DoFinal(byte[] output, int outOff);
		int DoFinal(byte[] input, byte[] output, int outOff);
		int DoFinal(byte[] input, int inOff, int length, byte[] output, int outOff);

		/// <summary>
		/// Reset the cipher. After resetting the cipher is in the same state
		/// as it was after the last init (if there was one).
		/// </summary>
        void Reset();
    }
}
