using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class Spendable : IBitcoinSerializable
	{
		public Spendable()
		{

		}
		public Spendable(OutPoint output, TxOut txout)
		{
			if(output == null)
				throw new ArgumentNullException("output");
			if(txout == null)
				throw new ArgumentNullException("txout");
			_Out = txout;
			_OutPoint = output;
		}

		private OutPoint _OutPoint;
		public OutPoint OutPoint
		{
			get
			{
				return _OutPoint;
			}
		}
		private TxOut _Out;
		public TxOut TxOut
		{
			get
			{
				return _Out;
			}
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _OutPoint);
			if(stream.Serializing)
			{
				TxOutCompressor compressor = new TxOutCompressor(_Out);
				stream.ReadWrite(ref compressor);
			}
			else
			{
				TxOutCompressor compressor = new TxOutCompressor();
				stream.ReadWrite(ref compressor);
				_Out = compressor.TxOut;
			}
		}

		#endregion
#if !PORTABLE
		internal Account _Account;

		public bool IsLocked
		{
			get
			{
				if(_Account == null)
					return false;
				return _Account._Locked.Contains(this.OutPoint);
			}
			set
			{
				if(_Account == null)
					throw new InvalidOperationException("This spendable is not associated to an account, impossible to lock");
				if(value)
					_Account.PushAccountEntry(new AccountEntry(AccountEntryReason.Lock, null, this.Clone(), Money.Zero, null));
				else
					_Account.PushAccountEntry(new AccountEntry(AccountEntryReason.Unlock, null, this.Clone(), Money.Zero, null));
			}
		}
		public override string ToString()
		{
			if(TxOut != null && TxOut.Value != null)
				return TxOut.Value.ToString() + (IsLocked ? "(locked)" : "");
			return "?";
		}
#endif
	}
}
