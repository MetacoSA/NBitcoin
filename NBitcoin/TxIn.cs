using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class TxIn : IBitcoinSerializable
	{
		public TxIn()
		{

		}
		public TxIn(Script scriptSig)
		{
			this.scriptSig = scriptSig;
		}
		public TxIn(OutPoint prevout, Script scriptSig)
		{
			this.prevout = prevout;
			this.scriptSig = scriptSig;
		}
		public TxIn(OutPoint prevout)
		{
			this.prevout = prevout;
		}
		OutPoint prevout = new OutPoint();
		Script scriptSig = Script.Empty;
		uint nSequence = uint.MaxValue;

		public Sequence Sequence
		{
			get
			{
				return nSequence;
			}
			set
			{
				nSequence = value.Value;
			}
		}
		public OutPoint PrevOut
		{
			get
			{
				return prevout;
			}
			set
			{
				prevout = value;
			}
		}


		public Script ScriptSig
		{
			get
			{
				return scriptSig;
			}
			set
			{
				scriptSig = value;
			}
		}

		WitScript witScript = WitScript.Empty;

		/// <summary>
		/// The witness script (Witness script is not serialized and deserialized at the TxIn level, but at the Transaction level)
		/// </summary>
		public WitScript WitScript
		{
			get
			{
				return witScript;
			}
			set
			{
				witScript = value;
			}
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref prevout);
			stream.ReadWrite(ref scriptSig);
			stream.ReadWrite(ref nSequence);
		}

		#endregion

		public bool IsFrom(PubKey pubKey)
		{
			var result = PayToPubkeyHashTemplate.Instance.ExtractScriptSigParameters(ScriptSig);
			return result != null && result.PublicKey == pubKey;
		}

		public bool IsFinal
		{
			get
			{
				return (nSequence == uint.MaxValue);
			}
		}

		public TxIn Clone()
		{
			var txin = BitcoinSerializableExtensions.Clone(this);
			txin.WitScript = (witScript ?? WitScript.Empty).Clone();
			return txin;
		}

		public static TxIn CreateCoinbase(int height)
		{
			var txin = new TxIn();
			txin.ScriptSig = new Script(Op.GetPushOp(height)) + OpcodeType.OP_0;
			return txin;
		}
	}
}
