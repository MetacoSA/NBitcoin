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
	public abstract class ScanStatePersister
	{
		public ScanStatePersister()
		{
			_AccountEntries = new Lazy<ObjectStream<AccountEntry>>(() =>
			{
				return this.CreateObjectStream<AccountEntry>();
			}, false);
			_ProcessedBlocks = new Lazy<ObjectStream<BlockHeader>>(() =>
			{
				return this.CreateObjectStream<BlockHeader>();
			}, false);
		}

		protected abstract ObjectStream<T> CreateObjectStream<T>() where T : class, IBitcoinSerializable, new();

		Lazy<ObjectStream<AccountEntry>> _AccountEntries;
		public ObjectStream<AccountEntry> AccountEntries
		{
			get
			{
				return _AccountEntries.Value;
			}
		}


		Lazy<ObjectStream<BlockHeader>> _ProcessedBlocks;
		public ObjectStream<BlockHeader> ProcessedBlocks
		{
			get
			{
				return _ProcessedBlocks.Value;
			}
		}

		public void Rewind()
		{
			ProcessedBlocks.Rewind();
			AccountEntries.Rewind();
		}

		public abstract void Init(int startHeight);

		public abstract int GetStartHeight();
	}
}
