using NBitcoin.Protocol;
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

		protected bool IsBlockHeader(Type type)
		{
			return IsAssignable(type, BlockHeaderType, _IsAssignableFromBlockHeader);
		}

		protected bool IsBlock(Type type)
		{
			return IsAssignable(type, BlockType, _IsAssignableFromBlock);
		}

		protected bool IsTransaction(Type type)
		{
			return IsAssignable(type, TransactionType, _IsAssignableFromTransaction);
		}

		private bool IsAssignable(Type type, TypeInfo baseType, ConcurrentDictionary<Type, bool> cache)
		{
			bool isAssignable = false;
			if (!cache.TryGetValue(type, out isAssignable))
			{
				isAssignable = baseType.IsAssignableFrom(type.GetTypeInfo());
				cache.TryAdd(type, isAssignable);
			}
			return isAssignable;
		}

		public virtual bool TryCreateNew(Type type, out IBitcoinSerializable result)
		{
			result = null;
			if (IsBlock(type))
			{
				result = CreateBlock();
				return true;
			}
			if (IsBlockHeader(type))
			{
				result = CreateBlockHeader();
				return true;
			}
			if (IsTransaction(type))
			{
				result = CreateTransaction();
				return true;
			}
			return false;
		}

		public bool TryCreateNew<T>(out T result) where T : IBitcoinSerializable
		{
			result = default(T);
			IBitcoinSerializable r = null;
			var success = TryCreateNew(typeof(T), out r);
			if (success)
				result = (T)r;
			return success;
		}

		public virtual ProtocolCapabilities GetProtocolCapabilities(uint protocolVersion)
		{
			return new ProtocolCapabilities()
			{
				PeerTooOld = protocolVersion < 209U,
				SupportTimeAddress = protocolVersion >= 31402U,
				SupportGetBlock = protocolVersion < 32000U || protocolVersion > 32400U,
				SupportPingPong = protocolVersion > 60000U,
				SupportMempoolQuery = protocolVersion >= 60002U,
				SupportReject = protocolVersion >= 70002U,
				SupportNodeBloom = protocolVersion >= 70011U,
				SupportSendHeaders = protocolVersion >= 70012U,
				SupportWitness = protocolVersion >= 70012U,
				SupportCompactBlocks = protocolVersion >= 70014U,
				SupportCheckSum = protocolVersion >= 60002,
				SupportUserAgent = protocolVersion >= 60002
			};
		}

		public virtual Block CreateBlock()
		{
#pragma warning disable CS0618 // Type or member is obsolete
			return new Block(CreateBlockHeader());
#pragma warning restore CS0618 // Type or member is obsolete
		}

		public virtual BlockHeader CreateBlockHeader()
		{
#pragma warning disable CS0618 // Type or member is obsolete
			return new BlockHeader();
#pragma warning restore CS0618 // Type or member is obsolete
		}

		public virtual Transaction CreateTransaction()
		{
#pragma warning disable CS0618 // Type or member is obsolete
			return new Transaction();
#pragma warning restore CS0618 // Type or member is obsolete
		}

		protected virtual TransactionBuilder CreateTransactionBuilderCore()
		{
#pragma warning disable CS0618 // Type or member is obsolete
			return new TransactionBuilder();
#pragma warning restore CS0618 // Type or member is obsolete
		}

		public TransactionBuilder CreateTransactionBuilder()
		{
#pragma warning disable CS0618 // Type or member is obsolete
			var builder = CreateTransactionBuilderCore();
			builder.SetConsensusFactory(this);
			return builder;
#pragma warning restore CS0618 // Type or member is obsolete
		}

		public TransactionBuilder CreateTransactionBuilder(int seed)
		{
#pragma warning disable CS0618 // Type or member is obsolete
			var builder = CreateTransactionBuilderCore();
			builder.SetConsensusFactory(this);
			builder.ShuffleRandom = new Random(seed);
			return builder;
#pragma warning restore CS0618 // Type or member is obsolete
		}
	}
}
