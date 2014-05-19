using NBitcoin.Crypto;
using Org.BouncyCastle.Math;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class StealthMetadata
	{
		public static StealthMetadata CreateMetadata(Key ephemKey, BitField bitField = null)
		{
			for(uint nonce = 0 ; nonce < uint.MaxValue ; nonce++)
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

		private static bool Fill(StealthMetadata output, Script metadata)
		{
			var ops = metadata.ToOps().ToArray();
			if(ops.Length != 2 || ops[0].Code != OpcodeType.OP_RETURN)
				return false;
			var data = ops[1].PushData;
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

	public class StealthSpendKey
	{
		private readonly StealthPayment _Payment;
		public StealthPayment Payment
		{
			get
			{
				return _Payment;
			}
		}
		private readonly KeyId _ID;
		public KeyId ID
		{
			get
			{
				return _ID;
			}
		}
		public StealthSpendKey(KeyId id, StealthPayment payment)
		{
			_ID = id;
			_Payment = payment;
		}

		public BitcoinAddress GetAddress(Network network)
		{
			return new BitcoinAddress(ID, network);
		}
	}

	public class StealthPayment
	{
		public StealthPayment(int sigCount, PubKey[] spendPubKeys, Key privateKey, PubKey publicKey, StealthMetadata metadata)
		{
			Metadata = metadata;
			if(sigCount == 1 && spendPubKeys.Length == 1)
			{
				var template = new PayToPubkeyHashTemplate();
				SpendableScript = template.GenerateScriptPubKey(spendPubKeys[0].Uncover(privateKey, publicKey).ID);
			}
			else
			{
				var template = new PayToMultiSigTemplate();
				SpendableScript = template.GenerateScriptPubKey(sigCount, spendPubKeys.Select(p => p.Uncover(privateKey, publicKey)).ToArray());
			}
			ParseSpendable();
		}





		public StealthSpendKey[] SpendKeys
		{
			get;
			private set;
		}
		public BitcoinAddress[] GetAddresses(Network network)
		{
			return SpendKeys.Select(k => k.GetAddress(network)).ToArray();
		}

		public StealthPayment(Script spendable, StealthMetadata metadata)
		{
			Metadata = metadata;
			SpendableScript = spendable;
			ParseSpendable();
		}

		private void ParseSpendable()
		{
			List<KeyId> pubkeys = new List<KeyId>();

			var payToHash = new PayToPubkeyHashTemplate();
			var keyId = payToHash.ExtractScriptPubKeyParameters(SpendableScript);
			if(keyId != null)
			{
				SpendKeys = new StealthSpendKey[] { new StealthSpendKey(keyId, this) };
			}
			else
			{
				var payToMultiSig = new PayToMultiSigTemplate();
				var para = payToMultiSig.ExtractScriptPubKeyParameters(SpendableScript);
				if(para == null)
					throw new ArgumentException("Invalid stealth spendable output script", "spendable");
				SpendKeys = para.PubKeys.Select(k => new StealthSpendKey(k.ID, this)).ToArray();
			}
		}

		public StealthMetadata Metadata
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
