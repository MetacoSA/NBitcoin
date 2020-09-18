/***********************************************************************

	Lyra2 .NET implementation written by Krzysztof Kabała <krzykab@gmail.com>.
	Repository: https://github.com/kkabala/Lyra2
	It is distributed under MIT license.
	It is based on C implementation written by The Lyra PHC Team,
	which can be found here: https://github.com/gincoin-dev/gincoin-core/blob/master/src/crypto/Lyra2Z/Lyra2.c
	Distribution of this file or code is only allowed with this header.

    -------------------------------------------------------------------
	WARNING: this is a BROKEN Lyra2 version for Phi2 hash computation.
    -------------------------------------------------------------------

	Original Argoneum coin uses a crypto library from YIIMP pool
	https://github.com/tpruvot/yiimp since Phi2 hash was introduced by
	Lux coin and first implemented there. Porting the code to C#, it was
	found that a regular Lyra2 hash is not the same as used by Phi2.
	The bug was found in this line of Lyra2 implementation:

	uint64_t *ptrWord = wholeMatrix;
	...
	ptrWord += BLOCK_LEN; //goes to next block of pad(pwd || salt || basil)
	https://github.com/tpruvot/yiimp/blob/eec1befbd3fba1614db023674361e995e6a62829/stratum/algos/Lyra2.c#L145

	It was supposed to change pointer by one block of 64 bytes (BLOCK_LEN),
	but in fact it is moved by 64 words since ptrWord is uint64_t pointer.
	As a result, instead of getting some input parameters, sponge absorbs
	zero bytes from empty memory matrix (thanks they are zeroed, not random).
	So the final hash is not a proper Lyra2 and requires some custom
	implementation to get the same hash. Here is that custom implementation.

	/osnwt

/***********************************************************************/

using System;
using System.Linq;

namespace NBitcoin.Altcoins.ArgoneumInternals.Lyra2Broken
{
    public class Lyra2
	{
		private const ulong BLOCK_LEN_BLAKE2_SAFE_INT64 = 8;
		private const ulong BLOCK_LEN_BLAKE2_SAFE_BYTES = BLOCK_LEN_BLAKE2_SAFE_INT64 * 8;

		private ulong[] state;
		MemoryMatrix memMatrix;
		byte[] wholeMatrix;
		Sponge sponge;

		long row; //index of row to be processed
		long prev; //index of prev (last row ever computed/modified)
		long i; //auxiliary iteration counter
		long tau; //Time Loop iterator
		long rowa; //index of row* (a previous row, deterministically picked during Setup and randomly picked while Wandering)
		long step; //Visitation step (used during Setup and Wandering phases)
		long window; //Visitation window (used to define which rows can be revisited during Setup)
		long gap; //Modifier to the step, assuming the values 1 or -1

		private static readonly ulong[] blake2b_IV = new ulong[]
		{
			0x6a09e667f3bcc908UL, 0xbb67ae8584caa73bUL,
			0x3c6ef372fe94f82bUL, 0xa54ff53a5f1d36f1UL,
			0x510e527fade682d1UL, 0x9b05688c2b3e6c1fUL,
			0x1f83d9abfb41bd6bUL, 0x5be0cd19137e2179UL
		};

		private void ClearSettings()
		{
			row = 2;
			prev = 1;
			i = 0;
			tau = 0;
			rowa = 0;
			step = 1;
			window = 2;
			gap = 1;
		}

		public void Calculate(byte[] K, byte[] pwd, byte[] salt, ulong timeCost, ulong nRows, ulong nCols)
		{
			ulong kLength = (ulong)K.Length;
			ulong pwdLength = (ulong)pwd.Length;
			ulong saltLength = (ulong)salt.Length;

			ClearSettings();

			ulong ROW_LEN_INT64 = LyraConstants.BLOCK_LEN_INT64 * nCols;
			ulong ROW_LEN_BYTES = ROW_LEN_INT64 * LyraConstants.UINT64_SIZE;

			i = (int)(nRows * ROW_LEN_BYTES);
			wholeMatrix = new byte[nRows * ROW_LEN_BYTES];
			for (int n = 0; n < wholeMatrix.Length; n++)
			{
				wholeMatrix[n] = 0;
			}

			memMatrix = new MemoryMatrix(wholeMatrix, nRows, nCols);

			ulong nBlocksInput = ((saltLength + pwdLength + 6 * sizeof(ulong)) / BLOCK_LEN_BLAKE2_SAFE_BYTES) + 1;

			var integerParameters = new[] { kLength, pwdLength, saltLength, timeCost, nRows, nCols };

			// Broken for Phi2 hash and replaced by 2 lines below
			//
			// var concatedParameters = pwd.Concat(salt).Concat(integerParameters.SelectMany(BitConverter.GetBytes)).ToArray();
			// Array.Copy(concatedParameters, wholeMatrix, concatedParameters.Length);
			// int firstFreeCellIndexOfWholeMatrix = concatedParameters.Length;
			// //Now comes the padding

			// wholeMatrix[firstFreeCellIndexOfWholeMatrix] = 0x80;
			// firstFreeCellIndexOfWholeMatrix = (int)(nBlocksInput * BLOCK_LEN_BLAKE2_SAFE_BYTES - 1);
			// wholeMatrix[firstFreeCellIndexOfWholeMatrix] ^= 0x01;

			var concatedParameters = pwd.Concat(salt).ToArray();
			Array.Copy(concatedParameters, wholeMatrix, concatedParameters.Length);

			Initialize();
			RunSetupPhase((int)nBlocksInput, nCols, nRows);
			RunWanderingPhase(timeCost, nCols, nRows);
			RunWrapUpPhase(K);
		}

		private void RunWrapUpPhase(byte[] k)
		{
			sponge.AbsorbBlock(rowa);
			sponge.Squeeze(k);
		}

		private void RunWanderingPhase(ulong timeCost, ulong nCols, ulong nRows)
		{
			row = 0;
			for (tau = 1; (ulong)tau <= timeCost; tau++)
			{
				step = (tau % 2 == 0) ? -1 : (int)nRows / 2 - 1;
				do
				{
                    rowa = (long)(((state[0])) % nRows);

					sponge.ReducedDuplexRow((ulong)prev, (ulong)rowa, (ulong)row, nCols);

					prev = row;

					var rowWithStep = row + step;
					var nRowsInt64 = (long)nRows;
					row = (rowWithStep % nRowsInt64 + nRowsInt64) % nRowsInt64;
				} while (row != 0);
			}
		}

		private void RunSetupPhase(int nBlocksInput, ulong nCols, ulong nRows)
		{
			for (i = 0; i < nBlocksInput; i++)
			{
				var arrayPassed = wholeMatrix.Skip((int)(i * (int)BLOCK_LEN_BLAKE2_SAFE_INT64 * (int)LyraConstants.UINT64_SIZE)).ToArray();
				sponge.AbsorbBlockBlake2Safe(arrayPassed);
			}

			sponge.ReducedSqueezeRow0(0, nCols);
			sponge.ReducedDuplexRow1(0, 1, nCols);

			do
			{
				sponge.ReducedDuplexRowSetup((ulong)prev, (ulong)rowa, (ulong)row, nCols);

				rowa = (rowa + step) & (window - 1);
				prev = row;
				row++;

				if (rowa == 0)
				{
					step = window + gap;
					window *= 2;
					gap = -1 * gap;
				}

			} while (row < (int)nRows);
		}

		private void Initialize()
		{
			state = new ulong[16];
			InitState();

			sponge = new Sponge(state, memMatrix);
		}

		private void InitState()
		{
			Array.Clear(state, 0, state.Length);

			for (int i = 8; i < 16; i++)
			{
				state[i] = blake2b_IV[i-8];
			}
		}
	}
}
