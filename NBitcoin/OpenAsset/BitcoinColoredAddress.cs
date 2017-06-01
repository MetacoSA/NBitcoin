using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class BitcoinColoredAddress : Base58Data, IDestination
	{
		public BitcoinColoredAddress(string base58, Network expectedNetwork = null)
			: base(base58, expectedNetwork)
		{
		}

		public BitcoinColoredAddress(BitcoinAddress address)
			: base(Build(address), address.Network)
		{

		}

		private static byte[] Build(BitcoinAddress address)
		{
			var version = address.Network.GetVersionBytes(address.Type);
			var data = address.ToBytes();
			return version.Concat(data).ToArray();
		}

		protected override bool IsValid
		{
			get
			{
				return Address != null;
			}
		}

		BitcoinAddress _Address;
		public BitcoinAddress Address
		{
			get
			{
				if(_Address == null)
				{
					var base58 = Encoders.Base58Check.EncodeData(vchData);
					_Address = BitcoinAddress.Create(base58, Network);
				}
				return _Address;
			}
		}

		public override Base58Type Type
		{
			get
			{
				return Base58Type.COLORED_ADDRESS;
			}
		}

		#region IDestination Members

		public Script ScriptPubKey
		{
			get
			{
				return Address.ScriptPubKey;
			}
		}

		#endregion

		public static string GetWrappedBase58(string base58, Network network)
		{
			var coloredVersion = network.GetVersionBytes(Base58Type.COLORED_ADDRESS);
			var inner = Encoders.Base58Check.DecodeData(base58);
			inner = inner.Skip(coloredVersion.Length).ToArray();
			return Encoders.Base58Check.EncodeData(inner);
		}
	}
}
