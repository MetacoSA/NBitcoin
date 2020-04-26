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
    public class MemoryMatrix
	{
		private readonly byte[] matrix;
		private readonly ulong nRows;
		private readonly ulong nCols;
		private readonly ulong bytesRowLength;

		public MemoryMatrix(byte[] matrix, ulong nRows, ulong nCols)
		{
			this.matrix = matrix;
			this.nRows = nRows;
			this.nCols = nCols;
			this.bytesRowLength = LyraConstants.CalculateBytesLowLength(nCols);
		}

		public void SetByteArrayCellWithValue(ulong row, ulong column, ulong value)
		{
			var valueBytes = BitConverter.GetBytes(value);
			var byteIndex = GetByteIndex(row, column);

			Array.Copy(valueBytes, 0, matrix, byteIndex, valueBytes.Length);
		}

		private int GetByteIndex(ulong row, ulong column)
		{
			var byteIndex = row * bytesRowLength;
			byteIndex += column * LyraConstants.BLOCK_LEN_BYTES;
			return (int)byteIndex;
		}

		public void SetColumnWithBlockData(ulong row, ulong column, ulong[] blockData)
		{
			var blockDataBytes = blockData.SelectMany(BitConverter.GetBytes).ToArray();
			var byteIndex = GetByteIndex(row, column);

			Array.Copy(blockDataBytes, 0, matrix, byteIndex, blockDataBytes.Length);
		}

		public ulong GetOneValueFromColumnBlockData(ulong row, ulong column, int blockDataColumn)
		{
			byte[] valueBytes = new byte[LyraConstants.UINT64_SIZE];
			var byteIndex = GetByteIndex(row, column) + blockDataColumn * (int)LyraConstants.UINT64_SIZE;

			Array.Copy(matrix, byteIndex, valueBytes, 0, valueBytes.Length);

			return BitConverter.ToUInt64(valueBytes, 0);
		}
	}
}