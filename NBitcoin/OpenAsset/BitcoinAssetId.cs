using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.OpenAsset
{
	/// <summary>
	/// Base58 representation of an asset id
	/// </summary>
	public class BitcoinAssetId : Base58Data
	{
		public BitcoinAssetId(string base58, Network expectedNetwork)
		{
			Init<BitcoinAssetId>(base58, expectedNetwork);
		}
		public BitcoinAssetId(byte[] raw, Network network)
			: base(raw, network)
		{
		}

		public BitcoinAssetId(AssetId assetId, Network network)
			: this(assetId._Bytes, network)
		{
			if (assetId == null)
				throw new ArgumentNullException(nameof(assetId));
			if (network == null)
				throw new ArgumentNullException(nameof(network));
		}

		AssetId _AssetId;
		public AssetId AssetId
		{
			get
			{
				if (_AssetId == null)
					_AssetId = new AssetId(vchData);
				return _AssetId;
			}
		}

		protected override bool IsValid
		{
			get
			{
				return vchData.Length == 20;
			}
		}

		public override Base58Type Type
		{
			get
			{
				return Base58Type.ASSET_ID;
			}
		}

		public static implicit operator AssetId(BitcoinAssetId id)
		{
			if (id == null)
				return null;
			return id.AssetId;
		}
	}
}
