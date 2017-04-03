using NBitcoin.Crypto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.BitcoinCore
{
	/** Undo information for a CTxIn
 *
 *  Contains the prevout's CTxOut being spent, and if this was the
 *  last output of the affected transaction, its metadata as well
 *  (coinbase or not, height, transaction version)
 */
	public class TxInUndo : IBitcoinSerializable
	{
		public TxInUndo()
		{

		}
		public TxInUndo(NBitcoin.TxOut txOut)
		{
			this.TxOut = txOut;
		}

		TxOut txout;         // the txout data before being spent

		public TxOut TxOut
		{
			get
			{
				return txout;
			}
			set
			{
				txout = value;
			}
		}
		bool fCoinBase;       // if the outpoint was the last unspent: whether it belonged to a coinbase

		public bool CoinBase
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
		uint nHeight; // if the outpoint was the last unspent: its height

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
		uint nVersion;        // if the outpoint was the last unspent: its version

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


		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			if(stream.Serializing)
			{
				uint o = (uint)(nHeight * 2 + (fCoinBase ? 1 : 0));
				stream.ReadWriteAsCompactVarInt(ref o);
				if(nHeight > 0)
					stream.ReadWriteAsCompactVarInt(ref nVersion);
				TxOutCompressor compressor = new TxOutCompressor(txout);
				stream.ReadWrite(ref compressor);
			}
			else
			{
				uint nCode = 0;
				stream.ReadWriteAsCompactVarInt(ref nCode);
				nHeight = nCode / 2;
				fCoinBase = (nCode & 1) != 0;
				if(nHeight > 0)
					stream.ReadWriteAsCompactVarInt(ref nVersion);
				TxOutCompressor compressor = new TxOutCompressor();
				stream.ReadWrite(ref compressor);
				txout = compressor.TxOut;
			}
		}

		#endregion
	}
	public class TxUndo : IBitcoinSerializable
	{
		// undo information for all txins
		List<TxInUndo> vprevout = new List<TxInUndo>();
		public List<TxInUndo> Prevout
		{
			get
			{
				return vprevout;
			}
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref vprevout);
		}

		#endregion
	}
	public class BlockUndo : IBitcoinSerializable
	{
		List<TxUndo> vtxundo = new List<TxUndo>();
		public List<TxUndo> TxUndo
		{
			get
			{
				return vtxundo;
			}
		}



		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref vtxundo);
		}

		#endregion


		public uint256 CalculatedChecksum
		{
			get;
			internal set;
		}
		public void ComputeChecksum(uint256 hashBlock)
		{
			MemoryStream ms = new MemoryStream();
			hashBlock.AsBitcoinSerializable().ReadWrite(ms, true);
			this.ReadWrite(ms, true);
			CalculatedChecksum = Hashes.Hash256(ms.ToArray());
		}

		public uint256 BlockId
		{
			get;
			set;
		}
	}
}
