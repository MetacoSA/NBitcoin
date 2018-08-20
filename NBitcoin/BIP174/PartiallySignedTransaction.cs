using System;
using System.Collections.Generic;

namespace NBitcoin.BIP174
{
	public class PSBTInput : IBitcoinSerializable
	{
		//TransactionRef non_witness_utxo;
		private TxOut witness_utxo;
		private Script redeem_script;
		private Script witness_script;
		private Script final_script_sig;
		private Script final_script_witness;

		private Dictionary<PubKey, uint[]> hd_keypaths;
		private Dictionary<KeyId, Tuple<PubKey, byte[]>> partial_sigs;
		private Dictionary<byte[], byte[]> unknown;
		int sighash_type = 0;

		public TxOut WitnessUtxo
		{
			get
			{
				return witness_utxo;
			}
			set
			{
				witness_utxo = value;
			}
		}

		public Script RedeemScript
		{
			get
			{
				return redeem_script;
			}
			set
			{
				redeem_script = value;
			}
		}

		public Script WitnessScript
		{
			get
			{
				return witness_script;
			}
			set
			{
				witness_script = value;
			}
		}

		public Script FinalScriptSig
		{
			get
			{
				return final_script_sig;
			}
			set
			{
				final_script_sig = value;
			}
		}

		public Script FinalScriptWitness
		{
			get
			{
				return final_script_witness;
			}
			set
			{
				final_script_witness = value;
			}
		}

		public Dictionary<PubKey, uint[]> HDKeyPaths
		{
			get
			{
				return hd_keypaths;
			}
		}

		public Dictionary<KeyId, Tuple<PubKey, byte[]>> PartialSigs
		{
			get
			{
				return partial_sigs;
			}
		}

		public Dictionary<byte[], byte[]> Unknown
		{
			get
			{
				return unknown;
			}
		}

		public PSBTInput()
		{
			hd_keypaths = new Dictionary<PubKey, uint[]>();
			partial_sigs= new Dictionary<KeyId, Tuple<PubKey, byte[]>>();
			unknown = new Dictionary<byte[], byte[]>();
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
		}

		#endregion
	}

	public class PSBTOutput : IBitcoinSerializable
	{
		private Script redeem_script;
		private Script witness_script;
		private Dictionary<PubKey, uint[]> hd_keypaths;
		private Dictionary<byte[], byte[]> unknown;

		public Script RedeemScript
		{
			get
			{
				return redeem_script;
			}
			set
			{
				redeem_script = value;
			}
		}

		public Script WitnessScript
		{
			get
			{
				return witness_script;
			}
			set
			{
				witness_script = value;
			}
		}

		public Dictionary<PubKey, uint[]> HDKeyPaths
		{
			get
			{
				return hd_keypaths;
			}
		}

		public Dictionary<byte[], byte[]> Unknown
		{
			get
			{
				return unknown;
			}
		}

		public PSBTOutput() 
		{
			hd_keypaths = new Dictionary<PubKey, uint[]>();
			unknown = new Dictionary<byte[], byte[]>();
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
		}

		#endregion
	}

	public class PartiallySignedTransaction : IBitcoinSerializable
	{
		private Transaction tx;
		private List<PSBTInput> inputs;
		private List<PSBTOutput> outputs;

		private Dictionary<byte[], byte[]> unknown;

		public PartiallySignedTransaction() 
		{
			inputs = new List<PSBTInput>();
			outputs= new List<PSBTOutput>();
			unknown = new Dictionary<byte[], byte[]>();
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
		}

		#endregion
	}
}
