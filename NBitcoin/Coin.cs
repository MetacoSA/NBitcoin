using NBitcoin.OpenAsset;
using NBitcoin.Stealth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
	public interface ICoin
	{
		Money Amount
		{
			get;
		}
		OutPoint Outpoint
		{
			get;
		}
		TxOut TxOut
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
	}

	public class ColoredCoin : IColoredCoin
	{
		public ColoredCoin()
		{

		}
		public ColoredCoin(Asset asset, Coin bearer)
		{
			Asset = asset;
			Bearer = bearer;
		}

		public ColoredCoin(Transaction tx, ColoredEntry entry)
			: this(entry.Asset, new Coin(tx, entry.Index))
		{
		}

		public AssetId AssetId
		{
			get
			{
				return Asset.Id;
			}
		}
		public Asset Asset
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

		public Money Amount
		{
			get
			{
				return Asset.Quantity;
			}
		}

		#endregion

		public static IEnumerable<ColoredCoin> Find(Transaction tx, ColoredTransaction colored)
		{
			return Find(null, tx, colored);
		}
		public static IEnumerable<ColoredCoin> Find(uint256 txId, Transaction tx, ColoredTransaction colored)
		{
			if(colored == null)
				throw new ArgumentNullException("colored");
			if(tx == null)
				throw new ArgumentNullException("tx");
			if(txId == null)
				txId = tx.GetHash();
			foreach(var entry in colored.Issuances.Concat(colored.Transfers))
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
			if(txId == null)
				txId = tx.GetHash();
			var colored = tx.GetColoredTransaction(repo);
			return Find(txId, tx, colored);
		}
	}
	public class Coin : ICoin
	{
		public Coin()
		{

		}
		public Coin(Spendable spendable)
		{
			Outpoint = spendable.OutPoint;
			TxOut = spendable.TxOut;
		}
		public Coin(OutPoint fromOutpoint, TxOut fromTxOut)
		{
			Outpoint = fromOutpoint;
			TxOut = fromTxOut;
		}

		public Coin(Transaction fromTx, uint fromOutputIndex)
		{
			Outpoint = new OutPoint(fromTx, fromOutputIndex);
			TxOut = fromTx.Outputs[fromOutputIndex];
		}

		public Coin(Transaction fromTx, TxOut fromOutput)
		{
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

		public ScriptCoin ToScriptCoin(Script redeemScript)
		{
			if(redeemScript == null)
				throw new ArgumentNullException("redeemScript");
			if(this is ScriptCoin)
				return (ScriptCoin)this;
			return new ScriptCoin(this, redeemScript);
		}

		public ColoredCoin ToColoredCoin(AssetId asset, ulong quantity)
		{
			return ToColoredCoin(new Asset(asset, quantity));
		}
		public ColoredCoin ToColoredCoin(BitcoinAssetId asset, ulong quantity)
		{
			return ToColoredCoin(new Asset(asset, quantity));
		}
		public ColoredCoin ToColoredCoin(Asset asset)
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
				return TxOut.Value;
			}
			set
			{
				TxOut.Value = value;
			}
		}

		#endregion

		public Script ScriptPubKey
		{
			get
			{
				return TxOut.ScriptPubKey;
			}
		}
	}

	public interface IScriptCoin : ICoin
	{
		Script Redeem
		{
			get;
		}
	}

	public class ScriptCoin : Coin, IScriptCoin
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
		public ScriptCoin(Coin coin, Script redeem)
			: base(coin.Outpoint, coin.TxOut)
		{
			Redeem = redeem;
			AssertCoherent();
		}

		private void AssertCoherent()
		{
			if(Redeem == null)
				throw new ArgumentException("redeem cannot be null", "redeem");
			var destination = TxOut.ScriptPubKey.GetDestination() as ScriptId;
			if(destination == null)
				throw new ArgumentException("the provided scriptPubKey is not P2SH");
			if(destination.ScriptPubKey != Redeem.Hash.ScriptPubKey)
				throw new ArgumentException("The redeem provided does not match the scriptPubKey of the coin");
		}
		public ScriptCoin(IndexedTxOut txOut, Script redeem)
			: base(txOut)
		{
			Redeem = redeem;
			AssertCoherent();
		}

		public ScriptCoin(uint256 txHash, uint outputIndex, Money amount, Script redeem)
			: base(txHash, outputIndex, amount, redeem.Hash.ScriptPubKey)
		{
			Redeem = redeem;
			AssertCoherent();
		}

		public Script Redeem
		{
			get;
			set;
		}
	}

	public class StealthCoin : Coin, IScriptCoin
	{
		public StealthCoin()
		{
		}
		public StealthCoin(OutPoint outpoint, TxOut txOut, Script redeem, StealthMetadata stealthMetadata, BitcoinStealthAddress address)
			: base(outpoint, txOut)
		{
			StealthMetadata = stealthMetadata;
			Address = address;
			Redeem = redeem;
		}
		public StealthMetadata StealthMetadata
		{
			get;
			set;
		}

		public BitcoinStealthAddress Address
		{
			get;
			set;
		}

		public Script Redeem
		{
			get;
			set;
		}

		/// <summary>
		/// Scan the Transaction for StealthCoin given address and scan key
		/// </summary>
		/// <param name="tx">The transaction to scan</param>
		/// <param name="address">The stealth address</param>
		/// <param name="scan">The scan private key</param>
		/// <returns></returns>
		public static StealthCoin Find(Transaction tx, BitcoinStealthAddress address, Key scan)
		{
			var payment = address.GetPayments(tx, scan).FirstOrDefault();
			if(payment == null)
				return null;
			var txId = tx.GetHash();
			var txout = tx.Outputs.First(o => o.ScriptPubKey == payment.ScriptPubKey);
			return new StealthCoin(new OutPoint(txId, tx.Outputs.IndexOf(txout)), txout, payment.Redeem, payment.Metadata, address);
		}

		public StealthPayment GetPayment()
		{
			return new StealthPayment(TxOut.ScriptPubKey, Redeem, StealthMetadata);
		}

		public PubKey[] Uncover(PubKey[] spendPubKeys, Key scanKey)
		{
			var pubKeys = new PubKey[spendPubKeys.Length];
			for(int i = 0 ; i < pubKeys.Length ; i++)
			{
				pubKeys[i] = spendPubKeys[i].UncoverReceiver(scanKey, StealthMetadata.EphemKey);
			}
			return pubKeys;
		}

		public Key[] Uncover(Key[] spendKeys, Key scanKey)
		{
			var keys = new Key[spendKeys.Length];
			for(int i = 0 ; i < keys.Length ; i++)
			{
				keys[i] = spendKeys[i].Uncover(scanKey, StealthMetadata.EphemKey);
			}
			return keys;
		}
	}
}
