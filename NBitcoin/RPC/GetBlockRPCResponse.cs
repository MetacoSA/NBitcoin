using System;
using System.Linq;
using System.Collections.Generic;
using NBitcoin;
namespace NBitcoin.RPC
{
	/// <summary>
	/// Verbosity option you can pass to `GetBlock` rpc call.
	/// If you want a raw block without metadata, (i.e. `0` verbosity for the rpc call) you should just call the method
	/// without this option.
	/// </summary>
	public enum GetBlockVerbosity
	{
		/// <summary>
		/// Verbosity `1` for the rpc call. Block itself will not be included in response if you specify this.
		/// However, txids in the block will be included in `TxIds` field in the response anyway.
		/// </summary>
		WithOnlyTxId = 1,
		/// <summary>
		/// Verbosity `2` for the rpc call. Use this if you want *both* full block and its metadata.
		/// </summary>
		WithFullTx = 2
	}

	public class GetBlockRPCResponse
	{
		public int Confirmations { get; set; }
		public int StrippedSize { get; set; }
		public int Size { get; set; }
		public int Weight { get; set; }
		public int Height { get; set; }
		public string VersionHex { get; set; }
		public uint MedianTimeUnix { get; set; }
		public double Difficulty { get; set; }
		public uint256 ChainWork { get; set; }

#nullable enable
		/// <summary>
		///  This field exists only when the block is not on the tip.
		/// </summary>
		public uint256? NextBlockHash { get; set; }
		/// <summary>
		/// This field exists only when you specified `WithFullTx` verbosity
		/// </summary>
		public Block? Block { get; set; }
#nullable disable

		public BlockHeader Header { get; set; }
		public List<uint256> TxIds { get; set; }
		public DateTimeOffset MedianTime => NBitcoin.Utils.UnixTimeToDateTime(MedianTimeUnix);
	}
}
