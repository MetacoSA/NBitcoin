using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
#if !USEBC
		using System.Security.Cryptography;
#endif

namespace NBitcoin
{
	public class UnsecureRandom : RandomUtils.IRandom
	{
		Random _Rand = new Random();
		#region IRandom Members

		public void GetBytes(byte[] output)
		{
			lock(_Rand)
			{
				_Rand.NextBytes(output);
			}
		}

		#endregion
	}
	public class RandomUtils
	{
#if !USEBC
		static RandomUtils()
		{
			//Thread safe http://msdn.microsoft.com/en-us/library/system.security.cryptography.rngcryptoserviceprovider(v=vs.110).aspx
			Random = new RNGCryptoServiceProviderRandom();
		}
		class RNGCryptoServiceProviderRandom : IRandom
		{
			readonly RNGCryptoServiceProvider _Instance;
			public RNGCryptoServiceProviderRandom()
			{
				_Instance = new RNGCryptoServiceProvider();
			}
		#region IRandom Members

			public void GetBytes(byte[] output)
			{
				_Instance.GetBytes(output);
			}

			#endregion
		}
#endif
#if USEBC && DEBUG 
		static RandomUtils()
		{
			Random = new UnsecureRandom();
		}
#endif
		public interface IRandom
		{
			void GetBytes(byte[] output);
		}

		public static IRandom Random
		{
			get;
			set;
		}

		public static byte[] GetBytes(int length)
		{
			byte[] data = new byte[length];
			if(Random == null)
				throw new InvalidOperationException("You must set the RNG (RandomUtils.Random) before generating random numbers");
			Random.GetBytes(data);
			return data;
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
			if(Random == null)
				throw new InvalidOperationException("You must set the RNG (RandomUtils.Random) before generating random numbers");
			Random.GetBytes(output);
		}
	}
}
