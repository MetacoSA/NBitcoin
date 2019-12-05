using System;
using System.Linq;
using System.Text;
using NBitcoin.Crypto;

namespace NBitcoin.Altcoins.Elements
{
	/// <summary>
	/// SLIP-0021 Implementation: Hierarchical derivation of symmetric keys
	/// https://github.com/satoshilabs/slips/blob/master/slip-0021.md
	/// </summary>
	public class Slip21Node
	{
		private byte[] _data;
		static readonly byte[]  HMAC_MASTER_NODE_KEY = Encoding.ASCII.GetBytes("Symmetric key seed");

		public Slip21Node(byte[] data = null)
		{
			_data = data ?? new byte[64];
		}

		public Key Key
		{
			get
			{
				var vchChainCode = new byte[32];
				Buffer.BlockCopy((Array) _data, 32, vchChainCode, 0, 32);
				return new Key(vchChainCode);
			}
		}

		public static Slip21Node NewMaster(string seed)
		{
			return NewMaster(Encoding.ASCII.GetBytes(seed));
		}

		public static Slip21Node NewMaster(byte[] seed)
		{
			return new Slip21Node(Hashes.HMACSHA512(HMAC_MASTER_NODE_KEY, seed));
		}

		public Slip21Node DeriveChild(string data)
		{
			return DeriveChild(Encoding.ASCII.GetBytes(data));
		}
		public Slip21Node DeriveChild(byte[] data)
		{
			return new Slip21Node(Hashes.HMACSHA512( _data.Take(32).ToArray(), new byte[]{0x00}.Concat(data).ToArray()));
		}
		public Slip21Node Clone()
		{
			return new Slip21Node(_data.ToArray());
		}

		public static Slip21Node GetSlip77MasterNode(byte[] seed)
		{
			return NewMaster(seed).DeriveChild("SLIP-0077");
		}
	}
}
