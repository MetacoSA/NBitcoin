#nullable enable
using NBitcoin.BuilderExtensions;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.OpenAsset;
using NBitcoin.Policy;
using NBitcoin.Stealth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using Builder = System.Func<NBitcoin.TransactionBuilder.TransactionBuildingContext, NBitcoin.IMoney?>;
using AssetBuilder = System.Func<NBitcoin.TransactionBuilder.TransactionBuildingContext, NBitcoin.OpenAsset.AssetMoney?>;

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

	/// <summary>
	/// Algorithm implemented by bitcoin core https://github.com/bitcoin/bitcoin/blob/3015e0bca6bc2cb8beb747873fdf7b80e74d679f/src/wallet.cpp#L1276
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
		public DefaultCoinSelector(Random random)
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

		public Money MinimumChange { get; set; } = Money.Coins(1.0m / 100);

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
			var targetMinChange = target.Add(MinimumChange);
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

		void ApproximateBestSubset(List<OutputGroup> groups, IMoney nTotalLower, IMoney nTargetValue,
								  out bool[] vfBest, out IMoney nBest, int iterations = 1000)
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

			for (int nRep = 0; nRep < iterations && nBest != nTargetValue; nRep++)
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
						if (nPass == 0 ? _Rand.Next(0, 2) == 0 : !vfIncluded[i])
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

	/// <summary>
	/// A class for building and signing all sort of transactions easily (http://www.codeproject.com/Articles/835098/NBitcoin-Build-Them-All)
	/// </summary>
	public class TransactionBuilder
	{
		internal class SignatureEvent
		{
			public SignatureEvent(PubKey pubKey, TransactionSignature sig, IndexedTxIn input)
			{
				Input = input;
				PubKey = pubKey;
				Signature = sig;
			}
			public PubKey PubKey;
			public TransactionSignature Signature;
			public IndexedTxIn Input;
		}
		internal class TransactionBuilderSigner : ISigner
		{
			private readonly TransactionSigningContext ctx;
			ICoin coin;
			SigHash sigHash;
			IndexedTxIn txIn;
			public TransactionBuilderSigner(TransactionSigningContext ctx, ICoin coin, SigHash sigHash, IndexedTxIn txIn)
			{
				this.ctx = ctx;
				this.coin = coin;
				this.sigHash = sigHash;
				this.txIn = txIn;
			}
#region ISigner Members

			public List<SignatureEvent> EmittedSignatures { get; } = new List<SignatureEvent>();

			public TransactionSignature Sign(PubKey pubKey)
			{
				var key = ctx.FindKey(pubKey);
				var sig = txIn.Sign(key, coin, sigHash, ctx.Builder.UseLowR);
				EmittedSignatures.Add(new SignatureEvent(key.PubKey, sig, txIn));
				return sig;
			}

#endregion
		}
		internal class TransactionBuilderKeyRepository : IKeyRepository
		{
			TransactionSigningContext _Ctx;
			public TransactionBuilderKeyRepository(TransactionSigningContext ctx)
			{
				_Ctx = ctx;
			}
#region IKeyRepository Members

			public PubKey? FindKey(Script scriptPubkey)
			{
				return _Ctx.FindKey(scriptPubkey)?.PubKey;
			}

#endregion
		}

		class KnownSignatureSigner : ISigner, IKeyRepository
		{
			private ICoin coin;
			private IndexedTxIn txIn;
			private readonly TransactionSigningContext signingContext;
			private List<Tuple<PubKey, ECDSASignature, OutPoint>> _KnownSignatures;
			private Dictionary<KeyId, ECDSASignature> _VerifiedSignatures = new Dictionary<KeyId, ECDSASignature>();

			public List<SignatureEvent> EmittedSignatures { get; } = new List<SignatureEvent>();

			public KnownSignatureSigner(TransactionSigningContext signingContext, List<Tuple<PubKey, ECDSASignature, OutPoint>> knownSignatures, ICoin coin, IndexedTxIn txIn)
			{
				this.signingContext = signingContext;
				this._KnownSignatures = knownSignatures;
				this.coin = coin;
				this.txIn = txIn;
			}

			public PubKey? FindKey(Script scriptPubKey)
			{
				foreach (var tv in _KnownSignatures.Where(tv => signingContext.Builder.IsCompatibleKeyFromScriptCode(tv.Item1, scriptPubKey)))
				{
					if (tv.Item3 != null && coin.Outpoint != tv.Item3)
						continue;
					var hash = txIn.GetSignatureHash(coin, signingContext.SigHash);
					if (tv.Item3 != null || tv.Item1.Verify(hash, tv.Item2))
					{
						EmittedSignatures.Add(new SignatureEvent(tv.Item1, new TransactionSignature(tv.Item2, signingContext.SigHash), txIn));
						_VerifiedSignatures.AddOrReplace(tv.Item1.Hash, tv.Item2);
						return tv.Item1;
					}
				}
				return null;
			}

			public TransactionSignature Sign(PubKey key)
			{
				return new TransactionSignature(_VerifiedSignatures[key.Hash], signingContext.SigHash);
			}
		}

		internal class TransactionSigningContext
		{
			public TransactionSigningContext(TransactionBuilder builder, Transaction transaction, SigHash sigHash)
			{
				Builder = builder;
				Transaction = transaction;
				if (transaction is IHasForkId hasForkId)
				{
					sigHash = (SigHash)((uint)sigHash | 0x40u);
				}
				SigHash = sigHash;
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

			internal Key? FindKey(Script scriptPubKey)
			{
				var key = Builder._Keys
					.Concat(AdditionalKeys)
					.FirstOrDefault(k => Builder.IsCompatibleKeyFromScriptCode(k.PubKey, scriptPubKey));
				if (key == null && Builder.KeyFinder != null)
				{
					key = Builder.KeyFinder(scriptPubKey);
				}
				return key;
			}
			internal Key FindKey(PubKey pubKey)
			{
				var key = Builder._Keys
					.Concat(AdditionalKeys)
					.FirstOrDefault(k => k.PubKey == pubKey);
				return key;
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

			public List<SignatureEvent> SignatureEvents { get; set; } = new List<SignatureEvent>();
			public uint? InputIndex { get; internal set; }
		}
		internal class TransactionBuildingContext
		{
			public TransactionBuildingContext(TransactionBuilder builder)
			{
				Builder = builder;
				Transaction = builder.Network.CreateTransaction();
				AdditionalFees = Money.Zero;
				Group = new BuilderGroup(builder);
				ChangeAmount = Money.Zero;
			}
			public TransactionBuilder.BuilderGroup Group
			{
				get;
				set;
			}

			private List<ICoin> _ConsumedCoins = new List<ICoin>();
			public List<ICoin> ConsumedCoins
			{
				get
				{
					return _ConsumedCoins;
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

			public Money AdditionalFees
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

			public void Finish()
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

			public IMoney ChangeAmount
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
				_ConsumedCoins = memento.ConsumedCoins.ToList();
			}

			public bool NonFinalSequenceSet
			{
				get;
				set;
			}

			public IMoney? CoverOnly
			{
				get;
				set;
			}

			public IMoney? Dust
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
				FeeWeight = 1.0m;
				Builders.Add(SetChange);
			}

			Money SetChange(TransactionBuildingContext ctx)
			{
				var changeAmount = (Money)ctx.ChangeAmount;
				if (changeAmount.Satoshi == 0)
					return Money.Zero;
				ctx.Transaction.Outputs.Add(changeAmount, ctx.Group.ChangeScript[(int)ChangeType.Uncolored]);
				return changeAmount;
			}
			internal List<Builder> Builders = new List<Builder>();
			internal Dictionary<OutPoint, ICoin> Coins = new Dictionary<OutPoint, ICoin>();
			internal List<AssetBuilder> IssuanceBuilders = new List<AssetBuilder>();
			internal Dictionary<AssetId, List<AssetBuilder>> BuildersByAsset = new Dictionary<AssetId, List<AssetBuilder>>();
			internal Script[] ChangeScript = new Script[3];
			internal bool sendAllToChange;
			internal bool preventSetChange;

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

			public Money? CoverOnly
			{
				get;
				set;
			}

			public string? Name
			{
				get;
				set;
			}

			public decimal FeeWeight
			{
				get;
				set;
			}
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
		public Func<Script, Key>? KeyFinder
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

		internal List<Key> _Keys = new List<Key>();

		public TransactionBuilder AddKeys(params ISecret[] keys)
		{
			AddKeys(keys.Select(k => k.PrivateKey).ToArray());
			return this;
		}

		public TransactionBuilder AddKeys(params Key[] keys)
		{
			_Keys.AddRange(keys);
			foreach (var k in keys)
			{
				AddKnownRedeems(k.PubKey.ScriptPubKey);
				AddKnownRedeems(k.PubKey.WitHash.ScriptPubKey);
				AddKnownRedeems(k.PubKey.Hash.ScriptPubKey);
			}
			return this;
		}

		public TransactionBuilder AddKnownSignature(PubKey pubKey, TransactionSignature signature, OutPoint signedOutpoint)
		{
			if (pubKey == null)
				throw new ArgumentNullException(nameof(pubKey));
			if (signature == null)
				throw new ArgumentNullException(nameof(signature));
			if (signedOutpoint == null)
				throw new ArgumentNullException(nameof(signedOutpoint));
			_KnownSignatures.Add(Tuple.Create(pubKey, signature.Signature, signedOutpoint));
			return this;
		}

		public TransactionBuilder AddKnownSignature(PubKey pubKey, ECDSASignature signature, OutPoint signedOutpoint)
		{
			if (pubKey == null)
				throw new ArgumentNullException(nameof(pubKey));
			if (signature == null)
				throw new ArgumentNullException(nameof(signature));
			_KnownSignatures.Add(Tuple.Create(pubKey, signature, signedOutpoint));
			return this;
		}

		public TransactionBuilder AddCoins(params ICoin[] coins)
		{
			return AddCoins((IEnumerable<ICoin>)coins);
		}

		public TransactionBuilder AddCoins(IEnumerable<ICoin> coins)
		{
			foreach (var coin in coins)
			{
				CurrentGroup.Coins.AddOrReplace(coin.Outpoint, coin);
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
		/// Do we try to get shorter signatures? (default: true)
		/// </summary>
		public bool UseLowR { get; set; } = true;

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
			var totalInput = CurrentGroup.Coins.Values.OfType<Coin>().Sum(coin => coin.Amount);
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
				SendFees(amount);
				return this;
			}

			var builder = new SendBuilder(CreateTxOut(amount, scriptPubKey));
			CurrentGroup.Builders.Add(builder.Build);
			_LastSendBuilder = builder;
			return this;
		}

		SendBuilder? _LastSendBuilder;
		SendBuilder? _SubstractFeeBuilder;

		class SendBuilder
		{
			internal TxOut _TxOut;

			public SendBuilder(TxOut txout)
			{
				_TxOut = txout;
			}

			public Money Build(TransactionBuildingContext ctx)
			{
				ctx.Transaction.Outputs.Add(_TxOut);
				return _TxOut.Value;
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
			_SubstractFeeBuilder = _LastSendBuilder;
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
					Send(scriptPubKey, amount);
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

		[Obsolete("Transaction builder is automatically shuffled")]
		public TransactionBuilder Shuffle()
		{
			DoShuffle();
			return this;
		}

		private void DoShuffle()
		{
			if (ShuffleRandom != null)
			{
				Utils.Shuffle(_BuilderGroups, ShuffleRandom);
				foreach (var group in _BuilderGroups)
					group.Shuffle();
			}
		}

		AssetMoney SetColoredChange(TransactionBuildingContext ctx)
		{
			var changeAmount = (AssetMoney)ctx.ChangeAmount;
			if (changeAmount.Quantity == 0)
				return changeAmount;
			var marker = ctx.GetColorMarker(false);
			var script = ctx.Group.ChangeScript[(int)ChangeType.Colored];
			var txout = ctx.Transaction.Outputs.Add(GetDust(script), script);
			marker.SetQuantity(ctx.Transaction.Outputs.Count - 2, changeAmount.Quantity);
			ctx.AdditionalFees += txout.Value;
			return changeAmount;
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
				builders.Add(SetColoredChange);
			}
			builders.Add(ctx =>
			{
				var marker = ctx.GetColorMarker(false);
				var txout = ctx.Transaction.Outputs.Add(GetDust(scriptPubKey), scriptPubKey);
				marker.SetQuantity(ctx.Transaction.Outputs.Count - 2, asset.Quantity);
				ctx.AdditionalFees += txout.Value;
				return asset;
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
			return CreateTxOut(Money.Zero, script).GetDustThreshold(StandardTransactionPolicy.MinRelayTxFee);
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

		public TransactionBuilder Send(BitcoinStealthAddress address, Money amount, Key? ephemKey = null)
		{
			if (amount < Money.Zero)
				throw new ArgumentOutOfRangeException(nameof(amount), "amount can't be negative");

			if (_OpReturnUser == null)
				_OpReturnUser = "Stealth Payment";
			else
				throw new InvalidOperationException("Op return already used for " + _OpReturnUser);

			CurrentGroup.Builders.Add(ctx =>
			{
				var payment = address.CreatePayment(ephemKey);
				payment.AddToTransaction(ctx.Transaction, amount);
				return amount;
			});
			return this;
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
					var issuance = ctx.Group.Coins.Values.OfType<IssuanceCoin>().Where(i => i.AssetId == asset.Id).FirstOrDefault();
					if (issuance == null)
						throw new InvalidOperationException("No issuance coin for emitting asset found");
					ctx.IssuanceCoin = issuance;
					var input = ctx.Transaction.Inputs.CreateNewTxIn();
					input.PrevOut = issuance.Outpoint;
					ctx.Transaction.Inputs.Insert(0, input);
					ctx.AdditionalFees -= issuance.Bearer.Amount;
					if (issuance.DefinitionUrl != null)
					{
						marker.SetMetadataUrl(issuance.DefinitionUrl);
					}
				}

				ctx.Transaction.Outputs.Insert(0, CreateTxOut(GetDust(scriptPubKey), scriptPubKey));
				marker.Quantities = new[] { checked((ulong)asset.Quantity) }.Concat(marker.Quantities).ToArray();
				ctx.AdditionalFees += ctx.Transaction.Outputs[0].Value;
				return asset;
			});
			return this;
		}

		public bool TrySignInput(Transaction transaction, uint index, SigHash sigHash, out TransactionSignature? signature)
		{
			if (transaction == null)
				throw new ArgumentNullException(nameof(transaction));
			var ctx = new TransactionSigningContext(this, transaction.Clone(), sigHash);
			ctx.InputIndex = index;
			this.SignTransactionInPlace(ctx);
			signature = ctx.SignatureEvents.FirstOrDefault()?.Signature;
			return signature != null;
		}

		public TransactionBuilder SendFees(Money fees)
		{
			if (fees == null)
				throw new ArgumentNullException(nameof(fees));
			CurrentGroup.Builders.Add(ctx => fees);
			_TotalFee += fees;
			return this;
		}

		Money _TotalFee = Money.Zero;

		/// <summary>
		/// Split the estimated fees across the several groups (separated by Then())
		/// </summary>
		/// <param name="feeRate"></param>
		/// <returns></returns>
		public TransactionBuilder SendEstimatedFees(FeeRate feeRate)
		{
			FilterUneconomicalCoinsRate = feeRate;
			var fee = EstimateFees(feeRate);
			SendFees(fee);
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
			var fee = EstimateFees(feeRate);
			SendFeesSplit(fee);
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
				group.Builders.Add(ctx => groupFee);
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
		/// <returns>A PSBT</returns>
		/// <exception cref="NBitcoin.NotEnoughFundsException">Not enough funds are available</exception>
		public PSBT BuildPSBT(bool sign)
		{
			return BuildPSBT(sign, SigHash.All);
		}

		/// <summary>
		/// Build a PSBT (Partially signed bitcoin transaction)
		/// </summary>
		/// <param name="sign">True if signs all inputs with the available keys</param>
		/// <param name="sigHash">The sighash for signing (ignored if sign is false)</param>
		/// <returns>A PSBT</returns>
		/// <exception cref="NBitcoin.NotEnoughFundsException">Not enough funds are available</exception>
		public PSBT BuildPSBT(bool sign, SigHash sigHash)
		{
			var tx = BuildTransaction(false, sigHash);
			return CreatePSBTFromCore(tx, sign, sigHash);
		}

		/// <summary>
		/// Create a PSBT from a transaction
		/// </summary>
		/// <param name="tx">The transaction</param>
		/// <param name="sign">If true, the transaction builder will sign this transaction</param>
		/// <param name="sigHash">The sighash for signing (ignored if sign is false)</param>
		/// <returns></returns>
		public PSBT CreatePSBTFrom(Transaction tx, bool sign, SigHash sigHash)
		{
			if (tx == null)
				throw new ArgumentNullException(nameof(tx));
			return CreatePSBTFromCore(tx.Clone(), sign, sigHash);
		}

		PSBT CreatePSBTFromCore(Transaction tx, bool sign, SigHash sigHash)
		{
			TransactionSigningContext signingContext = new TransactionSigningContext(this, tx, sigHash);
			if (sign)
				SignTransactionInPlace(signingContext);
			var psbt = tx.CreatePSBT(Network);
			UpdatePSBT(psbt);
			if (sign)
				UpdatePSBTSignatures(psbt, signingContext);
			return psbt;
		}

		private static void UpdatePSBTSignatures(PSBT psbt, TransactionSigningContext signingContext)
		{
			foreach (var signature in signingContext.SignatureEvents)
			{
				var psbtInput = psbt.Inputs.FindIndexedInput(signature.Input.PrevOut);
				psbtInput.PartialSigs.TryAdd(signature.PubKey, signature.Signature);
			}
		}

		public TransactionBuilder SignPSBT(PSBT psbt)
		{
			SignPSBT(psbt, SigHash.All);
			return this;
		}
		public TransactionBuilder SignPSBT(PSBT psbt, SigHash sigHash)
		{
			if (psbt == null)
				throw new ArgumentNullException(nameof(psbt));
			AddCoins(psbt);
			var tx = psbt.GetOriginalTransaction();
			var signingContext = new TransactionSigningContext(this, tx, sigHash);
			SignTransactionInPlace(signingContext);
			UpdatePSBTSignatures(psbt, signingContext);
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
				.Where(c => c != null)
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
			return BuildTransaction(sign, SigHash.All);
		}

		/// <summary>
		/// Build the transaction
		/// </summary>
		/// <param name="sign">True if signs all inputs with the available keys</param>
		/// <param name="sigHash">The type of signature</param>
		/// <returns>The transaction</returns>
		/// <exception cref="NBitcoin.NotEnoughFundsException">Not enough funds are available</exception>
		public Transaction BuildTransaction(bool sign, SigHash sigHash)
		{
			DoShuffle();
		retry:
			TransactionBuildingContext ctx = new TransactionBuildingContext(this);
			if (_CompletedTransaction != null)
				ctx.Transaction = _CompletedTransaction.Clone();
			if (_LockTime != null)
				ctx.Transaction.LockTime = _LockTime.Value;
			foreach (var group in _BuilderGroups)
			{
				ctx.Group = group;
				ctx.AdditionalFees = Money.Zero;

				ctx.ChangeType = ChangeType.Colored;
				foreach (var builder in group.IssuanceBuilders)
					builder(ctx);

				var buildersByAsset = group.BuildersByAsset.ToList();
				foreach (var builders in buildersByAsset)
				{
					var coins = group.Coins.Values.OfType<ColoredCoin>().Where(c => c.Amount.Id == builders.Key);

					ctx.Dust = new AssetMoney(builders.Key);
					ctx.CoverOnly = null;
					ctx.ChangeAmount = new AssetMoney(builders.Key);
					var btcSpent = BuildTransaction(ctx, group, builders.Value, coins, new AssetMoney(builders.Key))
						.OfType<IColoredCoin>().Select(c => c.Bearer.Amount).Sum();
					ctx.AdditionalFees -= btcSpent;
				}

				ctx.Dust = GetDust();
				ctx.ChangeAmount = Money.Zero;
				ctx.CoverOnly = group.CoverOnly;
				ctx.ChangeType = ChangeType.Uncolored;

				var builderList = group.Builders.ToList();
				ModifyBuildersForSubstractFees(ctx, group, builderList);
				builderList.Add(ctxx => ctxx.AdditionalFees);
				BuildTransaction(ctx, group, builderList, group.Coins.Values.OfType<Coin>().Where(IsEconomical), Money.Zero);
			}
			ctx.Finish();

			if (sign)
			{
				SignTransactionInPlace(ctx.Transaction, sigHash);
			}

			// Make some adjustments if we need to send more fees
			if (StandardTransactionPolicy.MinFee != null)
			{
				var consumed = ctx.ConsumedCoins.ToArray();
				var fee = ctx.Transaction.GetFee(consumed);
				if (fee != null && fee < StandardTransactionPolicy.MinFee)
				{
					SendFees(StandardTransactionPolicy.MinFee - fee);
					goto retry;
				}
			}

			_built = true;
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

		private IEnumerable<ICoin> BuildTransaction<TMoney>(
			TransactionBuildingContext ctx,
			BuilderGroup group,
			List<Func<TransactionBuilder.TransactionBuildingContext, TMoney?>> builders,
			IEnumerable<ICoin> coins,
			TMoney zero) where TMoney : class, IMoney
		{
			ICoin[]? selection = null;
		retry:
			var hasColoredCoins = _BuilderGroups.Any(g => g.BuildersByAsset.Count != 0 || g.IssuanceBuilders.Count != 0);
			var originalCtx = ctx.CreateMemento();
			var target = builders.Select(b => b(ctx)).Sum(zero);
			if (ctx.CoverOnly != null)
			{
				target = ctx.CoverOnly.Add(ctx.ChangeAmount);
			}

			var unconsumed = coins.Where(c => ctx.ConsumedCoins.All(cc => cc.Outpoint != c.Outpoint)).ToArray();
			if (selection == null)
			{
				if (group.sendAllToChange)
				{
					selection = unconsumed;
				}
				else
				{
					selection = CoinSelector.Select(unconsumed, target)?.ToArray();
				}
			}
			if (selection == null)
				throw new NotEnoughFundsException("Not enough funds to cover the target",
					group.Name,
					target.Sub(unconsumed.Select(u => u.Amount).Sum(zero))
					);
			var selectedAmount = selection.Select(s => s.Amount).Sum(zero);
			var change = selectedAmount.Sub(target);
			if (change.CompareTo(zero) == -1)
				throw new NotEnoughFundsException("Not enough funds to cover the target",
					group.Name,
					change.Negate()
				);
			if (change.CompareTo(ctx.Dust) == 1)
			{
				var changeScript = group.ChangeScript[(int)ctx.ChangeType];
				if (changeScript == null)
					throw new InvalidOperationException("A change address should be specified (" + ctx.ChangeType + ")");
				if (!(ctx.Dust is Money) || change.CompareTo(GetDust(changeScript)) == 1)
				{
					ctx.RestoreMemento(originalCtx);
					ctx.ChangeAmount = change;
					goto retry;
				}
			}
			ctx.ChangeAmount = zero;
			if (ShuffleRandom != null)
			{
				Utils.Shuffle(selection, ShuffleRandom);
			}
			foreach (var coin in selection)
			{
				ctx.ConsumedCoins.Add(coin);
				var input = ctx.Transaction.Inputs.FirstOrDefault(i => i.PrevOut == coin.Outpoint);
				if (input == null)
					input = ctx.Transaction.Inputs.Add(coin.Outpoint);

				if (OptInRBF)
				{
					input.Sequence = Sequence.OptInRBF;
					ctx.NonFinalSequenceSet = true;
				}
				if (_LockTime != null && !ctx.NonFinalSequenceSet)
				{
					input.Sequence = 0;
					ctx.NonFinalSequenceSet = true;
				}
			}
			if (MergeOutputs && !hasColoredCoins)
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
			AfterBuild(ctx.Transaction);
			return selection;
		}

		private void ModifyBuildersForSubstractFees(TransactionBuildingContext ctx, BuilderGroup group, List<Builder> builders)
		{
			for (int i = 0; i < builders.Count; i++)
			{
				if (builders[i].Target == _SubstractFeeBuilder)
				{
					builders.Remove(builders[i]);
					var newTxOut = _SubstractFeeBuilder._TxOut.Clone();
					var minimumTxOutValue = (DustPrevention ? GetDust(newTxOut.ScriptPubKey) : Money.Zero);
					newTxOut.Value -= _TotalFee + ctx.AdditionalFees;
					if (newTxOut.Value < Money.Zero)
					{
						throw new NotEnoughFundsException("Can't substract fee from this output because the amount is too small",
						group.Name,
						-newTxOut.Value
						);
					}
					if (newTxOut.Value >= minimumTxOutValue)
						builders.Insert(i, new SendBuilder(newTxOut).Build);
					else
					{
						ctx.AdditionalFees += newTxOut.Value;
					}
					break;
				}
			}
		}

		protected virtual void AfterBuild(Transaction transaction)
		{
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
			return SignTransactionInPlace(new TransactionSigningContext(this, transaction, sigHash));
		}
		Transaction SignTransactionInPlace(TransactionSigningContext ctx)
		{
			foreach (var input in ctx.Transaction.Inputs.AsIndexedInputs())
			{
				if (ctx.InputIndex is uint i && i != input.Index)
					continue;
				var coin = FindSignableCoin(input);
				if (coin != null)
				{
					Sign(ctx, coin, input);
				}
			}
			return ctx.Transaction;
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
			if (coin == null || coin is ScriptCoin || coin is StealthCoin)
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
		/// <param name="tx">The transaction to check</param>
		/// <param name="errors">Detected errors</param>
		/// <returns>True if no error</returns>
		public bool Verify(Transaction tx, out TransactionPolicyError[] errors)
		{
			return Verify(tx, null as Money, out errors);
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
			var coins = tx.Inputs.Select(i => FindCoin(i.PrevOut)).Where(c => c != null).ToArray();
			List<TransactionPolicyError> exceptions = new List<TransactionPolicyError>();
			var policyErrors = MinerTransactionPolicy.Instance.Check(tx, coins);
			exceptions.AddRange(policyErrors);
			policyErrors = StandardTransactionPolicy.Check(tx, coins);
			exceptions.AddRange(policyErrors);
			if (expectedFees != null)
			{
				var fees = tx.GetFee(coins);
				if (fees != null)
				{
					Money margin = Money.Zero;
					if (DustPrevention)
						margin = GetDust() * 2;
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
			var result = _BuilderGroups.Select(c => c.Coins.TryGet(outPoint)).FirstOrDefault(r => r != null);
			if (result == null && CoinFinder != null)
				result = CoinFinder(outPoint);
			return result;
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
				if (coin.GetHashVersion() == HashVersion.Witness)
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
			if (coin is ScriptCoin)
			{
				var scriptCoin = (ScriptCoin)coin;
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

			var scriptPubkey = coin.GetScriptCode();
			var scriptSigSize = -1;
			foreach (var extension in Extensions)
			{
				if (extension.CanEstimateScriptSigSize(scriptPubkey))
				{
					scriptSigSize = extension.EstimateScriptSigSize(scriptPubkey);
					break;
				}
			}

			if (scriptSigSize == -1)
				scriptSigSize += coin.TxOut.ScriptPubKey.Length; //Using heurestic to approximate size of unknown scriptPubKey
			if (coin.GetHashVersion() == HashVersion.Witness)
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

			List<Builder> feeBuilders = new List<Builder>();
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
					feeBuilders.Add(CurrentGroup.Builders[CurrentGroup.Builders.Count - 1]);
					feeSent += delta;
				}
			}
			finally
			{
				foreach (var feeBuilder in feeBuilders)
				{
					CurrentGroup.Builders.Remove(feeBuilder);
				}
				_TotalFee -= feeSent;
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

		private void Sign(TransactionSigningContext ctx, ICoin coin, IndexedTxIn txIn)
		{
			var input = txIn.TxIn;
			if (coin is StealthCoin stealthCoin)
			{
				var scanKey = ctx.FindKey(stealthCoin.Address.ScanPubKey.ScriptPubKey);
				if (scanKey == null)
					throw new KeyNotFoundException("Scan key for decrypting StealthCoin not found");
				var spendKeys = stealthCoin.Address.SpendPubKeys.Select(p => ctx.FindKey(p.ScriptPubKey)).Where(p => p != null).ToArray();
				ctx.AdditionalKeys.AddRange(stealthCoin.Uncover(spendKeys, scanKey));
				var normalCoin = new Coin(coin.Outpoint, coin.TxOut);
				if (stealthCoin.Redeem != null)
					normalCoin = normalCoin.ToScriptCoin(stealthCoin.Redeem);
				coin = normalCoin;
			}
			var scriptSig = CreateScriptSig(ctx, coin, txIn);
			if (scriptSig == null)
				return;
			ScriptCoin? scriptCoin = coin as ScriptCoin;

			Script? signatures = null;
			if (coin.GetHashVersion() == HashVersion.Witness)
			{
				signatures = txIn.WitScript;
				if (scriptCoin != null)
				{
					if (scriptCoin.IsP2SH)
						txIn.ScriptSig = Script.Empty;
					if (scriptCoin.RedeemType == RedeemType.WitnessV0)
						signatures = RemoveRedeem(signatures);
				}
			}
			else
			{
				signatures = txIn.ScriptSig;
				if (scriptCoin != null && scriptCoin.RedeemType == RedeemType.P2SH)
					signatures = RemoveRedeem(signatures);
			}


			signatures = CombineScriptSigs(coin, scriptSig, signatures);

			if (coin.GetHashVersion() == HashVersion.Witness)
			{
				txIn.WitScript = signatures;
				if (scriptCoin != null)
				{
					if (scriptCoin.IsP2SH)
						txIn.ScriptSig = new Script(Op.GetPushOp(scriptCoin.GetP2SHRedeem().ToBytes(true)));
					if (scriptCoin.RedeemType == RedeemType.WitnessV0)
						txIn.WitScript = txIn.WitScript + new WitScript(Op.GetPushOp(scriptCoin.Redeem.ToBytes(true)));
				}
			}
			else
			{
				txIn.ScriptSig = signatures;
				if (scriptCoin != null && scriptCoin.RedeemType == RedeemType.P2SH)
				{
					txIn.ScriptSig = input.ScriptSig + Op.GetPushOp(scriptCoin.GetP2SHRedeem().ToBytes(true));
				}
			}
		}


		private static Script RemoveRedeem(Script script)
		{
			if (script == Script.Empty)
				return script;
			var ops = script.ToOps().ToArray();
			return new Script(ops.Take(ops.Length - 1));
		}

		private Script CombineScriptSigs(ICoin coin, Script a, Script b)
		{
			var scriptPubkey = coin.GetScriptCode();
			if (Script.IsNullOrEmpty(a))
				return b ?? Script.Empty;
			if (Script.IsNullOrEmpty(b))
				return a ?? Script.Empty;

			foreach (var extension in Extensions)
			{
				if (extension.CanCombineScriptSig(scriptPubkey, a, b))
				{
					return extension.CombineScriptSig(scriptPubkey, a, b);
				}
			}
			return a.Length > b.Length ? a : b; //Heurestic
		}

		private Script? CreateScriptSig(TransactionSigningContext ctx, ICoin coin, IndexedTxIn txIn)
		{
			var scriptPubKey = coin.GetScriptCode();
			var keyRepo = new TransactionBuilderKeyRepository(ctx);
			var signer = new TransactionBuilderSigner(ctx, coin, ctx.SigHash, txIn);

			var signer2 = new KnownSignatureSigner(ctx, _KnownSignatures, coin, txIn);

			foreach (var extension in Extensions)
			{
				if (extension.CanGenerateScriptSig(scriptPubKey))
				{
					var scriptSig1 = extension.GenerateScriptSig(scriptPubKey, keyRepo, signer);
					var scriptSig2 = extension.GenerateScriptSig(scriptPubKey, signer2, signer2);
					if (scriptSig1 != null && scriptSig2 != null && extension.CanCombineScriptSig(scriptPubKey, scriptSig1, scriptSig2))
					{
						var combined = extension.CombineScriptSig(scriptPubKey, scriptSig1, scriptSig2);
						if (combined != null)
						{
							ctx.SignatureEvents.AddRange(signer.EmittedSignatures);
							ctx.SignatureEvents.AddRange(signer2.EmittedSignatures);
						}
						return combined;
					}
					if (scriptSig1 != null)
					{
						ctx.SignatureEvents.AddRange(signer.EmittedSignatures);
					}
					if (scriptSig2 != null)
					{
						ctx.SignatureEvents.AddRange(signer2.EmittedSignatures);
					}
					return scriptSig1 ?? scriptSig2;
				}
			}

			throw new NotSupportedException("Unsupported scriptPubKey");
		}

		List<Tuple<PubKey, ECDSASignature, OutPoint>> _KnownSignatures = new List<Tuple<PubKey, ECDSASignature, OutPoint>>();
		internal bool IsCompatibleKeyFromScriptCode(PubKey pubKey, Script scriptPubKey)
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


		Transaction? _CompletedTransaction;
		private bool _built = false;

		/// <summary>
		/// Allows to keep building on the top of a partially built transaction
		/// </summary>
		/// <param name="transaction">Transaction to complete</param>
		/// <returns></returns>
		public TransactionBuilder ContinueToBuild(Transaction transaction)
		{
			if (_built)
				throw new InvalidOperationException("ContinueToBuild must be called with a new TransactionBuilder instance");
			if (_CompletedTransaction != null)
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
			if (_CompletedTransaction == null)
				throw new InvalidOperationException("A partially built transaction should be specified by calling ContinueToBuild");

			var spent = _CompletedTransaction.Inputs.AsIndexedInputs().Select(txin =>
			{
				var c = FindCoin(txin.PrevOut);
				if (c == null)
					throw CoinNotFound(txin);
				if (!(c is Coin))
					return null;
				return (Coin)c;
			})
					.Select(c => c == null ? Money.Zero : c.Amount)
					.Sum();

			var toComplete = _CompletedTransaction.TotalOut - spent;
			CurrentGroup.Builders.Add(ctx =>
			{
				if (toComplete < Money.Zero)
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

		Dictionary<Script, Script> _ScriptPubKeyToRedeem = new Dictionary<Script, Script>();
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

		public Transaction? CombineSignatures(params Transaction[] transactions)
		{
			if (transactions.Length == 1)
				return transactions[0];
			if (transactions.Length == 0)
				return null;

			Transaction tx = transactions[0].Clone();
			for (int i = 1; i < transactions.Length; i++)
			{
				var signed = transactions[i];
				tx = CombineSignaturesCore(tx, signed);
			}
			return tx;
		}


		private readonly List<BuilderExtension> _Extensions = new List<BuilderExtension>();
		public List<BuilderExtension> Extensions
		{
			get
			{
				return _Extensions;
			}
		}

		private Transaction CombineSignaturesCore(Transaction signed1, Transaction signed2)
		{
			if (signed1 == null)
				return signed2;
			if (signed2 == null)
				return signed1;
			var tx = signed1.Clone();
			for (int i = 0; i < tx.Inputs.Count; i++)
			{
				if (i >= signed2.Inputs.Count)
					break;

				var txIn = tx.Inputs[i];

				var coin = FindCoin(txIn.PrevOut);
				var scriptPubKey = coin == null
					? (DeduceScriptPubKey(txIn.ScriptSig) ?? DeduceScriptPubKey(signed2.Inputs[i].ScriptSig))
					: coin.TxOut.ScriptPubKey;

				var txout = coin?.TxOut;
				if (txout == null)
				{
					txout = signed1.Outputs.CreateNewTxOut(null, scriptPubKey);
				}
				var result = Script.CombineSignatures(
									scriptPubKey,
									new TransactionChecker(tx, i, txout),
									 GetScriptSigs(signed1.Inputs.AsIndexedInputs().Skip(i).First()),
									 GetScriptSigs(signed2.Inputs.AsIndexedInputs().Skip(i).First()));
				var input = tx.Inputs.AsIndexedInputs().Skip(i).First();
				input.WitScript = result.WitSig;
				input.ScriptSig = result.ScriptSig;
			}
			return tx;
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
