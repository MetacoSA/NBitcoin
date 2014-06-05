using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Scanning
{
	public abstract class ScanStatePersister : IDisposable
	{
		protected abstract ObjectStream<T> CreateObjectStream<T>() where T : class, IBitcoinSerializable, new();

		ObjectStream<AccountEntry> _AccountEntries;
		public ObjectStream<AccountEntry> AccountEntries
		{
			get
			{
				EnsureOpen();
				return _AccountEntries;
			}
		}


		ObjectStream<ChainChange> _ChainChanges;
		public ObjectStream<ChainChange> ChainChanges
		{
			get
			{
				EnsureOpen();
				return _ChainChanges;
			}
		}

		public void Rewind()
		{
			_ChainChanges.Rewind();
			AccountEntries.Rewind();
		}

		public bool IsReadOnly
		{
			get;
			private set;
		}
		public bool IsOpen
		{
			get;
			private set;
		}

		private void EnsureOpen()
		{
			if(!IsOpen)
				throw new InvalidOperationException("Persister must be open");
		}

		/// <summary>
		/// Open the persister
		/// </summary>
		/// <param name="isReadOnly"></param>
		/// <returns>True if just opened, false if was already open</returns>
		public bool Open(bool isReadOnly)
		{
			if(IsOpen)
			{
				if(IsReadOnly == isReadOnly)
					return false;
				else
					throw new InvalidOperationException("Persister already open with different readonly setting");
			}
			_ChainChanges = CreateObjectStream<ChainChange>();
			_AccountEntries = CreateObjectStream<AccountEntry>();
			IsOpen = true;
			IsReadOnly = isReadOnly;
			return true;
		}


		#region IDisposable Members

		public void Dispose()
		{
			if(IsOpen)
			{
				ChainChanges.Dispose();
				AccountEntries.Dispose();
				_ChainChanges = null;
				_AccountEntries = null;
				IsOpen = false;
			}
		}

		#endregion
	}
}
