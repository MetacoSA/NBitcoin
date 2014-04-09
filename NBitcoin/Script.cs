using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	/** Script verification flags */
	[Flags]
	public enum ScriptVerify
	{
		None = 0,
		P2SH = 1, // evaluate P2SH (BIP16) subscripts
		StrictEnc = 2, // enforce strict conformance to DER and SEC2 for signatures and pubkeys
		EvenS = 4, // enforce even S values in signatures (depends on STRICTENC)
		NoCache = 8, // do not store results in signature cache (but do query it)
	};

	/** Signature hash types/flags */
	public enum SigHash : byte
	{
		All = 1,
		None = 2,
		Single = 3,
		AnyoneCanPay = 0x80,
	};


	public class Script : IBitcoinSerializable
	{
		byte[] _Script = new byte[0];
		public Script()
		{

		}
		public Script(string hex)
		{
			_Script = Encoders.Hex.DecodeData(hex);
		}
		public Script(byte[] data)
		{
			_Script = data;
		}
		public static bool VerifyScript(Script scriptSig, Script scriptPubKey, Transaction txTo, int nIn, ScriptVerify flags, SigHash nHashType)
		{
			throw new NotImplementedException();
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			VarString str = new VarString(_Script);
			stream.ReadWrite(ref str);
			if(!stream.Serializing)
				_Script = str.GetString();
		}

		#endregion

		public string ToHex()
		{
			return Encoders.Hex.EncodeData(_Script);
		}
	}
}
