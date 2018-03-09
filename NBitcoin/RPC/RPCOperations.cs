﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.RPC
{
	//from rpcserver.h
	public enum RPCOperations
	{
		getconnectioncount,
		getpeerinfo,
		ping,
		addnode,
		getaddednodeinfo,
		getnettotals,

		dumpprivkey,
		importprivkey,
		importaddress,
		dumpwallet,
		importwallet,

		getgenerate,
		setgenerate,
		generate,
		getnetworkhashps,
		gethashespersec,
		getmininginfo,
		prioritisetransaction,
		getwork,
		getblocktemplate,
		submitblock,
		estimatefee,
		estimatesmartfee,

		getnewaddress,
		getaccountaddress,
		getrawchangeaddress,
		setaccount,
		getaccount,
		getaddressesbyaccount,
		sendtoaddress,
		signmessage,
		verifymessage,
		getreceivedbyaddress,
		getreceivedbyaccount,
		getbalance,
		getunconfirmedbalance,
		movecmd,
		sendfrom,
		sendmany,
		addmultisigaddress,
		createmultisig,
		listreceivedbyaddress,
		listreceivedbyaccount,
		listtransactions,
		listaddressgroupings,
		listaccounts,
		listsinceblock,
		gettransaction,
		backupwallet,
		keypoolrefill,
		walletpassphrase,
		walletpassphrasechange,
		walletlock,
		encryptwallet,
		validateaddress,
		[Obsolete("Deprecated in Bitcoin Core 0.16.0 use getblockchaininfo, getnetworkinfo, getwalletinfo or getmininginfo instead")]
		getinfo,
		getwalletinfo,
		getblockchaininfo,
		getnetworkinfo,

		getrawtransaction,
		listunspent,
		lockunspent,
		listlockunspent,
		createrawtransaction,
		decoderawtransaction,
		decodescript,
		signrawtransaction,
		sendrawtransaction,
		gettxoutproof,
		verifytxoutproof,

		getblockcount,
		getbestblockhash,
		getdifficulty,
		settxfee,
		getmempoolinfo,
		getrawmempool,
		getblockhash,
		getblock,
		gettxoutsetinfo,
		gettxout,
		verifychain,
		getchaintips,
		invalidateblock
	}
}
