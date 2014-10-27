using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public interface ICoinSelector
	{
		IEnumerable<Coin> Select(IEnumerable<Coin> coins, Money target);
	}

	/// <summary>
	/// Algorithm implemented by bitcoin core https://github.com/bitcoin/bitcoin/blob/master/src/wallet.cpp#L1276
	/// Minimize the change
	/// </summary>
	public class DefaultCoinSelector : ICoinSelector
	{
		public DefaultCoinSelector()
		{

		}
		Random _Rand = new Random();
		public DefaultCoinSelector(int seed)
		{
			_Rand = new Random(seed);
		}
		#region ICoinSelector Members

		public IEnumerable<Coin> Select(IEnumerable<Coin> coins, Money target)
		{
			var targetCoin = coins
							.FirstOrDefault(c => c.TxOut.Value == target);
			//If any of your UTXO² matches the Target¹ it will be used.
			if(targetCoin != null)
				return new[] { targetCoin };

			var orderedCoins = coins.OrderBy(s => s.TxOut.Value).ToArray();
			List<Coin> result = new List<Coin>();
			Money total = Money.Zero;

			foreach(var coin in orderedCoins)
			{
				if(coin.TxOut.Value < target && total < target)
				{
					total += coin.TxOut.Value;
					result.Add(coin);
					//If the "sum of all your UTXO smaller than the Target" happens to match the Target, they will be used. (This is the case if you sweep a complete wallet.)
					if(total == target)
						return result;

				}
				else
				{
					if(total < target && coin.TxOut.Value > target)
					{
						//If the "sum of all your UTXO smaller than the Target" doesn't surpass the target, the smallest UTXO greater than your Target will be used.
						return new[] { coin };
					}
					else
					{
						//						Else Bitcoin Core does 1000 rounds of randomly combining unspent transaction outputs until their sum is greater than or equal to the Target. If it happens to find an exact match, it stops early and uses that.
						//Otherwise it finally settles for the minimum of
						//the smallest UTXO greater than the Target
						//the smallest combination of UTXO it discovered in Step 4.
						var allCoins = orderedCoins.ToArray();
						Money minTotal = null;
						List<Coin> minSelection = null;
						for(int _ = 0 ; _ < 1000 ; _++)
						{
							var selection = new List<Coin>();
							Shuffle(allCoins, _Rand);
							total = Money.Zero;
							for(int i = 0 ; i < allCoins.Length ; i++)
							{
								selection.Add(allCoins[i]);
								total += allCoins[i].TxOut.Value;
								if(total == target)
									return selection;
								if(total > target)
									break;
							}
							if(total < target)
							{
								return null;
							}
							if(minTotal == null || total < minTotal)
							{
								minTotal = total;
								minSelection = selection;
							}
						}
					}
				}
			}
			if(total < target)
				return null;
			return result;
		}

		internal static void Shuffle<T>(T[] list, Random random)
		{
			int n = list.Length;
			while(n > 1)
			{
				n--;
				int k = random.Next(n + 1);
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
		}
		internal static void Shuffle<T>(List<T> list, Random random)
		{
			int n = list.Count;
			while(n > 1)
			{
				n--;
				int k = random.Next(n + 1);
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
		}


		#endregion
	}

	public class NotEnoughFundsException : Exception
	{
		public NotEnoughFundsException()
		{
		}
		public NotEnoughFundsException(string message)
			: base(message)
		{
		}
		public NotEnoughFundsException(string message, Exception inner)
			: base(message, inner)
		{
		}
	}
	public class TransactionBuilder
	{
		public TransactionBuilder()
		{
			Fees = Money.Zero;
			_Rand = new Random();
			CoinSelector = new DefaultCoinSelector();
		}
		Random _Rand;
		public TransactionBuilder(int seed)
		{
			Fees = Money.Zero;
			_Rand = new Random(seed);
			CoinSelector = new DefaultCoinSelector(seed);
		}
		public Money Fees
		{
			get;
			set;
		}

		public ICoinSelector CoinSelector
		{
			get;
			set;
		}

		public Script ChangeScript
		{
			get;
			set;
		}
		List<Action<Transaction>> _Builders = new List<Action<Transaction>>();
		List<Coin> _Coins = new List<Coin>();
		public List<Coin> Coins
		{
			get
			{
				return _Coins;
			}
		}

		List<Key> _Keys = new List<Key>();

		public TransactionBuilder AddKeys(params Key[] keys)
		{
			_Keys.AddRange(keys);
			return this;
		}

		public TransactionBuilder AddCoins(params Coin[] coins)
		{
			Coins.AddRange(coins);
			return this;
		}
		public TransactionBuilder SendTo(BitcoinAddress destination, Money money)
		{
			return SendTo(destination.ID, money);
		}

		public TransactionBuilder SendTo(TxDestination id, Money money)
		{
			_Builders.Add(tx =>
			{
				tx.Outputs.Add(new TxOut(money, id.CreateScriptPubKey()));
			});
			return this;
		}

		BitcoinStealthAddress _StealthAddress;
		public TransactionBuilder SendTo(BitcoinStealthAddress address, Money money, Key ephemKey = null)
		{
			if(_StealthAddress != null && _StealthAddress != address)
				throw new InvalidOperationException("Can only pay one stealth address per transaction");
			_StealthAddress = address;
			_SupportTxOutShuffle = false;
			_Builders.Add(tx =>
			{
				var payment = address.CreatePayment(ephemKey);
				payment.AddToTransaction(tx, money);
			});
			return this;
		}

		public TransactionBuilder SetFees(Money fees)
		{
			if(fees == null)
				throw new ArgumentNullException("fees");
			Fees = fees;
			return this;
		}

		public TransactionBuilder SendChange(BitcoinAddress destination)
		{
			return SendChange(destination.ID);
		}

		private TransactionBuilder SendChange(TxDestination destination)
		{
			if(destination == null)
				throw new ArgumentNullException("destination");
			ChangeScript = destination.CreateScriptPubKey();
			return this;
		}
		public TransactionBuilder SetCoinSelector(ICoinSelector selector)
		{
			if(selector == null)
				throw new ArgumentNullException("selector");
			CoinSelector = selector;
			return this;
		}

		bool _SupportTxOutShuffle = true;

		public Transaction BuildTransaction(bool sign)
		{
			Transaction tx = new Transaction();
			foreach(var builder in _Builders)
				builder(tx);
			var target = tx.TotalOut + Fees;
			var selection = CoinSelector.Select(Coins, target);
			if(selection == null)
				throw new NotEnoughFundsException("Not enough fund to cover the target");
			var total = selection.Select(s => s.TxOut.Value).Sum();
			if(total < target)
				throw new NotEnoughFundsException("Not enough fund to cover the target");
			if(total != target)
			{
				if(ChangeScript == null)
					throw new InvalidOperationException("A change address should be specified");
				tx.Outputs.Add(new TxOut(total - target, ChangeScript));
			}
			if(_SupportTxOutShuffle)
				DefaultCoinSelector.Shuffle(tx.Outputs, _Rand);
			foreach(var coin in selection)
			{
				tx.AddInput(new TxIn(coin.Outpoint));
			}
			if(sign)
			{
				int i = 0;
				foreach(var coin in selection)
				{
					Sign(tx, tx.Inputs[i], coin, i);
					i++;
				}
			}
			return tx;
		}
		public Transaction SignTransaction(Transaction transaction)
		{
			var tx = transaction.Clone();
			SignTransactionInPlace(tx);
			return tx;
		}
		public void SignTransactionInPlace(Transaction transaction)
		{
			for(int i = 0 ; i < transaction.Inputs.Count ; i++)
			{
				var txIn = transaction.Inputs[i];
				var coin = FindCoin(txIn.PrevOut);
				if(coin != null)
				{
					Sign(transaction, txIn, coin, i);
				}
			}
		}

		public bool Verify(Transaction tx)
		{
			for(int i = 0 ; i < tx.Inputs.Count ; i++)
			{
				var txIn = tx.Inputs[i];
				var coin = FindCoin(txIn.PrevOut);
				if(coin == null)
					throw new KeyNotFoundException("Impossible to find the scriptPubKey of outpoint " + txIn.PrevOut);
				if(!Script.VerifyScript(txIn.ScriptSig, coin.TxOut.ScriptPubKey, tx, i))
					return false;
			}
			return true;
		}

		private Coin FindCoin(OutPoint outPoint)
		{
			return _Coins.FirstOrDefault(c => c.Outpoint == outPoint);
		}

		readonly static PayToScriptHashTemplate payToScriptHash = new PayToScriptHashTemplate();
		readonly static PayToPubkeyHashTemplate payToPubKeyHash = new PayToPubkeyHashTemplate();
		readonly static PayToPubkeyTemplate payToPubKey = new PayToPubkeyTemplate();
		readonly static PayToMultiSigTemplate payToMultiSig = new PayToMultiSigTemplate();

		private void Sign(Transaction tx, TxIn input, Coin coin, int n)
		{
			if(payToScriptHash.CheckScriptPubKey(coin.TxOut.ScriptPubKey))
			{
				var scriptCoin = coin as ScriptCoin;
				if(scriptCoin == null)
				{
					//Try to extract redeem from this transaction
					var p2shParams = payToScriptHash.ExtractScriptSigParameters(input.ScriptSig);
					if(p2shParams == null)
						throw new InvalidOperationException("A coin with a P2SH scriptPubKey was detected, however this coin is not a ScriptCoin");
					else
					{
						scriptCoin = new ScriptCoin(coin.Outpoint, coin.TxOut, p2shParams.RedeemScript);
					}
				}

				var original = input.ScriptSig;
				input.ScriptSig = CreateScriptSig(tx, input, coin, n, scriptCoin.Redeem);
				if(original != input.ScriptSig)
				{
					var ops = input.ScriptSig.ToOps().ToList();
					ops.Add(Op.GetPushOp(scriptCoin.Redeem.ToRawScript(true)));
					input.ScriptSig = new Script(ops.ToArray());
				}
			}
			else if(coin is StealthCoin)
			{
				var stealthCoin = (StealthCoin)coin;
				List<Key> tempKeys = new List<Key>();
				var scanKey = FindKey(stealthCoin.Address.ScanPubKey);
				if(scanKey == null)
					throw new KeyNotFoundException("Scan key for decrypting StealthCoin not found");
				foreach(var key in stealthCoin.Address.SpendPubKeys.Select(p => FindKey(p)).Where(p => p != null))
				{
					tempKeys.Add(key.Uncover(scanKey, stealthCoin.StealthMetadata.EphemKey));
				}
				_Keys.AddRange(tempKeys);
				try
				{
					input.ScriptSig = CreateScriptSig(tx, input, coin, n, coin.TxOut.ScriptPubKey);

				}
				finally
				{
					foreach(var tempKey in tempKeys)
					{
						_Keys.Remove(tempKey);
					}
				}
			}
			else
			{
				input.ScriptSig = CreateScriptSig(tx, input, coin, n, coin.TxOut.ScriptPubKey);
			}
		}


		private Script CreateScriptSig(Transaction tx, TxIn input, Coin coin, int n, Script scriptPubKey)
		{
			var originalScriptSig = input.ScriptSig;
			input.ScriptSig = scriptPubKey;

			var pubKeyHashParams = payToPubKeyHash.ExtractScriptPubKeyParameters(scriptPubKey);
			if(pubKeyHashParams != null)
			{
				var key = FindKey(pubKeyHashParams);
				if(key == null)
					return originalScriptSig;
				var hash = input.ScriptSig.SignatureHash(tx, n, SigHash.All);
				var sig = key.Sign(hash);
				return payToPubKeyHash.GenerateScriptSig(new TransactionSignature(sig, SigHash.All), key.PubKey);
			}

			var multiSigParams = payToMultiSig.ExtractScriptPubKeyParameters(scriptPubKey);
			if(multiSigParams != null)
			{
				var alreadySigned = payToMultiSig.ExtractScriptSigParameters(originalScriptSig);
				if(alreadySigned == null && !Script.IsNullOrEmpty(originalScriptSig)) //Maybe a P2SH
				{
					var ops = originalScriptSig.ToOps().ToList();
					ops.RemoveAt(ops.Count - 1);
					alreadySigned = payToMultiSig.ExtractScriptSigParameters(new Script(ops.ToArray()));
				}
				List<TransactionSignature> signatures = new List<TransactionSignature>();
				if(alreadySigned != null)
				{
					signatures.AddRange(alreadySigned);
				}
				var keys =
					multiSigParams
					.PubKeys
					.Select(p => FindKey(p))
					.ToArray();

				int sigCount = signatures.Where(s => s != TransactionSignature.Empty).Count();
				for(int i = 0 ; i < keys.Length ; i++)
				{
					if(sigCount == multiSigParams.SignatureCount)
						break;

					if(i >= signatures.Count)
					{
						signatures.Add(TransactionSignature.Empty);
					}
					if(keys[i] != null)
					{
						var hash = input.ScriptSig.SignatureHash(tx, n, SigHash.All);
						var sig = keys[i].Sign(hash);
						signatures[i] = new TransactionSignature(sig, SigHash.All);
						sigCount++;
					}
				}

				if(sigCount == multiSigParams.SignatureCount)
				{
					signatures = signatures.Where(s => s != TransactionSignature.Empty).ToList();
				}

				return payToMultiSig.GenerateScriptSig(
					signatures.ToArray());
			}

			var pubKeyParams = payToPubKey.ExtractScriptPubKeyParameters(scriptPubKey);
			if(pubKeyParams != null)
			{
				var key = FindKey(pubKeyParams);
				if(key == null)
					return originalScriptSig;
				var hash = input.ScriptSig.SignatureHash(tx, n, SigHash.All);
				var sig = key.Sign(hash);
				return payToPubKey.GenerateScriptSig(new TransactionSignature(sig, SigHash.All));
			}

			throw new NotSupportedException("Unsupported scriptPubKey");
		}


		private Key FindKey(TxDestination id)
		{
			return _Keys.FirstOrDefault(k => k.PubKey.ID == id);
		}

		private Key FindKey(PubKey pubKeyParams)
		{
			return _Keys.FirstOrDefault(k => k.PubKey == pubKeyParams);
		}
	}
}
