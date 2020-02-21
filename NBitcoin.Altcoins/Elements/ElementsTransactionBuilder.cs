#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace NBitcoin.Altcoins.Elements
{
	public class ElementsCoinSelector<TNetwork> : DefaultCoinSelector
	{
		public override IEnumerable<ICoin>? Select(IEnumerable<ICoin> coins, IMoney target)
		{
			var assetId = ElementsParams<TNetwork>.PeggedAssetId;
			if (target is AssetMoney assetMoney)
			{
				assetId = assetMoney.AssetId;
			}

			var eligibleCoins = coins.Where(coin =>
				(coin is AssetCoin assetCoin && assetCoin.Money.AssetId == assetId) ||
				(coin.TxOut is ElementsTxOut<TNetwork> elementsTxOut && elementsTxOut?.Asset?.AssetId == assetId));
			return base.Select(eligibleCoins, target);
		}
	}

	public class ElementsTransactionBuilder<TNetwork> : ElementsTransactionBuilder
	{
		internal ElementsTransactionBuilder(Network network) : base(network)
		{
		}

		protected override uint256 PeggedAssetId { get; } = ElementsParams<TNetwork>.PeggedAssetId;
		protected override ConfidentialAsset CreateConfidentialAsset(uint256 assetId)
		{
			return new ConfidentialAsset<TNetwork>(assetId);
		}
	}
	public abstract class ElementsTransactionBuilder : TransactionBuilder
	{
		internal ElementsTransactionBuilder(Network network): base(network)
		{

		}

		protected abstract uint256 PeggedAssetId { get; }
		protected abstract ConfidentialAsset CreateConfidentialAsset(uint256 assetId);

		public override TransactionBuilder Send(Script scriptPubKey, IMoney amount)
		{
			if (amount is AssetMoney assetMoney)
			{
				return SendAsset(scriptPubKey, assetMoney);
			}
			return base.Send(scriptPubKey, amount);
		}

		public TransactionBuilder SendAsset(IDestination destination, AssetMoney asset)
		{
			return SendAsset(destination.ScriptPubKey, asset);
		}

		public TransactionBuilder SendAsset(Script scriptPubKey, AssetMoney asset)
		{

			if (asset.Quantity < Money.Zero)
				throw new ArgumentOutOfRangeException(nameof(asset), "asset amount can't be negative");
			_LastSendBuilder = null; //If the amount is dust, we don't want the fee to be paid by the previous Send
			if (DustPrevention &&  asset.Quantity < GetDust(scriptPubKey) && !_OpReturnTemplate.CheckScriptPubKey(scriptPubKey))
			{
				if (asset.AssetId == PeggedAssetId)
				{
					SendFees(asset.Quantity);
					return this;
				}
				throw new ArgumentOutOfRangeException(nameof(asset), "asset amount is below the dust limit and is not the pegged network asset");
			}

			var builder = new SendBuilder(CreateTxOut(asset, scriptPubKey));
			CurrentGroup.Builders.Add(builder.Build);
			_LastSendBuilder = builder;
			return this;
		}

		protected override TxOut CreateTxOut(IMoney? amount = null, Script? script = null)
		{
			var txout = base.CreateTxOut(amount, script) as ElementsTxOut;
			if (txout != null && amount is AssetMoney assetMoney)
			{
				txout.Asset = CreateConfidentialAsset(assetMoney.AssetId);
			}

			return txout;
		}

		protected override IEnumerable<ICoin> GetSupportedCoins(IEnumerable<ICoin> coins)
		{
			return coins.Where(coin => coin is Coin || coin is AssetCoin);
		}


		protected override Money GetValue(IMoney money)
		{
			if (money is AssetMoney assetMoney)
			{
				return assetMoney.Quantity;
			}
			return base.GetValue(money);
		}

		/// <summary>
		/// Send assets (Open Asset) to a destination
		/// </summary>
		/// <param name="destination">The destination</param>
		/// <param name="assetId">The asset and amount</param>
		/// <returns></returns>
		public TransactionBuilder SendAsset(IDestination destination, uint256 assetId, ulong quantity)
		{
			return SendAsset(destination, new AssetMoney(assetId, quantity));
		}

		protected override void AfterBuild(Transaction transaction)
		{
			if (transaction.Outputs.OfType<ElementsTxOut>().All(o => !o.IsFee))
			{
				var totalInput =
					this.FindSpentCoins(transaction)
						.Select(c => c.TxOut)
						.OfType<ElementsTxOut>()
						.Where(o => o.IsPeggedAsset == true)
						.Select(c => c.Value)
						.OfType<Money>()
						.Sum();
				var totalOutput =
					transaction.Outputs.OfType<ElementsTxOut>()
						.Where(o => o.IsPeggedAsset == true)
						.Select(o => o.Value)
						.Sum();
				var fee = totalInput - totalOutput;
				if(fee > Money.Zero)
					transaction.Outputs.Add(fee, Script.Empty);
			}
		}
	}
}
