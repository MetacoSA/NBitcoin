using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.OpenAsset
{
	public class Asset : IBitcoinSerializable
	{
		int _Index;
		public int Index
		{
			get
			{
				return _Index;
			}
			set
			{
				_Index = value;
			}
		}

		ulong _Quantity;
		public ulong Quantity
		{
			get
			{
				return _Quantity;
			}
			set
			{
				_Quantity = value;
			}
		}

		ScriptId _AssetId = new ScriptId(0);
		public ScriptId AssetId
		{
			get
			{
				return _AssetId;
			}
			set
			{
				_AssetId = value;
			}
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			byte[] assetId = _AssetId.ToBytes();
			stream.ReadWrite(ref assetId);
			if(!stream.Serializing)
				_AssetId = new ScriptId(assetId);
			stream.ReadWrite(ref _Index);
			stream.ReadWrite(ref _Quantity);
		}

		#endregion
	}
}
