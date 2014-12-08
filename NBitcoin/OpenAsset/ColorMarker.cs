using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.OpenAsset
{
	public class ColorMarker : IBitcoinSerializable
	{
		const ushort Tag = 0x414f;
		public static ColorMarker TryParse(string script)
		{
			return TryParse(new Script(script));
		}
		public static ColorMarker TryParse(Script script)
		{
			try
			{
				ColorMarker result = new ColorMarker();
				if(!result.ReadScript(script))
					return null;
				return result;
			}
			catch(EndOfStreamException)
			{
				return null;
			}
		}

		private bool ReadScript(Script script)
		{
			var data = TxNullDataTemplate.Instance.ExtractScriptPubKeyParameters(script);
			if(data == null)
				return false;
			BitcoinStream stream = new BitcoinStream(data);
			ushort marker = 0;
			stream.ReadWrite(ref marker);
			if(marker != Tag)
				return false;
			stream.ReadWrite(ref _Version);
			if(_Version != 1)
				return false;

			ulong quantityCount = 0;
			stream.ReadWriteAsVarInt(ref quantityCount);
			Quantities = new ulong[quantityCount];
			try
			{
				for(ulong i = 0 ; i < quantityCount ; i++)
				{
					Quantities[i] = ReadLEB128(stream);
					if(Quantities[i] > MAX_QUANTITY)
						throw new FormatException();
				}
			}
			catch(FormatException)
			{
				return false;
			}
			stream.ReadWriteAsVarString(ref _Metadata);
			return true;
		}

		private static ulong ReadLEB128(BitcoinStream stream)
		{
			ulong value = 0;
			value = stream.ReadWrite((byte)0);
			if((value & 128uL) == 0uL)
			{
				return value;
			}
			value &= 127uL;
			ulong chunk = stream.ReadWrite((byte)0);
			value |= (chunk & 127uL) << 7;
			if((chunk & 128uL) == 0uL)
			{
				return value;
			}
			chunk = stream.ReadWrite((byte)0);
			value |= (chunk & 127uL) << 14;
			if((chunk & 128uL) == 0uL)
			{
				return value;
			}
			chunk = stream.ReadWrite((byte)0);
			value |= (chunk & 127uL) << 21;
			if((chunk & 128uL) == 0uL)
			{
				return value;
			}
			chunk = stream.ReadWrite((byte)0);
			value |= (chunk & 127uL) << 28;
			if((chunk & 128uL) == 0uL)
			{
				return value;
			}
			chunk = stream.ReadWrite((byte)0);
			value |= (chunk & 127uL) << 35;
			if((chunk & 128uL) == 0uL)
			{
				return value;
			}
			chunk = stream.ReadWrite((byte)0);
			value |= (chunk & 127uL) << 42;
			if((chunk & 128uL) == 0uL)
			{
				return value;
			}
			chunk = stream.ReadWrite((byte)0);
			value |= (chunk & 127uL) << 49;
			if((chunk & 128uL) == 0uL)
			{
				return value;
			}
			chunk = stream.ReadWrite((byte)0);
			value |= (chunk & 127uL) << 56;
			if((chunk & 128uL) == 0uL)
			{
				return value;
			}
			chunk = stream.ReadWrite((byte)0);
			value |= chunk << 63;
			if((chunk & 18446744073709551614uL) != 0uL)
			{
				throw new FormatException("Invalid LEB128 number");
			}
			return value;
		}
		private void WriteLEB128(ulong value, BitcoinStream stream)
		{
			byte[] bytes = new byte[10];
			int ioIndex = 0;
			int count = 0;
			do
			{
				bytes[ioIndex++] = (byte)((value & 127uL) | 128uL);
				count++;
			}
			while((value >>= 7) != 0uL);
			Array.Resize(ref bytes, count);
			bytes[bytes.Length - 1] &= 127;
			stream.ReadWrite(ref bytes);
		}

		public ColorMarker()
		{
			Quantities = new ulong[0];
		}
		public ColorMarker(Script script)
		{
			if(!ReadScript(script))
				throw new FormatException("Not a color marker");
		}

		public ColorMarker(ulong[] quantities)
		{
			if(quantities == null)
				throw new ArgumentNullException("quantities");
			Quantities = quantities;
		}
		ushort _Version = 1;
		public ushort Version
		{
			get
			{
				return _Version;
			}
			set
			{
				_Version = value;
			}
		}

		ulong[] _Quantities;
		public ulong[] Quantities
		{
			get
			{
				return _Quantities;
			}
			set
			{
				_Quantities = value;
			}
		}

		public void SetQuantity(uint index, ulong quantity)
		{
			if(Quantities == null)
				Quantities = new ulong[0];
			if(Quantities.Length <= index)
				Array.Resize(ref _Quantities, (int)index + 1);
			Quantities[index] = quantity;
		}

		public void SetQuantity(int index, ulong quantity)
		{
			SetQuantity((uint)index, quantity);
		}

		byte[] _Metadata = new byte[0];
		public byte[] Metadata
		{
			get
			{
				return _Metadata;
			}
			set
			{
				_Metadata = value;
			}
		}
		private const ulong MAX_QUANTITY = ((1UL << 63) - 1);

		public Script GetScript()
		{
			MemoryStream ms = new MemoryStream();
			BitcoinStream stream = new BitcoinStream(ms, true);
			stream.ReadWrite(Tag);
			stream.ReadWrite(ref _Version);
			var quantityCount = (uint)this.Quantities.Length;
			stream.ReadWriteAsVarInt(ref quantityCount);
			for(int i = 0 ; i < quantityCount ; i++)
			{
				if(Quantities[i] > MAX_QUANTITY)
					throw new ArgumentOutOfRangeException("Quantity should not exceed " + Quantities[i]);
				WriteLEB128(Quantities[i], stream);
			}
			stream.ReadWriteAsVarString(ref _Metadata);
			return TxNullDataTemplate.Instance.GenerateScriptPubKey(ms.ToArray());
		}

		public static ColorMarker Get(Transaction transaction)
		{
			uint i = 0;
			return Get(transaction, out i);
		}

		public static ColorMarker Get(Transaction transaction, out uint markerPosition)
		{
			uint resultIndex = 0;
			var result = transaction.Outputs.Select(o => TryParse(o.ScriptPubKey)).Where((o, i) =>
			{
				resultIndex = (uint)i;
				return o != null;
			}).FirstOrDefault();
			markerPosition = resultIndex;
			return result;
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			if(stream.Serializing)
			{
				var script = GetScript();
				stream.ReadWrite(ref script);
			}
			else
			{
				Script script = null;
				stream.ReadWrite(ref script);
				if(!ReadScript(script))
				{
					throw new FormatException("Invalid ColorMarker");
				}
			}
		}

		#endregion

		public static bool HasValidColorMarker(Transaction tx)
		{
			var marker = Get(tx);
			if(marker == null)
				return false;
			//If there are more items in the  asset quantity list  than the number of colorable outputs, the transaction is deemed invalid, and all outputs are uncolored.
			return marker.HasValidQuantitiesCount(tx);
		}

		public bool HasValidQuantitiesCount(Transaction tx)
		{
			return Quantities.Length <= tx.Outputs.Count - 1;
		}

		public Uri GetMetadataUrl()
		{
			if(Metadata == null || Metadata.Length == 0)
				return null;
			var result = Encoders.ASCII.EncodeData(Metadata);
			if(!result.StartsWith("u="))
				return null;
			Uri uri = null;
			Uri.TryCreate(result.Substring(2), UriKind.Absolute, out uri);
			return uri;
		}

		public void SetMetadataUrl(Uri uri)
		{
			if(uri == null)
			{
				Metadata = new byte[0];
				return;
			}
			Metadata = Encoders.ASCII.DecodeData("u=" + uri.AbsoluteUri);
			return;
		}
	}
}
