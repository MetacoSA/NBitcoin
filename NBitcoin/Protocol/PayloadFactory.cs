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
	}
}
