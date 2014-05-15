using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Crypto
{
	public static class Crypt
	{
		//http://stackoverflow.com/questions/13986877/decryptbytesfrombytes-aes-c-sharp
		public static byte[] DecryptBytesToBytes(byte[] cipherText, byte[] Key)
		{
			// Check arguments. 
			if(cipherText == null || cipherText.Length <= 0)
				throw new ArgumentNullException("cipherText");
			if(Key == null || Key.Length <= 0)
				throw new ArgumentNullException("Key");

			var aes = Aes.Create();
			aes.KeySize = 256;
			aes.Mode = CipherMode.ECB;
			aes.Key = Key;
			ICryptoTransform decryptor = aes.CreateDecryptor();

			byte[] decrypted = new byte[16];
			decryptor.TransformBlock(cipherText, 0, 16, decrypted, 0);
			return decrypted;
		}
	}
}
