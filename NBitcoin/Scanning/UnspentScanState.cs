using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Scanning
{
	public class UnspentScanState
	{
		public UnspentScanState(string dataFolder, Network network)
			: this(new DataDirectory(dataFolder, network))
		{

		}

		public UnspentScanState(DataDirectory dataDirectory)
			: this(dataDirectory.GetChain(), dataDirectory.GetCoinsView(),
			dataDirectory.GetIndexedBlockUndoStore(),
			dataDirectory.GetIndexedBlockStore())
		{
		}

		public UnspentScanState(
								Chain chain,
								CoinsView coinsView,
								IndexedBlockUndoStore undoIndex,
								IndexedBlockStore blockStore)
		{
			if(chain == null)
				throw new ArgumentNullException("chain");
			if(undoIndex == null)
				throw new ArgumentNullException("undoIndex");
			if(coinsView == null)
				throw new ArgumentNullException("coinsView");
			if(blockStore == null)
				throw new ArgumentNullException("blockStore");
			_Chain = chain;
			_BlockStore = blockStore;
			_Network = undoIndex.Store.Network;
			_CoinsView = coinsView;
			_UndoIndex = undoIndex;
			if(!_Chain.Initialized)
				throw new ArgumentException("Chain should be initialized", "chain");
			_Chain = chain;
		}
		private readonly Network _Network;
		public Network Network
		{
			get
			{
				return _Network;
			}
		}
		private readonly Chain _Chain;
		public Chain Chain
		{
			get
			{
				return _Chain;
			}
		}
		private readonly CoinsView _CoinsView;
		public CoinsView CoinsView
		{
			get
			{
				return _CoinsView;
			}
		}
		private readonly IndexedBlockStore _BlockStore;
		public IndexedBlockStore BlockStore
		{
			get
			{
				return _BlockStore;
			}
		}
		private readonly IndexedBlockUndoStore _UndoIndex;
		public IndexedBlockUndoStore UndoIndex
		{
			get
			{
				return _UndoIndex;
			}
		}

		public bool Process(Block block)
		{
			var prev = Chain.GetBlock(block.Header.HashPrevBlock);
			var forkBlock = Chain.FindFork(prev.EnumerateToGenesis().Select(s => s.HashBlock));

			if(block.Header.HashPrevBlock != Chain.Tip.HashBlock)
			{

				foreach(var b in Chain.Tip.EnumerateToGenesis()
										.TakeWhile(b => b != forkBlock)
										.ToList())
				{
					if(!DisconnectBlock(b))
					{
						return false;
					}
					Chain.SetTip(b.Previous);
				}
			}


			foreach(var b in prev.EnumerateToGenesis()
								.TakeWhile(p => p != forkBlock)
								.Reverse())
			{
				if(!ConnectBlock(b))
				{
					return false;
				}
				//Chain.SetTip(Chain.ad
			}

			return true;
		}

		private bool ConnectBlock(ChainedBlock pindex)
		{
			var block = BlockStore.GetBlock(pindex.HashBlock, null);

			ValidationState state = new ValidationState(Network);
			state.CheckProofOfWork = false;
			state.CheckMerkleRoot = false;

			if(!state.CheckBlock(block))
			{
				return false;
			}

			var view = CoinsView;
			// verify that the view's current state corresponds to the previous block
			uint256 hashPrevBlock = pindex.Previous == null ? new uint256(0) : pindex.Previous.HashBlock;
			assert(hashPrevBlock == view.GetBestBlock(), "Connecting blocks from chain in the wrong order");

			// Special case for the genesis block, skipping connection of its transactions
			// (its coinbase is unspendable)
			if(block.GetHash() == Network.GetGenesis().GetHash())
			{
				view.SetBestBlock(pindex.HashBlock);
				return true;
			}
			bool fScriptChecks = pindex.Height >= 0;//Checkpoints::GetTotalBlocksEstimate();

			// Do not allow blocks that contain transactions which 'overwrite' older transactions,
			// unless those are already completely spent.
			// If such overwrites are allowed, coinbases and transactions depending upon those
			// can be duplicated to remove the ability to spend the first instance -- even after
			// being sent to another address.
			// See BIP30 and http://r6.ca/blog/20120206T005236Z.html for more information.
			// This logic is not necessary for memory pool transactions, as AcceptToMemoryPool
			// already refuses previously-known transaction ids entirely.
			// This rule was originally applied all blocks whose timestamp was after March 15, 2012, 0:00 UTC.
			// Now that the whole chain is irreversibly beyond that time it is applied to all blocks except the
			// two in the chain that violate it. This prevents exploiting the issue against nodes in their
			// initial block download.
			bool fEnforceBIP30 = (pindex.HashBlock == null || pindex.HashBlock == new uint256(0)) || // Enforce on CreateNewBlock invocations which don't have a hash.
								  !((pindex.Height == 91842 && pindex.HashBlock == new uint256("0x00000000000a4d0a398161ffc163c503763b1f4360639393e0e4c8e300e0caec")) ||
								   (pindex.Height == 91880 && pindex.HashBlock == new uint256("0x00000000000743f190a18c5577a3c2d2a1f610ae9601ac046a38084ccb7cd721")));
			if(fEnforceBIP30)
			{
				for(int i = 0 ; i < block.Transactions.Count ; i++)
				{
					uint256 hash = block.GetTxHash(i);
					if(view.HaveCoins(hash) && !view.GetCoins(hash).IsPruned)
						return state.DoS(100, Utils.error("ConnectBlock() : tried to overwrite transaction"),
										 RejectCode.INVALID, "bad-txns-BIP30");
				}
			}

			// BIP16 didn't become active until Apr 1 2012
			var nBIP16SwitchTime = Utils.UnixTimeToDateTime(1333238400);
			bool fStrictPayToScriptHash = (pindex.Header.BlockTime >= nBIP16SwitchTime);

			ScriptVerify flags = ScriptVerify.NoCache |
								 (fStrictPayToScriptHash ? ScriptVerify.P2SH : ScriptVerify.None);

			BlockUndo blockundo = new BlockUndo();

			//CCheckQueueControl<CScriptCheck> control(fScriptChecks && nScriptCheckThreads ? &scriptcheckqueue : NULL);

			var nStart = DateTimeOffset.UtcNow;
			long nFees = 0;
			int nInputs = 0;
			uint nSigOps = 0;

			for(int i = 0 ; i < block.Transactions.Count ; i++)
			{
				Transaction tx = block.Transactions[i];

				nInputs += tx.Inputs.Count;
				nSigOps += GetLegacySigOpCount(tx);
				if(nSigOps > ValidationState.MAX_BLOCK_SIGOPS)
					return state.DoS(100, Utils.error("ConnectBlock() : too many sigops"),
									 RejectCode.INVALID, "bad-blk-sigops");

				if(!tx.IsCoinBase)
				{
					if(!view.HaveInputs(tx))
						return state.DoS(100, Utils.error("ConnectBlock() : inputs missing/spent"),
										 RejectCode.INVALID, "bad-txns-inputs-missingorspent");

					if(fStrictPayToScriptHash)
					{
						// Add in sigops done by pay-to-script-hash inputs;
						// this is to prevent a "rogue miner" from creating
						// an incredibly-expensive-to-validate block.
						nSigOps += GetP2SHSigOpCount(tx, view);
						if(nSigOps > ValidationState.MAX_BLOCK_SIGOPS)
							return state.DoS(100, Utils.error("ConnectBlock() : too many sigops"),
											 RejectCode.INVALID, "bad-blk-sigops");
					}

					nFees += view.GetValueIn(tx) - tx.TotalOut;

					//std::vector<CScriptCheck> vChecks;
					//if (!CheckInputs(tx, state, view, fScriptChecks, flags, nScriptCheckThreads ? &vChecks : NULL))
					//	return false;
					//control.Add(vChecks);
				}

				TxUndo txundo = new TxUndo();
				UpdateCoins(tx, state, view, txundo, pindex.Height, block.GetTxHash(i));
				if(!tx.IsCoinBase)
					blockundo.TxUndo.Add(txundo);

				//BlockStore.Put(block);
			}

			for(int i = 0 ; i < block.Transactions.Count ; i++)
			{
				Transaction tx = block.Transactions[i];

				nInputs += tx.Inputs.Count;
				nSigOps += GetLegacySigOpCount(tx);
				if(nSigOps > ValidationState.MAX_BLOCK_SIGOPS)
					return state.DoS(100, Utils.error("ConnectBlock() : too many sigops"),
									 RejectCode.INVALID, "bad-blk-sigops");

				if(!tx.IsCoinBase)
				{
					if(!view.HaveInputs(tx))
						return state.DoS(100, Utils.error("ConnectBlock() : inputs missing/spent"),
										 RejectCode.INVALID, "bad-txns-inputs-missingorspent");

					if(fStrictPayToScriptHash)
					{
						// Add in sigops done by pay-to-script-hash inputs;
						// this is to prevent a "rogue miner" from creating
						// an incredibly-expensive-to-validate block.
						nSigOps += GetP2SHSigOpCount(tx, view);
						if(nSigOps > ValidationState.MAX_BLOCK_SIGOPS)
							return state.DoS(100, Utils.error("ConnectBlock() : too many sigops"),
											 RejectCode.INVALID, "bad-blk-sigops");
					}

					nFees += view.GetValueIn(tx) - tx.TotalOut;

					//std::vector<CScriptCheck> vChecks;
					//if (!CheckInputs(tx, state, view, fScriptChecks, flags, nScriptCheckThreads ? &vChecks : NULL))
					//	return false;
					//control.Add(vChecks);
				}

				TxUndo txundo = new TxUndo();
				UpdateCoins(tx, state, view, txundo, pindex.Height, block.GetTxHash(i));
				if(!tx.IsCoinBase)
					blockundo.TxUndo.Add(txundo);

				//BlockStore.Put(block);
			}


			var nTime = DateTimeOffset.UtcNow - nStart;
			//if (fBenchmark)
			//	LogPrintf("- Connect %u transactions: %.2fms (%.3fms/tx, %.3fms/txin)\n", (unsigned)block.vtx.size(), 0.001 * nTime, 0.001 * nTime / block.vtx.size(), nInputs <= 1 ? 0 : 0.001 * nTime / (nInputs-1));

			if(block.Transactions[0].TotalOut > Network.GetReward(pindex.Height) + new Money(nFees))
				return state.DoS(100,
								 Utils.error("ConnectBlock() : coinbase pays too much (actual=%d vs limit=%d)",
									   block.Transactions[0].TotalOut, Network.GetReward(pindex.Height) + new Money(nFees)),
									   RejectCode.INVALID, "bad-cb-amount");

			//if (!control.Wait())
			//	return state.DoS(100, false);

			var nTime2 = DateTimeOffset.UtcNow - nStart;
			//if (fBenchmark)
			//	LogPrintf("- Verify %u txins: %.2fms (%.3fms/txin)\n", nInputs - 1, 0.001 * nTime2, nInputs <= 1 ? 0 : 0.001 * nTime2 / (nInputs-1));

			//if (fJustCheck)
			//	return true;

			// Correct transaction counts.
			//pindex->nTx = block.vtx.size();
			//if (pindex.Previous != null)
			//	pindex.nChainTx = pindex->pprev->nChainTx + block.vtx.size();

			//// Write undo information to disk
			//if (pindex->GetUndoPos().IsNull() || !pindex->IsValid(BLOCK_VALID_SCRIPTS))
			//{
			//	if (pindex->GetUndoPos().IsNull()) {
			//		CDiskBlockPos pos;
			//		if (!FindUndoPos(state, pindex->nFile, pos, ::GetSerializeSize(blockundo, SER_DISK, CLIENT_VERSION) + 40))
			//			return error("ConnectBlock() : FindUndoPos failed");
			//		if (!blockundo.WriteToDisk(pos, pindex->pprev->GetBlockHash()))
			//			return state.Abort(_("Failed to write undo data"));

			//		// update nUndoPos in block index
			//		pindex->nUndoPos = pos.nPos;
			//		pindex->nStatus |= BLOCK_HAVE_UNDO;
			//	}

			//	pindex->RaiseValidity(BLOCK_VALID_SCRIPTS);

			//	CDiskBlockIndex blockindex(pindex);
			//	if (!pblocktree->WriteBlockIndex(blockindex))
			//		return state.Abort(_("Failed to write block index"));
			//}

			//if (fTxIndex)
			//	if (!pblocktree->WriteTxIndex(vPos))
			//		return state.Abort(_("Failed to write transaction index"));

			//// add this block to the view's block chain
			//bool ret;
			//ret = view.SetBestBlock(pindex->GetBlockHash());
			//assert(ret);

			//// Watch for transactions paying to me
			//for (unsigned int i = 0; i < block.vtx.size(); i++)
			//	g_signals.SyncTransaction(block.GetTxHash(i), block.vtx[i], &block);

			return true;
		}

		private void UpdateCoins(Transaction tx, ValidationState state, CoinsView inputs, TxUndo txundo, int nHeight, uint256 txhash)
		{
			bool ret;
			// mark inputs spent
			if(!tx.IsCoinBase)
			{
				foreach(TxIn txin in tx.Inputs)
				{
					Coins coins = inputs.GetCoins(txin.PrevOut.Hash);
					TxInUndo undo;
					ret = coins.Spend((int)txin.PrevOut.N, out undo);
					assert(ret, "double spend");
					txundo.Prevout.Add(undo);
				}
			}

			// add outputs
			inputs.SetCoins(txhash, new Coins(tx, nHeight));
			//assert(ret);
		}

		private uint GetP2SHSigOpCount(Transaction tx, CoinsView inputs)
		{
			if(tx.IsCoinBase)
				return 0;

			uint nSigOps = 0;
			for(int i = 0 ; i < tx.Inputs.Count ; i++)
			{
				TxOut prevout = inputs.GetOutputFor(tx.Inputs[i]);
				if(prevout.ScriptPubKey.IsPayToScriptHash)
					nSigOps += prevout.ScriptPubKey.GetSigOpCount(tx.Inputs[i].ScriptSig);
			}
			return nSigOps;
		}

		private uint GetLegacySigOpCount(Transaction tx)
		{
			uint nSigOps = 0;
			foreach(TxIn txin in tx.Inputs)
			{
				nSigOps += txin.ScriptSig.GetSigOpCount(false);
			}
			foreach(TxOut txout in tx.Outputs)
			{
				nSigOps += txout.ScriptPubKey.GetSigOpCount(false);
			}
			return nSigOps;
		}

		private bool DisconnectBlock(ChainedBlock pindex)
		{
			var view = CoinsView;
			assert(pindex.HashBlock == CoinsView.GetBestBlock(), "Disconnecting blocks from chain in the wrong order");

			Block block = BlockStore.GetBlock(pindex.HashBlock, null);

			BlockUndo blockUndo = UndoIndex.Get(pindex.HashBlock);
			if(blockUndo == null)
				return Utils.error("DisconnectBlock() : no undo data available");
			if(blockUndo.TxUndo.Count + 1 != block.Transactions.Count)
				return Utils.error("DisconnectBlock() : block and undo data inconsistent");

			// undo transactions in reverse order
			for(int i = block.Transactions.Count - 1 ; i >= 0 ; i--)
			{
				Transaction tx = block.Transactions[i];
				uint256 hash = tx.GetHash();

				// Check that all outputs are available and match the outputs in the block itself
				// exactly. Note that transactions with only provably unspendable outputs won't
				// have outputs available even in the block itself, so we handle that case
				// specially with outsEmpty.
				Coins outs = view.GetCoins(hash) ?? new Coins();
				outs.ClearUnspendable();

				Coins outsBlock = new Coins(tx, pindex.Height);
				// The CCoins serialization does not serialize negative numbers.
				// No network rules currently depend on the version here, so an inconsistency is harmless
				// but it must be corrected before txout nversion ever influences a network rule.
				if(outsBlock.Version < 0)
					outs.Version = outsBlock.Version;
				if(!Utils.ArrayEqual(outs.ToBytes(), outsBlock.ToBytes()))
				{
					Utils.error("DisconnectBlock() : added transaction mismatch? database corrupted");
				}

				// remove outputs
				outs = new Coins();

				// restore inputs
				if(i > 0)
				{ // not coinbases
					TxUndo txundo = blockUndo.TxUndo[i - 1];
					if(txundo.Prevout.Count != tx.Inputs.Count)
						return Utils.error("DisconnectBlock() : transaction and undo data inconsistent");
					for(int j = tx.Inputs.Count ; j-- > 0 ; )
					{
						OutPoint @out = tx.Inputs[j].PrevOut;
						TxInUndo undo = txundo.Prevout[j];
						Coins coins = view.GetCoins(@out.Hash); // this can fail if the prevout was already entirely spent
						if(undo.Height != 0)
						{
							// undo data contains height: this is the last output of the prevout tx being spent
							if(!coins.IsPruned)
							{
								Utils.error("DisconnectBlock() : undo data overwriting existing transaction");
							}
							coins = new Coins();
							coins.Coinbase = undo.CoinBase;
							coins.Height = undo.Height;
							coins.Version = undo.Version;
						}
						else
						{
							if(coins.IsPruned)
							{
								Utils.error("DisconnectBlock() : undo data adding output to missing transaction");
							}
						}
						if(coins.IsAvailable(@out.N))
						{
							Utils.error("DisconnectBlock() : undo data overwriting existing output");
						}
						if(coins.Outputs.Count < @out.N + 1)
							coins.Outputs.Resize((int)(@out.N + 1));
						coins.Outputs[(int)@out.N] = undo.TxOut;
						view.SetCoins(@out.Hash, coins);
					}
				}
			}
			view.SetBestBlock(pindex.Previous.HashBlock);
			return true;
		}

		void assert(bool value, string msg)
		{
			if(!value)
			{
				Utils.error(msg);
				throw new InvalidProgramException(msg);
			}
		}
	}
}
