using System;
using System.Collections.Generic;
using NBitcoin.Protocol;

namespace NBitcoin.Altcoins.Elements
{
	public static class BitcoinStreamExtensions
	{
		internal static  void ReadWriteListBytes(this BitcoinStream bitcoinStream, ref List<byte[]> data)
		{
			var dataArray = data?.ToArray();
			if (bitcoinStream.Serializing && dataArray == null)
			{
				dataArray = new byte[0][];
			}
			bitcoinStream.ReadWriteArray(ref dataArray);
			if (!bitcoinStream.Serializing)
			{
				if (data == null)
					data = new List<byte[]>();
				else
					data.Clear();
				data.AddRange(dataArray);
			}
		}

		private static void ReadWriteArray(this BitcoinStream bitcoinStream, ref byte[][] data)
		{
			if (data == null && bitcoinStream.Serializing)
				throw new ArgumentNullException("Impossible to serialize a null array");
			if (bitcoinStream.Serializing)
			{
				var len = data == null ? 0 : (ulong)data.Length;
				if (len > (uint)bitcoinStream.MaxArraySize)
					throw new ArgumentOutOfRangeException("Array size too big");
				VarInt.StaticWrite(bitcoinStream, len);
				if (len == 0)
					return;
			}
			else
			{
				var len = VarInt.StaticRead(bitcoinStream);
				if (len > (uint)bitcoinStream.MaxArraySize)
					throw new ArgumentOutOfRangeException("Array size too big");
				data = new byte[len][];
			}
			for (int i = 0; i < data.Length; i++)
			{
				bitcoinStream.ReadWriteArray(ref data[i]);
			}
		}

		private static void ReadWriteArray(this BitcoinStream bitcoinStream,  ref byte[] data)
		{
			if (data == null && bitcoinStream.Serializing)
				throw new ArgumentNullException("Impossible to serialize a null array");
			if (bitcoinStream.Serializing)
			{
				var len = data == null ? 0 : (ulong)data.Length;
				if (len > (uint)bitcoinStream.MaxArraySize)
					throw new ArgumentOutOfRangeException("Array size too big");
				VarInt.StaticWrite(bitcoinStream, len);
				if (len == 0)
					return;
			}
			else
			{
				var len = VarInt.StaticRead(bitcoinStream);
				if (len > (uint)bitcoinStream.MaxArraySize)
					throw new ArgumentOutOfRangeException("Array size too big");
				data = new byte[len];
			}
			bitcoinStream.ReadWriteBytes(ref data);
		}
	}
}
