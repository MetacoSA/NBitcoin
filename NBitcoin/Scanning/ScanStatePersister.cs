using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Scanning
{
	public abstract class ObjectStream<T> where T : class, IBitcoinSerializable, new()
	{
		public IEnumerable<T> Enumerate()
		{
			T o = null;
			while((o = ReadNext()) != null)
			{
				yield return o;
			}
		}

		public T ReadNext()
		{
			var result = ReadNextCore();
			if(result == null)
			{
				if(!EOF)
					throw new InvalidProgramException("EOF should be true if there is no object left in the stream");
			}
			return result;
		}

		public void WriteNext(T obj)
		{
			if(obj == null)
				throw new ArgumentNullException("obj");
			if(!EOF)
				throw new InvalidOperationException("EOF should be true before writing more");
			WriteNextCore(obj);
		}

		public abstract void Rewind();
		protected abstract void WriteNextCore(T obj);
		protected abstract T ReadNextCore();

		public abstract bool EOF
		{
			get;
		}
	}
	public abstract class ScanStatePersister : IDisposable
	{
		protected abstract ObjectStream<T> CreateObjectStream<T>() where T : class, IBitcoinSerializable, new();
		public abstract void CloseStream<T>(ObjectStream<T> stream) where T : class, IBitcoinSerializable, new();

		ObjectStream<AccountEntry> _AccountEntries;
		public ObjectStream<AccountEntry> AccountEntries
		{
			get
			{
				EnsureOpen();
				return _AccountEntries;
			}
		}


		ObjectStream<BlockHeader> _ProcessedBlocks;
		public ObjectStream<BlockHeader> ProcessedBlocks
		{
			get
			{
				EnsureOpen();
				return _ProcessedBlocks;
			}
		}

		public void Rewind()
		{
			ProcessedBlocks.Rewind();
			AccountEntries.Rewind();
		}

		public abstract void Init(int startHeight);

		public abstract int GetStartHeight();

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
			_ProcessedBlocks = CreateObjectStream<BlockHeader>();
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
				CloseStream(ProcessedBlocks);
				CloseStream(AccountEntries);
				_ProcessedBlocks = null;
				_AccountEntries = null;
				IsOpen = false;
			}
		}

		#endregion
	}
}
