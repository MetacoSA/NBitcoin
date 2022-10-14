using NBitcoin.OpenAsset;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NBitcoin
{
	public interface IColoredCoin : ICoin
	{
		AssetId AssetId
		{
			get;
		}
		Coin Bearer
		{
			get;
		}
	}
	public interface ICoinable
	{
		OutPoint Outpoint
		{
			get;
		}
		TxOut TxOut
		{
			get;
		}
	}
	public interface ICoin : ICoinable
	{
		IMoney Amount
		{
			get;
		}

		/// <summary>
		/// Returns the script actually signed and executed
		/// </summary>
		/// <exception cref="System.InvalidOperationException">Additional information needed to get the ScriptCode</exception>
		/// <returns>The executed script</returns>
		Script GetScriptCode();
		void OverrideScriptCode(Script scriptCode);
		bool CanGetScriptCode
		{
			get;
		}
		HashVersion GetHashVersion();
		bool IsMalleable
		{
			get;
		}
	}

	public class IssuanceCoin : IColoredCoin
	{
		public IssuanceCoin()
		{

		}
		public IssuanceCoin(Coin bearer)
		{
			Bearer = bearer;
		}

		public IssuanceCoin(OutPoint outpoint, TxOut txout)
		{
			Bearer = new Coin(outpoint, txout);
		}

		public bool IsMalleable => Bearer.IsMalleable;
		public AssetId AssetId
		{
			get
			{
				return Bearer.TxOut.ScriptPubKey.Hash.ToAssetId();
			}
		}

		public Uri DefinitionUrl
		{
			get;
			set;
		}

		#region ICoin Members


		public Money Amount
		{
			get
			{
				return Bearer.TxOut.Value;
			}
			set
			{
				Bearer.TxOut.Value = value;
			}
		}

		public TxOut TxOut
		{
			get
			{
				return Bearer.TxOut;
			}
		}

		#endregion

		public Script ScriptPubKey
		{
			get
			{
				return Bearer.TxOut.ScriptPubKey;
			}
		}

		#region IColoredCoin Members


		public Coin Bearer
		{
			get;
			set;
		}


		public OutPoint Outpoint
		{
			get
			{
				return Bearer.Outpoint;
			}
		}

		#endregion

		#region IColoredCoin Members

		AssetId IColoredCoin.AssetId
		{
			get
			{
				return AssetId;
			}
		}

		Coin IColoredCoin.Bearer
		{
			get
			{
				return Bearer;
			}
		}

		#endregion

		#region ICoin Members

		IMoney ICoin.Amount
		{
			get
			{
				return Amount;
			}
		}

		OutPoint ICoinable.Outpoint
		{
			get
			{
				return Outpoint;
			}
		}

		TxOut ICoinable.TxOut
		{
			get
			{
				return TxOut;
			}
		}

		#endregion

		#region ICoin Members


		public Script GetScriptCode()
		{
			return this.Bearer.GetScriptCode();
		}

		public bool CanGetScriptCode
		{
			get
			{
				return this.Bearer.CanGetScriptCode;
			}
		}

		public HashVersion GetHashVersion()
		{
			return this.Bearer.GetHashVersion();
		}

		public void OverrideScriptCode(Script scriptCode)
		{
			this.Bearer.OverrideScriptCode(scriptCode);
		}

		#endregion
	}

	public class ColoredCoin : IColoredCoin
	{
		public ColoredCoin()
		{

		}
		public ColoredCoin(AssetMoney asset, Coin bearer)
		{
			Amount = asset;
			Bearer = bearer;
		}

		public ColoredCoin(Transaction tx, ColoredEntry entry)
			: this(entry.Asset, new Coin(tx, entry.Index))
		{
			if (tx == null)
				throw new ArgumentNullException(nameof(tx));
			if (entry == null)
				throw new ArgumentNullException(nameof(entry));
		}

		public AssetId AssetId
		{
			get
			{
				return Amount.Id;
			}
		}
		public bool IsMalleable => Bearer.IsMalleable;
		public AssetMoney Amount
		{
			get;
			set;
		}

		public Coin Bearer
		{
			get;
			set;
		}

		public TxOut TxOut
		{
			get
			{
				return Bearer.TxOut;
			}
		}

		#region ICoin Members

		public OutPoint Outpoint
		{
			get
			{
				return Bearer.Outpoint;
			}
		}

		public Script ScriptPubKey
		{
			get
			{
				return Bearer.ScriptPubKey;
			}
		}

		#endregion

		public static IEnumerable<ColoredCoin> Find(Transaction tx, ColoredTransaction colored)
		{
			return Find(null, tx, colored);
		}
		public static IEnumerable<ColoredCoin> Find(uint256 txId, Transaction tx, ColoredTransaction colored)
		{
			if (colored == null)
				throw new ArgumentNullException(nameof(colored));
			if (tx == null)
				throw new ArgumentNullException(nameof(tx));
			if (txId == null)
				txId = tx.GetHash();
			foreach (var entry in colored.Issuances.Concat(colored.Transfers))
			{
				var txout = tx.Outputs[entry.Index];
				yield return new ColoredCoin(entry.Asset, new Coin(new OutPoint(txId, entry.Index), txout));
			}
		}

		public static IEnumerable<ColoredCoin> Find(Transaction tx, IColoredTransactionRepository repo)
		{
			return Find(null, tx, repo);
		}
		public static IEnumerable<ColoredCoin> Find(uint256 txId, Transaction tx, IColoredTransactionRepository repo)
		{
			if (txId == null)
				txId = tx.GetHash();
			var colored = tx.GetColoredTransaction(repo);
			return Find(txId, tx, colored);
		}

		#region IColoredCoin Members

		AssetId IColoredCoin.AssetId
		{
			get
			{
				return AssetId;
			}
		}

		Coin IColoredCoin.Bearer
		{
			get
			{
				return Bearer;
			}
		}

		#endregion

		#region ICoin Members

		IMoney ICoin.Amount
		{
			get
			{
				return Amount;
			}
		}

		OutPoint ICoinable.Outpoint
		{
			get
			{
				return Outpoint;
			}
		}

		TxOut ICoinable.TxOut
		{
			get
			{
				return TxOut;
			}
		}

		public Script GetScriptCode()
		{
			return this.Bearer.GetScriptCode();
		}

		public bool CanGetScriptCode
		{
			get
			{
				return this.Bearer.CanGetScriptCode;
			}
		}

		public HashVersion GetHashVersion()
		{
			return this.Bearer.GetHashVersion();
		}

		public void OverrideScriptCode(Script scriptCode)
		{
			this.Bearer.OverrideScriptCode(scriptCode);
		}

		#endregion
	}
	public class Coin : ICoin
	{
		public Coin()
		{

		}
		public Coin(OutPoint fromOutpoint, TxOut fromTxOut)
		{
			Outpoint = fromOutpoint;
			TxOut = fromTxOut;
		}

		public Coin(Transaction fromTx, uint fromOutputIndex)
		{
			if (fromTx == null)
				throw new ArgumentNullException(nameof(fromTx));
			Outpoint = new OutPoint(fromTx, fromOutputIndex);
			TxOut = fromTx.Outputs[fromOutputIndex];
		}

		public Coin(Transaction fromTx, TxOut fromOutput)
		{
			if (fromTx == null)
				throw new ArgumentNullException(nameof(fromTx));
			if (fromOutput == null)
				throw new ArgumentNullException(nameof(fromOutput));
			uint outputIndex = (uint)fromTx.Outputs.FindIndex(r => Object.ReferenceEquals(fromOutput, r));
			Outpoint = new OutPoint(fromTx, outputIndex);
			TxOut = fromOutput;
		}
		public Coin(IndexedTxOut txOut)
		{
			Outpoint = new OutPoint(txOut.Transaction.GetHash(), txOut.N);
			TxOut = txOut.TxOut;
		}

		public Coin(uint256 fromTxHash, uint fromOutputIndex, Money amount, Script scriptPubKey)
		{
			Outpoint = new OutPoint(fromTxHash, fromOutputIndex);
			TxOut = new TxOut(amount, scriptPubKey);
		}

		public virtual Script GetScriptCode()
		{
			if (!CanGetScriptCode)
				throw new InvalidOperationException("You need to provide P2WSH or P2SH redeem script with Coin.ToScriptCoin()");
			if (_OverrideScriptCode != null)
				return _OverrideScriptCode;
			var key = PayToWitPubKeyHashTemplate.Instance.ExtractScriptPubKeyParameters(ScriptPubKey);
			if (key != null)
				return key.AsKeyId().ScriptPubKey;
			return ScriptPubKey;
		}
		public bool IsMalleable => !(ScriptPubKey.IsScriptType(ScriptType.Taproot) ||
								     GetHashVersion() == HashVersion.WitnessV0);
		public virtual bool CanGetScriptCode
		{
			get
			{
				return _OverrideScriptCode != null || !ScriptPubKey.IsScriptType(ScriptType.P2SH) && !PayToWitScriptHashTemplate.Instance.CheckScriptPubKey(ScriptPubKey);
			}
		}

		public virtual HashVersion GetHashVersion()
		{
			if (ScriptPubKey.IsScriptType(ScriptType.Taproot))
				return HashVersion.Taproot;
			if (PayToWitTemplate.Instance.CheckScriptPubKey(ScriptPubKey))
				return HashVersion.WitnessV0;
			return HashVersion.Original;
		}

		public ScriptCoin ToScriptCoin(Script redeemScript)
		{
			if (redeemScript == null)
				throw new ArgumentNullException(nameof(redeemScript));
			var scriptCoin = this as ScriptCoin;
			if (scriptCoin != null)
				return scriptCoin;
			if (!ScriptCoin.IsCoherent(TxOut.ScriptPubKey, redeemScript, out var error))
				throw new ArgumentException(paramName: nameof(redeemScript), message: error);
			return new ScriptCoin(this, redeemScript);
		}

		public ScriptCoin TryToScriptCoin(Script redeemScript)
		{
			if (redeemScript == null)
				throw new ArgumentNullException(nameof(redeemScript));
			var scriptCoin = this as ScriptCoin;
			if (scriptCoin != null)
				return scriptCoin;
			if (!ScriptCoin.IsCoherent(TxOut.ScriptPubKey, redeemScript, out var error))
				return null;
			return new ScriptCoin(this, redeemScript);
		}

		public ScriptCoin TryToScriptCoin(PubKey pubKey)
		{
			if (pubKey == null)
				throw new ArgumentNullException(nameof(pubKey));
			return TryToScriptCoin(pubKey.WitHash.ScriptPubKey) ?? TryToScriptCoin(pubKey.ScriptPubKey);
		}

		public ColoredCoin ToColoredCoin(AssetId asset, ulong quantity)
		{
			return ToColoredCoin(new AssetMoney(asset, quantity));
		}
		public ColoredCoin ToColoredCoin(BitcoinAssetId asset, ulong quantity)
		{
			return ToColoredCoin(new AssetMoney(asset, quantity));
		}
		public ColoredCoin ToColoredCoin(AssetMoney asset)
		{
			return new ColoredCoin(asset, this);
		}

		public OutPoint Outpoint
		{
			get;
			set;
		}
		public TxOut TxOut
		{
			get;
			set;
		}

		#region ICoin Members


		public Money Amount
		{
			get
			{
				if (TxOut == null)
					return Money.Zero;
				return TxOut.Value;
			}
			set
			{
				EnsureTxOut();
				TxOut.Value = value;
			}
		}

		private void EnsureTxOut()
		{
			if (TxOut == null)
				TxOut = new TxOut();
		}

		protected Script _OverrideScriptCode;
		public void OverrideScriptCode(Script scriptCode)
		{
			_OverrideScriptCode = scriptCode;
		}

		#endregion

		public Script ScriptPubKey
		{
			get
			{
				if (TxOut == null)
					return Script.Empty;
				return TxOut.ScriptPubKey;
			}
			set
			{
				EnsureTxOut();
				TxOut.ScriptPubKey = value;
			}
		}

		#region ICoin Members

		IMoney ICoin.Amount
		{
			get
			{
				return Amount;
			}
		}

		OutPoint ICoinable.Outpoint
		{
			get
			{
				return Outpoint;
			}
		}

		TxOut ICoinable.TxOut
		{
			get
			{
				return TxOut;
			}
		}

		#endregion
	}


	public enum RedeemType
	{
		P2SH,
		WitnessV0
	}

#nullable enable
	public class CoinOptions
	{
		public CoinOptions()
		{
			Sequence = null;
		}

		public Sequence? Sequence { get; set; }
		public KeyPair? KeyPair { get; set; }
	}
#nullable restore


	/// <summary>
	/// Represent a coin which need a redeem script to be spent (P2SH or P2WSH)
	/// </summary>
	public class ScriptCoin : Coin
	{
		public ScriptCoin()
		{

		}

		public ScriptCoin(OutPoint fromOutpoint, TxOut fromTxOut, Script redeem)
			: base(fromOutpoint, fromTxOut)
		{
			Redeem = redeem;
			AssertCoherent();
		}

		public ScriptCoin(Transaction fromTx, uint fromOutputIndex, Script redeem)
			: base(fromTx, fromOutputIndex)
		{
			Redeem = redeem;
			AssertCoherent();
		}

		public ScriptCoin(Transaction fromTx, TxOut fromOutput, Script redeem)
			: base(fromTx, fromOutput)
		{
			Redeem = redeem;
			AssertCoherent();
		}
		public ScriptCoin(ICoin coin, Script redeem)
			: base(coin.Outpoint, coin.TxOut)
		{
			Redeem = redeem;
			AssertCoherent();
		}

		private void AssertCoherent(string paramName = null)
		{
			if (!IsCoherent(TxOut.ScriptPubKey, Redeem, out var error))
				throw new ArgumentException(paramName: paramName ?? "redeem", message: error);
		}

		public bool IsP2SH
		{
			get
			{
				return ScriptPubKey.ToBytes(true)[0] == (byte)OpcodeType.OP_HASH160;
			}
		}


		/// <summary>
		/// Get the P2SH redeem script
		/// </summary>
		/// <returns>The P2SH redeem script or null if this coin is not P2SH.</returns>
		public Script GetP2SHRedeem()
		{
			if (!IsP2SH)
				return null;
			var p2shRedeem = RedeemType == RedeemType.P2SH ? Redeem :
							RedeemType == RedeemType.WitnessV0 ? Redeem.WitHash.ScriptPubKey :
							null;
			if (p2shRedeem == null)
				throw new NotSupportedException("RedeemType not supported for getting the P2SH script, contact the library author");
			return p2shRedeem;
		}

		public RedeemType RedeemType
		{
			get
			{
				return
					Redeem.Hash.ScriptPubKey == TxOut.ScriptPubKey ?
					RedeemType.P2SH :
					RedeemType.WitnessV0;
			}
		}

		public static bool IsCoherent(Script scriptPubKey, Script redeem, out string error)
		{
			if (redeem == null)
				throw new ArgumentNullException(nameof(redeem));
			if (scriptPubKey == null)
				throw new ArgumentNullException(nameof(scriptPubKey));

			var expectedDestination = GetRedeemHash(scriptPubKey);
			if (expectedDestination == null)
			{
				error = "the provided scriptPubKey is not P2SH or P2WSH";
				return false;
			}
			if (expectedDestination is ScriptId)
			{
				if (PayToWitScriptHashTemplate.Instance.CheckScriptPubKey(redeem))
				{
					error = "The redeem script provided must be the witness one, not the P2SH one";
					return false;
				}

				if (expectedDestination.ScriptPubKey != redeem.Hash.ScriptPubKey)
				{
					if (redeem.WitHash.ScriptPubKey.Hash.ScriptPubKey != expectedDestination.ScriptPubKey)
					{
						error = "The redeem provided does not match the scriptPubKey of the coin";
						return false;
					}
				}
			}
			else if (expectedDestination is WitScriptId)
			{
				if (expectedDestination.ScriptPubKey != redeem.WitHash.ScriptPubKey)
				{
					error = "The redeem provided does not match the scriptPubKey of the coin";
					return false;
				}
			}
			else
			{
				error = "Not supported redeemed scriptPubkey";
				return false;
			}
			error = null;
			return true;
		}

		public ScriptCoin(IndexedTxOut txOut, Script redeem)
			: base(txOut)
		{
			Redeem = redeem;
			AssertCoherent();
		}

		public ScriptCoin(uint256 txHash, uint outputIndex, Money amount, Script scriptPubKey, Script redeem)
			: base(txHash, outputIndex, amount, scriptPubKey)
		{
			Redeem = redeem;
			AssertCoherent();
		}

		public Script Redeem
		{
			get;
			set;
		}

		public override Script GetScriptCode()
		{
			if (!CanGetScriptCode)
				throw new InvalidOperationException("You need to provide the P2WSH redeem script with ScriptCoin.ToScriptCoin()");
			if (_OverrideScriptCode != null)
				return _OverrideScriptCode;
			var key = PayToWitPubKeyHashTemplate.Instance.ExtractScriptPubKeyParameters(Redeem);
			if (key != null)
				return key.AsKeyId().ScriptPubKey;
			return Redeem;
		}

		public override bool CanGetScriptCode
		{
			get
			{
				return _OverrideScriptCode != null || !IsP2SH || !PayToWitScriptHashTemplate.Instance.CheckScriptPubKey(Redeem);
			}
		}

		public override HashVersion GetHashVersion()
		{
			var isWitness = PayToWitTemplate.Instance.CheckScriptPubKey(ScriptPubKey) ||
							PayToWitTemplate.Instance.CheckScriptPubKey(Redeem) ||
							RedeemType == NBitcoin.RedeemType.WitnessV0;
			return isWitness ? HashVersion.WitnessV0 : HashVersion.Original;
		}

		/// <summary>
		/// Returns the hash contained in the scriptPubKey (P2SH or P2WSH)
		/// </summary>
		/// <param name="scriptPubKey">The scriptPubKey</param>
		/// <returns>The hash of the scriptPubkey</returns>
		public static IAddressableDestination GetRedeemHash(Script scriptPubKey)
		{
			if (scriptPubKey == null)
				throw new ArgumentNullException(nameof(scriptPubKey));
			return PayToScriptHashTemplate.Instance.ExtractScriptPubKeyParameters(scriptPubKey) as IAddressableDestination
					??
					PayToWitScriptHashTemplate.Instance.ExtractScriptPubKeyParameters(scriptPubKey);
		}
	}
}
