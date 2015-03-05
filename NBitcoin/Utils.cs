using NBitcoin.DataEncoders;
using NBitcoin.BouncyCastle.Crypto.Digests;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin.Protocol;
using System.Runtime.ExceptionServices;
#if !PORTABLE
using System.Security.Cryptography;
using System.Net.Sockets;
#endif

namespace NBitcoin
{
	public static class Extensions
	{
		public static Block GetBlock(this IBlockRepository repository, uint256 blockId)
		{
			try
			{
				return repository.GetBlockAsync(blockId).Result;
			}
			catch(AggregateException aex)
			{
				ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
				return null; //Can't happen
			}
		}


		public static T ToNetwork<T>(this T base58, Network network) where T : Base58Data
		{
			if(network == null)
				throw new ArgumentNullException("network");
			if(base58.Network == network)
				return base58;
			if(base58 == null)
				throw new ArgumentNullException("base58");
			var inner = base58.ToBytes();
			if(base58.Type != Base58Type.COLORED_ADDRESS)
			{
				byte[] version = network.GetVersionBytes(base58.Type);
				var newBase58 = Encoders.Base58Check.EncodeData(version.Concat(inner).ToArray());
				return Network.CreateFromBase58Data<T>(newBase58, network);
			}
			else
			{
				var colored = BitcoinColoredAddress.GetWrappedBase58(base58.ToWif(), base58.Network);
				var address = Network.CreateFromBase58Data<BitcoinAddress>(colored).ToNetwork(network);
				return (T)(object)address.ToColoredAddress();
			}
		}
		public static byte[] ReadBytes(this Stream stream, int count)
		{
			byte[] result = new byte[count];
			stream.Read(result, 0, count);
			return result;
		}
		public static IEnumerable<T> Resize<T>(this List<T> list, int count)
		{
			if(list.Count == count)
				return new T[0];

			List<T> removed = new List<T>();

			for(int i = list.Count - 1 ; i + 1 > count ; i--)
			{
				removed.Add(list[i]);
				list.RemoveAt(i);
			}

			while(list.Count < count)
			{
				list.Add(default(T));
			}
			return removed;
		}
		public static IEnumerable<List<T>> Partition<T>(this IEnumerable<T> source, int max)
		{
			return Partition<T>(source, () => max);
		}
		public static IEnumerable<List<T>> Partition<T>(this IEnumerable<T> source, Func<int> max)
		{
			var partitionSize = max();
			List<T> toReturn = new List<T>(partitionSize);
			foreach(var item in source)
			{
				toReturn.Add(item);
				if(toReturn.Count == partitionSize)
				{
					yield return toReturn;
					toReturn = new List<T>(partitionSize);
				}
			}
			if(toReturn.Any())
			{
				yield return toReturn;
			}
		}

#if !PORTABLE
		public static int ReadEx(this Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellation = default(CancellationToken))
		{
			int readen = 0;
			while(readen < count)
			{
				int thisRead = 0;
				if(stream is NetworkStream) //Big performance problem with begin read for other stream than NetworkStream
				{
					var ar = stream.BeginRead(buffer, offset + readen, count - readen, null, null);
					if(!ar.CompletedSynchronously)
					{
						WaitHandle.WaitAny(new WaitHandle[] { ar.AsyncWaitHandle, cancellation.WaitHandle }, -1);
					}
					cancellation.ThrowIfCancellationRequested();
					thisRead = stream.EndRead(ar);
					if(thisRead == 0 && (stream is Message.CustomNetworkStream) && ((Message.CustomNetworkStream)stream).Connected)
					{
						return -1;
					}
				}
				else
				{
					cancellation.ThrowIfCancellationRequested();
					thisRead = stream.Read(buffer, offset + readen, count - readen);
				}
				if(thisRead == -1)
					return -1;
				if(thisRead == 0 && (stream is FileStream || stream is MemoryStream))
				{
					if(stream.Length == stream.Position)
						return -1;
				}
				readen += thisRead;
			}
			return readen;
		}
#else

		public static int ReadEx(this Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellation = default(CancellationToken))
		{
			int readen = 0;
			while(readen < count)
			{
				int thisRead = 0;

				cancellation.ThrowIfCancellationRequested();
				thisRead = stream.Read(buffer, offset + readen, count - readen);

				if(thisRead == -1)
					return -1;
				if(thisRead == 0 && (stream is MemoryStream))
				{
					if(stream.Length == stream.Position)
						return -1;
				}
				readen += thisRead;
			}
			return readen;
		}
#endif
		public static void AddOrReplace<TKey, TValue>(this IDictionary<TKey, TValue> dico, TKey key, TValue value)
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

		public static TValue TryGet<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
		{
			TValue value;
			dictionary.TryGetValue(key, out value);
			return value;
		}

		public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
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

		internal static Array BigIntegerToBytes(NBitcoin.BouncyCastle.Math.BigInteger b, int numBytes)
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



#if !NOBIGINT
		//https://en.bitcoin.it/wiki/Script
		public static byte[] BigIntegerToBytes(BigInteger num)
#else
		internal static byte[] BigIntegerToBytes(BigInteger num)
#endif
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

#if !NOBIGINT
		public static BigInteger BytesToBigInteger(byte[] data)
#else
		internal static BigInteger BytesToBigInteger(byte[] data)
#endif
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

		public static bool error(string msg, params object[] args)
		{
			_TraceSource.TraceEvent(TraceEventType.Error, 0, msg, args);
			return false;
		}
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
			return (uint)DateTimeToUnixTimeLong(dt);
		}

		internal static ulong DateTimeToUnixTimeLong(DateTimeOffset dt)
		{
			dt = dt.ToUniversalTime();
			if(dt < unixRef)
				throw new ArgumentOutOfRangeException("The supplied datetime can't be expressed in unix timestamp");
			var result = (dt - unixRef).TotalSeconds;
			if(result > UInt32.MaxValue)
				throw new ArgumentOutOfRangeException("The supplied datetime can't be expressed in unix timestamp");
			return (ulong)result;
		}

		public static DateTimeOffset UnixTimeToDateTime(uint timestamp)
		{
			var span = TimeSpan.FromSeconds(timestamp);
			return unixRef + span;
		}
		public static DateTimeOffset UnixTimeToDateTime(ulong timestamp)
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


#if !PORTABLE
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
#endif
		internal static byte[] ToBytes(uint value, bool littleEndian)
		{
			if(littleEndian)
			{
				return new byte[]
				{
					(byte)value,
					(byte)(value >> 8),
					(byte)(value >> 16),
					(byte)(value >> 24),
				};
			}
			else
			{
				return new byte[]
				{
					(byte)(value >> 24),
					(byte)(value >> 16),
					(byte)(value >> 8),
					(byte)value,
				};
			}
		}

		internal static uint ToUInt32(byte[] value, bool littleEndian)
		{

			if(littleEndian)
			{
				return value[0]
					   + ((uint)value[1] << 8)
					   + ((uint)value[2] << 16)
					   + ((uint)value[3] << 24);
			}
			else
			{
				return value[3]
					   + ((uint)value[2] << 8)
					   + ((uint)value[1] << 16)
					   + ((uint)value[0] << 24);
			}
		}


#if !PORTABLE
		public static IPEndPoint ParseIpEndpoint(string endpoint, int defaultPort)
		{
			var splitted = endpoint.Split(':');
			var port = splitted.Length == 1 ? defaultPort : int.Parse(splitted[1]);
			IPAddress address = null;
			try
			{
				address = IPAddress.Parse(splitted[0]);
			}
			catch(FormatException)
			{
				address = Dns.GetHostEntry(splitted[0]).AddressList[0];
			}
			return new IPEndPoint(address, port);
		}
#endif
		public static int GetHashCode(byte[] array)
		{
			unchecked
			{
				if(array == null)
				{
					return 0;
				}
				int hash = 17;
				for(int i = 0 ; i < array.Length ; i++)
				{
					hash = hash * 31 + array[i];
				}
				return hash;
			}
		}
	}
}
