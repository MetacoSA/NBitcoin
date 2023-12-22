using System;
using NBitcoin.Protocol.Payloads;

namespace NBitcoin.Protocol
{
	public class PayloadFactory
	{
		public static Payload Create(string command)
		{
			switch (command)
			{
#if !NOSOCKET
				case "addr": return new AddrPayload();
				case "addrv2": return new AddrV2Payload();
				case "version": return new VersionPayload();
#endif
				case "block": return new BlockPayload();
				case "blocktxn": return new BlockTxnPayload();
				case "cmpctblock": return new CmpctBlockPayload();
				case "cfilter": return new CompactFilterPayload();
				case "cfcheckpt": return new CompactFilterCheckPointPayload();
				case "cfheaders": return new CompactFilterHeadersPayload();
				case "feefilter": return new FeeFilterPayload();
				case "filteradd": return new FilterAddPayload();
				case "filterload": return new FilterLoadPayload();
				case "getaddr": return new GetAddrPayload();
				case "getblocktxn": return new GetBlockTxnPayload();
				case "getblocks": return new GetBlocksPayload();
				case "getcfilters": return new GetCompactFiltersPayload();
				case "getcfheaders": return new GetCompactFilterHeadersPayload();
				case "getcfcheckpt": return new GetCompactFilterCheckPointPayload();
				case "getdata": return new GetDataPayload();
				case "getheaders": return new GetHeadersPayload();
				case "havewitness": return new HaveWitnessPayload();
				case "headers": return new HeadersPayload();
				case "inv": return new InvPayload();
				case "mempool": return new MempoolPayload();
				case "merkleblock": return new MerkleBlockPayload();
				case "ping": return new PingPayload();
				case "pong": return new PongPayload();
				case "sendaddrv2": return new SendAddrV2Payload();
				case "sendcmpct": return new SendCmpctPayload();
				case "sendheaders": return new SendHeadersPayload();
				case "tx": return new TxPayload();
				case "utxos": return new UTxOutputPayload();
				case "verack": return new VerAckPayload();
				case "wtxidrelay": return new WTxIdRelayPayload();
				default: return new UnknownPayload(command);
			}
		}

		public static string GetCommand(Payload payload)
		{
#if !NOSOCKET
			if (payload is AddrV2Payload) return "addrv2";
			if (payload is AddrPayload) return "addr";
			if (payload is VersionPayload) return "version";
#endif
			if (payload is BlockPayload) return "block";
			if (payload is BlockTxnPayload) return "blocktxn";
			if (payload is CmpctBlockPayload) return "cmpctblock";
			if (payload is CompactFilterPayload) return "cfilter";
			if (payload is CompactFilterHeadersPayload) return "cfheaders";
			if (payload is CompactFilterCheckPointPayload) return "cfcheckpt";
			if (payload is FeeFilterPayload) return "feefilter";
			if (payload is FilterAddPayload) return "filteradd";
			if (payload is FilterLoadPayload) return "filterload";
			if (payload is GetAddrPayload) return "getaddr";
			if (payload is GetBlockTxnPayload) return "getblocktxn";
			if (payload is GetBlocksPayload) return "getblocks";
			if (payload is GetCompactFiltersPayload) return "getcfilters";
			if (payload is GetCompactFilterHeadersPayload) return "getcfheaders";
			if (payload is GetCompactFilterCheckPointPayload) return "getcfcheckpt";
			if (payload is GetDataPayload) return "getdata";
			if (payload is GetHeadersPayload) return "getheaders";
			if (payload is HaveWitnessPayload) return "havewitness";
			if (payload is HeadersPayload) return "headers";
			if (payload is InvPayload) return "inv";
			if (payload is MempoolPayload) return "mempool";
			if (payload is MerkleBlockPayload) return "merkleblock";
			if (payload is PingPayload) return "ping";
			if (payload is PongPayload) return "pong";
			if (payload is SendAddrV2Payload) return "sendaddrv2";
			if (payload is SendCmpctPayload) return "sendcmpct";
			if (payload is SendHeadersPayload) return "sendheaders";
			if (payload is TxPayload) return "tx";
			if (payload is UTxOutputPayload) return "utxos";
			if (payload is VerAckPayload) return "verack";
			if (payload is WTxIdRelayPayload) return "wtxidrelay";
			throw new NotSupportedException($"Unknown command for Payload type: {payload.GetType().Name}");
		}
	}
}
