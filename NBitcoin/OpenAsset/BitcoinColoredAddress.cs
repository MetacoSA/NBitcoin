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
		public BitcoinColoredAddress(string base58, Network expectedNetwork)
		{
			Init<BitcoinColoredAddress>(base58, expectedNetwork);
		}

		public BitcoinColoredAddress(BitcoinAddress address)
			: base(Build(address), address.Network)
		{

		}

		private static byte[] Build(BitcoinAddress address)
		{
			if (address is IBase58Data)
			{
				var b58 = (IBase58Data)address;
				var version = address.Network.GetVersionBytes(b58.Type, true);
				var data = address.Network.NetworkStringParser.GetBase58CheckEncoder().DecodeData(b58.ToString()).Skip(version.Length).ToArray();
				return version.Concat(data).ToArray();
			}
			else
			{
				throw new NotSupportedException("Building a colored address out of a non base58 string is not supported");
			}
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
				if (_Address == null)
				{
					var base58 = Network.NetworkStringParser.GetBase58CheckEncoder().EncodeData(vchData);
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
			var coloredVersion = network.GetVersionBytes(Base58Type.COLORED_ADDRESS, true);
			var inner = network.NetworkStringParser.GetBase58CheckEncoder().DecodeData(base58);
			inner = inner.Skip(coloredVersion.Length).ToArray();
			return network.NetworkStringParser.GetBase58CheckEncoder().EncodeData(inner);
		}
	}
}
