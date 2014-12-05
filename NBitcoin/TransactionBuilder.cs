using NBitcoin.DataEncoders;
using NBitcoin.OpenAsset;
using NBitcoin.Stealth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Builder = System.Func<NBitcoin.TransactionBuilder.TransactionBuildingContext, NBitcoin.Money>;

namespace NBitcoin
{
	[Flags]
	public enum ChangeType : int
	{
		All = 3,
		Colored = 1,
		Uncolored = 2
	}
	public interface ICoinSelector
	{
		IEnumerable<ICoin> Select(IEnumerable<ICoin> coins, Money target);
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

		public IEnumerable<ICoin> Select(IEnumerable<ICoin> coins, Money target)
		{
			var targetCoin = coins
							.FirstOrDefault(c => c.Amount == target);
			//If any of your UTXO² matches the Target¹ it will be used.
			if(targetCoin != null)
				return new[] { targetCoin };

			List<ICoin> result = new List<ICoin>();
			Money total = Money.Zero;

			if(target == Money.Zero)
				return result;

			var orderedCoins = coins.OrderBy(s => s.Amount).ToArray();

			foreach(var coin in orderedCoins)
			{
				if(coin.Amount < target && total < target)
				{
					total += coin.Amount;
					result.Add(coin);
					//If the "sum of all your UTXO smaller than the Target" happens to match the Target, they will be used. (This is the case if you sweep a complete wallet.)
					if(total == target)
						return result;

				}
				else
				{
					if(total < target && coin.Amount > target)
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
						List<ICoin> minSelection = null;
						for(int _ = 0 ; _ < 1000 ; _++)
						{
							var selection = new List<ICoin>();
							Shuffle(allCoins, _Rand);
							total = Money.Zero;
							for(int i = 0 ; i < allCoins.Length ; i++)
							{
								selection.Add(allCoins[i]);
								total += allCoins[i].Amount;
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
		internal class TransactionSigningContext
		{
			public TransactionSigningContext(TransactionBuilder builder, Transaction transaction)
			{
				Builder = builder;
				Transaction = transaction;
			}

			public Transaction Transaction
			{
				get;
				set;
			}
			public TransactionBuilder Builder
			{
				get;
				set;
			}

			private readonly List<Key> _AdditionalKeys = new List<Key>();
			public List<Key> AdditionalKeys
			{
				get
				{
					return _AdditionalKeys;
				}
			}

			public SigHash SigHash
			{
				get;
				set;
			}
		}
		internal class TransactionBuildingContext
		{
			public TransactionBuildingContext(TransactionBuilder builder)
			{
				Builder = builder;
				Transaction = new Transaction();
				ChangeAmount = Money.Zero;
				AdditionalFees = Money.Zero;
			}
			public TransactionBuilder.BuilderGroup Group
			{
				get;
				set;
			}
			public TransactionBuilder Builder
			{
				get;
				set;
			}
			public Transaction Transaction
			{
				get;
				set;
			}

			public Money AdditionalFees
			{
				get;
				set;
			}

			private readonly List<Builder> _AdditionalBuilders = new List<Builder>();
			public List<Builder> AdditionalBuilders
			{
				get
				{
					return _AdditionalBuilders;
				}
			}

			ColorMarker _Marker;

			public ColorMarker GetColorMarker(bool issuance)
			{
				if(_Marker == null)
					_Marker = new ColorMarker();
				if(!issuance)
					EnsureMarkerInserted();
				return _Marker;
			}

			private TxOut EnsureMarkerInserted()
			{
				uint position;
				if(ColorMarker.Get(Transaction, out position) != null)
					return Transaction.Outputs[position];
				var txout = Transaction.AddOutput(new TxOut()
				{
					ScriptPubKey = new ColorMarker().GetScript()
				});
				txout.Value = Money.Zero;
				return txout;
			}

			public void Finish()
			{
				if(_Marker != null)
				{
					var txout = EnsureMarkerInserted();
					txout.ScriptPubKey = _Marker.GetScript();
				}
			}

			public IssuanceCoin IssuanceCoin
			{
				get;
				set;
			}

			public Money ChangeAmount
			{
				get;
				set;
			}

			public TransactionBuildingContext CreateMemento()
			{
				var memento = new TransactionBuildingContext(Builder);
				memento.RestoreMemento(this);
				return memento;
			}

			public void RestoreMemento(TransactionBuildingContext memento)
			{
				_Marker = memento._Marker == null ? null : new ColorMarker(memento._Marker.GetScript());
				Transaction = memento.Transaction.Clone();
				AdditionalFees = memento.AdditionalFees;
			}

			public bool NonFinalSequenceSet
			{
				get;
				set;
			}

			public Money CoverOnly
			{
				get;
				set;
			}

			public Money Dust
			{
				get;
				set;
			}

			public ChangeType ChangeType
			{
				get;
				set;
			}
		}

		internal class BuilderGroup
		{
			TransactionBuilder _Parent;
			public BuilderGroup(TransactionBuilder parent)
			{
				_Parent = parent;
				Builders.Add(SetChange);
			}

			Money SetChange(TransactionBuildingContext ctx)
			{
				if(ctx.ChangeAmount == Money.Zero)
					return Money.Zero;
				ctx.Transaction.AddOutput(new TxOut(ctx.ChangeAmount, ctx.Group.ChangeScript[(int)ChangeType.Uncolored]));
				return ctx.ChangeAmount;
			}
			internal List<Builder> Builders = new List<Builder>();
			internal Dictionary<OutPoint, ICoin> Coins = new Dictionary<OutPoint, ICoin>();
			internal List<Builder> IssuanceBuilders = new List<Builder>();
			internal Dictionary<AssetId, List<Builder>> BuildersByAsset = new Dictionary<AssetId, List<Builder>>();
			internal Script[] ChangeScript = new Script[3];
			internal void Shuffle()
			{
				Shuffle(Builders);
				foreach(var builders in BuildersByAsset)
					Shuffle(builders.Value);
				Shuffle(IssuanceBuilders);
			}
			private void Shuffle(List<Builder> builders)
			{
				DefaultCoinSelector.Shuffle(builders, _Parent._Rand);
			}

			public Money CoverOnly
			{
				get;
				set;
			}
		}

		List<BuilderGroup> _BuilderGroups = new List<BuilderGroup>();
		BuilderGroup _CurrentGroup = null;
		internal BuilderGroup CurrentGroup
		{
			get
			{
				if(_CurrentGroup == null)
				{
					_CurrentGroup = new BuilderGroup(this);
					_BuilderGroups.Add(_CurrentGroup);
				}
				return _CurrentGroup;
			}
		}
		public TransactionBuilder()
		{
			_Rand = new Random();
			CoinSelector = new DefaultCoinSelector();
			ColoredDust = Money.Dust;
		}
		internal Random _Rand;
		public TransactionBuilder(int seed)
		{
			_Rand = new Random(seed);
			CoinSelector = new DefaultCoinSelector(seed);
			ColoredDust = Money.Dust;
		}

		public ICoinSelector CoinSelector
		{
			get;
			set;
		}

		public Func<OutPoint, ICoin> CoinFinder
		{
			get;
			set;
		}

		LockTime? _LockTime;
		public TransactionBuilder SetLockTime(LockTime lockTime)
		{
			_LockTime = lockTime;
			return this;
		}

		List<Key> _Keys = new List<Key>();

		public TransactionBuilder AddKeys(params BitcoinSecret[] keys)
		{
			_Keys.AddRange(keys.Select(k => k.Key));
			return this;
		}

		public TransactionBuilder AddKeys(params Key[] keys)
		{
			_Keys.AddRange(keys);
			return this;
		}

		public TransactionBuilder AddCoins(params ICoin[] coins)
		{
			return AddCoins((IEnumerable<ICoin>)coins);
		}

		public TransactionBuilder AddCoins(IEnumerable<ICoin> coins)
		{
			foreach(var coin in coins)
			{
				CurrentGroup.Coins.AddOrReplace(coin.Outpoint, coin);
			}
			return this;
		}

		public TransactionBuilder Send(BitcoinAddress destination, Money amount)
		{
			return Send(destination.ID, amount);
		}

		public TransactionBuilder Send(TxDestination id, Money amount)
		{
			return Send(id.CreateScriptPubKey(), amount);
		}
		public TransactionBuilder Send(Script scriptPubKey, Money amount)
		{
			CurrentGroup.Builders.Add(ctx =>
			{
				ctx.Transaction.Outputs.Add(new TxOut(amount, scriptPubKey));
				return amount;
			});
			return this;
		}

		public TransactionBuilder SendAsset(BitcoinAddress destination, Asset asset)
		{
			return SendAsset(destination.ID, asset);
		}

		public TransactionBuilder SendAsset(BitcoinAddress destination, AssetId assetId, ulong quantity)
		{
			return SendAsset(destination.ID, new Asset(assetId, quantity));
		}

		public TransactionBuilder SendAsset(PubKey pubKey, Asset asset)
		{
			return SendAsset(pubKey.PaymentScript, asset);
		}

		public TransactionBuilder SendAsset(PubKey pubKey, AssetId assetId, ulong quantity)
		{
			return SendAsset(pubKey.PaymentScript, new Asset(assetId, quantity));
		}

		public TransactionBuilder SendAsset(TxDestination id, Asset asset)
		{
			return SendAsset(id.CreateScriptPubKey(), asset);
		}

		public TransactionBuilder SendAsset(TxDestination id, AssetId assetId, ulong quantity)
		{
			return SendAsset(id.CreateScriptPubKey(), new Asset(assetId, quantity));
		}

		public TransactionBuilder Shuffle()
		{
			DefaultCoinSelector.Shuffle(_BuilderGroups, _Rand);
			foreach(var group in _BuilderGroups)
				group.Shuffle();
			return this;
		}

		Money SetColoredChange(TransactionBuildingContext ctx)
		{
			if(ctx.ChangeAmount == Money.Zero)
				return Money.Zero;
			var marker = ctx.GetColorMarker(false);
			var txout = ctx.Transaction.AddOutput(new TxOut(ColoredDust, ctx.Group.ChangeScript[(int)ChangeType.Colored]));
			marker.SetQuantity(ctx.Transaction.Outputs.Count - 2, ctx.ChangeAmount);
			ctx.AdditionalFees += ColoredDust;
			return ctx.ChangeAmount;
		}

		public TransactionBuilder SendAsset(Script scriptPubKey, AssetId assetId, ulong assetQuantity)
		{
			return SendAsset(scriptPubKey, new Asset(assetId, assetQuantity));
		}

		public TransactionBuilder SendAsset(Script scriptPubKey, Asset asset)
		{
			AssertOpReturn("Colored Coin");
			var builders = CurrentGroup.BuildersByAsset.TryGet(asset.Id);
			if(builders == null)
			{
				builders = new List<Builder>();
				CurrentGroup.BuildersByAsset.Add(asset.Id, builders);
				builders.Add(SetColoredChange);
			}
			builders.Add(ctx =>
			{
				var marker = ctx.GetColorMarker(false);
				ctx.Transaction.AddOutput(new TxOut(ColoredDust, scriptPubKey));
				marker.SetQuantity(ctx.Transaction.Outputs.Count - 2, asset.Quantity);
				ctx.AdditionalFees += ColoredDust;
				return asset.Quantity;
			});
			return this;
		}

		public Money ColoredDust
		{
			get;
			set;
		}


		string _OpReturnUser;
		private void AssertOpReturn(string name)
		{
			if(_OpReturnUser == null)
			{
				_OpReturnUser = name;
			}
			else
			{
				if(_OpReturnUser != name)
					throw new InvalidOperationException("Op return already used for " + _OpReturnUser);
			}
		}

		public TransactionBuilder Send(BitcoinStealthAddress address, Money money, Key ephemKey = null)
		{
			if(_OpReturnUser == null)
				_OpReturnUser = "Stealth Payment";
			else
				throw new InvalidOperationException("Op return already used for " + _OpReturnUser);

			CurrentGroup.Builders.Add(ctx =>
			{
				var payment = address.CreatePayment(ephemKey);
				payment.AddToTransaction(ctx.Transaction, money);
				return money;
			});
			return this;
		}

		public TransactionBuilder IssueAsset(BitcoinAddress address, Asset asset)
		{
			return IssueAsset(address.ID, asset);
		}

		public TransactionBuilder IssueAsset(TxDestination destination, Asset asset)
		{
			return IssueAsset(destination.CreateScriptPubKey(), asset);
		}
		public TransactionBuilder IssueAsset(PubKey destination, Asset asset)
		{
			return IssueAsset(destination.PaymentScript, asset);
		}

		AssetId _IssuedAsset;

		public TransactionBuilder IssueAsset(Script scriptPubKey, Asset asset)
		{
			AssertOpReturn("Colored Coin");
			if(_IssuedAsset == null)
				_IssuedAsset = asset.Id;
			else if(_IssuedAsset != asset.Id)
				throw new InvalidOperationException("You can issue only one asset type in a transaction");

			CurrentGroup.IssuanceBuilders.Add(ctx =>
			{
				var marker = ctx.GetColorMarker(true);
				if(ctx.IssuanceCoin == null)
				{
					var issuance = ctx.Group.Coins.Values.OfType<IssuanceCoin>().Where(i => i.AssetId == asset.Id).FirstOrDefault();
					if(issuance == null)
						throw new InvalidOperationException("No issuance coin for emitting asset found");
					ctx.IssuanceCoin = issuance;
					ctx.Transaction.Inputs.Insert(0, new TxIn(issuance.Outpoint));
					ctx.AdditionalFees -= issuance.Bearer.Amount;
				}

				ctx.Transaction.AddOutput(ColoredDust, scriptPubKey);
				marker.SetQuantity(ctx.Transaction.Outputs.Count - 1, asset.Quantity);
				ctx.AdditionalFees += ColoredDust;
				return asset.Quantity;
			});
			return this;
		}

		public TransactionBuilder SendFees(Money fees)
		{
			if(fees == null)
				throw new ArgumentNullException("fees");
			CurrentGroup.Builders.Add(ctx => fees);
			return this;
		}

		public TransactionBuilder SetChange(BitcoinAddress destination, ChangeType changeType = ChangeType.All)
		{
			return SetChange(destination.ID, changeType);
		}

		public TransactionBuilder SetChange(TxDestination destination, ChangeType changeType = ChangeType.All)
		{
			return SetChange(destination.CreateScriptPubKey(), changeType);
		}

		public TransactionBuilder SetChange(PubKey pubKey, ChangeType changeType = ChangeType.All)
		{
			return SetChange(pubKey.PaymentScript, changeType);
		}

		public TransactionBuilder SetChange(Script scriptPubKey, ChangeType changeType = ChangeType.All)
		{
			if((changeType & ChangeType.Colored) != 0)
			{
				CurrentGroup.ChangeScript[(int)ChangeType.Colored] = scriptPubKey;
			}
			if((changeType & ChangeType.Uncolored) != 0)
			{
				CurrentGroup.ChangeScript[(int)ChangeType.Uncolored] = scriptPubKey;
			}
			return this;
		}

		public TransactionBuilder SetCoinSelector(ICoinSelector selector)
		{
			if(selector == null)
				throw new ArgumentNullException("selector");
			CoinSelector = selector;
			return this;
		}

		public Transaction BuildTransaction(bool sign)
		{
			return BuildTransaction(sign, SigHash.All);
		}
		public Transaction BuildTransaction(bool sign, SigHash sigHash)
		{
			TransactionBuildingContext ctx = new TransactionBuildingContext(this);
			if(_CompletedTransaction != null)
				ctx.Transaction = _CompletedTransaction;
			if(_LockTime != null && _LockTime.HasValue)
				ctx.Transaction.LockTime = _LockTime.Value;
			foreach(var group in _BuilderGroups)
			{
				ctx.Group = group;
				ctx.AdditionalBuilders.Clear();
				ctx.AdditionalFees = Money.Zero;

				ctx.ChangeType = ChangeType.Colored;
				foreach(var builder in group.IssuanceBuilders)
					builder(ctx);

				var buildersByAsset = group.BuildersByAsset.ToList();
				foreach(var builders in buildersByAsset)
				{
					var coins = group.Coins.Values.OfType<ColoredCoin>().Where(c => c.Asset.Id == builders.Key).OfType<ICoin>();

					ctx.Dust = Money.Zero;
					ctx.CoverOnly = null;
					var btcSpent = BuildTransaction(ctx, group, builders.Value, coins)
						.OfType<IColoredCoin>().Select(c => c.Bearer.Amount).Sum();
					ctx.AdditionalFees -= btcSpent;
				}

				ctx.AdditionalBuilders.Add(_ => _.AdditionalFees);
				ctx.Dust = Money.Dust;
				ctx.CoverOnly = group.CoverOnly;
				ctx.ChangeType = ChangeType.Uncolored;
				BuildTransaction(ctx, group, group.Builders, group.Coins.Values.OfType<Coin>());
			}
			ctx.Finish();

			if(sign)
			{
				SignTransactionInPlace(ctx.Transaction, sigHash);
			}
			return ctx.Transaction;
		}

		private IEnumerable<ICoin> BuildTransaction(
			TransactionBuildingContext ctx,
			BuilderGroup group,
			IEnumerable<Builder> builders,
			IEnumerable<ICoin> coins)
		{
			var originalCtx = ctx.CreateMemento();
			var target = builders.Concat(ctx.AdditionalBuilders).Select(b => b(ctx)).Sum();
			if(ctx.CoverOnly != null)
			{
				target = ctx.CoverOnly + ctx.ChangeAmount;
			}
			var selection = CoinSelector.Select(coins, target);
			if(selection == null)
				throw new NotEnoughFundsException("Not enough fund to cover the target");
			var total = selection.Select(s => s.Amount).Sum();
			var change = total - target;
			if(change < Money.Zero)
				throw new NotEnoughFundsException("Not enough fund to cover the target");
			if(change > ctx.Dust)
			{
				if(group.ChangeScript[(int)ctx.ChangeType] == null)
					throw new InvalidOperationException("A change address should be specified (" + ctx.ChangeType + ")");

				ctx.RestoreMemento(originalCtx);
				ctx.ChangeAmount = change;
				try
				{
					return BuildTransaction(ctx, group, builders, coins);
				}
				finally
				{
					ctx.ChangeAmount = Money.Zero;
				}
			}
			foreach(var coin in selection)
			{
				var input = ctx.Transaction.AddInput(new TxIn(coin.Outpoint));
				if(_LockTime != null && _LockTime.HasValue && !ctx.NonFinalSequenceSet)
				{
					input.Sequence = 0;
					ctx.NonFinalSequenceSet = true;
				}
			}
			return selection;
		}

		public Transaction SignTransaction(Transaction transaction, SigHash sigHash)
		{
			var tx = transaction.Clone();
			SignTransactionInPlace(tx, sigHash);
			return tx;
		}

		public Transaction SignTransaction(Transaction transaction)
		{
			return SignTransaction(transaction, SigHash.All);
		}
		public Transaction SignTransactionInPlace(Transaction transaction)
		{
			return SignTransactionInPlace(transaction, SigHash.All);
		}
		public Transaction SignTransactionInPlace(Transaction transaction, SigHash sigHash)
		{
			TransactionSigningContext ctx = new TransactionSigningContext(this, transaction);
			ctx.SigHash = sigHash;
			for(int i = 0 ; i < transaction.Inputs.Count ; i++)
			{
				var txIn = transaction.Inputs[i];
				var coin = FindSignableCoin(txIn);
				if(coin != null)
				{
					Sign(ctx, txIn, coin, i);
				}
			}
			return transaction;
		}

		public ICoin FindSignableCoin(TxIn txIn)
		{
			var coin = FindCoin(txIn.PrevOut);
			if(coin == null)
				return coin;
			if(coin is IColoredCoin)
				coin = ((IColoredCoin)coin).Bearer;

			if(PayToScriptHashTemplate.Instance.CheckScriptPubKey(coin.ScriptPubKey))
			{
				var scriptCoin = coin as IScriptCoin;
				if(scriptCoin == null)
				{
					var expectedId = PayToScriptHashTemplate.Instance.ExtractScriptPubKeyParameters(coin.ScriptPubKey);
					//Try to extract redeem from this transaction
					var p2shParams = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(txIn.ScriptSig);
					if(p2shParams == null || p2shParams.RedeemScript.ID != expectedId)
					{
						var redeem = _ScriptIdToRedeem.TryGet(expectedId);
						if(redeem == null)
							return null;
						//throw new InvalidOperationException("A coin with a P2SH scriptPubKey was detected, however this coin is not a ScriptCoin, and no information about the redeem script was found in the input, and from the KnownRedeems");
						else
							return new ScriptCoin(coin.Outpoint, ((Coin)coin).TxOut, redeem);
					}
					else
					{
						return new ScriptCoin(coin.Outpoint, ((Coin)coin).TxOut, p2shParams.RedeemScript);
					}
				}
			}
			return coin;
		}

		public bool Verify(Transaction tx, Money expectFees = null)
		{
			Money spent = Money.Zero;
			for(int i = 0 ; i < tx.Inputs.Count ; i++)
			{
				var txIn = tx.Inputs[i];
				var coin = FindCoin(txIn.PrevOut);
				if(coin == null)
					throw CoinNotFound(txIn);
				spent += coin is IColoredCoin ? ((IColoredCoin)coin).Bearer.Amount : coin.Amount;
				if(!Script.VerifyScript(txIn.ScriptSig, coin.ScriptPubKey, tx, i))
					return false;
			}
			if(spent < tx.TotalOut)
				throw new NotEnoughFundsException("Not enough funds in this transaction");

			var fees = (spent - tx.TotalOut);
			if(expectFees != null && expectFees != fees)
				throw new NotEnoughFundsException("Fees different than expect (" + fees.ToString() + ")");
			return true;
		}

		private Exception CoinNotFound(TxIn txIn)
		{
			return new KeyNotFoundException("Impossible to find the scriptPubKey of outpoint " + txIn.PrevOut);
		}

		public ICoin FindCoin(OutPoint outPoint)
		{
			var result = _BuilderGroups.Select(c => c.Coins.TryGet(outPoint)).Where(r => r != null).SingleOrDefault();
			if(result == null && CoinFinder != null)
				result = CoinFinder(outPoint);
			return result;
		}

		public int EstimateSize(Transaction tx)
		{
			var clone = tx.Clone();
			clone.Inputs.Clear();
			var baseSize = clone.ToBytes().Length;

			int inputSize = 0;
			for(int i = 0 ; i < tx.Inputs.Count ; i++)
			{
				var txin = tx.Inputs[i];
				var coin = FindCoin(txin.PrevOut);
				if(coin == null)
					throw CoinNotFound(txin);
				inputSize += EstimateScriptSigSize(coin) + 41;
			}

			return baseSize + inputSize;
		}

		static PubKey DummyPubKey = new PubKey(Encoders.Hex.DecodeData("022c2b9e61169fb1b1f2f3ff15ad52a21745e268d358ba821d36da7d7cd92dee0e"));
		static TransactionSignature DummySignature = new TransactionSignature(Encoders.Hex.DecodeData("3045022100b9d685584f46554977343009c04b3091e768c23884fa8d2ce2fb59e5290aa45302203b2d49201c7f695f434a597342eb32dfd81137014fcfb3bb5edc7a19c77774d201"));
		private int EstimateScriptSigSize(ICoin coin)
		{
			if(coin is IColoredCoin)
				coin = ((IColoredCoin)coin).Bearer;

			int size = 0;
			if(coin is ScriptCoin)
			{
				var scriptCoin = (ScriptCoin)coin;
				coin = new Coin(scriptCoin.Outpoint, new TxOut(scriptCoin.Amount, scriptCoin.Redeem));
				size += new Script(Op.GetPushOp(scriptCoin.Redeem.ToRawScript(true))).Length;
			}

			var p2pk = PayToPubkeyTemplate.Instance.ExtractScriptPubKeyParameters(coin.ScriptPubKey);
			if(p2pk != null)
			{
				size += PayToPubkeyTemplate.Instance.GenerateScriptSig(DummySignature).Length;
				return size;
			}

			var p2pkh = PayToPubkeyHashTemplate.Instance.ExtractScriptPubKeyParameters(coin.ScriptPubKey);
			if(p2pkh != null)
			{
				size += PayToPubkeyHashTemplate.Instance.GenerateScriptSig(DummySignature, DummyPubKey).Length;
				return size;
			}

			var p2mk = PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters(coin.ScriptPubKey);
			if(p2mk != null)
			{
				size += PayToMultiSigTemplate.Instance.GenerateScriptSig(Enumerable.Range(0, p2mk.SignatureCount).Select(o => DummySignature).ToArray()).Length;
				return size;
			}

			size += coin.ScriptPubKey.Length; //Using heurestic to approximate size of unknown scriptPubKey
			return size;
		}

		/// <summary>
		/// Estimate fees of an unsigned transaction
		/// </summary>
		/// <param name="tx"></param>
		/// <returns></returns>
		public Money EstimateFees(Transaction tx)
		{
			var len = EstimateSize(tx);
			long nBaseFee = 10000;
			long nMinFee = (1 + (long)len / 1000) * nBaseFee;
			return new Money(nMinFee);
		}

		private void Sign(TransactionSigningContext ctx, TxIn input, ICoin coin, int n)
		{

			if(coin is StealthCoin)
			{
				var stealthCoin = (StealthCoin)coin;
				var scanKey = FindKey(ctx, stealthCoin.Address.ScanPubKey);
				if(scanKey == null)
					throw new KeyNotFoundException("Scan key for decrypting StealthCoin not found");
				var spendKeys = stealthCoin.Address.SpendPubKeys.Select(p => FindKey(ctx, p)).Where(p => p != null).ToArray();
				ctx.AdditionalKeys.AddRange(stealthCoin.Uncover(spendKeys, scanKey));
			}

			if(PayToScriptHashTemplate.Instance.CheckScriptPubKey(coin.ScriptPubKey))
			{
				var scriptCoin = (IScriptCoin)coin;
				var original = input.ScriptSig;
				input.ScriptSig = CreateScriptSig(ctx, input, coin, n, scriptCoin.Redeem);
				if(original != input.ScriptSig)
				{
					input.ScriptSig = input.ScriptSig + Op.GetPushOp(scriptCoin.Redeem.ToRawScript(true));
				}
			}
			else
			{
				input.ScriptSig = CreateScriptSig(ctx, input, coin, n, coin.ScriptPubKey);
			}

		}

		private Script CreateScriptSig(TransactionSigningContext ctx, TxIn input, ICoin coin, int n, Script scriptPubKey)
		{
			var originalScriptSig = input.ScriptSig;
			input.ScriptSig = scriptPubKey;

			var pubKeyHashParams = PayToPubkeyHashTemplate.Instance.ExtractScriptPubKeyParameters(scriptPubKey);
			if(pubKeyHashParams != null)
			{
				var key = FindKey(ctx, pubKeyHashParams);
				if(key == null)
					return originalScriptSig;
				var hash = input.ScriptSig.SignatureHash(ctx.Transaction, n, ctx.SigHash);
				var sig = key.Sign(hash);
				return PayToPubkeyHashTemplate.Instance.GenerateScriptSig(new TransactionSignature(sig, ctx.SigHash), key.PubKey);
			}

			var multiSigParams = PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters(scriptPubKey);
			if(multiSigParams != null)
			{
				var alreadySigned = PayToMultiSigTemplate.Instance.ExtractScriptSigParameters(originalScriptSig);
				if(alreadySigned == null && !Script.IsNullOrEmpty(originalScriptSig)) //Maybe a P2SH
				{
					var ops = originalScriptSig.ToOps().ToList();
					ops.RemoveAt(ops.Count - 1);
					alreadySigned = PayToMultiSigTemplate.Instance.ExtractScriptSigParameters(new Script(ops));
				}
				List<TransactionSignature> signatures = new List<TransactionSignature>();
				if(alreadySigned != null)
				{
					signatures.AddRange(alreadySigned);
				}
				var keys =
					multiSigParams
					.PubKeys
					.Select(p => FindKey(ctx, p))
					.ToArray();

				int sigCount = signatures.Where(s => s != TransactionSignature.Empty && s != null).Count();
				for(int i = 0 ; i < keys.Length ; i++)
				{
					if(sigCount == multiSigParams.SignatureCount)
						break;

					if(i >= signatures.Count)
					{
						signatures.Add(null);
					}
					if(keys[i] != null)
					{
						var hash = input.ScriptSig.SignatureHash(ctx.Transaction, n, ctx.SigHash);
						var sig = keys[i].Sign(hash);
						signatures[i] = new TransactionSignature(sig, ctx.SigHash);
						sigCount++;
					}
				}

				IEnumerable<TransactionSignature> sigs = signatures;
				if(sigCount == multiSigParams.SignatureCount)
				{
					sigs = sigs.Where(s => s != TransactionSignature.Empty && s != null);
				}

				return PayToMultiSigTemplate.Instance.GenerateScriptSig(sigs);
			}

			var pubKeyParams = PayToPubkeyTemplate.Instance.ExtractScriptPubKeyParameters(scriptPubKey);
			if(pubKeyParams != null)
			{
				var key = FindKey(ctx, pubKeyParams);
				if(key == null)
					return originalScriptSig;
				var hash = input.ScriptSig.SignatureHash(ctx.Transaction, n, ctx.SigHash);
				var sig = key.Sign(hash);
				return PayToPubkeyTemplate.Instance.GenerateScriptSig(new TransactionSignature(sig, ctx.SigHash));
			}

			throw new NotSupportedException("Unsupported scriptPubKey");
		}


		private Key FindKey(TransactionSigningContext ctx, TxDestination id)
		{
			return _Keys
					.Concat(ctx.AdditionalKeys)
					.FirstOrDefault(k => k.PubKey.ID == id);
		}

		private Key FindKey(TransactionSigningContext ctx, PubKey pubKey)
		{
			return _Keys
				.Concat(ctx.AdditionalKeys)
				.FirstOrDefault(k => k.PubKey == pubKey);
		}

		public TransactionBuilder Then()
		{
			_CurrentGroup = null;
			return this;
		}

		public TransactionBuilder Send(PubKey pubKey, Money amount)
		{
			return Send(pubKey.PaymentScript, amount);
		}

		/// <summary>
		/// Specify the amount of money to cover txouts, if not specified all txout will be covered
		/// </summary>
		/// <param name="amount"></param>
		/// <returns></returns>
		public TransactionBuilder CoverOnly(Money amount)
		{
			CurrentGroup.CoverOnly = amount;
			return this;
		}


		Transaction _CompletedTransaction;

		/// <summary>
		/// Allows to keep building on the top of a partially built transaction
		/// </summary>
		/// <param name="transaction">Transaction to complete</param>
		/// <returns></returns>
		public TransactionBuilder ContinueToBuild(Transaction transaction)
		{
			if(_CompletedTransaction != null)
				throw new InvalidOperationException("Transaction to complete already set");
			_CompletedTransaction = transaction.Clone();
			return this;
		}

		/// <summary>
		/// Will cover the remaining amount of TxOut of a partially built transaction (to call after ContinueToBuild)
		/// </summary>
		/// <returns></returns>
		public TransactionBuilder CoverTheRest()
		{
			if(_CompletedTransaction == null)
				throw new InvalidOperationException("A partially built transaction should be specified by calling ContinueToBuild");

			var spent = _CompletedTransaction.Inputs.Select(txin =>
			{
				var c = FindCoin(txin.PrevOut);
				if(c == null)
					throw CoinNotFound(txin);
				if(c is IColoredCoin)
					return null;
				return c;
			})
					.Where(c => c != null)
					.Select(c => c.Amount)
					.Sum();

			var toComplete = _CompletedTransaction.TotalOut - spent;
			CurrentGroup.Builders.Add(ctx =>
			{
				if(toComplete < Money.Zero)
					return Money.Zero;
				return toComplete;
			});
			return this;
		}

		public TransactionBuilder AddCoins(Transaction transaction)
		{
			var txId = transaction.GetHash();
			AddCoins(transaction.Outputs.Select((o, i) => new Coin(txId, (uint)i, o.Value, o.ScriptPubKey)).ToArray());
			return this;
		}

		Dictionary<ScriptId, Script> _ScriptIdToRedeem = new Dictionary<ScriptId, Script>();
		public TransactionBuilder AddKnownRedeems(params Script[] knownRedeems)
		{
			foreach(var redeem in knownRedeems)
			{
				_ScriptIdToRedeem.AddOrReplace(redeem.ID, redeem);
			}
			return this;
		}

		public Transaction CombineSignatures(params Transaction[] transactions)
		{
			if(transactions.Length == 1)
				return transactions[0];
			if(transactions.Length == 0)
				return null;

			Transaction tx = transactions[0].Clone();
			for(int i = 1 ; i < transactions.Length ; i++)
			{
				var signed = transactions[i];
				tx = CombineSignaturesCore(tx, signed);
			}
			return tx;
		}

		private Transaction CombineSignaturesCore(Transaction signed1, Transaction signed2)
		{
			if(signed1 == null)
				return signed2;
			if(signed2 == null)
				return signed1;
			var tx = signed1.Clone();
			for(int i = 0 ; i < tx.Inputs.Count ; i++)
			{
				if(i >= signed2.Inputs.Count)
					break;

				var txIn = tx.Inputs[i];

				var coin = FindCoin(txIn.PrevOut);
				var scriptPubKey = coin == null ? DeduceScriptPubKey(txIn.ScriptSig) : coin.ScriptPubKey;
				if(scriptPubKey != null)
				{
					tx.Inputs[i].ScriptSig = Script.CombineSignatures(
											scriptPubKey,
											tx,
											 i,
											 signed1.Inputs[i].ScriptSig,
											 signed2.Inputs[i].ScriptSig);
				}
			}
			return tx;
		}

		private Script DeduceScriptPubKey(Script scriptSig)
		{
			var p2sh = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(scriptSig);
			if(p2sh != null)
			{
				return p2sh.RedeemScript.ID.CreateScriptPubKey();
			}
			return null;
		}
	}
}
