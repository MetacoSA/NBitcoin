using System.Collections.Generic;
using System.Linq;

namespace NBitcoin.BitcoinCore
{
	[System.Obsolete]
	public class Coins : IBitcoinSerializable
	{
		// whether transaction is a coinbase
		public bool CoinBase { get; private set; }

		// unspent transaction outputs; spent outputs are .IsNull(); spent outputs at the end of the array are dropped
		public List<TxOut> Outputs { get; private set; } = new List<TxOut>();

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

		public Money Value { get; private set; }


		public static readonly TxOut NullTxOut = new TxOut(new Money(-1), Script.Empty);
		public Coins()
		{

		}
		public Coins(Transaction tx, int height)
		{
			CoinBase = tx.IsCoinBase;
			Outputs = tx.Outputs.ToList();
			nVersion = tx.Version;
			nHeight = (uint)height;
			ClearUnspendable();
			UpdateValue();
		}

		private void UpdateValue()
		{
			Value = Outputs
				.Where(o => !IsNull(o))
				.Sum(o=> o.Value);
		}

		private bool IsNull(TxOut o) => o.Value.Satoshi == -1;
		public bool IsEmpty => Outputs.Count == 0;

		private void Cleanup()
		{
			var count = Outputs.Count;
			// remove spent outputs at the end of vout
			for(int i = count - 1; i >= 0; i--)
			{
				if(IsNull(Outputs[i]))
					Outputs.RemoveAt(i);
				else
					break;
			}
		}

		public int UnspentCount => Outputs.Count(c => !IsNull(c));

#pragma warning disable CS0612 // Type or member is obsolete
		public bool Spend(int position, out TxInUndo undo)
		{
			undo = null;
			if(position >= Outputs.Count)
				return false;
			if(IsNull(Outputs[position]))
				return false;
			undo = new TxInUndo(Outputs[position].Clone());
#pragma warning restore CS0612 // Type or member is obsolete
			Outputs[position] = NullTxOut;
			Cleanup();
			if(IsEmpty)
			{
				undo.Height = nHeight;
				undo.CoinBase = CoinBase;
				undo.Version = nVersion;
			}
			return true;
		}

		public bool Spend(int position)
		{
#pragma warning disable CS0612 // Type or member is obsolete
			TxInUndo undo;
#pragma warning restore CS0612 // Type or member is obsolete
			return Spend(position, out undo);
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			if(stream.Serializing)
			{
				uint nMaskSize = 0, nMaskCode = 0;
				CalcMaskSize(ref nMaskSize, ref nMaskCode);
				bool fFirst = Outputs.Count > 0 && !IsNull(Outputs[0]);
				bool fSecond = Outputs.Count > 1 && !IsNull(Outputs[1]);
				uint nCode = unchecked((uint)(8 * (nMaskCode - (fFirst || fSecond ? 0 : 1)) + (CoinBase ? 1 : 0) + (fFirst ? 2 : 0) + (fSecond ? 4 : 0)));
				// version
				stream.ReadWriteAsVarInt(ref nVersion);
				// size of header code
				stream.ReadWriteAsVarInt(ref nCode);
				// spentness bitmask
				for(uint b = 0; b < nMaskSize; b++)
				{
					byte chAvail = 0;
					for(uint i = 0; i < 8 && 2 + b * 8 + i < Outputs.Count; i++)
						if(!IsNull(Outputs[2 + (int)b * 8 + (int)i]))
							chAvail |= (byte)(1 << (int)i);
					stream.ReadWrite(ref chAvail);
				}

				// txouts themself
				for(uint i = 0; i < Outputs.Count; i++)
				{
					if(!IsNull(Outputs[(int)i]))
					{
						var compressedTx = new TxOutCompressor(Outputs[(int)i]);
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
				CoinBase = (nCode & 1) != 0;
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
				Outputs = Enumerable.Range(0, vAvail.Count).Select(_ => NullTxOut).ToList();
				for(uint i = 0; i < vAvail.Count; i++)
				{
					if(vAvail[(int)i])
					{
						TxOutCompressor compressed = new TxOutCompressor();
						stream.ReadWrite(ref compressed);
						Outputs[(int)i] = compressed.TxOut;
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
				nHeight = nHeight,
				nVersion = nVersion,
				CoinBase = CoinBase,
				Value = Value,
				Outputs = Outputs.Select(txout => txout.Clone()).ToList(),
			};
		}

		// calculate number of bytes for the bitmask, and its number of non-zero bytes
		// each bit in the bitmask represents the availability of one output, but the
		// availabilities of the first two outputs are encoded separately
		private void CalcMaskSize(ref uint nBytes, ref uint nNonzeroBytes)
		{
			uint nLastUsedByte = 0;
			for(uint b = 0; 2 + b * 8 < Outputs.Count; b++)
			{
				bool fZero = true;
				for(uint i = 0; i < 8 && 2 + b * 8 + i < Outputs.Count; i++)
				{
					if(!IsNull(Outputs[2 + (int)b * 8 + (int)i]))
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
			return (position <= int.MaxValue && position < Outputs.Count && !IsNull(Outputs[(int)position]));
		}

		public TxOut TryGetOutput(uint position)
		{
			if(!IsAvailable(position))
				return null;
			return Outputs[(int)position];
		}

		// check whether the entire CCoins is spent
		// note that only !IsPruned() CCoins can be serialized
		public bool IsPruned => IsEmpty || Outputs.All(v => IsNull(v));


		#endregion

		public void ClearUnspendable()
		{
			for(int i = 0; i < Outputs.Count; i++)
			{
				var o = Outputs[i];
				if(o.ScriptPubKey.IsUnspendable)
				{
					Outputs[i] = NullTxOut;
				}
			}
			Cleanup();
		}

		public void MergeFrom(Coins otherCoin)
		{
			var diff = otherCoin.Outputs.Count - this.Outputs.Count;
			if(diff > 0)
				for(int i = 0; i < diff; i++)
				{
					Outputs.Add(NullTxOut);
				}
			for(int i = 0; i < otherCoin.Outputs.Count; i++)
			{
				Outputs[i] = otherCoin.Outputs[i];
			}
			UpdateValue();
		}
	}
}
