using NBitcoin.Crypto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class StealthMetadataOutput
	{
		public static StealthMetadataOutput TryParse(Script metadata)
		{
			StealthMetadataOutput result = new StealthMetadataOutput();
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
		private StealthMetadataOutput()
		{
		}
		public StealthMetadataOutput(Script metadata)
		{
			if(!Fill(this, metadata))
				throw new ArgumentException("Invalid metadata script");
		}

		private static bool Fill(StealthMetadataOutput output, Script metadata)
		{
			var ops = metadata.ToOps().ToArray();
			if(ops.Length != 2 || ops[0].Code != OpcodeType.OP_RETURN)
				return false;
			var data = ops[1].PushData;
			if(data == null || data.Length != 1 + 4 + 33)
				return false;
			MemoryStream ms = new MemoryStream(data);
			output.Hash = Hashes.Hash256(data);
			var msprefix = new MemoryStream(output.Hash.ToBytes(false));

			output.BitField = Utils.ToUInt32(msprefix.ReadBytes(4), true);

			output.Version = ms.ReadByte();
			if(output.Version != 6)
				return false;
			output.Nonce = ms.ReadBytes(4);
			output.EphemereKey = new PubKey(ms.ReadBytes(33));
			output.Script = metadata;
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
		public PubKey EphemereKey
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
	public class StealthPayment
	{

		public StealthPayment(Script spendable, StealthMetadataOutput metadata)
		{
			Metadata = metadata;
			SpendableScript = spendable;
		}

		public StealthMetadataOutput Metadata
		{
			get;
			private set;
		}
		public Script SpendableScript
		{
			get;
			private set;
		}
	}
}
