using NBitcoin.Protocol;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace NBitcoin
{
	public class ConsensusFactory
	{
		static readonly TypeInfo BlockHeaderType = typeof(BlockHeader).GetTypeInfo();
		static readonly TypeInfo BlockType = typeof(Block).GetTypeInfo();
		static readonly TypeInfo TransactionType = typeof(Transaction).GetTypeInfo();
		static readonly TypeInfo TxInType = typeof(TxIn).GetTypeInfo();
		static readonly TypeInfo TxOutType = typeof(TxOut).GetTypeInfo();
		static readonly TypeInfo PSBTType = typeof(PSBT).GetTypeInfo();

		protected bool IsBlockHeader(Type type)
		{
			return BlockHeaderType.IsAssignableFrom(type.GetTypeInfo());
		}

		protected bool IsTxIn(Type type)
		{
			return TxInType.IsAssignableFrom(type.GetTypeInfo());
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
			if (IsTxIn(type))
			{
				result = CreateTxIn();
				return true;
			}
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

		public virtual Payload CreatePayload(string command)
		{
			return command switch
			{
				"inv" => new InvPayload(),
				"tx" => new TxPayload(),
				"getdata" => new GetDataPayload(),
				"headers" => new HeadersPayload(),
				"block" => new BlockPayload(),
#if !NOSOCKET
				"addr" => new AddrPayload(),
				"addrv2" => new AddrV2Payload(),
				"version" => new VersionPayload(),
#endif
				"ping" => new PingPayload(),
				"pong" => new PongPayload(),
				"getaddr" => new GetAddrPayload(),
				"blocktxn" => new BlockTxnPayload(),
				"cmpctblock" => new CmpctBlockPayload(),
				"cfilter" => new CompactFilterPayload(),
				"cfcheckpt" => new CompactFilterCheckPointPayload(),
				"cfheaders" => new CompactFilterHeadersPayload(),
				"feefilter" => new FeeFilterPayload(),
				"filteradd" => new FilterAddPayload(),
				"filterload" => new FilterLoadPayload(),
				"getblocktxn" => new GetBlockTxnPayload(),
				"getblocks" => new GetBlocksPayload(),
				"getcfilters" => new GetCompactFiltersPayload(),
				"getcfheaders" => new GetCompactFilterHeadersPayload(),
				"getcfcheckpt" => new GetCompactFilterCheckPointPayload(),
				"getheaders" => new GetHeadersPayload(),
				"havewitness" => new HaveWitnessPayload(),
				"mempool" => new MempoolPayload(),
				"merkleblock" => new MerkleBlockPayload(),
				"sendaddrv2" => new SendAddrV2Payload(),
				"sendcmpct" => new SendCmpctPayload(),
				"sendheaders" => new SendHeadersPayload(),
				"utxos" => new UTxOutputPayload(),
				"verack" => new VerAckPayload(),
				"wtxidrelay" => new WTxIdRelayPayload(),
				_ => new UnknownPayload(command)
			};
		}

		// Altcoins can override to provide a unique data parsing. If this
		// method returns false, the default parsing in RPCClient >
		// ParseVerboseBlock will be used.
		public virtual bool ParseGetBlockRPCRespose(JObject json, bool withFullTx, out BlockHeader blockHeader, out Block block, out List<uint256> txids)
		{
			blockHeader = null;
			block = null;
			txids = null;
			return false;
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
				SupportNodeBloom = protocolVersion >= 70011U,
				SupportSendHeaders = protocolVersion >= 70012U,
				SupportWitness = protocolVersion >= 70012U,
				SupportCompactBlocks = protocolVersion >= 70014U,
				SupportCheckSum = protocolVersion >= 60002,
				SupportUserAgent = protocolVersion >= 60002,
				SupportAddrv2 = protocolVersion >= 70016U,
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
			return new Transaction();
		}

		public virtual TxIn CreateTxIn()
		{
			return new TxIn();
		}

		public virtual TxOut CreateTxOut()
		{
			return new TxOut();
		}

		protected virtual TransactionBuilder CreateTransactionBuilderCore(Network network)
		{
			return new TransactionBuilder(network);
		}

		internal TransactionBuilder CreateTransactionBuilderCore2(Network network)
		{
			return CreateTransactionBuilderCore(network);
		}
	}
}
