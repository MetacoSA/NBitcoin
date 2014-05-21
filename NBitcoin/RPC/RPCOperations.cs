using System;
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
		dumpwallet,
		importwallet,

		getgenerate,
		setgenerate,
		getnetworkhashps,
		gethashespersec,
		getmininginfo,
		getwork,
		getblocktemplate,
		submitblock,

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
		getinfo,
		getwalletinfo,

		getrawtransaction,
		listunspent,
		lockunspent,
		listlockunspent,
		createrawtransaction,
		decoderawtransaction,
		decodescript,
		signrawtransaction,
		sendrawtransaction,

		getblockcount,
		getbestblockhash,
		getdifficulty,
		settxfee,
		getrawmempool,
		getblockhash,
		getblock,
		gettxoutsetinfo,
		gettxout,
		verifychain,
	}
}
