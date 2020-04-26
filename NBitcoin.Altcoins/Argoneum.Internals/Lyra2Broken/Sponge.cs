/***********************************************************************

	Lyra2 .NET implementation written by Krzysztof Kabała <krzykab@gmail.com>.
	Repository: https://github.com/kkabala/Lyra2
	It is distributed under MIT license.
	It is based on C implementation written by The Lyra PHC Team, 
	which can be found here: https://github.com/gincoin-dev/gincoin-core/blob/master/src/crypto/Lyra2Z/Lyra2.c 
	Distribution of this file or code is only allowed with this header.

/***********************************************************************/

using System;
using System.Linq;

namespace NBitcoin.Altcoins.ArgoneumInternals.Lyra2Broken
{
	public class Sponge
	{
		private readonly ulong[] state;
		private readonly MemoryMatrix memoryMatrix;

		public Sponge(ulong[] state, MemoryMatrix memoryMatrix)
		{
			this.state = state;
			this.memoryMatrix = memoryMatrix;
		}

		/*Blake2b's G function*/
		private void G(int r, int i, ulong a, ulong b, ulong c, ulong d)
		{
			state[a] = state[a] + state[b];
			state[d] = Rotr64(state[d] ^ state[a], 32);
			state[c] = state[c] + state[d];
			state[b] = Rotr64(state[b] ^ state[c], 24);
			state[a] = state[a] + state[b];
			state[d] = Rotr64(state[d] ^ state[a], 16);
			state[c] = state[c] + state[d];
			state[b] = Rotr64(state[b] ^ state[c], 63);
		}

		private void RoundLyra(int r)
		{
			G(r, 0, 0, 4, 8, 12);
			G(r, 1, 1, 5, 9, 13);
			G(r, 2, 2, 6, 10, 14);
			G(r, 3, 3, 7, 11, 15);
			G(r, 4, 0, 5, 10, 15);
			G(r, 5, 1, 6, 11, 12);
			G(r, 6, 2, 7, 8, 13);
			G(r, 7, 3, 4, 9, 14);
		}

		private ulong Rotr64(ulong w, int c)
		{
			return (w >> c) | (w << (64 - c));
		}

		private void Blake2bLyra(int numberOfIterations)
		{
			for (int i = 0; i < numberOfIterations; i++)
			{
				RoundLyra(i);
			}
		}

		public void AbsorbBlockBlake2Safe(byte[] inByteArray)
		{
			var inArray = ConvertByteToUInt64Array(inByteArray);
			for (int i = 0; i < 8; i++)
			{
				state[i] ^= inArray[i];
			}

			Blake2bLyra(12);
		}

		private ulong[] ConvertByteToUInt64Array(byte[] byteArray)
		{
			var size = byteArray.Length / sizeof(ulong);
			var ints = new ulong[size];
			for (var index = 0; index < size; index++)
			{
				ints[index] = BitConverter.ToUInt64(byteArray, index * sizeof(ulong));
			}

			return ints;
		}

		public void ReducedSqueezeRow0(ulong rowOut, ulong nCols) // I screwed up something here
		{
			for (int i = 0; i < (int)nCols; i++)
			{
				ulong currentCol = nCols - 1 - (ulong)i;
				memoryMatrix.SetColumnWithBlockData(rowOut, currentCol, state.Take(12).ToArray());
				Blake2bLyra(1);
			}
		}

		public void ReducedDuplexRow1(ulong rowIn, ulong rowOut, ulong nCols)
		{
			for (ulong i = 0; i < nCols; i++)
			{
				//Output: goes to previous column
				ulong ptrWordOut = nCols - 1 - i;
				//Input: next column (i.e., next block in sequence)

				//caching the values - for speed purposes and not duplicating the same code
				var ptrWordIn = new ulong[12];
				var ptrWordInOut = new ulong[12];
				for (int k = 0; k < 12; k++)
				{
					ptrWordIn[k] = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, i, k);
					state[k] ^= ptrWordIn[k];
				}

				Blake2bLyra(1);

				var ptrWordOutNewBlockData = new ulong[12];
				for (int k = 0; k < 12; k++)
				{
					ptrWordOutNewBlockData[k] = ptrWordIn[k] ^ state[k];
				};

				memoryMatrix.SetColumnWithBlockData(rowOut, ptrWordOut, ptrWordOutNewBlockData);
			}
		}


		public void ReducedDuplexRowSetup(ulong rowIn, ulong rowInOut, ulong rowOut,
			ulong nCols)
		{
			for (ulong i = 0; i < nCols; i++)
			{
				//Output: goes to previous column
				ulong ptrWordOutColumn = nCols - 1 - i;
				//Input: next column (i.e., next block in sequence)
				ulong ptrWordInColumn = i;

				//caching the values - for speed purposes and not duplicating the same code
				var ptrWordIn = new ulong[12];
				var ptrWordInOut = new ulong[12];
				for (int k = 0; k < 12; k++)
				{
					ptrWordIn[k] = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordInColumn, k);
					ptrWordInOut[k] = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordInColumn, k);
					state[k] ^= ptrWordIn[k] + ptrWordInOut[k];
				}

				//Applies the reduced-round transformation f to the sponge's v_state
				Blake2bLyra(1);

				//M[row][C-1-col] = M[prev][col] XOR rand
				var ptrWordOutNewBlockData = new ulong[12];
				var ptrWordInOutNewBlockData = new ulong[12];

				for (int k = 0; k < 12; k++)
				{
					ptrWordOutNewBlockData[k] = ptrWordIn[k] ^ state[k];
					var shiftedIterator = k - 1 < 0 ? 11 : k - 1;
					ptrWordInOutNewBlockData[k] = ptrWordInOut[k] ^ state[shiftedIterator];
				}

				memoryMatrix.SetColumnWithBlockData(rowOut, ptrWordOutColumn, ptrWordOutNewBlockData);

				memoryMatrix.SetColumnWithBlockData(rowInOut, ptrWordInColumn, ptrWordInOutNewBlockData);
			}
		}

		public void ReducedDuplexRow(ulong rowIn, ulong rowInOut, ulong rowOut, ulong nCols)
		{
			for (ulong i = 0; i < nCols; i++)
			{
				//Output: goes to previous column
				//Input: next column (i.e., next block in sequence)
				//INPUT AND OUTPUT COLUMN IS HERE THE SAME!
				ulong ptrWordColumn = i;

				var ptrWordInOutGenerator = new Func<int, ulong>((blockDataColumn) => memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordColumn, blockDataColumn));
				var ptrWordIn = new ulong[12];

				for (int k = 0; k < 12; k++)
				{
					ptrWordIn[k] = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordColumn, k);
					state[k] ^= ptrWordIn[k] + ptrWordInOutGenerator(k);
				}

				Blake2bLyra(1);

				var ptrWordOutNewBlockData = new ulong[12];

				for (int k = 0; k < 12; k++)
				{
					ptrWordOutNewBlockData[k] = memoryMatrix.GetOneValueFromColumnBlockData(rowOut, ptrWordColumn, k) ^ state[k];
				};

				memoryMatrix.SetColumnWithBlockData(rowOut, ptrWordColumn, ptrWordOutNewBlockData);

				var ptrWordInOutNewBlockData = new ulong[12];

				for (int k = 0; k < 12; k++)
				{
					var shiftedIterator = k - 1 < 0 ? 11 : k - 1;
					ptrWordInOutNewBlockData[k] = ptrWordInOutGenerator(k) ^ state[shiftedIterator];
				}

				memoryMatrix.SetColumnWithBlockData(rowInOut, ptrWordColumn, ptrWordInOutNewBlockData);
			}
		}

		public void AbsorbBlock(long rowa)
		{
			for (int i = 0; i < 12; i++)
			{
				state[i] ^= memoryMatrix.GetOneValueFromColumnBlockData((ulong)rowa, 0, i);
			}

			Blake2bLyra(12);
		}

		public void Squeeze(byte[] K)
		{
			var len = (ulong)K.Length;
			var fullBlocks = len / LyraConstants.BLOCK_LEN_BYTES;
			var ptr = 0;

			var stateBytes = state.SelectMany(BitConverter.GetBytes).ToArray();
			for (ulong i = 0; i < fullBlocks; i++)
			{
				Array.Copy(stateBytes, 0, K, ptr, (int)LyraConstants.BLOCK_LEN_BYTES);
				Blake2bLyra(12);
				ptr += (int)LyraConstants.BLOCK_LEN_BYTES;
			}

			Array.Copy(stateBytes, 0, K, ptr, (int)(len % LyraConstants.BLOCK_LEN_BYTES));
		}
	}
}