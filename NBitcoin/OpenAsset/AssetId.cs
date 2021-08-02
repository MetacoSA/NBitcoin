using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.OpenAsset
{
	/// <summary>
	/// A unique Id for an asset
	/// </summary>
	public class AssetId
	{
		internal byte[] _Bytes;

		public AssetId()
		{
			_Bytes = new byte[] { 0 };
		}

		public AssetId(IDestination assetScriptPubKey)
			: this(assetScriptPubKey.ScriptPubKey)
		{
			if (assetScriptPubKey == null)
				throw new ArgumentNullException(nameof(assetScriptPubKey));
		}

		public AssetId(BitcoinAssetId assetId)
		{
			if (assetId == null)
				throw new ArgumentNullException(nameof(assetId));
			_Bytes = assetId.AssetId._Bytes;
		}

		public AssetId(Script assetScriptPubKey)
			: this(assetScriptPubKey.Hash)
		{
			if (assetScriptPubKey == null)
				throw new ArgumentNullException(nameof(assetScriptPubKey));
		}

		public AssetId(ScriptId scriptId)
		{
			if (scriptId == null)
				throw new ArgumentNullException(nameof(scriptId));
			_Bytes = scriptId.ToBytes();
		}

		public AssetId(byte[] value)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			_Bytes = value;
		}
		public AssetId(uint160 value)
			: this(value.ToBytes())
		{
		}

		public AssetId(string value)
		{
			_Bytes = Encoders.Hex.DecodeData(value);
			_Str = value;
		}

		public BitcoinAssetId GetWif(Network network)
		{
			if (network == null)
				throw new ArgumentNullException(nameof(network));
			return new BitcoinAssetId(this, network);
		}

		public byte[] ToBytes()
		{
			return ToBytes(false);
		}
		public byte[] ToBytes(bool @unsafe)
		{
			if (@unsafe)
				return _Bytes;
			var array = new byte[_Bytes.Length];
			Array.Copy(_Bytes, array, _Bytes.Length);
			return array;
		}

		public override bool Equals(object obj)
		{
			AssetId item = obj as AssetId;
			if (item == null)
				return false;
			return Utils.ArrayEqual(_Bytes, item._Bytes);
		}
		public static bool operator ==(AssetId a, AssetId b)
		{
			if (System.Object.ReferenceEquals(a, b))
				return true;
			if (((object)a == null) || ((object)b == null))
				return false;
			return Utils.ArrayEqual(a._Bytes, b._Bytes);
		}

		public static bool operator !=(AssetId a, AssetId b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return Utils.GetHashCode(_Bytes);
		}

		string _Str;
		public override string ToString()
		{
			if (_Str == null)
				_Str = Encoders.Hex.EncodeData(_Bytes);
			return _Str;
		}

		public string ToString(Network network)
		{
			if (network == null)
				throw new ArgumentNullException(nameof(network));
			return new BitcoinAssetId(this, network).ToString();
		}
	}
}
