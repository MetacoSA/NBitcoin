using NBitcoin.BouncyCastle.Crypto.Digests;
using System.Linq;

namespace NBitcoin.Altcoins
{
    public class HeavyHash 
    {
        public byte[] GetSha3(byte[] input) { 
            var sha3 = new Sha3Digest();
            sha3.BlockUpdate(input, 0, input.Length); // equivalent _block.Length

            byte[] hash = new byte[32];
            sha3.DoFinal(hash, 0);

            return hash;
        }

		public byte[] GetHash(byte[] input, ulong[,] matrix){
			byte[] hash1 = GetSha3(input);
			byte[] x = new byte[64];

			foreach (int i in Enumerable.Range(0, 32))
			{
				x[2*i] = (byte)(hash1[i] >> 4);
				x[2*i+1] = (byte)(hash1[i] & 0x0F);
			}

			ulong[] product = Multiply(matrix, x);

			for (int i = 0; i < product.GetLength(0); i++) {  
				product[i] = product[i] >> 10;
			} 

			byte[] preout = new byte[32];

			for (int i = 0; i < preout.GetLength(0); i++) {  
				ulong a = product[2*i];
				ulong b = product[2*i+1];
				preout[i] = (byte)((a << 4 | b) ^ hash1[i]);	
			}

			return GetSha3(preout);
		}

		public ulong[] Multiply(ulong[,] matrix, byte[] arrayInput) { 
			ulong[] product = new ulong[HeavyHashMatrix.RANK];
			
			for (int matrix_row = 0; matrix_row < HeavyHashMatrix.RANK; matrix_row++) {   
				for (int matrix_col = 0; matrix_col < HeavyHashMatrix.RANK; matrix_col++) {  
					product[matrix_row] +=   
					matrix[matrix_row, matrix_col] *   
					arrayInput[matrix_col];  
				}  
			}  
			
			return product;  
		} 

    }
}