#nullable enable
using NBitcoin.BuilderExtensions;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.OpenAsset;
using NBitcoin.Policy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using Builder = System.Action<NBitcoin.TransactionBuilder.TransactionBuildingContext>;
using AssetBuilder = System.Action<NBitcoin.TransactionBuilder.TransactionBuildingContext>;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;

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
		IEnumerable<ICoin>? Select(IEnumerable<ICoin> coins, IMoney target);
	}

	public class SigningOptions
	{
		public SigningOptions()
		{
		}
		public SigningOptions(SigHash sigHash)
		{
			SigHash = sigHash;
			TaprootSigHash = (TaprootSigHash)sigHash;
			if (sigHash == SigHash.All)
				TaprootSigHash = TaprootSigHash.Default;
		}
		public SigningOptions(TaprootSigHash sigHash, TaprootReadyPrecomputedTransactionData precomputedTransactionData)
		{
			if (precomputedTransactionData is null)
				throw new ArgumentNullException(nameof(precomputedTransactionData));
			TaprootSigHash = sigHash;
			SigHash = (SigHash)sigHash;
			if (sigHash == TaprootSigHash.Default)
				SigHash = SigHash.All;
			PrecomputedTransactionData = precomputedTransactionData;
		}
		public SigningOptions(SigHash sigHash, bool useLowR)
		{
			SigHash = sigHash;
			TaprootSigHash = (TaprootSigHash)sigHash;
			if (sigHash == SigHash.All)
				TaprootSigHash = TaprootSigHash.Default;
			EnforceLowR = useLowR;
		}

		/// <summary>
		/// What are we signing (default: SigHash.All)
		/// </summary>
		public SigHash SigHash { get; set; } = SigHash.All;
		/// <summary>
		/// What are we signing for taproot (default: SigHash.Default)
		/// </summary>
		public TaprootSigHash TaprootSigHash { get; set; } = TaprootSigHash.Default;
		/// <summary>
		/// Do we try to get shorter signatures? (default: true)
		/// </summary>
		public bool EnforceLowR { get; set; } = true;
		/// <summary>
		/// Providing the PrecomputedTransactionData speed up signing time, by pre computing one several hashes need
		/// for the calculation of the signatures of every input.
		/// 
		/// For taproot transaction signing, the precomputed transaction data is required if some of the inputs does not
		/// belong to the signer.
		/// </summary>
		public PrecomputedTransactionData? PrecomputedTransactionData { get; set; }
		public SigningOptions Clone()
		{
			return new SigningOptions()
			{
				SigHash = SigHash,
				EnforceLowR = EnforceLowR,
				TaprootSigHash = TaprootSigHash,
				PrecomputedTransactionData = PrecomputedTransactionData
			};
		}
	}

	/// <summary>
	/// Algorithm implemented by bitcoin core https://github.com/bitcoin/bitcoin/blob/3015e0bca6bc2cb8beb747873fdf7b80e74d679f/src/wallet.cpp#L1276
	/// Minimize the change
	/// </summary>
	public class DefaultCoinSelector : ICoinSelector
	{
		public DefaultCoinSelector()
		{
			_Rand = new Random();
		}

		Random? _Rand;

		public DefaultCoinSelector(int seed)
		{
			_Rand = new Random(seed);
		}

		public DefaultCoinSelector(Random? random)
		{
			_Rand = random;
		}

		/// <summary>
		/// Select all coins belonging to same scriptPubKey together to protect privacy. (Default: true)
		/// </summary>
		public bool GroupByScriptPubKey
		{
			get; set;
		} = true;

		public IMoney MinimumChange { get; set; } = Money.Coins(1.0m / 100);

		#region ICoinSelector Members

		class OutputGroup
		{
			public OutputGroup(IMoney amount, ICoin[] coins)
			{
				Amount = amount;
				Coins = coins;
			}
			public IMoney Amount { get; set; }
			public ICoin[] Coins { get; set; }
		}
		public IEnumerable<ICoin>? Select(IEnumerable<ICoin> coins, IMoney target)
		{
			List<OutputGroup> setCoinsRet = new List<OutputGroup>();
			var zero = target.Sub(target);

			var groups = GroupByScriptPubKey ? coins.GroupBy(c => c.TxOut.ScriptPubKey)
												.Select(scriptPubKeyCoins => new OutputGroup
												(
													amount: scriptPubKeyCoins.Select(c => c.Amount).Sum(zero),
													coins: scriptPubKeyCoins.ToArray()
												)).ToArray()
											 : coins.Select(c => new OutputGroup(c.Amount, new ICoin[] { c })).ToArray();


			// List of values less than target
			OutputGroup? lowest_larger = null;
			List<OutputGroup> applicable_groups = new List<OutputGroup>();
			var nTotalLower = zero;
			var targetMinChange = target.IsCompatible(MinimumChange) ? target.Add(MinimumChange) : target;
			if (_Rand != null)
				Utils.Shuffle(groups, _Rand);

			foreach (var group in groups)
			{
				if (group.Amount.Equals(target))
				{
					setCoinsRet.Add(group);
					return setCoinsRet.SelectMany(s => s.Coins);
				}
				else if (group.Amount.CompareTo(targetMinChange) < 0)
				{
					applicable_groups.Add(group);
					nTotalLower = nTotalLower.Add(group.Amount);
				}
				else if (lowest_larger == null || group.Amount.CompareTo(lowest_larger.Amount) < 0)
				{
					lowest_larger = group;
				}
			}

			if (nTotalLower.Equals(target))
			{
				foreach (var group in applicable_groups)
				{
					setCoinsRet.Add(group);
				}
				return setCoinsRet.SelectMany(s => s.Coins);
			}

			if (nTotalLower.CompareTo(target) < 0)
			{
				if (lowest_larger == null) return null;
				setCoinsRet.Add(lowest_larger);
				return setCoinsRet.SelectMany(s => s.Coins);
			}

			// Solve subset sum by stochastic approximation
			applicable_groups = applicable_groups.OrderByDescending(g => g.Amount).ToList();
			bool[] vfBest;
			IMoney nBest;
			ApproximateBestSubset(applicable_groups, nTotalLower, target, out vfBest, out nBest);
			if (!nBest.Equals(target) && (nTotalLower.CompareTo(targetMinChange) is var v && (v == 0 || v > 0)))
			{
				ApproximateBestSubset(applicable_groups, nTotalLower, targetMinChange, out vfBest, out nBest);
			}

			// If we have a bigger coin and (either the stochastic approximation didn't find a good solution,
			//                                   or the next bigger coin is closer), return the bigger coin
			if (lowest_larger != null &&
				((!nBest.Equals(target) && nBest.CompareTo(targetMinChange) < 0) ||
				(lowest_larger.Amount.CompareTo(nBest) is var vv && (vv == 0 || vv < 0))))
			{
				setCoinsRet.Add(lowest_larger);
			}
			else
			{
				for (int i = 0; i < applicable_groups.Count; i++)
				{
					if (vfBest[i])
					{
						setCoinsRet.Add(applicable_groups[i]);
					}
				}
			}

			return setCoinsRet.SelectMany(s => s.Coins);
		}

#if NO_ARRAY_FILL
		void ArrayFill<T>(T[] array, T value)
		{
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = value;
			}
		}
#endif
		/// <summary>
		/// Number of iterations in the knapsack algorithm (Default: 100)
		/// </summary>
		public int Iterations { get; set; } = 100;
		void ApproximateBestSubset(List<OutputGroup> groups, IMoney nTotalLower, IMoney nTargetValue,
								  out bool[] vfBest, out IMoney nBest)
		{
			var zero = nTargetValue.Sub(nTargetValue);
			vfBest = new bool[groups.Count];
#if NO_ARRAY_FILL
			ArrayFill(vfBest, true);
#else
			Array.Fill(vfBest, true);
#endif
			bool[] vfIncluded = new bool[groups.Count];
			nBest = nTotalLower;

			for (int nRep = 0; nRep < Iterations && nBest != nTargetValue; nRep++)
			{
#if NO_ARRAY_FILL
				ArrayFill(vfIncluded, false);
#else
				Array.Fill(vfIncluded, false);
#endif
				var nTotal = zero;
				bool fReachedTarget = false;
				for (int nPass = 0; nPass < 2 && !fReachedTarget; nPass++)
				{
					for (int i = 0; i < groups.Count; i++)
					{
						//The solver here uses a randomized algorithm,
						//the randomness serves no real security purpose but is just
						//needed to prevent degenerate behavior and it is important
						//that the rng is fast. We do not use a constant random sequence,
						//because there may be some privacy improvement by making
						//the selection random.
						if (nPass == 0 ? (_Rand is null || _Rand.Next(0, 2) == 0)
							: !vfIncluded[i])
						{
							nTotal = nTotal.Add(groups[i].Amount);
							vfIncluded[i] = true;
							if (nTotal.CompareTo(nTargetValue) is var v && (v == 0 || v > 0))
							{
								fReachedTarget = true;
								if (nTotal.CompareTo(nBest) < 0)
								{
									nBest = nTotal;
									Array.Copy(vfIncluded, vfBest, groups.Count);
								}
								nTotal = nTotal.Sub(groups[i].Amount);
								vfIncluded[i] = false;
							}
						}
					}
				}
			}
		}

		#endregion
	}

	/// <summary>
	/// Exception thrown when not enough funds are present for verifying or building a transaction
	/// </summary>
	public class NotEnoughFundsException : Exception
	{
		public NotEnoughFundsException(string message, string? group, IMoney missing)
			: base(BuildMessage(message, group, missing))
		{
			Missing = missing;
			Group = group;
		}

		private static string BuildMessage(string message, string? group, IMoney missing)
		{
			StringBuilder builder = new StringBuilder();
			builder.Append(message);
			if (group != null)
				builder.Append(" in group " + group);
			if (missing != null)
				builder.Append(" with missing amount " + missing);
			return builder.ToString();
		}

		/// <summary>
		/// The group name who is missing the funds
		/// </summary>
		public string? Group
		{
			get;
			private set;
		}

		/// <summary>
		/// Amount of Money missing
		/// </summary>
		public IMoney Missing
		{
			get;
			private set;
		}
	}

	public class OutputTooSmallException : NotEnoughFundsException
	{
		public OutputTooSmallException(string message, string? group, IMoney missing) : base(message, group, missing)
		{

		}
	}

	/// <summary>
	/// A class for building and signing all sort of transactions easily (http://www.codeproject.com/Articles/835098/NBitcoin-Build-Them-All)
	/// </summary>
	public class TransactionBuilder
	{
		internal class TransactionBuilderSigner : ISigner
		{
			private readonly InputSigningContext inputCtx;
			public TransactionBuilderSigner(InputSigningContext ctx)
			{
				this.inputCtx = ctx;
			}
			#region ISigner Members

			public ITransactionSignature? Sign(IPubKey pubKey)
			{
				var keypair = inputCtx.CoinOptions?.KeyPair ?? inputCtx.TransactionContext.FindKey(pubKey);
				if (keypair is null)
					return null;
				var indexedTxIn = inputCtx.Input.GetIndexedInput();
#if HAS_SPAN
				if (keypair is TaprootKeyPair tkp)
				{
					return indexedTxIn.SignTaprootKeySpend(tkp, inputCtx.Coin, inputCtx.TransactionContext.SigningOptions);
				}
#endif
				return indexedTxIn.Sign(keypair.Key, inputCtx.Coin, inputCtx.TransactionContext.SigningOptions);
			}

			#endregion
		}

		class CompositeKeyRepository : IKeyRepository
		{
			public CompositeKeyRepository(IKeyRepository[] keyRepositories)
			{
				KeyRepositories = keyRepositories;
			}

			public IKeyRepository[] KeyRepositories { get; }

			public IPubKey? FindKey(Script scriptPubKey)
			{
				return KeyRepositories
						.Select(k => k.FindKey(scriptPubKey))
						.Where(k => k != null)
						.FirstOrDefault();
			}
		}
		class CompositeSigner : ISigner
		{
			public CompositeSigner(ISigner[] signers)
			{
				Signers = signers;
			}

			public ISigner[] Signers { get; }

			public ITransactionSignature? Sign(IPubKey key)
			{
				return Signers.Select(k => k.Sign(key))
					.Where(k => k != null)
					.FirstOrDefault();
			}
		}
		internal class TransactionBuilderKeyRepository : IKeyRepository
		{
			InputSigningContext _Ctx;
			public TransactionBuilderKeyRepository(InputSigningContext ctx)
			{
				_Ctx = ctx;
			}
			#region IKeyRepository Members

			public IPubKey? FindKey(Script scriptPubkey)
			{
				return _Ctx.CoinOptions?.KeyPair?.PubKey ?? _Ctx.TransactionContext.FindKey(scriptPubkey)?.PubKey;
			}

			#endregion
		}

		class KnownSignatureSigner : ISigner, IKeyRepository
		{
			private ICoin coin;
			private PSBTInput txIn;
			private readonly TransactionSigningContext signingContext;
			private List<Tuple<IPubKey, ITransactionSignature, OutPoint>> _KnownSignatures;
			private InputSigningContext inputCtx;
			public KnownSignatureSigner(InputSigningContext inputSigningContext, List<Tuple<IPubKey, ITransactionSignature, OutPoint>> knownSignatures)
			{
				this.signingContext = inputSigningContext.TransactionContext;
				this._KnownSignatures = knownSignatures;
				this.coin = inputSigningContext.Coin;
				this.txIn = inputSigningContext.Input;
				this.inputCtx = inputSigningContext;
			}

			public IPubKey? FindKey(Script scriptPubKey)
			{
				foreach (var tv in _KnownSignatures.Where(tv => inputCtx.Extension.IsCompatibleKey(tv.Item1, scriptPubKey)))
				{
					if (tv.Item3 != null && coin.Outpoint != tv.Item3)
						continue;
					return tv.Item1;
				}
				return null;
			}

			public ITransactionSignature? Sign(IPubKey key)
			{
				foreach (var tv in _KnownSignatures.Where(k => k.Item1.Equals(key)))
				{
					if (tv.Item3 != null && coin.Outpoint != tv.Item3)
						continue;
					return tv.Item2;
				}
				return null;
			}
		}

		internal class TransactionSigningContext
		{
			public TransactionSigningContext(TransactionBuilder builder, PSBT psbt, SigningOptions signingOptions)
				: this(builder, psbt, null, psbt.GetSigningOptions(signingOptions))
			{
			}
			public TransactionSigningContext(TransactionBuilder builder, Transaction transaction, SigningOptions signingOptions)
				: this(builder, transaction.CreatePSBT(builder.Network), transaction, signingOptions)
			{
			}
			public TransactionSigningContext(TransactionBuilder builder, PSBT psbt, Transaction? transaction, SigningOptions signingOptions)
			{
				Builder = builder;
				Transaction = transaction;
				PSBT = psbt;
				if (signingOptions.PrecomputedTransactionData is null)
				{
					signingOptions = signingOptions.Clone();
					var prevTxous = psbt.tx.Inputs.Select(txin => builder.FindCoin(txin.PrevOut)?.TxOut).ToArray();
					if (prevTxous.All(p => p != null))
						signingOptions.PrecomputedTransactionData = psbt.tx.PrecomputeTransactionData(prevTxous!);
					else
						signingOptions.PrecomputedTransactionData = psbt.tx.PrecomputeTransactionData();
				}
				SigningOptions = signingOptions;
			}

			public PSBT PSBT { get; set; }
			public Transaction? Transaction
			{
				get;
				set;
			}
			public TransactionBuilder Builder
			{
				get;
				set;
			}

			internal KeyPair? FindKey(Script scriptPubKey)
			{
				var keypair = (Builder._Keys
					.FirstOrDefault(k => Builder.IsCompatibleKeyFromScriptCode(k.PubKey, scriptPubKey)));
				if (keypair is null && Builder.KeyFinder != null)
				{
					keypair = Builder.KeyFinder(scriptPubKey);
				}
				return keypair;
			}
			internal KeyPair? FindKey(IPubKey pubKey)
			{
				var key = Builder._Keys
					.FirstOrDefault(k => k.PubKey.Equals(pubKey));
				return key;
			}
			public SigningOptions SigningOptions { get; }
			public uint? SignOnlyInputIndex { get; internal set; }

			public InputSigningContext CreateInputContext(ICoin coin, PSBTInput input, TxIn? originalInput, BuilderExtension extension)
			{
				return new InputSigningContext(this, coin, this.Builder.FindCoinOptions(coin.Outpoint), input, originalInput, extension);
			}

			public IEnumerable<InputSigningContext> GetInputSigningContexts()
			{
				foreach (var input in PSBT.Inputs)
				{
					if (SignOnlyInputIndex is uint i && i != input.Index)
						continue;
					ICoin? coin = null;
					TxIn? txin = null;
					coin = input.GetSignableCoin();
					if (Transaction is null)
					{
						txin = null;
						coin = coin ?? Builder.FindSignableCoin(input.TxIn);
					}
					else
					{
						txin = Transaction.Inputs[input.Index];
						coin = coin ?? Builder.FindSignableCoin(txin);
					}

					if (coin is ICoin)
					{
						var ext = Builder.Extensions.FirstOrDefault(e => e.Match(coin, input));
						if (ext is BuilderExtension)
						{
							input.UpdateFromCoin(coin);
							yield return CreateInputContext(coin, input, txin, ext);
						}
					}
				}
			}
		}
		internal class TransactionBuildingContext
		{
			internal class GroupContext
			{
				public GroupContext(BuilderGroup builderGroup)
				{
					DustPreventionTotalRemoved = new MoneyBag();
					LeftOverChange = new MoneyBag();
					Group = builderGroup;
					FixedFee = builderGroup.FixedFee.GetAmount(Money.Zero);
					SentOutput = new MoneyBag();
					if (builderGroup._Parent.StandardTransactionPolicy.MinFee is Money v)
						MinFee = v;
				}
				/// <summary>
				/// Additional change that should be swept to change later
				/// </summary>
				public MoneyBag LeftOverChange { get; set; }
				/// <summary>
				/// Total of output value that was removed via dust prevention mechanism
				/// </summary>
				public MoneyBag DustPreventionTotalRemoved { get; set; }
				/// <summary>
				/// What is curently sent
				/// </summary>
				public MoneyBag SentOutput { get; set; }
				/// <summary>
				/// The fee txout (change or substracted output)
				/// </summary>
				public TxOut? FeeTxOut { get; set; }
				/// <summary>
				/// Whether fee are already paid
				/// </summary>
				public bool FeePaid { get; set; }
				/// <summary>
				/// Fixed fee of SendFee
				/// </summary>
				public Money FixedFee { get; set; }
				public MoneyBag Fee
				{
					get
					{
						return new MoneyBag(Money.Max(MinFee, FixedFee + SizeFee));
					}
				}
				public TransactionBuilder.BuilderGroup Group { get; set; }
				public List<ICoin> Selection { get; internal set; } = new List<ICoin>();
				/// <summary>
				/// Dogecoin has weird requirement.
				/// </summary>
				public Money MinFee { get; internal set; } = Money.Zero;
				/// <summary>
				/// Size fee is initially sent to 0 for the first pass.
				/// Then we can do a second pass with the right Size fee.
				/// </summary>
				public Money SizeFee { get; set; } = Money.Zero;
				/// <summary>
				/// The minimum UTXO value to select
				/// </summary>
				public Money MinValue { get; set; } = Money.Zero;
			}
			public TransactionBuildingContext(TransactionBuilder builder)
			{
				Zero = Money.Zero;
				Builder = builder;
				Transaction = builder.Network.CreateTransaction();
				// This group context is unused but it make sure we don't get NRE.
				CurrentGroupContext = new GroupContext(new BuilderGroup(builder));
				AddChangeTxOut = (a, ctx) => throw new InvalidOperationException("BUG in NBitcoin (AddChangeTxOut not set)");
			}
			public GroupContext CurrentGroupContext { get; set; }
			public TransactionBuilder.BuilderGroup Group
			{
				get
				{
					return CurrentGroupContext.Group;
				}
			}

			public bool CanMergeOutputs { get; set; } = true;
			public bool CanShuffleOutputs { get; set; } = true;
			public bool CanShuffleInputs { get; set; } = true;

			private HashSet<OutPoint> _ConsumedOutpoints = new HashSet<OutPoint>();
			public HashSet<OutPoint> ConsumedOutpoints
			{
				get
				{
					return _ConsumedOutpoints;
				}
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

			ColorMarker? _Marker;

			public ColorMarker GetColorMarker(bool issuance)
			{
				if (_Marker == null)
					_Marker = new ColorMarker();
				if (!issuance)
					EnsureMarkerInserted();
				return _Marker;
			}

			private TxOut EnsureMarkerInserted()
			{
				uint position;
				var dummy = Transaction.Inputs.Add(new OutPoint(new uint256(1), 0)); //Since a transaction without input will be considered without marker, insert a dummy
				try
				{
					if (ColorMarker.Get(Transaction, out position) != null)
						return Transaction.Outputs[position];
				}
				finally
				{
					Transaction.Inputs.Remove(dummy);
				}
				var txout = Transaction.Outputs.Add(scriptPubKey: new ColorMarker().GetScript());
				txout.Value = Money.Zero;
				return txout;
			}

			public void InsertOpenAssetMarker()
			{
				if (_Marker != null)
				{
					var txout = EnsureMarkerInserted();
					txout.ScriptPubKey = _Marker.GetScript();
				}
			}

			public IssuanceCoin? IssuanceCoin
			{
				get;
				set;
			}

			public Func<IMoney, TransactionBuildingContext, TxOut> AddChangeTxOut
			{
				get;
				set;
			}

			public Func<Script, IMoney>? GetDust
			{
				get;
				set;
			}

			public ChangeType ChangeType
			{
				get;
				set;
			}
			public IEnumerable<ICoin> Selection => GroupContexts.SelectMany(g => g.Selection);
			public IMoney Zero { get; internal set; }

			public IEnumerable<GroupContext> GroupContexts => _GroupContexts.Values;

			public bool SmallInputsExcluded { get; internal set; }

			Dictionary<BuilderGroup, GroupContext> _GroupContexts = new Dictionary<BuilderGroup, GroupContext>();
			public void SetGroup(BuilderGroup group)
			{
				if (_GroupContexts.TryGetValue(group, out var gctx))
					CurrentGroupContext = gctx;
				else
				{
					gctx = new GroupContext(group);
					_GroupContexts.Add(group, gctx);
				}
				CurrentGroupContext = gctx;
			}
		}

		internal class CoinWithOptions
		{
			public CoinWithOptions(ICoin coin, CoinOptions? options)
			{
				Coin = coin;
				Options = options;
			}

			public ICoin Coin;
			public CoinOptions? Options;
		}

		internal class BuilderGroup
		{
			internal TransactionBuilder _Parent;
			public BuilderGroup(TransactionBuilder parent)
			{
				_Parent = parent;
				FeeWeight = 1.0m;
			}
			internal List<Builder> Builders = new List<Builder>();
			internal Dictionary<OutPoint, CoinWithOptions> CoinsWithOptions = new Dictionary<OutPoint, CoinWithOptions>();
			internal List<AssetBuilder> IssuanceBuilders = new List<AssetBuilder>();
			internal Dictionary<AssetId, List<AssetBuilder>> BuildersByAsset = new Dictionary<AssetId, List<AssetBuilder>>();
			internal Script[] ChangeScript = new Script[3];
			internal bool sendAllToChange;
			internal bool preventSetChange;
			internal MoneyBag FixedFee = new MoneyBag();

			internal IEnumerable<T> CoinsOfType<T>()
			{
				return CoinsWithOptions.Values.Select(coinWithOptions => coinWithOptions.Coin).OfType<T>();
			}

			internal void Shuffle()
			{
				Shuffle(Builders);
				foreach (var builders in BuildersByAsset)
					Shuffle(builders.Value);
				Shuffle(IssuanceBuilders);
			}
			private void Shuffle<T>(List<T> builders)
			{
				Utils.Shuffle(builders, _Parent.ShuffleRandom);
			}

			public CoinOptions? GetOptions(ICoin coin)
			{
				CoinsWithOptions.TryGetValue(coin.Outpoint, out var coinWithOptions);
				return coinWithOptions?.Options;
			}

			public string? Name
			{
				get;
				set;
			}

			public FeeRate? FeeRate { get; set; }
			public decimal FeeWeight
			{
				get;
				set;
			}
			/// <summary>
			/// Money which has been recovered from outputs that are too small to be included
			/// we should be able to pay fees with that
			/// </summary>
			public Money SurrendedValue { get; set; } = Money.Zero;
		}

		List<BuilderGroup> _BuilderGroups = new List<BuilderGroup>();
		BuilderGroup? _CurrentGroup = null;
		internal BuilderGroup CurrentGroup
		{
			get
			{
				if (_CurrentGroup == null)
				{
					_CurrentGroup = new BuilderGroup(this);
					_BuilderGroups.Add(_CurrentGroup);
				}
				return _CurrentGroup;
			}
		}

		internal TransactionBuilder(Network network)
		{
			if (network == null)
				throw new ArgumentNullException(nameof(network));
			Network = network;
			ShuffleRandom = new Random();
			CoinSelector = new DefaultCoinSelector(ShuffleRandom);
			StandardTransactionPolicy = new StandardTransactionPolicy();
			DustPrevention = true;
			OptInRBF = false;
			InitExtensions();
		}

		private void InitExtensions()
		{
			Extensions.Add(new P2PKHBuilderExtension());
			Extensions.Add(new P2MultiSigBuilderExtension());
			Extensions.Add(new P2PKBuilderExtension());
			Extensions.Add(new OPTrueExtension());
#if HAS_SPAN
			if (Network.Consensus.SupportTaproot)
			{
				Extensions.Add(new TaprootKeySpendExtension());
			}
#endif
		}

		/// <summary>
		/// The random number generator used for shuffling transaction outputs or selected coins
		/// </summary>
		public Random ShuffleRandom { get; set; } = new Random();

		public ICoinSelector CoinSelector
		{
			get;
			set;
		}

		/// <summary>
		/// If true, it will remove any TxOut below Dust, so the transaction get correctly relayed by the network. (Default: true)
		/// </summary>
		public bool DustPrevention
		{
			get;
			set;
		}

		/// <summary>
		/// If true, it will signal the transaction replaceability in every input. (Default: false)
		/// </summary>
		public bool OptInRBF
		{
			get;
			set;
		}

		/// <summary>
		/// If true, the transaction builder tries to shuffle inputs
		/// </summary>
		public bool ShuffleInputs
		{
			get; set;
		} = true;

		/// <summary>
		/// If true, the transaction builder tries to shuffles outputs
		/// </summary>
		public bool ShuffleOutputs
		{
			get; set;
		} = true;

		/// <summary>
		/// If true and the transaction has two outputs sending to the same scriptPubKey, those will be merged into a single output. (Default: true)
		/// </summary>
		public bool MergeOutputs
		{
			get; set;
		} = true;

		/// <summary>
		/// If true, the TransactionBuilder will not select coins whose fee to spend is higher than its value. (Default: true)
		/// The cost of spending a coin is based on the <see cref="FilterUneconomicalCoinsRate"/>.
		/// </summary>
		public bool FilterUneconomicalCoins
		{
			get; set;
		} = true;

		/// <summary>
		/// If <see cref="FilterUneconomicalCoins"/> is true, this rate is used to know if an output is economical.
		/// This property is set automatically when calling <see cref="SendEstimatedFees(FeeRate)"/> or <see cref="SendEstimatedFeesSplit(FeeRate)"/>.
		/// </summary>
		public FeeRate? FilterUneconomicalCoinsRate
		{
			get; set;
		}

		/// <summary>
		/// A callback used by the TransactionBuilder when it does not find the coin for an input
		/// </summary>
		public Func<OutPoint, ICoin>? CoinFinder
		{
			get;
			set;
		}

		/// <summary>
		/// A callback used by the TransactionBuilder when it does not find the key for a scriptPubKey
		/// </summary>
		public Func<Script, KeyPair>? KeyFinder
		{
			get;
			set;
		}

		LockTime _LockTime;
		public TransactionBuilder SetLockTime(LockTime lockTime)
		{
			_LockTime = lockTime;
			return this;
		}

		uint? _Version = 1;
		public TransactionBuilder SetVersion(uint version)
		{
			_Version = version;
			return this;
		}

		internal List<KeyPair> _Keys = new List<KeyPair>();

		public TransactionBuilder AddKeys(params ISecret[] keys)
		{
			if (keys == null)
				throw new ArgumentNullException(nameof(keys));
			AddKeys(keys.Select(k => k.PrivateKey).ToArray());
			return this;
		}

		public TransactionBuilder AddKeys(params Key[] keys)
		{
			if (keys == null)
				throw new ArgumentNullException(nameof(keys));
			List<KeyPair> pairs = new List<KeyPair>(keys.Length * 2);
#if HAS_SPAN
			if (Network.Consensus.SupportTaproot)
				pairs.AddRange(keys.Select(k => KeyPair.CreateTaprootPair(k)));
#endif
			pairs.AddRange(keys.Select(k => KeyPair.CreateECDSAPair(k)));
			return AddKeys(pairs.ToArray());
		}
		public TransactionBuilder AddKeys(params KeyPair[] keyPairs)
		{
			if (keyPairs == null)
				throw new ArgumentNullException(nameof(keyPairs));
			_Keys.AddRange(keyPairs);
			foreach (var pk in keyPairs.Select(k => k.PubKey).OfType<PubKey>())
			{
				AddKnownRedeems(pk.ScriptPubKey);
				AddKnownRedeems(pk.WitHash.ScriptPubKey);
				AddKnownRedeems(pk.Hash.ScriptPubKey);
			}
			return this;
		}

		public TransactionBuilder AddKnownSignature(IPubKey pubKey, ITransactionSignature signature, OutPoint signedOutpoint)
		{
			if (pubKey == null)
				throw new ArgumentNullException(nameof(pubKey));
			if (signature == null)
				throw new ArgumentNullException(nameof(signature));
			if (signedOutpoint == null)
				throw new ArgumentNullException(nameof(signedOutpoint));
			_KnownSignatures.Add(Tuple.Create(pubKey, signature, signedOutpoint));
			return this;
		}

		public TransactionBuilder SetOptInRBF(bool rbf)
		{
			OptInRBF = rbf;
			return this;
		}

		public TransactionBuilder AddCoin(ICoin coin)
		{
			return AddCoin(coin, new CoinOptions());
		}

		public TransactionBuilder AddCoin(ICoin coin, CoinOptions? options)
		{
			if (coin == null)
				throw new ArgumentNullException(nameof(coin));
			if (coin.TxOut.ScriptPubKey.IsUnspendable)
				throw new InvalidOperationException("You cannot add an unspendable coin");
			CurrentGroup.CoinsWithOptions.AddOrReplace(coin.Outpoint, new CoinWithOptions(coin, options));
			return this;
		}

		public TransactionBuilder AddCoins(params ICoin?[] coins)
		{
			return AddCoins((IEnumerable<ICoin?>)coins);
		}

		public TransactionBuilder AddCoins(IEnumerable<ICoin?> coins)
		{
			foreach (var coin in coins)
			{
				if (coin is ICoin)
					AddCoin(coin);
			}
			return this;
		}

		public TransactionBuilder AddCoins(PSBT psbt)
		{
			if (psbt == null)
				throw new ArgumentNullException(nameof(psbt));
			return AddCoins(psbt.Inputs.Select(p => p.GetSignableCoin()).Where(p => p != null).ToArray());
		}

		/// <summary>
		/// Set the name of this group (group are separated by call to Then())
		/// </summary>
		/// <param name="groupName">Name of the group</param>
		/// <returns></returns>
		public TransactionBuilder SetGroupName(string groupName)
		{
			CurrentGroup.Name = groupName;
			return this;
		}

		/// <summary>
		/// Send bitcoins to a destination
		/// </summary>
		/// <param name="destination">The destination</param>
		/// <param name="amount">The amount</param>
		/// <returns></returns>
		public TransactionBuilder Send(IDestination destination, Money amount)
		{
			if (destination == null)
				throw new ArgumentNullException(nameof(destination));
			return Send(destination.ScriptPubKey, amount);
		}

		/// <summary>
		/// Send all coins added so far with no change (sweep), substracting fees from the total amount
		/// </summary>
		/// <param name="destination"></param>
		/// <returns></returns>
		public TransactionBuilder SendAll(IDestination destination)
		{
			if (destination == null)
				throw new ArgumentNullException(nameof(destination));
			return SendAll(destination.ScriptPubKey);
		}


		/// <summary>
		/// Send all coins added so far with no change (sweep), substracting fees from the total amount
		/// </summary>
		/// <param name="scriptPubKey"></param>
		/// <returns></returns>
		public TransactionBuilder SendAll(Script scriptPubKey)
		{
			if (scriptPubKey == null)
				throw new ArgumentNullException(nameof(scriptPubKey));
			var totalInput = CurrentGroup.CoinsOfType<Coin>().Sum(coin => coin.Amount);
			return Send(scriptPubKey, totalInput).SubtractFees();
		}

		/// <summary>
		/// Send all the remaining available coins to this destination
		/// </summary>
		/// <param name="destination"></param>
		/// <returns></returns>
		public TransactionBuilder SendAllRemaining(IDestination destination)
		{
			if (destination == null)
				throw new ArgumentNullException(nameof(destination));
			return SendAllRemaining(destination.ScriptPubKey);
		}

		/// <summary>
		/// Send all the remaining available coins to this destination
		/// </summary>
		/// <param name="scriptPubKey"></param>
		/// <returns></returns>
		public TransactionBuilder SendAllRemaining(Script scriptPubKey)
		{
			if (scriptPubKey == null)
				throw new ArgumentNullException(nameof(scriptPubKey));
			SetChange(scriptPubKey);
			CurrentGroup.sendAllToChange = true;
			CurrentGroup.preventSetChange = true;
			return this;
		}

		/// <summary>
		/// Send all the remaining available coins to the change
		/// </summary>
		/// <returns></returns>
		public TransactionBuilder SendAllRemainingToChange()
		{
			CurrentGroup.preventSetChange = false;
			CurrentGroup.sendAllToChange = true;
			return this;
		}

		readonly static TxNullDataTemplate _OpReturnTemplate = new TxNullDataTemplate(1024 * 1024);

		/// <summary>
		/// Send bitcoins to a destination
		/// </summary>
		/// <param name="scriptPubKey">The destination</param>
		/// <param name="amount">The amount</param>
		/// <returns></returns>
		public TransactionBuilder Send(Script scriptPubKey, Money amount)
		{
			if (amount < Money.Zero)
				throw new ArgumentOutOfRangeException(nameof(amount), "amount can't be negative");
			_LastSendBuilder = null; //If the amount is dust, we don't want the fee to be paid by the previous Send
			if (DustPrevention && amount < GetDust(scriptPubKey) && !_OpReturnTemplate.CheckScriptPubKey(scriptPubKey))
			{
				CurrentGroup.SurrendedValue += amount;
				return this;
			}

			var builder = new SendBuilder(this, amount, scriptPubKey);
			CurrentGroup.Builders.Add(builder.Build);
			_LastSendBuilder = builder;
			return this;
		}

		SendBuilder? _LastSendBuilder;

		internal class SendBuilder
		{
			private readonly TransactionBuilder parent;
			private readonly Money amount;
			private readonly Script scriptPubKey;
			public bool SubstractFee { get; set; }

			public SendBuilder(TransactionBuilder parent, Money amount, Script scriptPubKey)
			{
				this.parent = parent;
				this.amount = amount;
				this.scriptPubKey = scriptPubKey;
			}

			public void Build(TransactionBuildingContext ctx)
			{
				var txout = parent.CreateTxOut(amount, scriptPubKey);
				if (SubstractFee && !ctx.CurrentGroupContext.FeePaid)
				{
					var fee = ctx.CurrentGroupContext.Fee.GetAmount(Money.Zero);
					txout.Value -= fee;

					var minimumTxOutValue = (parent.DustPrevention ? parent.GetDust(txout.ScriptPubKey) : Money.Zero);
					if (txout.Value < Money.Zero)
					{
						throw new OutputTooSmallException("Can't substract fee from this output because the amount is too small",
						ctx.Group.Name,
						-txout.Value
						);
					}
					ctx.CurrentGroupContext.FeePaid = true;
					if (txout.Value < minimumTxOutValue)
					{
						// Between zero and dust, should strip this output.
						ctx.CurrentGroupContext.DustPreventionTotalRemoved += txout.Value;
						return;
					}
					ctx.CurrentGroupContext.FeeTxOut = txout;
				}

				ctx.CurrentGroupContext.SentOutput += txout.Value;
				ctx.Transaction.Outputs.Add(txout);
			}
		}

		/// <summary>
		/// Will subtract fees from the previous TxOut added by the last TransactionBuilder.Send() call
		/// </summary>
		/// <returns></returns>
		public TransactionBuilder SubtractFees()
		{
			if (_LastSendBuilder == null)
				throw new InvalidOperationException("No call to TransactionBuilder.Send has been done which can support the fees");
			_LastSendBuilder.SubstractFee = true;
			return this;
		}

		/// <summary>
		/// Send a money amount to the destination
		/// </summary>
		/// <param name="destination">The destination</param>
		/// <param name="amount">The amount (supported : Money, AssetMoney, MoneyBag)</param>
		/// <returns></returns>
		/// <exception cref="System.NotSupportedException">The coin type is not supported</exception>
		public TransactionBuilder Send(IDestination destination, IMoney amount)
		{
			return Send(destination.ScriptPubKey, amount);
		}
		/// <summary>
		/// Send a money amount to the destination
		/// </summary>
		/// <param name="scriptPubKey">The destination</param>
		/// <param name="amount">The amount (supported : Money, AssetMoney, MoneyBag)</param>
		/// <returns></returns>
		/// <exception cref="System.NotSupportedException">The coin type is not supported</exception>
		public TransactionBuilder Send(Script scriptPubKey, IMoney amount)
		{
			if (amount is MoneyBag bag)
			{
				foreach (var money in bag)
					Send(scriptPubKey, money);
				return this;
			}
			if (amount is Money coinAmount)
				return Send(scriptPubKey, coinAmount);
			if (amount is AssetMoney assetAmount)
				return SendAsset(scriptPubKey, assetAmount);
			throw new NotSupportedException("Type of Money not supported");
		}

		/// <summary>
		/// Send assets (Open Asset) to a destination
		/// </summary>
		/// <param name="destination">The destination</param>
		/// <param name="asset">The asset and amount</param>
		/// <returns></returns>
		public TransactionBuilder SendAsset(IDestination destination, AssetMoney asset)
		{
			return SendAsset(destination.ScriptPubKey, asset);
		}

		/// <summary>
		/// Send assets (Open Asset) to a destination
		/// </summary>
		/// <param name="destination">The destination</param>
		/// <param name="assetId">The asset and amount</param>
		/// <returns></returns>
		public TransactionBuilder SendAsset(IDestination destination, AssetId assetId, ulong quantity)
		{
			return SendAsset(destination, new AssetMoney(assetId, quantity));
		}

		private void DoShuffleGroups()
		{
			if (ShuffleRandom != null && ShuffleOutputs)
			{
				Utils.Shuffle(_BuilderGroups, ShuffleRandom);
				foreach (var group in _BuilderGroups)
					group.Shuffle();
			}
		}

		TxOut SetColoredChange(IMoney changeAmount, TransactionBuildingContext ctx)
		{
			var marker = ctx.GetColorMarker(false);
			var script = ctx.Group.ChangeScript[(int)ChangeType.Colored];
			var txout = ctx.Transaction.Outputs.Add(GetDust(script), script);
			marker.SetQuantity(ctx.Transaction.Outputs.Count - 2, ((AssetMoney)changeAmount).Quantity);
			ctx.CurrentGroupContext.SentOutput += txout.Value;
			ctx.CanMergeOutputs = false;
			ctx.CanShuffleOutputs = false;
			return txout;
		}
		TxOut SetChange(IMoney changeAmount, TransactionBuildingContext ctx)
		{
			var txout = ctx.Transaction.Outputs.CreateNewTxOut((Money)changeAmount, ctx.Group.ChangeScript[(int)ChangeType.Uncolored]);
			ctx.Transaction.Outputs.Add(txout);
			return txout;
		}

		public TransactionBuilder SendAsset(Script scriptPubKey, AssetId assetId, ulong assetQuantity)
		{
			return SendAsset(scriptPubKey, new AssetMoney(assetId, assetQuantity));
		}

		public TransactionBuilder SendAsset(Script scriptPubKey, AssetMoney asset)
		{
			if (asset.Quantity < 0)
				throw new ArgumentOutOfRangeException(nameof(asset), "Asset amount can't be negative");
			if (asset.Quantity == 0)
				return this;
			AssertOpReturn("Colored Coin");
			var builders = CurrentGroup.BuildersByAsset.TryGet(asset.Id);
			if (builders == null)
			{
				builders = new List<AssetBuilder>();
				CurrentGroup.BuildersByAsset.Add(asset.Id, builders);
			}
			builders.Add(ctx =>
			{
				var marker = ctx.GetColorMarker(false);
				var txout = ctx.Transaction.Outputs.Add(GetDust(scriptPubKey), scriptPubKey);
				marker.SetQuantity(ctx.Transaction.Outputs.Count - 2, asset.Quantity);
				ctx.CurrentGroupContext.SentOutput += txout.Value;
				ctx.CurrentGroupContext.SentOutput += asset;
				ctx.CanMergeOutputs = false;
				ctx.CanShuffleOutputs = false;
			});
			return this;
		}

		internal Money GetDust()
		{
			return GetDust(new Script(new byte[25]));
		}
		internal Money GetDust(Script script)
		{
			if (StandardTransactionPolicy == null || StandardTransactionPolicy.MinRelayTxFee == null)
				return Money.Zero;
			return CreateTxOut(Money.Zero, script).GetDustThreshold();
		}

		/// <summary>
		/// Set transaction policy fluently
		/// </summary>
		/// <param name="policy">The policy</param>
		/// <returns>this</returns>
		public TransactionBuilder SetTransactionPolicy(StandardTransactionPolicy policy)
		{
			StandardTransactionPolicy = policy;
			return this;
		}
		public StandardTransactionPolicy StandardTransactionPolicy
		{
			get;
			set;
		}


		string? _OpReturnUser;
		private void AssertOpReturn(string name)
		{
			if (_OpReturnUser == null)
			{
				_OpReturnUser = name;
			}
			else
			{
				if (_OpReturnUser != name)
					throw new InvalidOperationException("Op return already used for " + _OpReturnUser);
			}
		}

		public TransactionBuilder IssueAsset(IDestination destination, AssetMoney asset)
		{
			return IssueAsset(destination.ScriptPubKey, asset);
		}

		AssetId? _IssuedAsset;

		public TransactionBuilder IssueAsset(Script scriptPubKey, AssetMoney asset)
		{
			AssertOpReturn("Colored Coin");
			if (_IssuedAsset == null)
				_IssuedAsset = asset.Id;
			else if (_IssuedAsset != asset.Id)
				throw new InvalidOperationException("You can issue only one asset type in a transaction");
			CurrentGroup.IssuanceBuilders.Add(ctx =>
			{
				var marker = ctx.GetColorMarker(true);
				if (ctx.IssuanceCoin == null)
				{
					var issuance = ctx.Group.CoinsOfType<IssuanceCoin>().Where(i => i.AssetId == asset.Id).FirstOrDefault();
					if (issuance == null)
						throw new InvalidOperationException("No issuance coin for emitting asset found");
					ctx.IssuanceCoin = issuance;
					var input = ctx.Transaction.Inputs.CreateNewTxIn();
					input.PrevOut = issuance.Outpoint;
					ctx.Transaction.Inputs.Insert(0, input);
					ctx.ConsumedOutpoints.Add(issuance.Outpoint);
					ctx.CurrentGroupContext.LeftOverChange += issuance.Bearer.Amount;
					if (issuance.DefinitionUrl != null)
					{
						marker.SetMetadataUrl(issuance.DefinitionUrl);
					}
				}

				ctx.Transaction.Outputs.Insert(0, CreateTxOut(GetDust(scriptPubKey), scriptPubKey));
				marker.Quantities = new[] { checked((ulong)asset.Quantity) }.Concat(marker.Quantities).ToArray();
				ctx.CurrentGroupContext.SentOutput += ctx.Transaction.Outputs[0].Value;
				ctx.CurrentGroupContext.SentOutput += asset;
				ctx.CanShuffleOutputs = false;
				ctx.CanMergeOutputs = false;
			});
			return this;
		}
		public TransactionBuilder SetSigningOptions(SigningOptions? signingOptions)
		{
			if (signingOptions == null)
				throw new ArgumentNullException(nameof(signingOptions));
			this.signingOptions = signingOptions ?? new SigningOptions();
			return this;
		}
		public TransactionBuilder SetSigningOptions(SigHash sigHash)
		{
			this.signingOptions = new SigningOptions(sigHash);
			return this;
		}
		public TransactionBuilder SetSigningOptions(SigHash sigHash, bool enforceLowR)
		{
			this.signingOptions = new SigningOptions(sigHash, enforceLowR);
			return this;
		}

		public bool TrySignInput(Transaction transaction, uint index, [MaybeNullWhen(false)] out ITransactionSignature signature)
		{
			if (transaction == null)
				throw new ArgumentNullException(nameof(transaction));
			var ctx = new TransactionSigningContext(this, transaction.Clone(), signingOptions);
			ctx.SignOnlyInputIndex = index;
			this.SignTransactionContext(ctx);
			var input = ctx.PSBT.Inputs[(int)index];
			signature = input.PartialSigs.Select(p => p.Value).FirstOrDefault();
			if (signature is null)
			{
				signature = input.TaprootKeySignature;
			}
			if (signature is null)
			{
				// TODO: Add PSBT_IN_TAP_SCRIPT_SIG
			}
			return signature != null;
		}

		public TransactionBuilder SendFees(Money fees)
		{
			if (fees == null)
				throw new ArgumentNullException(nameof(fees));
			CurrentGroup.FixedFee += fees;
			return this;
		}

		/// <summary>
		/// Split the estimated fees across the several groups (separated by Then())
		/// </summary>
		/// <param name="feeRate"></param>
		/// <returns></returns>
		public TransactionBuilder SendEstimatedFees(FeeRate feeRate)
		{
			FilterUneconomicalCoinsRate = feeRate;
			CurrentGroup.FeeRate = feeRate;
			return this;
		}

		/// <summary>
		/// Estimate the fee needed for the transaction, and split among groups according to their fee weight
		/// </summary>
		/// <param name="feeRate"></param>
		/// <returns></returns>
		public TransactionBuilder SendEstimatedFeesSplit(FeeRate feeRate)
		{
			FilterUneconomicalCoinsRate = feeRate;
			var lastGroup = CurrentGroup; //Make sure at least one group exists
			var totalWeight = _BuilderGroups.Select(b => b.FeeWeight).Sum();
			Money totalSent = Money.Zero;
			var fees = feeRate.GetFee(1000);
			foreach (var group in _BuilderGroups)
			{
				var groupFee = Money.Satoshis((group.FeeWeight / totalWeight) * fees.Satoshi);
				totalSent += groupFee;
				if (_BuilderGroups.Last() == group)
				{
					var leftOver = fees - totalSent;
					groupFee += leftOver;
				}
				group.FeeRate = new FeeRate(groupFee, 1000);
			}
			return this;
		}

		/// <summary>
		/// Send the fee splitted among groups according to their fee weight
		/// </summary>
		/// <param name="fees"></param>
		/// <returns></returns>
		public TransactionBuilder SendFeesSplit(Money fees)
		{
			if (fees == null)
				throw new ArgumentNullException(nameof(fees));
			var lastGroup = CurrentGroup; //Make sure at least one group exists
			var totalWeight = _BuilderGroups.Select(b => b.FeeWeight).Sum();
			Money totalSent = Money.Zero;
			foreach (var group in _BuilderGroups)
			{
				var groupFee = Money.Satoshis((group.FeeWeight / totalWeight) * fees.Satoshi);
				totalSent += groupFee;
				if (_BuilderGroups.Last() == group)
				{
					var leftOver = fees - totalSent;
					groupFee += leftOver;
				}
				group.FixedFee += groupFee;
			}
			return this;
		}


		/// <summary>
		/// If using SendFeesSplit or SendEstimatedFeesSplit, determine the weight this group participate in paying the fees
		/// </summary>
		/// <param name="feeWeight">The weight of fee participation</param>
		/// <returns></returns>
		public TransactionBuilder SetFeeWeight(decimal feeWeight)
		{
			CurrentGroup.FeeWeight = feeWeight;
			return this;
		}

		public TransactionBuilder SetChange(IDestination destination, ChangeType changeType = ChangeType.All)
		{
			return SetChange(destination.ScriptPubKey, changeType);
		}

		public TransactionBuilder SetChange(Script scriptPubKey, ChangeType changeType = ChangeType.All)
		{
			if (CurrentGroup.preventSetChange)
				throw new InvalidOperationException($"You should not call {nameof(SetChange)} after {nameof(SendAllRemaining)}, maybe you should call {nameof(SendAllRemainingToChange)} instead of {nameof(SendAllRemaining)}");
			if ((changeType & ChangeType.Colored) != 0)
			{
				CurrentGroup.ChangeScript[(int)ChangeType.Colored] = scriptPubKey;
			}
			if ((changeType & ChangeType.Uncolored) != 0)
			{
				CurrentGroup.ChangeScript[(int)ChangeType.Uncolored] = scriptPubKey;
			}
			return this;
		}

		public Network Network
		{
			get;
		}

		public TransactionBuilder SetCoinSelector(ICoinSelector selector)
		{
			if (selector == null)
				throw new ArgumentNullException(nameof(selector));
			CoinSelector = selector;
			return this;
		}

		/// <summary>
		/// Build a PSBT (Partially signed bitcoin transaction)
		/// </summary>
		/// <param name="sign">True if signs all inputs with the available keys</param>
		/// <param name="sigHash">The sighash for signing (ignored if sign is false)</param>
		/// <returns>A PSBT</returns>
		/// <exception cref="NBitcoin.NotEnoughFundsException">Not enough funds are available</exception>
		public PSBT BuildPSBT(bool sign)
		{
			var tx = BuildTransaction(false);
			return CreatePSBTFromCore(tx, sign);
		}

		/// <summary>
		/// Create a PSBT from a transaction
		/// </summary>
		/// <param name="tx">The transaction</param>
		/// <param name="sign">If true, the transaction builder will sign this transaction</param>
		/// <returns></returns>
		public PSBT CreatePSBTFrom(Transaction tx, bool sign)
		{
			if (tx == null)
				throw new ArgumentNullException(nameof(tx));
			return CreatePSBTFromCore(tx.Clone(), sign);
		}

		PSBT CreatePSBTFromCore(Transaction tx, bool sign)
		{
			TransactionSigningContext signingContext = new TransactionSigningContext(this, tx, signingOptions);
			if (sign)
				SignTransactionContext(signingContext);
			var psbt = signingContext.PSBT;
			UpdatePSBT(psbt);
			return psbt;
		}

		public TransactionBuilder SignPSBT(PSBT psbt)
		{
			if (psbt == null)
				throw new ArgumentNullException(nameof(psbt));
			var signingContext = new TransactionSigningContext(this, psbt.Clone(), signingOptions);
			SignTransactionContext(signingContext);
			psbt.Combine(signingContext.PSBT);
			return this;
		}

		public TransactionBuilder SignPSBTInput(PSBTInput psbtInput)
		{
			if (psbtInput == null)
				throw new ArgumentNullException(nameof(psbtInput));
			var psbt = psbtInput.PSBT;
			var signingContext = new TransactionSigningContext(this, psbt, signingOptions)
			{
				SignOnlyInputIndex = psbtInput.Index
			};
			SignTransactionContext(signingContext);
			return this;
		}

		public TransactionBuilder FinalizePSBT(PSBT psbt)
		{
			if (psbt is null)
				throw new ArgumentNullException(nameof(psbt));
			var signingContext = new TransactionSigningContext(this, psbt.Clone(), signingOptions);
			FinalizeTransactionContext(signingContext);
			psbt.Combine(signingContext.PSBT);
			return this;
		}

		public TransactionBuilder FinalizePSBTInput(PSBTInput psbtInput)
		{
			if (psbtInput is null)
				throw new ArgumentNullException(nameof(psbtInput));
			var psbt = psbtInput.PSBT;
			var tx = psbt.Settings.IsSmart && (!Script.IsNullOrEmpty(psbtInput.originalScriptSig) ||
					 !WitScript.IsNullOrEmpty(psbtInput.originalWitScript)) ? psbt.GetOriginalTransaction() : null;
			var signingContext = new TransactionSigningContext(this, psbtInput.PSBT, tx, signingOptions)
			{
				SignOnlyInputIndex = psbtInput.Index
			};
			FinalizeTransactionContext(signingContext);
			return this;
		}

		/// <summary>
		/// Try to extract the signatures from <paramref name="transaction"/> into the <paramref name="psbt"/>.
		/// </summary>
		/// <param name="psbt">The PSBT to extract signatures to.</param>
		/// <param name="transaction">The transaction from which signatures will get extracted.</param>
		/// <returns></returns>
		public TransactionBuilder ExtractSignatures(PSBT psbt, Transaction transaction)
		{
			if (psbt is null)
				throw new ArgumentNullException(nameof(psbt));
			if (transaction is null)
				throw new ArgumentNullException(nameof(transaction));
			var signingContext = new TransactionSigningContext(this, transaction, signingOptions);
			ExtractExistingSignatures(signingContext);
			psbt.Combine(signingContext.PSBT);
			return this;
		}

		/// <summary>
		/// Update information in the PSBT with informations that the transaction builder is holding
		/// </summary>
		/// <param name="psbt">A PSBT</param>
		public TransactionBuilder UpdatePSBT(PSBT psbt)
		{
			if (psbt == null)
				throw new ArgumentNullException(nameof(psbt));
			var tx = psbt.GetOriginalTransaction();
			psbt.AddCoins(tx.Inputs.AsIndexedInputs()
				.Select(i => this.FindSignableCoin(i) ?? this.FindCoin(i.PrevOut))
				.ToArray());

			psbt.AddScripts(_ScriptPubKeyToRedeem.Values.ToArray());
			return this;
		}

		/// <summary>
		/// Build the transaction
		/// </summary>
		/// <param name="sign">True if signs all inputs with the available keys</param>
		/// <returns>The transaction</returns>
		/// <exception cref="NBitcoin.NotEnoughFundsException">Not enough funds are available</exception>
		public Transaction BuildTransaction(bool sign)
		{
			int totalRepass = 5;
			DoShuffleGroups();
			TransactionBuildingContext ctx = new TransactionBuildingContext(this);
			retry:
			ctx.Transaction.LockTime = _LockTime;
			if (_Version is uint v)
				ctx.Transaction.Version = v;
			foreach (var group in _BuilderGroups)
			{
				ctx.SetGroup(group);
				ctx.ChangeType = ChangeType.Colored;
				ctx.AddChangeTxOut = SetColoredChange;
				foreach (var builder in group.IssuanceBuilders)
					builder(ctx);

				var buildersByAsset = group.BuildersByAsset.ToList();
				foreach (var builders in buildersByAsset)
				{
					ctx.Zero = new AssetMoney(builders.Key);
					var coins = group.CoinsOfType<ColoredCoin>().Where(c => c.Amount.Id == builders.Key);
					ctx.GetDust = null;
					var btcSpent = BuildTransaction(ctx, group, builders.Value, coins)
									.OfType<IColoredCoin>().Select(c => c.Bearer.Amount).Sum();
					ctx.CurrentGroupContext.LeftOverChange += btcSpent;
				}
				ctx.GetDust = GetDust;
				ctx.Zero = Money.Zero;
				ctx.ChangeType = ChangeType.Uncolored;

				var builderList = group.Builders.ToList();
				ctx.AddChangeTxOut = SetChange;
				BuildTransaction(ctx, group, builderList, group.CoinsOfType<Coin>()
															   .Where(c => c.Amount >= ctx.CurrentGroupContext.MinValue)
															   .Where(IsEconomical));
			}

			if (ShuffleRandom != null)
			{
				if (ShuffleInputs && ctx.CanShuffleInputs)
					Utils.Shuffle(ctx.Transaction.Inputs,
								0,
								ShuffleRandom);
				if (ShuffleOutputs && ctx.CanShuffleOutputs)
					Utils.Shuffle(ctx.Transaction.Outputs,
								0,
								ShuffleRandom);
			}
			if (MergeOutputs && ctx.CanMergeOutputs)
			{
				var collapsedOutputs = ctx.Transaction.Outputs
							   .GroupBy(o => o.ScriptPubKey)
							   .Select(o => o.Count() == 1 ? o.First() : ctx.Transaction.Outputs.CreateNewTxOut(o.Select(txout => txout.Value).Sum(), o.Key))
							   .ToArray();
				if (collapsedOutputs.Length < ctx.Transaction.Outputs.Count)
				{
					ctx.Transaction.Outputs.Clear();
					ctx.Transaction.Outputs.AddRange(collapsedOutputs);
				}
			}
			ctx.InsertOpenAssetMarker();
			AfterBuild(ctx.Transaction);

			// The first pass always have SizeFee to 0 because we can't
			// know them before we can get a reasonable guess of transaction
			// size.
			bool needRepass = false;
			var estimatedSize = this.EstimateSize(ctx.Transaction, true);
			var consumed = ctx.ConsumedOutpoints.Select(c => FindCoin(c)).Where(c => c != null).ToArray();
			var fee = ctx.Transaction.GetFee(consumed);
			if (fee is null)
				throw new InvalidOperationException("Can't get fee after transaction building, this should never happen. (contact NBitcoin's authors if you see this)");
			foreach (var gctx in ctx.GroupContexts)
			{
				var oldSizeFee = gctx.SizeFee;
				var newSizeFee = gctx.Group.FeeRate?.GetFee(estimatedSize);
				if (newSizeFee != null)
				{
					gctx.SizeFee = newSizeFee;
					var additionalFee = newSizeFee - oldSizeFee;
					if (additionalFee < Money.Zero)
					{
						// If the size decreased, try to give more money back to the fee output
						// so we don't have to do a repass every times a coin selector
						// is choosing a slightly different coin set.
						if (gctx.FeeTxOut is TxOut txout)
						{
							txout.Value += -additionalFee;
						}
						// No change, so we can't give back money. Though, let's make sure
						// we don't do infinite loop for peanuts
						else if (totalRepass-- > 0)
						{
							needRepass = true;
						}
					}
					// We need to pay more, so we need to repass
					if (additionalFee > Money.Zero)
					{
						needRepass = true;
					}
				}
			}

			// The transaction is too big and would never be accepted by policy rules.
			// We try to repass by removing small value inputs.
			if (estimatedSize > MAX_TX_VSIZE)
			{
				needRepass = true;
				var bytesPerInput = (double)estimatedSize / (double)ctx.Transaction.Inputs.Count;
				var maxInputCount = (int)((double)MAX_TX_VSIZE / bytesPerInput);
				var inputsToDelete = ctx.Transaction.Inputs.Count - maxInputCount;
				var minValue = ctx.Selection.OfType<Coin>()
										.OrderBy(c => c.Amount)
										.Skip(inputsToDelete - 1)
										.Select(c => c.Amount + Money.Satoshis(1))
										.First();
				ctx.SmallInputsExcluded = true;
				foreach (var group in _BuilderGroups)
				{
					ctx.SetGroup(group);
					// We need to recalculate the SizeFee
					ctx.CurrentGroupContext.SizeFee = Money.Zero;
					ctx.CurrentGroupContext.MinValue = minValue;
				}
			}

			if (needRepass)
			{
				var newCtx = new TransactionBuildingContext(this);
				newCtx.SmallInputsExcluded = ctx.SmallInputsExcluded;
				foreach (var group in _BuilderGroups)
				{
					newCtx.SetGroup(group);
					ctx.SetGroup(group);
					newCtx.CurrentGroupContext.SizeFee = ctx.CurrentGroupContext.SizeFee;
					newCtx.CurrentGroupContext.MinValue = ctx.CurrentGroupContext.MinValue;
				}
				ctx = newCtx;
				goto retry;
			}


			if (sign)
			{
				SignTransactionInPlace(ctx.Transaction);
			}

			if (ctx.Transaction.Outputs.Count == 0)
				throw new NotEnoughFundsException("Not enough funds to create even one change output", null, GetDust());
			return ctx.Transaction;
		}

		bool IsEconomical(Coin c)
		{
			if (!FilterUneconomicalCoins || FilterUneconomicalCoinsRate == null)
				return true;
			int witSize = 0;
			int baseSize = 0;
			EstimateScriptSigSize(c, ref witSize, ref baseSize);
			var vSize = witSize / Transaction.WITNESS_SCALE_FACTOR + baseSize;
			return c.Amount >= FilterUneconomicalCoinsRate.GetFee(vSize);
		}

		private ICoin[] BuildTransaction(
			TransactionBuildingContext ctx,
			BuilderGroup group,
			List<Action<TransactionBuilder.TransactionBuildingContext>> builders,
			IEnumerable<ICoin> coins)
		{
			var gctx = ctx.CurrentGroupContext;
			IMoney zero = ctx.Zero;
			IMoney surrendedMoney = zero is Money ? group.SurrendedValue : zero;
			foreach (var builder in builders)
				builder(ctx);

			IMoney selectionTarget = (gctx.SentOutput + gctx.Fee - gctx.LeftOverChange + surrendedMoney).GetAmount(zero);

			var unconsumed = coins.Where(c => !ctx.ConsumedOutpoints.Contains(c.Outpoint)).ToArray();

			var selection =
				group.sendAllToChange ? unconsumed :
				selectionTarget.CompareTo(zero) <= 0 ? new ICoin[0] :
				CoinSelector.Select(unconsumed, selectionTarget)?.ToArray();

			var notEnoughFundsMessage =
				ctx.SmallInputsExcluded ? "You may have enough funds to cover the target, but the transaction's size would be too high."
										: "Not enough funds to cover the target";
			if (selection == null)
				throw new NotEnoughFundsException(notEnoughFundsMessage,
						group.Name,
						selectionTarget.Sub(unconsumed.Select(u => u.Amount).Sum(zero)));

			var totalInput = selection.Select(s => s.Amount).Sum(zero);
			var change = totalInput.Sub(selectionTarget).Add(surrendedMoney);
			if (change.CompareTo(zero) == -1)
				throw new NotEnoughFundsException(notEnoughFundsMessage,
					group.Name,
					change.Negate()
				);


			if (change.CompareTo(zero) > 0)
			{
				var changeScript = group.ChangeScript[(int)ctx.ChangeType];
				if (changeScript is null)
					throw new InvalidOperationException("A change address should be specified (" + ctx.ChangeType + ")");
				var dust = ctx.GetDust == null ? null : ctx.GetDust(changeScript);
				if (!DustPrevention || dust == null || change.CompareTo(dust) >= 0)
				{
					var changeTxout = ctx.AddChangeTxOut(change, ctx);
					if (zero is Money)
						ctx.CurrentGroupContext.FeeTxOut ??= changeTxout;
					ctx.CurrentGroupContext.SentOutput += change;
				}
				else if (change.CompareTo(dust) > 0)
				{
					gctx.DustPreventionTotalRemoved += change;
				}
			}
			var inputsPerOutpoints = ctx.Transaction.Inputs.ToDictionary(o => o.PrevOut);
			foreach (var coin in selection)
			{
				ctx.ConsumedOutpoints.Add(coin.Outpoint);
				if (!inputsPerOutpoints.TryGetValue(coin.Outpoint, out var input))
				{
					input = ctx.Transaction.Inputs.Add(coin.Outpoint);
					inputsPerOutpoints.Add(coin.Outpoint, input);
				}
				var options = group.GetOptions(coin);
				if (options?.Sequence is Sequence seq)
				{
					input.Sequence = seq;
				}
				else if (OptInRBF)
				{
					input.Sequence = Sequence.OptInRBF;
				}
				else if (_LockTime != LockTime.Zero)
				{
					input.Sequence = Sequence.FeeSnipping;
				}
			}
			gctx.Selection.AddRange(selection);
			return selection;
		}

		protected virtual void AfterBuild(Transaction transaction)
		{
		}

		public Transaction SignTransaction(Transaction transaction)
		{
			var tx = transaction.Clone();
			SignTransactionInPlace(tx);
			return tx;
		}

		/// <summary>
		/// Sign the transaction passed as parameter
		/// </summary>
		/// <param name="transaction">The transaction</param>
		/// <returns>The transaction object as the one passed as parameter</returns>
		public Transaction SignTransactionInPlace(Transaction transaction)
		{
			var ctx = new TransactionSigningContext(this, transaction, signingOptions);
			ExtractExistingSignatures(ctx);
			SignTransactionContext(ctx);
			FinalizeTransactionContext(ctx);
			MergePartialSignatures(ctx);
			SetFinalScripts(ctx, transaction);
			return transaction;
		}

		private static void SetFinalScripts(TransactionSigningContext ctx, Transaction transaction)
		{
			foreach (var input in ctx.PSBT.Inputs)
			{
				if (input.IsFinalized())
				{
					var txin = transaction.Inputs[input.Index];
					txin.ScriptSig = input.FinalScriptSig ?? Script.Empty;
					txin.WitScript = input.FinalScriptWitness ?? WitScript.Empty;
				}
			}
		}

		/// <summary>
		/// Estimate the fee rate of the transaction once it is fully signed.
		/// </summary>
		/// <param name="tx">The transaction to be signed</param>
		/// <returns>The fee rate, or null if the transaction builder is missing previous coins</returns>
		/// <exception cref="CoinNotFoundException">If the transaction builder is missing some coins</exception>
		public FeeRate EstimateFeeRate(Transaction tx)
		{
			if (tx == null)
				throw new ArgumentNullException(nameof(tx));
			var vSize = this.EstimateSize(tx, true);
			var fee = tx.GetFee(this.FindSpentCoins(tx));
			return new FeeRate(fee, vSize);
		}

		void ExtractExistingSignatures(TransactionSigningContext ctx)
		{
			foreach (var inputCtx in ctx.GetInputSigningContexts())
			{
				inputCtx.Extension.ExtractExistingSignatures(inputCtx);
			}
		}

		void SignTransactionContext(TransactionSigningContext ctx)
		{

			foreach (var inputCtx in ctx.GetInputSigningContexts())
			{
				if (inputCtx.Coin.TxOut.ScriptPubKey.IsScriptType(ScriptType.Taproot)
					&& !(ctx.SigningOptions.PrecomputedTransactionData is TaprootReadyPrecomputedTransactionData))
				{
					throw new InvalidOperationException($"Impossible to sign taproot input {inputCtx.Input.Index}.\n" +
						$"Either use TransactionBuilder.AddCoins and add all the coins spent by the transaction to sign, or set SigningOptions.PrecomputedTransactionData via TransactionBuilder.SetSigningOptions to an instance of type TaprootReadyPrecomputedTransactionData.");
				}
				var signer = new CompositeSigner(GetSigners(inputCtx).ToArray());
				var keyrepo = new CompositeKeyRepository(GetKeyRepositories(inputCtx).ToArray());
				inputCtx.Extension.Sign(inputCtx, keyrepo, signer);
			}
		}

		private void MergePartialSignatures(TransactionSigningContext ctx)
		{
			foreach (var inputCtx in ctx.GetInputSigningContexts())
			{
				if (inputCtx.Input.IsFinalized())
					continue;
				inputCtx.Extension.MergePartialSignatures(inputCtx);
			}
		}

		void FinalizeTransactionContext(TransactionSigningContext ctx)
		{
			foreach (var inputCtx in ctx.GetInputSigningContexts())
			{
				if (inputCtx.Input.IsFinalized())
					continue;
				inputCtx.Extension.Finalize(inputCtx);
				if (!inputCtx.Input.IsFinalized())
					continue;
				var txIn = inputCtx.Input;
				ScriptCoin? scriptCoin = inputCtx.Coin as ScriptCoin;
				if (!inputCtx.Coin.IsMalleable && WitScript.IsNullOrEmpty(txIn.FinalScriptWitness))
				{
					txIn.FinalScriptWitness = txIn.FinalScriptSig;
					txIn.FinalScriptSig = Script.Empty;
					if (scriptCoin != null)
					{
						if (scriptCoin.IsP2SH)
							txIn.FinalScriptSig = new Script(Op.GetPushOp(scriptCoin.GetP2SHRedeem().ToBytes(true)));
						if (scriptCoin.RedeemType == RedeemType.WitnessV0)
							txIn.FinalScriptWitness = txIn.FinalScriptWitness + new WitScript(Op.GetPushOp(scriptCoin.Redeem.ToBytes(true)));
					}
				}
				else
				{
					if (scriptCoin != null && scriptCoin.RedeemType == RedeemType.P2SH)
					{
						txIn.FinalScriptSig = txIn.FinalScriptSig + Op.GetPushOp(scriptCoin.GetP2SHRedeem().ToBytes(true));
					}
				}
			}
		}

		IEnumerable<ISigner> GetSigners(InputSigningContext inputCtx)
		{
			yield return new TransactionBuilderSigner(inputCtx);
			yield return new KnownSignatureSigner(inputCtx, _KnownSignatures);
		}
		IEnumerable<IKeyRepository> GetKeyRepositories(InputSigningContext inputCtx)
		{
			yield return new TransactionBuilderKeyRepository(inputCtx);
			yield return new KnownSignatureSigner(inputCtx, _KnownSignatures);
		}
		public ICoin? FindSignableCoin(IndexedTxIn txIn)
		{
			return FindSignableCoin(txIn.TxIn);
		}

		public ICoin? FindSignableCoin(TxIn txIn)
		{
			var coin = FindCoin(txIn.PrevOut);
			if (coin is IColoredCoin)
				coin = ((IColoredCoin)coin).Bearer;
			if (coin == null || coin is ScriptCoin)
				return coin;

			var hash = ScriptCoin.GetRedeemHash(coin.TxOut.ScriptPubKey);
			if (hash != null)
			{
				var redeem = _ScriptPubKeyToRedeem.TryGet(coin.TxOut.ScriptPubKey);
				if (redeem != null && PayToWitScriptHashTemplate.Instance.CheckScriptPubKey(redeem))
					redeem = _ScriptPubKeyToRedeem.TryGet(redeem);
				if (redeem == null)
				{
					if (hash is WitScriptId)
						redeem = PayToWitScriptHashTemplate.Instance.ExtractWitScriptParameters(txIn.WitScript, (WitScriptId)hash);
					if (hash is ScriptId)
					{
						var parameters = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(txIn.ScriptSig, (ScriptId)hash);
						if (parameters != null)
						{
							redeem = parameters.RedeemScript;
							var witHash = PayToWitScriptHashTemplate.Instance.ExtractScriptPubKeyParameters(redeem);
							if (witHash != null)
							{
								redeem = PayToWitScriptHashTemplate.Instance.ExtractWitScriptParameters(txIn.WitScript, witHash) ?? redeem;
							}
						}
					}
				}
				if (redeem != null)
					return new ScriptCoin(coin, redeem);
			}
			return coin;
		}

		TxOut[] GetSpentOutputs(Transaction tx)
		{
			TxOut[] outputs = new TxOut[tx.Inputs.Count];
			foreach (var input in tx.Inputs.AsIndexedInputs())
			{
				var c = FindCoin(input.PrevOut);
				if (c is ICoin)
					outputs[input.Index] = c.TxOut;
				else
					throw new CoinNotFoundException(input);
			}
			return outputs;
		}

		/// <summary>
		/// Verify that a transaction is fully signed and have enough fees
		/// </summary>
		/// <param name="tx">The transaction to check</param>
		/// <returns>True if no error</returns>
		public bool Verify(Transaction tx)
		{
			TransactionPolicyError[] errors;
			return Verify(tx, null as Money, out errors);
		}
		/// <summary>
		/// Verify that a transaction is fully signed and have enough fees
		/// </summary>
		/// <param name="validator">The transaction validator</param>
		/// <returns>True if no error</returns>
		public bool Verify(TransactionValidator validator)
		{
			TransactionPolicyError[] errors;
			return Verify(validator, null as Money, out errors);
		}
		/// <summary>
		/// Verify that a transaction is fully signed and have enough fees
		/// </summary>
		/// <param name="tx">The transaction to check</param>
		/// <param name="expectedFees">The expected fees (more or less 10%)</param>
		/// <returns>True if no error</returns>
		public bool Verify(Transaction tx, Money expectedFees)
		{
			TransactionPolicyError[] errors;
			return Verify(tx, expectedFees, out errors);
		}
		/// <summary>
		/// Verify that a transaction is fully signed and have enough fees
		/// </summary>
		/// <param name="validator">The transaction validator</param>
		/// <param name="expectedFees">The expected fees (more or less 10%)</param>
		/// <returns>True if no error</returns>
		public bool Verify(TransactionValidator validator, Money expectedFees)
		{
			TransactionPolicyError[] errors;
			return Verify(validator, expectedFees, out errors);
		}

		/// <summary>
		/// Verify that a transaction is fully signed and have enough fees
		/// </summary>
		/// <param name="tx">The transaction to check</param>
		/// <param name="expectedFeeRate">The expected fee rate</param>
		/// <returns>True if no error</returns>
		public bool Verify(Transaction tx, FeeRate expectedFeeRate)
		{
			TransactionPolicyError[] errors;
			return Verify(tx, expectedFeeRate, out errors);
		}
		/// <summary>
		/// Verify that a transaction is fully signed and have enough fees
		/// </summary>
		/// <param name="validator">The transaction validator</param>
		/// <param name="expectedFeeRate">The expected fee rate</param>
		/// <returns>True if no error</returns>
		public bool Verify(TransactionValidator validator, FeeRate expectedFeeRate)
		{
			TransactionPolicyError[] errors;
			return Verify(validator, expectedFeeRate, out errors);
		}

		/// <summary>
		/// Verify that a transaction is fully signed and have enough fees
		/// </summary>
		/// <param name="tx">The transaction to check</param>
		/// <param name="errors">Detected errors</param>
		/// <returns>True if no error</returns>
		public bool Verify(Transaction tx, out TransactionPolicyError[] errors)
		{
			return Verify(tx, null as Money, out errors);
		}
		/// <summary>
		/// Verify that a transaction is fully signed and have enough fees
		/// </summary>
		/// <param name="validator">The transaction validator</param>
		/// <param name="errors">Detected errors</param>
		/// <returns>True if no error</returns>
		public bool Verify(TransactionValidator validator, out TransactionPolicyError[] errors)
		{
			return Verify(validator, null as Money, out errors);
		}

		public TransactionValidator CreateTransactionValidatorFromCoins(Transaction tx)
		{
			if (tx == null)
				throw new ArgumentNullException(nameof(tx));
			var validator = tx.CreateValidator(GetSpentOutputs(tx));
			if (StandardTransactionPolicy.ScriptVerify is ScriptVerify s)
				validator.ScriptVerify = s;
			return validator;
		}

		/// <summary>
		/// Verify that a transaction is fully signed, have enough fees, and follow the Standard and Miner Transaction Policy rules
		/// </summary>
		/// <param name="tx">The transaction to check</param>
		/// <param name="expectedFees">The expected fees (more or less 10%)</param>
		/// <param name="errors">Detected errors</param>
		/// <returns>True if no error</returns>
		public bool Verify(Transaction tx, Money? expectedFees, out TransactionPolicyError[] errors)
		{
			if (tx == null)
				throw new ArgumentNullException(nameof(tx));
			return Verify(CreateTransactionValidatorFromCoins(tx), expectedFees, out errors);
		}

		/// <summary>
		/// Verify that a transaction is fully signed, have enough fees, and follow the Standard and Miner Transaction Policy rules
		/// </summary>
		/// <param name="validator">The transaction validator</param>
		/// <param name="expectedFees">The expected fees (more or less 10%)</param>
		/// <param name="errors">Detected errors</param>
		/// <returns>True if no error</returns>
		public bool Verify(TransactionValidator validator, Money? expectedFees, out TransactionPolicyError[] errors)
		{
			if (validator == null)
				throw new ArgumentNullException(nameof(validator));
			List<TransactionPolicyError> exceptions = new List<TransactionPolicyError>();
			var policyErrors = MinerTransactionPolicy.Instance.Check(validator);
			exceptions.AddRange(policyErrors);
			policyErrors = StandardTransactionPolicy.Check(validator);
			exceptions.AddRange(policyErrors);
			if (expectedFees != null)
			{
				var fees = validator.Transaction.GetFee(validator.SpentOutputs);
				if (fees != null)
				{
					Money margin = Money.Zero;
					if (!fees.Almost(expectedFees, margin))
						exceptions.Add(new NotEnoughFundsPolicyError("Fees different than expected", expectedFees - fees));
				}
			}
			errors = exceptions.ToArray();
			return errors.Length == 0;
		}
		/// <summary>
		/// Verify that a transaction is fully signed and have enough fees
		/// </summary>
		/// <param name="tx">The transaction to check</param>
		/// <param name="expectedFeeRate">The expected fee rate</param>
		/// <param name="errors">Detected errors</param>
		/// <returns>True if no error</returns>
		public bool Verify(Transaction tx, FeeRate expectedFeeRate, out TransactionPolicyError[] errors)
		{
			if (tx == null)
				throw new ArgumentNullException(nameof(tx));
			return Verify(tx, expectedFeeRate == null ? null : expectedFeeRate.GetFee(tx), out errors);
		}
		/// <summary>
		/// Verify that a transaction is fully signed and have enough fees
		/// </summary>
		/// <param name="validator">The transaction validator</param>
		/// <param name="expectedFeeRate">The expected fee rate</param>
		/// <param name="errors">Detected errors</param>
		/// <returns>True if no error</returns>
		public bool Verify(TransactionValidator validator, FeeRate expectedFeeRate, out TransactionPolicyError[] errors)
		{
			if (validator == null)
				throw new ArgumentNullException(nameof(validator));
			return Verify(validator, expectedFeeRate == null ? null : expectedFeeRate.GetFee(validator.Transaction), out errors);
		}

		/// <summary>
		/// Verify that a transaction is fully signed and have enough fees
		/// </summary>
		/// <param name="tx">he transaction to check</param>
		/// <param name="expectedFeeRate">The expected fee rate</param>
		/// <returns>Detected errors</returns>
		public TransactionPolicyError[] Check(Transaction tx, FeeRate expectedFeeRate)
		{
			return Check(tx, expectedFeeRate == null ? null : expectedFeeRate.GetFee(tx));
		}
		/// <summary>
		/// Verify that a transaction is fully signed and have enough fees
		/// </summary>
		/// <param name="tx">he transaction to check</param>
		/// <param name="expectedFee">The expected fee</param>
		/// <returns>Detected errors</returns>
		public TransactionPolicyError[] Check(Transaction tx, Money? expectedFee)
		{
			TransactionPolicyError[] errors;
			Verify(tx, expectedFee, out errors);
			return errors;
		}
		/// <summary>
		/// Verify that a transaction is fully signed and have enough fees
		/// </summary>
		/// <param name="tx">he transaction to check</param>
		/// <returns>Detected errors</returns>
		public TransactionPolicyError[] Check(Transaction tx)
		{
			return Check(tx, null as Money);
		}

		private CoinNotFoundException CoinNotFound(IndexedTxIn txIn)
		{
			return new CoinNotFoundException(txIn);
		}


		public ICoin? FindCoin(OutPoint outPoint)
		{
			var result = _BuilderGroups.Select(c => c.CoinsWithOptions.TryGet(outPoint)).FirstOrDefault(r => r != null)?.Coin;
			if (result == null && CoinFinder != null)
				result = CoinFinder(outPoint);
			return result;
		}

		internal CoinOptions? FindCoinOptions(OutPoint outPoint)
		{
			return _BuilderGroups.Select(c => c.CoinsWithOptions.TryGet(outPoint)).FirstOrDefault(r => r != null)?.Options;
		}

		/// <summary>
		/// Find spent coins of a transaction
		/// </summary>
		/// <param name="tx">The transaction</param>
		/// <returns>Array of size tx.Input.Count, if a coin is not fund, a null coin is returned.</returns>
		public ICoin[] FindSpentCoins(Transaction tx)
		{
			var dummy = new Coin();
			return
				tx
				.Inputs
				.Select(i => FindCoin(i.PrevOut) ?? dummy)
				.Where(c => c != dummy)
				.ToArray();
		}

		/// <summary>
		/// Estimate the physical size of the transaction
		/// </summary>
		/// <param name="tx">The transaction to be estimated</param>
		/// <returns></returns>
		public int EstimateSize(Transaction tx)
		{
			return EstimateSize(tx, false);
		}

		/// <summary>
		/// Estimate the size of the transaction
		/// </summary>
		/// <param name="tx">The transaction to be estimated</param>
		/// <param name="virtualSize">If true, returns the size on which fee calculation are based, else returns the physical byte size</param>
		/// <returns></returns>
		public int EstimateSize(Transaction tx, bool virtualSize)
		{
			if (tx == null)
				throw new ArgumentNullException(nameof(tx));
			EstimateSizes(tx, out var witSize, out var baseSize);
			if (virtualSize)
			{
				var totalSize = witSize + baseSize;
				var strippedSize = baseSize;
				var weight = strippedSize * (Transaction.WITNESS_SCALE_FACTOR - 1) + totalSize;
				return (weight + Transaction.WITNESS_SCALE_FACTOR - 1) / Transaction.WITNESS_SCALE_FACTOR;
			}
			return witSize + baseSize;
		}

		/// <summary>
		/// Estimate the witness size and the base size of a transaction
		/// </summary>
		/// <param name="tx">The transaction</param>
		/// <param name="witSize">The witness size</param>
		/// <param name="baseSize">The base size</param>
		/// <exception cref="CoinNotFoundException">If the transaction builder is missing some coins</exception>
		public void EstimateSizes(Transaction tx, out int witSize, out int baseSize)
		{
			if (tx == null)
				throw new ArgumentNullException(nameof(tx));
			var clone = tx.Clone();
			clone.Inputs.Clear();
			baseSize = clone.GetSerializedSize() - 1;
			baseSize += new Protocol.VarInt((ulong)tx.Inputs.Count).GetSerializedSize();

			witSize = 0;
			int nonWitnessCount = 0;
			bool hasWitness = tx.HasWitness;
			foreach (var txin in tx.Inputs.AsIndexedInputs())
			{
				var coin = FindSignableCoin(txin) ?? FindCoin(txin.PrevOut);
				if (coin == null)
					throw CoinNotFound(txin);
				if (!coin.IsMalleable)
					hasWitness = true;
				else
					nonWitnessCount++;
				EstimateScriptSigSize(coin, ref witSize, ref baseSize);
				baseSize += (32 + 4) + 4;
			}


			if (hasWitness)
			{
				witSize += 2; // 1 Dummy + 1 Flag
				witSize += nonWitnessCount; // Non witness inputs have 1 byte pushed on the witness
			}
		}

		private TxOut CreateTxOut(Money? amount = null, Script? script = null)
		{
			if (!this.Network.Consensus.ConsensusFactory.TryCreateNew<TxOut>(out var txOut))
				txOut = new TxOut();
			if (amount != null)
				txOut.Value = amount;
			if (script != null)
				txOut.ScriptPubKey = script;
			return txOut;
		}

		private void EstimateScriptSigSize(ICoin coin, ref int witSize, ref int baseSize)
		{
			if (coin is IColoredCoin)
				coin = ((IColoredCoin)coin).Bearer;

			int p2shPushRedeemSize = 0;
			int segwitPushRedeemSize = 0;
			if (coin is ScriptCoin scriptCoin)
			{
				var p2sh = scriptCoin.GetP2SHRedeem();
				if (p2sh != null)
				{
					coin = new Coin(scriptCoin.Outpoint, CreateTxOut(scriptCoin.Amount, p2sh));
					p2shPushRedeemSize = new Script(Op.GetPushOp(p2sh.ToBytes(true))).Length;
					baseSize += p2shPushRedeemSize;
					if (scriptCoin.RedeemType == RedeemType.WitnessV0)
					{
						coin = new ScriptCoin(coin, scriptCoin.Redeem);
					}
				}

				if (scriptCoin.RedeemType == RedeemType.WitnessV0)
				{
					segwitPushRedeemSize = new Script(Op.GetPushOp(scriptCoin.Redeem.ToBytes(true))).Length;
					witSize += segwitPushRedeemSize;
				}
			}

			var scriptSigSize = -1;
			foreach (var extension in Extensions)
			{
				if (extension.CanEstimateScriptSigSize(coin))
				{
					scriptSigSize = extension.EstimateScriptSigSize(coin);
					break;
				}
			}

			if (scriptSigSize == -1)
				scriptSigSize += coin.TxOut.ScriptPubKey.Length; //Using heurestic to approximate size of unknown scriptPubKey
			if (!coin.IsMalleable)
			{
				baseSize += new Protocol.VarInt((ulong)p2shPushRedeemSize).GetSerializedSize();
				witSize += scriptSigSize + new Protocol.VarInt((ulong)(scriptSigSize + segwitPushRedeemSize)).GetSerializedSize();
			}
			if (coin.GetHashVersion() == HashVersion.Original)
				baseSize += scriptSigSize + new Protocol.VarInt((ulong)(scriptSigSize + p2shPushRedeemSize)).GetSerializedSize();
		}

		/// <summary>
		/// Estimate fees of the built transaction
		/// </summary>
		/// <param name="feeRate">Fee rate</param>
		/// <returns></returns>
		public Money EstimateFees(FeeRate feeRate)
		{
			if (feeRate == null)
				throw new ArgumentNullException(nameof(feeRate));

			Money feeSent = Money.Zero;
			try
			{
				while (true)
				{
					var tx = BuildTransaction(false);
					var shouldSend = EstimateFees(tx, feeRate);
					var delta = shouldSend - feeSent;
					if (delta <= Money.Zero)
						break;
					SendFees(delta);
					feeSent += delta;
				}
			}
			finally
			{
				CurrentGroup.FixedFee -= feeSent;
			}
			return feeSent;
		}

		/// <summary>
		/// Estimate fees of an unsigned transaction
		/// </summary>
		/// <param name="tx"></param>
		/// <param name="feeRate">Fee rate</param>
		/// <returns></returns>
		public Money EstimateFees(Transaction tx, FeeRate feeRate)
		{
			if (tx == null)
				throw new ArgumentNullException(nameof(tx));
			if (feeRate == null)
				throw new ArgumentNullException(nameof(feeRate));

			var estimation = EstimateSize(tx, true);
			return feeRate.GetFee(estimation);
		}

		private static void AdjustFinalScripts(ICoin coin, PSBTInput txIn)
		{
			if (txIn.FinalScriptSig is Script)
			{
				ScriptCoin? scriptCoin = coin as ScriptCoin;
				if (!coin.IsMalleable)
				{
					txIn.FinalScriptWitness = txIn.FinalScriptSig;
					txIn.FinalScriptSig = null;
					if (scriptCoin != null)
					{
						if (scriptCoin.IsP2SH)
							txIn.FinalScriptSig = new Script(Op.GetPushOp(scriptCoin.GetP2SHRedeem().ToBytes(true)));
						if (scriptCoin.RedeemType == RedeemType.WitnessV0)
							txIn.FinalScriptWitness = txIn.FinalScriptWitness + new WitScript(Op.GetPushOp(scriptCoin.Redeem.ToBytes(true)));
					}
				}
				else
				{
					if (scriptCoin != null && scriptCoin.RedeemType == RedeemType.P2SH)
					{
						txIn.FinalScriptSig = txIn.FinalScriptSig + Op.GetPushOp(scriptCoin.GetP2SHRedeem().ToBytes(true));
					}
				}
			}
		}

		List<Tuple<IPubKey, ITransactionSignature, OutPoint>> _KnownSignatures = new List<Tuple<IPubKey, ITransactionSignature, OutPoint>>();
		internal bool IsCompatibleKeyFromScriptCode(IPubKey pubKey, Script scriptPubKey)
		{
			return _Extensions.Any(e => e.IsCompatibleKey(pubKey, scriptPubKey));
		}

		/// <summary>
		/// Create a new participant in the transaction with its own set of coins and keys
		/// </summary>
		/// <returns></returns>
		public TransactionBuilder Then()
		{
			_CurrentGroup = null;
			_LastSendBuilder = null;
			return this;
		}

		/// <summary>
		/// Switch to another participant in the transaction, or create a new one if it is not found.
		/// </summary>
		/// <returns></returns>
		public TransactionBuilder Then(string groupName)
		{
			var group = _BuilderGroups.FirstOrDefault(g => g.Name == groupName);
			if (group == null)
			{
				group = new BuilderGroup(this);
				_BuilderGroups.Add(group);
				group.Name = groupName;
			}
			_CurrentGroup = group;
			return this;
		}

		public TransactionBuilder AddCoins(Transaction transaction)
		{
			var txId = transaction.GetHash();
			AddCoins(transaction.Outputs.Select((o, i) => new Coin(txId, (uint)i, o.Value, o.ScriptPubKey)).ToArray());
			return this;
		}

		Dictionary<Script, Script> _ScriptPubKeyToRedeem = new Dictionary<Script, Script>();
		private SigningOptions signingOptions = new SigningOptions();

		public TransactionBuilder AddKnownRedeems(params Script[] knownRedeems)
		{
			foreach (var redeem in knownRedeems)
			{
				_ScriptPubKeyToRedeem.AddOrReplace(redeem.WitHash.ScriptPubKey.Hash.ScriptPubKey, redeem); //Might be P2SH(PWSH)
				_ScriptPubKeyToRedeem.AddOrReplace(redeem.Hash.ScriptPubKey, redeem); //Might be P2SH
				_ScriptPubKeyToRedeem.AddOrReplace(redeem.WitHash.ScriptPubKey, redeem); //Might be PWSH
			}
			return this;
		}

		[Obsolete("Use PSBTs rather than raw transactions to combine signatures.")]
		public Transaction? CombineSignatures(params Transaction[] transactions)
		{
			if (transactions.Length == 1)
				return transactions[0];
			if (transactions.Length == 0)
				return null;

			PSBT[] psbts = new PSBT[transactions.Length];

			for (int i = 0; i < psbts.Length; i++)
			{
				var ctx = new TransactionSigningContext(this, transactions[i], signingOptions);
				ExtractExistingSignatures(ctx);
				psbts[i] = ctx.PSBT;
			}
			var psbt = psbts[0];
			for (int i = 1; i < psbts.Length; i++)
			{
				psbt = psbt.Combine(psbts[i]);
			}
			var ctx2 = new TransactionSigningContext(this, psbt, psbt.GetOriginalTransaction(), signingOptions);
			FinalizeTransactionContext(ctx2);
			MergePartialSignatures(ctx2);
			var tx = ctx2.Transaction!;
			SetFinalScripts(ctx2, tx);
			for (int i = 0; i < tx.Inputs.Count; i++)
			{
				var txin = tx.Inputs[i];
				if (txin.ScriptSig == Script.Empty)
				{
					txin.ScriptSig = transactions
										.Select(tx => tx.Inputs[i].ScriptSig).FirstOrDefault(s => !Script.IsNullOrEmpty(s))
										?? Script.Empty;
				}
				if (txin.WitScript == WitScript.Empty)
				{
					txin.WitScript = transactions
									.Select(tx => tx.Inputs[i].WitScript).FirstOrDefault(s => !WitScript.IsNullOrEmpty(s))
										?? WitScript.Empty;
				}
			}
			return tx;
		}
		private readonly List<BuilderExtension> _Extensions = new List<BuilderExtension>();
		private const int MAX_TX_VSIZE = 100_000;

		public List<BuilderExtension> Extensions
		{
			get
			{
				return _Extensions;
			}
		}

		private ScriptSigs GetScriptSigs(IndexedTxIn indexedTxIn)
		{
			return new ScriptSigs()
			{
				ScriptSig = indexedTxIn.ScriptSig,
				WitSig = indexedTxIn.WitScript
			};
		}

		private Script? DeduceScriptPubKey(Script scriptSig)
		{
			var p2sh = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(scriptSig);
			if (p2sh != null && p2sh.RedeemScript != null)
			{
				return p2sh.RedeemScript.Hash.ScriptPubKey;
			}
			foreach (var extension in Extensions)
			{
				if (extension.CanDeduceScriptPubKey(scriptSig))
				{
					return extension.DeduceScriptPubKey(scriptSig);
				}
			}
			return null;
		}
	}

	public class CoinNotFoundException : KeyNotFoundException
	{
		public CoinNotFoundException(IndexedTxIn txIn)
			: base("No coin matching " + txIn.PrevOut + " was found")
		{
			_OutPoint = txIn.PrevOut;
			_InputIndex = txIn.Index;
		}

		private readonly OutPoint _OutPoint;
		public OutPoint OutPoint
		{
			get
			{
				return _OutPoint;
			}
		}

		private readonly uint _InputIndex;
		public uint InputIndex
		{
			get
			{
				return _InputIndex;
			}
		}
	}
}
#nullable disable
