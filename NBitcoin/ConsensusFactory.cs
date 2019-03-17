﻿using NBitcoin.Protocol;
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
		static readonly TypeInfo BlockHeaderType = typeof(BlockHeader).GetTypeInfo();
		static readonly TypeInfo BlockType = typeof(Block).GetTypeInfo();
		static readonly TypeInfo TransactionType = typeof(Transaction).GetTypeInfo();
		static readonly TypeInfo TxOutType = typeof(TxOut).GetTypeInfo();

		protected bool IsBlockHeader(Type type)
		{
			return BlockHeaderType.IsAssignableFrom(type.GetTypeInfo());
		}

		protected bool IsTxOut(Type type)
		{
			return TxOutType.IsAssignableFrom(type.GetTypeInfo());
		}

		protected bool IsBlock(Type type)
		{
			return BlockType.IsAssignableFrom(type.GetTypeInfo());
		}

		protected bool IsTransaction(Type type)
		{
			return TransactionType.IsAssignableFrom(type.GetTypeInfo());
		}

		public virtual bool TryCreateNew(Type type, out IBitcoinSerializable result)
		{
			result = null;
			if (IsTxOut(type))
			{
				result = CreateTxOut();
				return true;
			}
			if (IsTransaction(type))
			{
				result = CreateTransaction();
				return true;
			}
			if (IsBlockHeader(type))
			{
				result = CreateBlockHeader();
				return true;
			}
			if (IsBlock(type))
			{
				result = CreateBlock();
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

		public virtual TxOut CreateTxOut()
		{
#pragma warning disable CS0618 // Type or member is obsolete
			return new TxOut();
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
