using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.OpenAsset
{
	public class Asset : IBitcoinSerializable
	{
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

		ScriptId _Id = new ScriptId(0);

		public Asset(ScriptId id, ulong quantity)
		{
			if(id == null)
				throw new ArgumentNullException("id");
			Quantity = quantity;
			Id = id;
		}
		public Asset()
		{

		}
		public ScriptId Id
		{
			get
			{
				return _Id;
			}
			set
			{
				_Id = value;
			}
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			byte[] assetId = _Id.ToBytes();
			stream.ReadWrite(ref assetId);
			if(!stream.Serializing)
				_Id = new ScriptId(assetId);
			stream.ReadWrite(ref _Quantity);
		}

		#endregion

		public override string ToString()
		{
			return Quantity + "-" + Id;
		}
	}
}
