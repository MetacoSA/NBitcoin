using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.OpenAsset
{
	public class OpenAssetPayload : IBitcoinSerializable
	{
		const ushort Tag = 0x414f;
		public static OpenAssetPayload TryParse(Script script)
		{
			try
			{
				OpenAssetPayload result = new OpenAssetPayload();
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
			var data = _NullTemplate.ExtractScriptPubKeyParameters(script);
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

		public static OpenAssetPayload Get(Transaction transaction)
		{
			int i = 0;
			return Get(transaction, out i);
		}

		public static OpenAssetPayload Get(Transaction transaction, out int markerPosition)
		{
			int resultIndex = 0;
			var result = transaction.Outputs.Select(o => TryParse(o.ScriptPubKey)).Where((o, i) =>
			{
				resultIndex = i;
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
				var script = GetScript().ToRawScript(true);
				stream.ReadWrite(ref script);
			}
			else
			{
				byte[] script = null;
				stream.ReadWrite(ref script);
				if(!ReadScript(new Script(script)))
				{
					throw new FormatException("Invalid OpenAssetPayload");
				}
			}
		}

		#endregion
	}
}
