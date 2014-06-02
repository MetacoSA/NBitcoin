using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class Coins : IBitcoinSerializable
	{
		// whether transaction is a coinbase
		bool fCoinBase;

		// unspent transaction outputs; spent outputs are .IsNull(); spent outputs at the end of the array are dropped
		List<TxOut> vout = new List<TxOut>();

		// at which height this transaction was included in the active block chain
		uint nHeight;

		// version of the CTransaction; accesses to this value should probably check for nHeight as well,
		// as new tx version will probably only be introduced at certain heights
		uint nVersion;

		public List<TxOut> Outputs
		{
			get
			{
				return vout;
			}
		}

		Money _Value;
		public Money Value
		{
			get
			{
				return _Value;
			}
		}

		public Coins()
		{

		}
		public Coins(Transaction tx, int height)
			: this(tx, null, height)
		{
		}
		public Coins(Transaction tx, Func<TxOut, bool> belongsToCoins, int height)
		{
			if(belongsToCoins == null)
				belongsToCoins = o => !o.ScriptPubKey.IsUnspendable;
			fCoinBase = tx.IsCoinBase;
			vout = tx.Outputs.ToList();
			nVersion = tx.Version;
			nHeight = (uint)height;
			ClearUnused(belongsToCoins);
			UpdateValue();
		}

		private void UpdateValue()
		{
			_Value = Outputs.Where(o => !o.IsNull).Select(o => o.Value).Sum();
		}

		public bool IsEmpty
		{
			get
			{
				return vout.Count != 0;
			}
		}

		private void ClearUnused(Func<TxOut, bool> belongsToCoins)
		{
			for(int i = 0 ; i < vout.Count ; i++)
			{
				var o = vout[i];
				if(o.ScriptPubKey.IsUnspendable || !belongsToCoins(o))
				{
					vout[i] = new TxOut();
				}
			}
			Cleanup();
		}

		private void Cleanup()
		{
			// remove spent outputs at the end of vout
			foreach(var o in Enumerable.Reverse(vout).Reverse().ToList())
			{
				if(o.ScriptPubKey == null)
					vout.Remove(o);
				else
					break;
			}
		}


		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			if(stream.Serializing)
			{
				uint nMaskSize = 0, nMaskCode = 0;
				CalcMaskSize(ref nMaskSize, ref nMaskCode);
				bool fFirst = vout.Count > 0 && !vout[0].IsNull;
				bool fSecond = vout.Count > 1 && !vout[1].IsNull;
				uint nCode = unchecked((uint)(8 * (nMaskCode - (fFirst || fSecond ? 0 : 1)) + (fCoinBase ? 1 : 0) + (fFirst ? 2 : 0) + (fSecond ? 4 : 0)));
				// version
				stream.ReadWriteAsVarInt(ref nVersion);
				// size of header code
				stream.ReadWriteAsVarInt(ref nCode);
				// spentness bitmask
				for(uint b = 0 ; b < nMaskSize ; b++)
				{
					byte chAvail = 0;
					for(uint i = 0 ; i < 8 && 2 + b * 8 + i < vout.Count ; i++)
						if(!vout[2 + (int)b * 8 + (int)i].IsNull)
							chAvail |= (byte)(1 << (int)i);
					stream.ReadWrite(ref chAvail);
				}

				// txouts themself
				for(uint i = 0 ; i < vout.Count ; i++)
				{
					if(!vout[(int)i].IsNull)
					{
						var compressedTx = new TxOutCompressor(vout[(int)i]);
						stream.ReadWrite(ref compressedTx);
					}
				}
				// coinbase height
				stream.ReadWriteAsVarInt(ref nHeight);
			}
			else
			{
				uint nCode = 0;
				// version
				stream.ReadWriteAsVarInt(ref nVersion);
				//// header code
				stream.ReadWriteAsVarInt(ref nCode);
				fCoinBase = (nCode & 1) != 0;
				List<bool> vAvail = new List<bool>() { false, false };
				vAvail[0] = (nCode & 2) != 0;
				vAvail[1] = (nCode & 4) != 0;
				uint nMaskCode = unchecked((uint)((nCode / 8) + ((nCode & 6) != 0 ? 0 : 1)));
				//// spentness bitmask
				while(nMaskCode > 0)
				{
					byte chAvail = 0;
					stream.ReadWrite(ref chAvail);
					for(uint p = 0 ; p < 8 ; p++)
					{
						bool f = (chAvail & (1 << (int)p)) != 0;
						vAvail.Add(f);
					}
					if(chAvail != 0)
						nMaskCode--;
				}
				// txouts themself
				vout = Enumerable.Range(0, vAvail.Count).Select(_ => new TxOut()).ToList();
				for(uint i = 0 ; i < vAvail.Count ; i++)
				{
					if(vAvail[(int)i])
					{
						TxOutCompressor compressed = new TxOutCompressor();
						stream.ReadWrite(ref compressed);
						vout[(int)i] = compressed.TxOut;
					}
				}
				//// coinbase height
				stream.ReadWriteAsVarInt(ref nHeight);
				Cleanup();
				UpdateValue();
			}
		}

		// calculate number of bytes for the bitmask, and its number of non-zero bytes
		// each bit in the bitmask represents the availability of one output, but the
		// availabilities of the first two outputs are encoded separately
		private void CalcMaskSize(ref uint nBytes, ref uint nNonzeroBytes)
		{
			uint nLastUsedByte = 0;
			for(uint b = 0 ; 2 + b * 8 < vout.Count ; b++)
			{
				bool fZero = true;
				for(uint i = 0 ; i < 8 && 2 + b * 8 + i < vout.Count ; i++)
				{
					if(!vout[2 + (int)b * 8 + (int)i].IsNull)
					{
						fZero = false;
						continue;
					}
				}
				if(!fZero)
				{
					nLastUsedByte = b + 1;
					nNonzeroBytes++;
				}
			}
			nBytes += nLastUsedByte;
		}

		#endregion
	}
}
