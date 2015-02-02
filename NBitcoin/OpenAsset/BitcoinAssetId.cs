using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.OpenAsset
{
	public class BitcoinAssetId : Base58Data
	{
		public BitcoinAssetId(string base58, Network expectedNetwork = null)
			: base(base58, expectedNetwork)
		{
		}
		public BitcoinAssetId(byte[] raw, Network network)
			: base(raw, network)
		{
		}

		public BitcoinAssetId(AssetId assetId, Network network)
			: this(assetId._Bytes, network)
		{
		}

		AssetId _AssetId;
		public AssetId AssetId
		{
			get
			{
				if(_AssetId == null)
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
	}
}
