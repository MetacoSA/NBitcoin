using System;
using System.Linq;
using System.Text;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;

namespace NBitcoin.Altcoins.Elements
{
	/// <summary>
	/// SLIP-0021 Implementation: Hierarchical derivation of symmetric keys
	/// https://github.com/satoshilabs/slips/blob/master/slip-0021.md
	/// </summary>
	public class Slip21Node
	{
		private static readonly int KEY_SIZE = 32;
		private byte[] _data;
		static readonly byte[]  HMAC_MASTER_NODE_KEY = Encoding.ASCII.GetBytes("Symmetric key seed");

		public Slip21Node(byte[] data)
		{
			_data = data.Length == 64
				? data
				: throw new ArgumentException("The data array has to be 64 bytes long.", nameof(data));
		}


		public Key Key => new Key(_data.SafeSubarray(KEY_SIZE, KEY_SIZE));

		public static Slip21Node FromSeed(string seed)
		{
			if (HexEncoder.IsWellFormed(seed))
			{
				return FromSeed(Encoders.Hex.DecodeData(seed));
			}
			return FromSeed(Encoding.ASCII.GetBytes(seed));
		}

		public static Slip21Node FromSeed(byte[] seed)
		{
			return new Slip21Node(Hashes.HMACSHA512(HMAC_MASTER_NODE_KEY, seed));
		}

		public Slip21Node DeriveChild(string label)
		{
			if (string.IsNullOrEmpty(label))
			{
				throw new ArgumentException("label must not be null or empty", nameof(label));
			}
			if (HexEncoder.IsWellFormed(label))
			{
				return DeriveChild(Encoders.Hex.DecodeData(label));
			}
			return DeriveChild(Encoding.ASCII.GetBytes(label));
		}

		public Slip21Node DeriveChild(byte[] label)
		{
			if (label is null)
			{
				throw new ArgumentException("label must not be null", nameof(label));
			}
			return new Slip21Node(
				Hashes.HMACSHA512(_data.SafeSubarray(0, KEY_SIZE), new byte[] {0x00}.Concat(label).ToArray()));
		}
	}
}
