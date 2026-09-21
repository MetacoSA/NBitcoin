#nullable enable
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
		/// Verbosity `1` for the `getblock` RPC call. Block itself will not be included in response if you specify this.
		/// However, txids in the block will be included in `TxIds` field in the response anyway.
		/// </summary>
		WithOnlyTxId = 1,

		/// <summary>
		/// Verbosity `2` for the `getblock` RPC call. Use this if you want *both* full block and its metadata.
		/// </summary>
		WithFullTx = 2,

		/// <summary>
		/// Verbosity `3` for the `getblock` RPC call. Use this if you want *both* full block, its metadata, and prevout information for inputs.
		/// </summary>
		/// <remarks>Only available for unpruned blocks in the current best chain in Bitcoin Core 23.0 and newer.</remarks>
		WithFullTxAndPrevouts = 3,
	}

	public class GetBlockRPCResponse
	{
		public int Confirmations { get; set; }
		public int StrippedSize { get; set; }
		public int Size { get; set; }
		public int Weight { get; set; }
		public int Height { get; set; }
#nullable disable
		public string VersionHex { get; set; }
		public uint MedianTimeUnix { get; set; }
		public double Difficulty { get; set; }
		public uint256 ChainWork { get; set; }
#nullable enable
		/// <summary>This field exists only when the block is not on the tip.</summary>
		public uint256? NextBlockHash { get; set; }

		/// <summary>This field exists only when you specified `WithFullTx` verbosity</summary>
		public Block? Block { get; set; }

#nullable disable
		public BlockHeader Header { get; set; }
		public List<uint256> TxIds { get; set; }
#nullable enable
		public DateTimeOffset MedianTime => NBitcoin.Utils.UnixTimeToDateTime(MedianTimeUnix);

		/// <summary>Prevout information for each input of each transaction. Only populated when verbosity 3 was requested.</summary>
		/// <remarks>
		/// Outer list is indexed by transaction, in the same order as <see cref="TxIds"/> / <see cref="Block"/>.Transactions.
		/// Inner list is indexed by input (vin) number within that transaction.
		/// An entry is <see langword="null"/> when no prevout data is available for that input (e.g. a coinbase input, or the node is pruned).
		/// </remarks>
		public List<List<PrevOutInfo?>>? PrevOuts { get; set; }
	}

	/// <summary>
	/// Prevout information for a spent input as returned by <c>getblock</c> verbosity 3.
	/// </summary>
	/// <param name="Generated"><c>true</c> if the spent coin was a coinbase output.</param>
	/// <param name="Height">Height of the block that created the spent output.</param>
	/// <param name="Value">Value of the UTXO in bitcoin.</param>
	/// <param name="ScriptPubKey">ScriptPubKey of the spent output.</param>
	public class PrevOutInfo(bool Generated, int Height, Money Value, Script? ScriptPubKey)
	{
		public bool Generated { get; } = Generated;
		public int Height { get; } = Height;
		public Money Value { get; } = Value;
		public Script? ScriptPubKey { get; } = ScriptPubKey;
	}
}
