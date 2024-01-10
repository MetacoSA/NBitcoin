using System;
using NBitcoin.Protocol.Payloads;

namespace NBitcoin.Protocol
{
	public class PayloadFactory
	{
		public static readonly PayloadFactory Instance = new PayloadFactory();

		private PayloadFactory()
		{
		}


		public Payload Create(string command)
		{
			return command switch
			{
#if !NOSOCKET
				"addr" => new AddrPayload(),
				"addrv2" => new AddrV2Payload(),
				"version" => new VersionPayload(),
#endif
				"ping" => new PingPayload(),
				"pong" => new PongPayload(),
				"inv" => new InvPayload(),
				"getdata" => new GetDataPayload(),
				"tx" => new TxPayload(),
				"getaddr" => new GetAddrPayload(),
				"headers" => new HeadersPayload(),
				"block" => new BlockPayload(),
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
				_ =>  new UnknownPayload(command)
			};
		}

		public string GetCommand(Payload payload)
		{
			return payload switch
			{
#if !NOSOCKET
				AddrV2Payload => "addrv2",
				AddrPayload => "addr",
				VersionPayload => "version",
#endif
				PingPayload => "ping",
				PongPayload => "pong",
				InvPayload => "inv",
				TxPayload => "tx",
				GetDataPayload => "getdata",
				HeadersPayload => "headers",
				BlockPayload => "block",
				BlockTxnPayload => "blocktxn",
				CmpctBlockPayload => "cmpctblock",
				CompactFilterPayload => "cfilter",
				CompactFilterHeadersPayload => "cfheaders",
				CompactFilterCheckPointPayload => "cfcheckpt",
				FeeFilterPayload => "feefilter",
				FilterAddPayload => "filteradd",
				FilterLoadPayload => "filterload",
				GetAddrPayload => "getaddr",
				GetBlockTxnPayload => "getblocktxn",
				GetBlocksPayload => "getblocks",
				GetCompactFiltersPayload => "getcfilters",
				GetCompactFilterHeadersPayload => "getcfheaders",
				GetCompactFilterCheckPointPayload => "getcfcheckpt",
				GetHeadersPayload => "getheaders",
				HaveWitnessPayload => "havewitness",
				MempoolPayload => "mempool",
				MerkleBlockPayload => "merkleblock",
				SendAddrV2Payload => "sendaddrv2",
				SendCmpctPayload => "sendcmpct",
				SendHeadersPayload => "sendheaders",
				UTxOutputPayload => "utxos",
				VerAckPayload => "verack",
				WTxIdRelayPayload => "wtxidrelay",
				_ => throw new NotSupportedException($"Unknown command for Payload type: {payload.GetType().Name}")
			};
		}
	}
}
