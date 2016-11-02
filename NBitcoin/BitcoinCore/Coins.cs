using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.BitcoinCore
{
	public class Coins : IBitcoinSerializable
	{
		// whether transaction is a coinbase
		bool fCoinBase;
		public bool Coinbase
		{
			get
			{
				return fCoinBase;
			}
			set
			{
				fCoinBase = value;
			}
		}

		// unspent transaction outputs; spent outputs are .IsNull(); spent outputs at the end of the array are dropped
		List<TxOut> vout = new List<TxOut>();

		// at which height this transaction was included in the active block chain
		uint nHeight;
		public uint Height
		{
			get
			{
				return nHeight;
			}
			set
			{
				nHeight = value;
			}
		}

		// version of the CTransaction; accesses to this value should probably check for nHeight as well,
		// as new tx version will probably only be introduced at certain heights
		uint nVersion;
		public uint Version
		{
			get
			{
				return nVersion;
			}
			set
			{
				nVersion = value;
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

		private readonly TxOut NullTxOut = new TxOut(new Money(-1), Script.Empty);
		public Coins()
		{

		}
		public Coins(Transaction tx, int height)
		{
			fCoinBase = tx.IsCoinBase;
			vout = tx.Outputs.ToList();
			nVersion = tx.Version;
			nHeight = (uint)height;
			ClearUnspendable();
			UpdateValue();
		}

		private void UpdateValue()
		{
			_Value = vout.Where(o => !IsNull(o))
							.Select(o => o.Value).Sum();
		}

		private bool IsNull(TxOut o)
		{
			return o.Value.Satoshi == -1;
		}

		public bool IsEmpty
		{
			get
			{
				return vout.Count == 0;
			}
		}

		private void Cleanup()
		{
			var count = vout.Count;
			// remove spent outputs at the end of vout
			for(int i = count - 1; i >= 0; i--)
			{
				if(IsNull(vout[i]))
					vout.RemoveAt(i);
				else
					break;
			}
		}

		public int UnspentCount
		{
			get
			{
				return vout.Where(c => !IsNull(c)).Count();
			}
		}

		public bool Spend(int position, out TxInUndo undo)
		{
			undo = null;
			if(position >= vout.Count)
				return false;
			if(IsNull(vout[position]))
				return false;
			undo = new TxInUndo(vout[position].Clone());
			vout[position] = NullTxOut;
			Cleanup();
			if(vout.Count == 0)
			{
				undo.Height = nHeight;
				undo.CoinBase = fCoinBase;
				undo.Version = nVersion;
			}
			return true;
		}

		public bool Spend(int position)
		{
			TxInUndo undo;
			return Spend(position, out undo);
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			if(stream.Serializing)
			{
				uint nMaskSize = 0, nMaskCode = 0;
				CalcMaskSize(ref nMaskSize, ref nMaskCode);
				bool fFirst = vout.Count > 0 && !IsNull(vout[0]);
				bool fSecond = vout.Count > 1 && !IsNull(vout[1]);
				uint nCode = unchecked((uint)(8 * (nMaskCode - (fFirst || fSecond ? 0 : 1)) + (fCoinBase ? 1 : 0) + (fFirst ? 2 : 0) + (fSecond ? 4 : 0)));
				// version
				stream.ReadWriteAsVarInt(ref nVersion);
				// size of header code
				stream.ReadWriteAsVarInt(ref nCode);
				// spentness bitmask
				for(uint b = 0; b < nMaskSize; b++)
				{
					byte chAvail = 0;
					for(uint i = 0; i < 8 && 2 + b * 8 + i < vout.Count; i++)
						if(!IsNull(vout[2 + (int)b * 8 + (int)i]))
							chAvail |= (byte)(1 << (int)i);
					stream.ReadWrite(ref chAvail);
				}

				// txouts themself
				for(uint i = 0; i < vout.Count; i++)
				{
					if(!IsNull(vout[(int)i]))
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
					for(uint p = 0; p < 8; p++)
					{
						bool f = (chAvail & (1 << (int)p)) != 0;
						vAvail.Add(f);
					}
					if(chAvail != 0)
						nMaskCode--;
				}
				// txouts themself
				vout = Enumerable.Range(0, vAvail.Count).Select(_ => NullTxOut).ToList();
				for(uint i = 0; i < vAvail.Count; i++)
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

		public Coins Clone()
		{
			return new Coins()
			{
				fCoinBase = fCoinBase,
				nHeight = nHeight,
				nVersion = nVersion,
				vout = vout.Select(txout => txout.Clone()).ToList(),
				_Value = _Value
			};
		}

		// calculate number of bytes for the bitmask, and its number of non-zero bytes
		// each bit in the bitmask represents the availability of one output, but the
		// availabilities of the first two outputs are encoded separately
		private void CalcMaskSize(ref uint nBytes, ref uint nNonzeroBytes)
		{
			uint nLastUsedByte = 0;
			for(uint b = 0; 2 + b * 8 < vout.Count; b++)
			{
				bool fZero = true;
				for(uint i = 0; i < 8 && 2 + b * 8 + i < vout.Count; i++)
				{
					if(!IsNull(vout[2 + (int)b * 8 + (int)i]))
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

		// check whether a particular output is still available
		public bool IsAvailable(uint position)
		{
			return (position <= int.MaxValue && position < vout.Count && !IsNull(vout[(int)position]));
		}

		public TxOut TryGetOutput(uint position)
		{
			if(!IsAvailable(position))
				return null;
			return vout[(int)position];
		}

		// check whether the entire CCoins is spent
		// note that only !IsPruned() CCoins can be serialized
		public bool IsPruned
		{
			get
			{
				return vout.Count == 0 || vout.All(v => IsNull(v));
			}
		}

		#endregion

		public void ClearUnspendable()
		{
			for(int i = 0; i < vout.Count; i++)
			{
				var o = vout[i];
				if(o.ScriptPubKey.IsUnspendable)
				{
					vout[i] = NullTxOut;
				}
			}
			Cleanup();
		}

		public void MergeFrom(Coins otherCoin)
		{
			var diff = otherCoin.vout.Count - this.vout.Count;
			if(diff > 0)
				for(int i = 0; i < diff; i++)
				{
					vout.Add(NullTxOut);
				}
			for(int i = 0; i < otherCoin.vout.Count; i++)
			{
				vout[i] = otherCoin.vout[i];
			}
			UpdateValue();
		}
	}
}
