using NBitcoin.DataEncoders;
using Org.BouncyCastle.Crypto.Digests;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public static class Extensions
	{
		public static void AddOrReplace<TKey, TValue>(this Dictionary<TKey, TValue> dico, TKey key, TValue value)
		{
			if(dico.ContainsKey(key))
			{
				dico.Remove(key);
				dico.Add(key, value);
			}
			else
			{
				dico.Add(key, value);
			}
		}
		public static Money Sum(this IEnumerable<Money> money)
		{
			Money running = Money.Zero;
			foreach(var m in money)
				running += m;
			return running;
		}
		public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
		{
			if(!dictionary.ContainsKey(key))
			{
				dictionary.Add(key, value);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Converts a given DateTime into a Unix timestamp
		/// </summary>
		/// <param name="value">Any DateTime</param>
		/// <returns>The given DateTime in Unix timestamp format</returns>
		public static int ToUnixTimestamp(this DateTime value)
		{
			return (int)Math.Truncate((value.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds);
		}

		/// <summary>
		/// Gets a Unix timestamp representing the current moment
		/// </summary>
		/// <param name="ignored">Parameter ignored</param>
		/// <returns>Now expressed as a Unix timestamp</returns>
		public static int UnixTimestamp(this DateTime ignored)
		{
			return (int)Math.Truncate((DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds);
		}
	}
	public class Utils
	{
		public static bool ArrayEqual(byte[] a, byte[] b)
		{
			if(a == null && b == null)
				return true;
			if(a == null)
				return false;
			if(b == null)
				return false;
			return ArrayEqual(a, 0, b, 0, Math.Max(a.Length, b.Length));
		}
		public static bool ArrayEqual(byte[] a, int startA, byte[] b, int startB, int length)
		{
			if(a == null && b == null)
				return true;
			if(a == null)
				return false;
			if(b == null)
				return false;
			var alen = a.Length - startA;
			var blen = b.Length - startB;

			if(alen < length || blen < length)
				return false;

			for(int ai = startA, bi = startB ; ai < startA + length ; ai++, bi++)
			{
				if(a[ai] != b[bi])
					return false;
			}
			return true;
		}


		public static String BITCOIN_SIGNED_MESSAGE_HEADER = "Bitcoin Signed Message:\n";
		public static byte[] BITCOIN_SIGNED_MESSAGE_HEADER_BYTES = Encoding.UTF8.GetBytes(BITCOIN_SIGNED_MESSAGE_HEADER);

		//http://bitcoinj.googlecode.com/git-history/keychain/core/src/main/java/com/google/bitcoin/core/Utils.java
		public static byte[] FormatMessageForSigning(string messageText)
		{
			MemoryStream ms = new MemoryStream();
			var message = Encoding.UTF8.GetBytes(messageText);

			ms.WriteByte((byte)BITCOIN_SIGNED_MESSAGE_HEADER_BYTES.Length);
			Write(ms, BITCOIN_SIGNED_MESSAGE_HEADER_BYTES);

			VarInt size = new VarInt((ulong)message.Length);
			Write(ms, size.ToBytes());
			Write(ms, message);
			return ms.ToArray();
		}


		private static void Write(MemoryStream ms, byte[] bytes)
		{
			ms.Write(bytes, 0, bytes.Length);
		}

		internal static Array BigIntegerToBytes(Org.BouncyCastle.Math.BigInteger b, int numBytes)
		{
			if(b == null)
			{
				return null;
			}
			byte[] bytes = new byte[numBytes];
			byte[] biBytes = b.ToByteArray();
			int start = (biBytes.Length == numBytes + 1) ? 1 : 0;
			int length = Math.Min(biBytes.Length, numBytes);
			Array.Copy(biBytes, start, bytes, numBytes - length, length);
			return bytes;

		}




		//https://en.bitcoin.it/wiki/Script
		public static byte[] BigIntegerToBytes(BigInteger num)
		{
			if(num == 0)
				//Positive 0 is represented by a null-length vector
				return new byte[0];

			bool isPositive = true;
			if(num < 0)
			{
				isPositive = false;
				num *= -1;
			}
			var array = num.ToByteArray();
			if(!isPositive)
				array[array.Length - 1] |= 0x80;
			return array;
		}

		public static BigInteger BytesToBigInteger(byte[] data)
		{
			if(data == null)
				throw new ArgumentNullException("data");
			if(data.Length == 0)
				return BigInteger.Zero;
			data = data.ToArray();
			var positive = (data[data.Length - 1] & 0x80) == 0;
			if(!positive)
			{
				data[data.Length - 1] &= unchecked((byte)~0x80);
				return -new BigInteger(data);
			}
			return new BigInteger(data);
		}

		static readonly TraceSource _TraceSource = new TraceSource("NBitcoin");
		public static bool error(string msg)
		{
			_TraceSource.TraceEvent(TraceEventType.Error, 0, msg);
			return false;
		}

		internal static void log(string msg)
		{
			_TraceSource.TraceEvent(TraceEventType.Information, 0, msg);
		}


		static DateTimeOffset unixRef = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

		public static uint DateTimeToUnixTime(DateTimeOffset dt)
		{
			dt = dt.ToUniversalTime();
			if(dt < unixRef)
				throw new ArgumentOutOfRangeException("The supplied datetime can't be expressed in unix timestamp");
			var result = (dt - unixRef).TotalSeconds;
			if(result > UInt32.MaxValue)
				throw new ArgumentOutOfRangeException("The supplied datetime can't be expressed in unix timestamp");
			return (uint)result;
		}

		public static DateTimeOffset UnixTimeToDateTime(uint timestamp)
		{
			var span = TimeSpan.FromSeconds(timestamp);
			return unixRef + span;
		}



		public static string ExceptionToString(Exception exception)
		{
			Exception ex = exception;
			StringBuilder stringBuilder = new StringBuilder(128);
			while(ex != null)
			{
				stringBuilder.Append(ex.GetType().Name);
				stringBuilder.Append(": ");
				stringBuilder.Append(ex.Message);
				stringBuilder.AppendLine(ex.StackTrace);
				ex = ex.InnerException;
				if(ex != null)
				{
					stringBuilder.Append(" ---> ");
				}
			}
			return stringBuilder.ToString();
		}

		public static void Shuffle<T>(T[] arr)
		{
			Random rand = new Random();
			for(int i = 0 ; i < arr.Length ; i++)
			{
				var fromIndex = rand.Next(arr.Length);
				var from = arr[fromIndex];

				var toIndex = rand.Next(arr.Length);
				var to = arr[toIndex];

				arr[toIndex] = from;
				arr[fromIndex] = to;
			}
		}



		internal static void SafeCloseSocket(System.Net.Sockets.Socket socket)
		{
			try
			{
				socket.Disconnect(false);
			}
			catch
			{
			}
			try
			{
				socket.Dispose();
			}
			catch
			{

			}
		}

		public static System.Net.IPEndPoint EnsureIPv6(System.Net.IPEndPoint endpoint)
		{
			if(endpoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
				return endpoint;
			return new IPEndPoint(endpoint.Address.MapToIPv6(), endpoint.Port);
		}

		public static string Serialize<T>(T obj)
		{
			DataContractSerializer seria = new DataContractSerializer(typeof(T));
			MemoryStream ms = new MemoryStream();
			seria.WriteObject(ms, obj);
			ms.Position = 0;
			return new StreamReader(ms).ReadToEnd();
		}

		public static T Deserialize<T>(string str)
		{
			DataContractSerializer seria = new DataContractSerializer(typeof(T));
			MemoryStream ms = new MemoryStream();
			StreamWriter writer = new StreamWriter(ms);
			writer.Write(str);
			writer.Flush();
			ms.Position = 0;
			return (T)seria.ReadObject(ms);
		}
	}
}
