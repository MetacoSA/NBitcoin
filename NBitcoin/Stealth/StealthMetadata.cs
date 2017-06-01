using NBitcoin.Crypto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Stealth
{
	public class StealthMetadata
	{
		public static StealthMetadata CreateMetadata(Key ephemKey, BitField bitField = null)
		{
			for(uint nonce = 0; nonce < uint.MaxValue; nonce++)
			{
				var metadata = new StealthMetadata(ephemKey, nonce);
				if(bitField == null || bitField.Match(metadata.BitField))
					return metadata;
			}
			throw new ArgumentException("No nonce can satisfy the given bitfield, use another ephemKey");
		}
		public static StealthMetadata TryParse(Script metadata)
		{
			StealthMetadata result = new StealthMetadata();
			try
			{
				if(!Fill(result, metadata))
					return null;
			}
			catch(Exception)
			{
				return null;
			}
			return result;
		}
		private StealthMetadata()
		{
		}
		public StealthMetadata(Script metadata)
		{
			if(!Fill(this, metadata))
				throw new ArgumentException("Invalid metadata script");
		}

		public StealthMetadata(Key ephemKey, uint nonce)
		{
			var data = new MemoryStream();
			data.WriteByte(6);
			var b = Utils.ToBytes(nonce, true);
			data.Write(b, 0, b.Length);
			data.Write(ephemKey.PubKey.Compress().ToBytes(), 0, 33);
			Fill(this, new Script(OpcodeType.OP_RETURN, Op.GetPushOp(data.ToArray())));
		}

		static TxNullDataTemplate _Template = new TxNullDataTemplate(1024 * 4);
		private static bool Fill(StealthMetadata output, Script metadata)
		{
			var datas = _Template.ExtractScriptPubKeyParameters(metadata);
			if(datas == null)
				return false;
			foreach(var data in datas)
			{
				if(Fill(output, metadata, data))
					return true;
			}
			return false;
		}

		private static bool Fill(StealthMetadata output, Script metadata, byte[] data)
		{
			if(data == null || data.Length != 1 + 4 + 33)
				return false;
			MemoryStream ms = new MemoryStream(data);
			output.Version = ms.ReadByte();
			if(output.Version != 6)
				return false;
			output.Nonce = ms.ReadBytes(4);
			output.EphemKey = new PubKey(ms.ReadBytes(33));
			output.Script = metadata;
			output.Hash = Hashes.Hash256(data);
			var msprefix = new MemoryStream(output.Hash.ToBytes(false));
			output.BitField = Utils.ToUInt32(msprefix.ReadBytes(4), true);
			return true;
		}

		public uint BitField
		{
			get;
			private set;
		}

		public int Version
		{
			get;
			private set;
		}
		public byte[] Nonce
		{
			get;
			private set;
		}
		public PubKey EphemKey
		{
			get;
			private set;
		}
		public uint256 Hash
		{
			get;
			private set;
		}
		public Script Script
		{
			get;
			private set;
		}
	}
}
