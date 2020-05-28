using System;
using System.Text;
#if !USEBC
using System.Security.Cryptography;
#endif
using NBitcoin.Crypto;

namespace NBitcoin
{
	public class UnsecureRandom : IRandom
	{
		Random _Rand = new Random();
		#region IRandom Members

		public void GetBytes(byte[] output)
		{
			lock (_Rand)
			{
				_Rand.NextBytes(output);
			}
		}
#if HAS_SPAN
		public void GetBytes(Span<byte> output)
		{
			_Rand.NextBytes(output);
		}
#endif
#endregion

	}


	public interface IRandom
	{
		void GetBytes(byte[] output);
#if HAS_SPAN
		void GetBytes(Span<byte> output);
#endif
	}

	public partial class RandomUtils
	{
		public static bool UseAdditionalEntropy { get; set; } = true;

		public static IRandom Random
		{
			get;
			set;
		}

		public static byte[] GetBytes(int length)
		{
			byte[] data = new byte[length];
			if (Random == null)
				throw new InvalidOperationException("You must set the RNG (RandomUtils.Random) before generating random numbers");
			Random.GetBytes(data);
			PushEntropy(data);
			return data;
		}

#if HAS_SPAN
		public static void GetBytes(Span<byte> span)
		{
			if (Random == null)
				throw new InvalidOperationException("You must set the RNG (RandomUtils.Random) before generating random numbers");
			Random.GetBytes(span);
		}
#endif

		private static void PushEntropy(byte[] data)
		{
			if (!UseAdditionalEntropy || additionalEntropy == null || data.Length == 0)
				return;
			int pos = entropyIndex;
			var entropy = additionalEntropy;
			for (int i = 0; i < data.Length; i++)
			{
				data[i] ^= entropy[pos % 32];
				pos++;
			}
			entropy = Hashes.SHA256(data);
			for (int i = 0; i < data.Length; i++)
			{
				data[i] ^= entropy[pos % 32];
				pos++;
			}
			entropyIndex = pos % 32;
		}

		static volatile byte[] additionalEntropy = null;
		static volatile int entropyIndex = 0;

		public static void AddEntropy(string data)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));
			AddEntropy(Encoding.UTF8.GetBytes(data));
		}

		public static void AddEntropy(byte[] data)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));
			var entropy = Hashes.SHA256(data);
			if (additionalEntropy == null)
				additionalEntropy = entropy;
			else
			{
				for (int i = 0; i < 32; i++)
				{
					additionalEntropy[i] ^= entropy[i];
				}
				additionalEntropy = Hashes.SHA256(additionalEntropy);
			}
		}

		public static uint256 GetUInt256()
		{
			return new uint256(GetBytes(32));
		}

		public static uint GetUInt32()
		{
			return BitConverter.ToUInt32(GetBytes(sizeof(uint)), 0);
		}

		public static int GetInt32()
		{
			return BitConverter.ToInt32(GetBytes(sizeof(int)), 0);
		}
		public static ulong GetUInt64()
		{
			return BitConverter.ToUInt64(GetBytes(sizeof(ulong)), 0);
		}

		public static long GetInt64()
		{
			return BitConverter.ToInt64(GetBytes(sizeof(long)), 0);
		}

		public static void GetBytes(byte[] output)
		{
			if (Random == null)
				throw new InvalidOperationException("You must set the RNG (RandomUtils.Random) before generating random numbers");
			Random.GetBytes(output);
			PushEntropy(output);
		}
	}
}
