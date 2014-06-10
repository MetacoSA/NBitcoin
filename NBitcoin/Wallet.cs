using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public enum BlockType
	{
		Main,
		Side
	}

	public enum WalletEntryType : byte
	{
		Income,
		Outcome
	}

	public class WalletEntry
	{
		public int Confidence
		{
			get
			{
				if(Block == null)
					return 0;
				return 1;
			}
		}

		public Script ScriptPubKey
		{
			get;
			set;
		}
		public OutPoint OutPoint
		{
			get;
			set;
		}

		public uint256 Block
		{
			get;
			set;
		}

		public Money Value
		{
			get;
			set;
		}

		internal Spendable GetSpendable()
		{
			if(Value == null || OutPoint == null)
				return null;
			return new Spendable(OutPoint, new TxOut(Value, ScriptPubKey));
		}

		public WalletEntryType Type
		{
			get
			{
				return Value == null ? WalletEntryType.Outcome : WalletEntryType.Income;
			}
		}
	}


	
	

	public delegate void WalletBalanceChangedDelegate(Wallet wallet, Transaction tx, Money oldBalance, Money newBalance);
	public class Wallet
	{
		public event WalletBalanceChangedDelegate BalanceChanged;
		public event WalletBalanceChangedDelegate CoinsReceived;
		public event WalletBalanceChangedDelegate CoinsSent;

		private Network _Network;
		public Network Network
		{
			get
			{
				return _Network;
			}
		}
		byte _NetworkByte;

		private Wallet()
		{

		}
		public Wallet(Network network)
		{
			_NetworkByte = network == Network.Main ? (byte)0 :
							network == Network.TestNet ? (byte)1 :
							network == Network.RegTest ? (byte)2 : (byte)3;
			InitNetwork();
			this._Network = network;
			this._Accounts = new Accounts();
		}

		private void InitNetwork()
		{
			_Network = _NetworkByte == 0 ? Network.Main :
				_NetworkByte == 1 ? Network.TestNet :
				_NetworkByte == 2 ? Network.RegTest : null;
			if(_Network == null)
				throw new FormatException("Incorrect network byte detected");
		}



		public void AddKey(BitcoinSecret key)
		{
			if(key.Network != Network)
				throw new InvalidOperationException("This key is does not belong to the same network as the wallet");
			AddKey(key.Key);
		}

		List<Key> _Keys = new List<Key>();

		public void AddKey(Key key)
		{
			_Keys.Add(key);
		}

		public Money Balance
		{
			get
			{
				return Accounts.Available.Balance;
			}
		}

		Accounts _Accounts;
		public Accounts Accounts
		{
			get
			{
				return _Accounts;
			}
		}

		public void UnconfirmedTransaction(Transaction tx)
		{
			ReceiveTransaction(null, null, tx);
		}


		private void ReceiveTransaction(uint256 block, BlockType? blockType, Transaction tx)
		{
			var oldBalance = Balance;

			var txHash = tx.GetHash();
			for(int i = 0 ; i < tx.Outputs.Count ; i++)
			{
				foreach(var key in _Keys)
				{
					if(tx.Outputs[i].IsTo(key.PubKey))
					{
						var entry = new WalletEntry();
						entry.Block = block;
						entry.ScriptPubKey = tx.Outputs[i].ScriptPubKey;
						entry.OutPoint = new OutPoint(tx, i);
						entry.Value = tx.Outputs[i].Value;
						_Accounts.PushEntry(entry, blockType);
					}
				}
			}

			foreach(var txin in tx.Inputs)
			{
				foreach(var key in _Keys)
				{
					if(txin.IsFrom(key.PubKey))
					{
						var entry = new WalletEntry();
						entry.Block = block;
						entry.OutPoint = txin.PrevOut;
						_Accounts.PushEntry(entry, blockType);
					}
				}
			}

			OnBalanceChanged(tx, oldBalance, Balance);
		}

		private void OnBalanceChanged(Transaction tx, Money oldBalance, Money newBalance)
		{
			if(oldBalance < newBalance)
			{
				if(CoinsReceived != null)
					CoinsReceived(this, tx, oldBalance, newBalance);
			}
			if(oldBalance > newBalance)
			{
				if(CoinsSent != null)
					CoinsSent(this, tx, oldBalance, newBalance);
			}
			if(oldBalance != newBalance)
			{
				if(BalanceChanged != null)
					BalanceChanged(this, tx, oldBalance, newBalance);
			}
		}

		private void ReceiveBlock(Block block, BlockType blockType = BlockType.Main)
		{
			var hash = block.GetHash();
			foreach(var tx in block.Transactions)
			{
				ReceiveTransaction(hash, blockType, tx);
			}
		}

		public bool CompleteTx(Transaction tx, Account pool = null)
		{
			return CompleteTx(tx, Money.Zero, pool);
		}
		public bool CompleteTx(Transaction tx, Money fees, Account pool = null)
		{
			pool = pool ?? Accounts.Available;
			var entries = pool.GetEntriesToCover(tx.TotalOut + fees, false);
			if(entries == null)
				return false;

			foreach(var entry in entries)
			{
				tx.AddInput(new TxIn(entry.OutPoint));
			}
			var surplus = entries.Sum(i => i.TxOut.Value) - fees - tx.TotalOut;
			if(surplus != Money.Zero)
				tx.AddOutput(surplus, GetKey().PubKey.ID);

			for(int i = 0 ; i < entries.Length ; i++)
			{
				var entry = entries[i];
				var vin = tx.Inputs[i];
				var key = GetKey(entry.TxOut.ScriptPubKey.GetDestination());
				tx.Inputs[i].ScriptSig = new PayToPubkeyHashTemplate().GenerateScriptPubKey(key.PubKey);
				var hash = tx.Inputs[i].ScriptSig.SignatureHash(tx, i, SigHash.All);
				var sig = key.Sign(hash);
				tx.Inputs[i].ScriptSig = new PayToPubkeyHashTemplate().GenerateScriptSig(new TransactionSignature(sig, SigHash.All), key.PubKey);
			}
			return true;
		}

		public bool SignedByMe(Transaction tx)
		{
			for(int i = 0 ; i < tx.Inputs.Count ; i++)
			{
				var vin = tx.Inputs[i];
				var key = GetKey(vin.ScriptSig.GetSourcePubKey());
				if(key == null)
					return false;
				var pubkeyScript = new PayToPubkeyHashTemplate().GenerateScriptPubKey(key.PubKey);
				var eval = new ScriptEvaluationContext();
				eval.SigHash = SigHash.All;
				if(!eval.VerifyScript(vin.ScriptSig, pubkeyScript, tx, i))
					return false;
			}
			return true;
		}



		Random _RandKey = new Random();
		private Key GetKey()
		{
			return _Keys[_RandKey.Next(0, _Keys.Count)];
		}

		public Key GetKey(PubKey pubKey)
		{
			return _Keys.FirstOrDefault(k => k.PubKey == pubKey);
		}
		public Key GetKey(KeyId pubKeyId)
		{
			return _Keys.FirstOrDefault(k => k.PubKey.ID == pubKeyId);
		}

		public Transaction CreateSend(BitcoinAddress bitcoinAddress, Money value)
		{
			if(bitcoinAddress.Network != Network)
				throw new InvalidOperationException("This bitcoin address does not belong to the network");
			Transaction tx = new Transaction();
			tx.AddOutput(value, bitcoinAddress);
			this.CompleteTx(tx);
			return tx;
		}



		Chain _CurrentChain;

		public bool Update(Chain chain, IndexedBlockStore store)
		{
			if(_CurrentChain == null || !chain.SameTip(_CurrentChain))
			{
				List<ChainedBlock> unprocessed = null;
				Accounts.Update(chain);
				if(_CurrentChain == null)
				{
					_CurrentChain = chain.Clone(new StreamObjectStream<ChainChange>());
					unprocessed = chain.ToEnumerable(false).ToList();
				}
				else
				{
					var fork = _CurrentChain.SetTip(chain.Tip);
					unprocessed = _CurrentChain.EnumerateAfter(fork).ToList();
				}

				foreach(var block in unprocessed)
				{
					ReceiveBlock(store.Get(block.HashBlock));
				}
				return true;
			}
			return false;
		}
	}
}
