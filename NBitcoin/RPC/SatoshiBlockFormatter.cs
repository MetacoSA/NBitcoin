using System.Collections.Generic;
using System.Linq;
using NBitcoin.DataEncoders;
using Newtonsoft.Json.Linq;

namespace NBitcoin.RPC
{
	public class RPCScriptSig
	{
		public string asm { get; set; }
		public string hex { get; set; }
	}

	public class RPCVin
	{
		public string coinbase { get; set; }
		public object sequence { get; set; }
		public string txid { get; set; }
		public int? vout { get; set; }
		public RPCScriptSig scriptSig { get; set; }
	}

	public class RPCScriptPubKey
	{
		public string asm { get; set; }
		public string hex { get; set; }
		public string type { get; set; }
		public int? reqSigs { get; set; }
		public List<string> addresses { get; set; }
	}

	public class RPCVout
	{
		public double value { get; set; }
		public int n { get; set; }
		public RPCScriptPubKey scriptPubKey { get; set; }
	}

	public class RPCTx
	{
		public string txid { get; set; }
		public int version { get; set; }
		public int time { get; set; }
		public int locktime { get; set; }
		public List<RPCVin> vin { get; set; }
		public List<RPCVout> vout { get; set; }
	}

	public class RPCBlock
	{
		public string hash { get; set; }
		public int confirmations { get; set; }
		public int size { get; set; }
		public int height { get; set; }
		public int version { get; set; }
		public string merkleroot { get; set; }
		public int mint { get; set; }
		public uint time { get; set; }
		public uint nonce { get; set; }
		public string bits { get; set; }
		public double difficulty { get; set; }
		public string blocktrust { get; set; }
		public string chaintrust { get; set; }
		public string previousblockhash { get; set; }
		public string nextblockhash { get; set; }
		public string flags { get; set; }
		public string proofhash { get; set; }
		public int entropybit { get; set; }
		public string modifier { get; set; }
		public string modifierv2 { get; set; }
		public List<string> tx { get; set; }
		public string signature { get; set; }
	}

	public class SatoshiBlockFormatter
	{
		public static RPCBlock Parse(JObject json)
		{
			return json.ToObject<RPCBlock>();
		}

		public static Block ToBlock(RPCBlock rpcBlock)
		{
			var header = new BlockHeader()
			{
				Time = rpcBlock.time,
				//BlockStake = new BlockStake
				//{
				//	HashProof = uint256.Parse( rpcBlock.proofhash),
				//	Mint = rpcBlock.mint,
				//	StakeModifierV2 = uint256.Parse(rpcBlock.modifierv2)
				//},
				HashMerkleRoot = uint256.Parse(rpcBlock.merkleroot),
				Bits = new Target(Encoders.Hex.DecodeData(rpcBlock.bits)),
				HashPrevBlock = uint256.Parse(rpcBlock.previousblockhash),
				Nonce = rpcBlock.nonce,
				Version = rpcBlock.version,
			};

			var block = new Block(header);

			if (!string.IsNullOrEmpty(rpcBlock.signature))
			{
				block.BlockSignatur.Signature = Encoders.Hex.DecodeData(rpcBlock.signature);
			}

			// todo: parse transactions
			block.Transactions = rpcBlock.tx.Select(t => new Transaction()).ToList();

			return block;
		}
	}
}
