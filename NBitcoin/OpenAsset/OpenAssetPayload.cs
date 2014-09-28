using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.OpenAsset
{
	public class OpenAssetPayload
	{
		const ushort Tag = 0x414f;
		public static OpenAssetPayload TryParse(Script script)
		{
			try
			{
				OpenAssetPayload result = new OpenAssetPayload();
				var data = _NullTemplate.ExtractScriptPubKeyParameters(script);
				if(data == null)
					return null;
				BitcoinStream stream = new BitcoinStream(data);
				ushort marker = 0;
				stream.ReadWrite(ref marker);
				if(marker != Tag)
					return null;
				stream.ReadWrite(ref result._Version);
				if(result._Version != 1)
					return null;

				ulong quantityCount = 0;
				stream.ReadWriteAsVarInt(ref quantityCount);
				result.Quantities = new ulong[quantityCount];
				try
				{
					for(ulong i = 0 ; i < quantityCount ; i++)
					{
						result.Quantities[i] = ReadLEB128(stream);
					}
				}
				catch(FormatException)
				{
					return null;
				}
				stream.ReadWriteAsVarString(ref result._Metadata);
				return result;
			}
			catch(EndOfStreamException)
			{
				return null;
			}
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

		public OpenAssetPayload()
		{

		}

		ushort _Version;
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

		public ulong[] Quantities
		{
			get;
			set;
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
		static TxNullDataTemplate _NullTemplate = new TxNullDataTemplate();
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
				WriteLEB128(Quantities[i], stream);
			}
			stream.ReadWriteAsVarString(ref _Metadata);
			return _NullTemplate.GenerateScriptPubKey(ms.ToArray());
		}
	}
}
