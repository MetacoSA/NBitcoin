using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
    public class ConsensusFactory
    {
		ConcurrentDictionary<Type, bool> _IsAssignableFromBlockHeader = new ConcurrentDictionary<Type, bool>();
		TypeInfo BlockHeaderType = typeof(BlockHeader).GetTypeInfo();

		ConcurrentDictionary<Type, bool> _IsAssignableFromBlock = new ConcurrentDictionary<Type, bool>();
		TypeInfo BlockType = typeof(Block).GetTypeInfo();

		ConcurrentDictionary<Type, bool> _IsAssignableFromTransaction = new ConcurrentDictionary<Type, bool>();
		TypeInfo TransactionType = typeof(Transaction).GetTypeInfo();

		protected bool IsBlockHeader<T>()
		{
			return IsAssignable<T>(BlockHeaderType, _IsAssignableFromBlockHeader);
		}

		protected bool IsBlock<T>()
		{
			return IsAssignable<T>(BlockType, _IsAssignableFromBlock);
		}

		protected bool IsTransaction<T>()
		{
			return IsAssignable<T>(TransactionType, _IsAssignableFromTransaction);
		}

		private bool IsAssignable<T>(TypeInfo type, ConcurrentDictionary<Type, bool> cache)
		{
			bool isAssignable = false;
			if(!cache.TryGetValue(typeof(T), out isAssignable))
			{
				isAssignable = type.IsAssignableFrom(typeof(T).GetTypeInfo());
				_IsAssignableFromBlockHeader.TryAdd(typeof(T), isAssignable);
			}
			return isAssignable;
		}

		public virtual bool TryCreateNew<T>(out T result) where T : IBitcoinSerializable
		{
			result = default(T);
			if(IsBlock<T>())
			{
				result = (T)(object)CreateBlock();
				return true;
			}
			if(IsBlockHeader<T>())
			{
				result = (T)(object)CreateBlockHeader();
				return true;
			}
			if(IsTransaction<T>())
			{
				result = (T)(object)CreateTransaction();
				return true;
			}
			return false;
		}

		public virtual Block CreateBlock()
		{
#pragma warning disable CS0612 // Type or member is obsolete
			return new Block(CreateBlockHeader());
#pragma warning restore CS0612 // Type or member is obsolete
		}

		public virtual BlockHeader CreateBlockHeader()
		{
#pragma warning disable CS0618 // Type or member is obsolete
			return new BlockHeader();
#pragma warning restore CS0618 // Type or member is obsolete
		}

		public virtual Transaction CreateTransaction()
		{
			return new Transaction();
		}
	}
}
