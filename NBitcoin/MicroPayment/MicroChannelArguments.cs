using NBitcoin.JsonConverters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.MicroPayment
{

	public class MicroChannelParticipant
	{
		[JsonConverter(typeof(BitcoinSerializableJsonConverter))]
		public PubKey PaymentPubKey
		{
			get;
			set;
		}
		[JsonConverter(typeof(ScriptJsonConverter))]
		public Script ScriptPubkey
		{
			get;
			set;
		}
	}
	public class MicroChannelArguments
	{
		public MicroChannelArguments Parse(string json)
		{
			JsonSerializerSettings settings = new JsonSerializerSettings();
			settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
			return JsonConvert.DeserializeObject<MicroChannelArguments>(json, settings);
		}
		public MicroChannelArguments()
		{
			Fees = Money.Coins(0.0001m);
		}
		[JsonConverter(typeof(MoneyJsonConverter))]
		public Money Amount
		{
			get;
			set;
		}
		[JsonConverter(typeof(MoneyJsonConverter))]
		public Money Fees
		{
			get;
			set;
		}
		[JsonConverter(typeof(BitcoinSerializableJsonConverter))]
		public LockTime Expiration
		{
			get;
			set;
		}

		public MicroChannelParticipant Payer
		{
			get;
			set;
		}
		public MicroChannelParticipant Payee
		{
			get;
			set;
		}
		[JsonConverter(typeof(ScriptJsonConverter))]
		public Script Redeem
		{
			get;
			set;
		}

		public override string ToString()
		{
			JsonSerializerSettings settings = new JsonSerializerSettings();
			settings.Formatting = Formatting.Indented;
			settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
			return JsonConvert.SerializeObject(this, settings);
		}

		public Transaction CreatePayment(Money paid, ScriptCoin fundingCoin)
		{
			if(paid > GetFundAmount())
				throw new MicroPaymentException("Payment reached the maximum");
			var builder = new TransactionBuilder();
			if(fundingCoin.Redeem != Redeem || fundingCoin.Amount != Amount + Fees)
				throw new MicroPaymentException("Invalid funding coin");

			var fees = Money.Min(paid, Fees);
			var toPayer = GetFundAmount() - paid;
			var toPayee = paid - fees;
			return builder
				.AddCoins(fundingCoin)
				.Send(Payee.ScriptPubkey, toPayee)
				.Send(Payer.ScriptPubkey, toPayer)
				.SendFees(fees)
				.Shuffle()
				.BuildTransaction(false);
		}

		public MicroChannelArguments MakeRedeem()
		{
			if(Payer == null || Payee == null || Payer.PaymentPubKey == null || Payee.PaymentPubKey == null)
				throw new InvalidOperationException("Payer or Payee should ne set before calling MakeRedeem");
			var pubkeys = new[] { Payer.PaymentPubKey, Payee.PaymentPubKey };
			Random rnd = new Random();
			pubkeys = pubkeys.OrderBy(x => rnd.Next()).ToArray();
			Redeem = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(2, pubkeys);
			return this;
		}

		public bool Verify()
		{
			if(Payee == null ||
				Payee.PaymentPubKey == null ||
				Payee.ScriptPubkey == null ||
				Payer == null ||
				Payer.PaymentPubKey == null ||
				Payer.ScriptPubkey == null ||
				Fees == null ||
				Amount == null ||
				Expiration == default(LockTime) ||
				Redeem == null)
				return false;

			var pubkeys = Redeem.GetDestinationPublicKeys();
			if(!pubkeys.Contains(Payer.PaymentPubKey) ||
				!pubkeys.Contains(Payee.PaymentPubKey))
				return false;
			if(Payee.ScriptPubkey == Payer.ScriptPubkey)
				return false;
			if(Fees < Money.Zero)
				return false;
			if(Amount < Money.Zero)
				return false;
			return true;
		}

		internal Money GetPaid(Transaction transaction)
		{
			if(transaction == null)
				return Money.Zero;
			var fees = GetFundAmount() - transaction.TotalOut;
			return transaction.Outputs
						  .Where(o => o.ScriptPubKey == Payee.ScriptPubkey)
						  .Select(o => o.Value).Sum() + fees;
		}

		public void Assert(Transaction payment, bool isRefund, Money paid, ScriptCoin fundCoin = null)
		{
			if(payment.Inputs.Count != 1)
				throw new MicroPaymentException("The payment should have one input");

			var actualFees = GetFundAmount() - payment.TotalOut;
			var expectedFees = Money.Min(paid, Fees);
			if(actualFees < Money.Zero || !actualFees.Almost(expectedFees,0.1m))
				throw new MicroPaymentException("Unexpected fees in the payment");

			if(!GetPaid(payment).Almost(paid))
				throw new MicroPaymentException("Unexpected amount in the payment");

			if(paid > GetFundAmount())
				throw new MicroPaymentException("Payment reached the maximum");

			if(fundCoin != null)
			{
				if(payment.Inputs[0].PrevOut != fundCoin.Outpoint)
					throw new MicroPaymentException("The input reference is incorrect");
				if(fundCoin.Amount != Fees + Amount)
					throw new MicroPaymentException("The fund coin is incorrect");
			}
			if(isRefund)
			{
				if(payment.Outputs.Count != 1)
					throw new MicroPaymentException("The refund should have one output");

				if(payment.Outputs[0].Value != Amount)
					throw new MicroPaymentException("Unexpected amount in the output of the refund transaction");

				if(payment.Outputs[0].ScriptPubKey != Payer.ScriptPubkey)
					throw new MicroPaymentException("The refund address in the refund transaction is not equal to the expected one");

				if(payment.LockTime != Expiration)
					throw new MicroPaymentException("The refund transaction has invalid locktime");
			}
			else
			{
				if(payment.LockTime != default(LockTime))
					throw new MicroPaymentException("The payment transaction has invalid locktime");
				if(payment.Outputs.Count != 1 && payment.Outputs.Count != 2)
					throw new MicroPaymentException("The payment should have one or two outputs");
			}
		}

		public Transaction SignPayment(Transaction payment, Key key, ScriptCoin fundingCoin)
		{
			return new TransactionBuilder()
			   .AddKeys(key)
			   .AddCoins(fundingCoin)
			   .SignTransaction(payment);
		}


		public TransactionSignature ExtractSignature(MicroChannelParticipant participant, Transaction payment)
		{
			var args = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(payment.Inputs[0].ScriptSig);
			if(args == null || args.RedeemScript != Redeem)
				throw new MicroPaymentException("Payment uncorrectly signed");
			var multiArgs = PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters(Redeem);
			if(multiArgs == null || !multiArgs.PubKeys.Contains(participant.PaymentPubKey))
				throw new MicroPaymentException("Payment uncorrectly signed");
			var i = Array.IndexOf(multiArgs.PubKeys, participant.PaymentPubKey);
			var sig = args.Signatures[i];
			if(sig == null)
				throw new MicroPaymentException("Payment uncorrectly signed");
			if(args.Signatures.Where(s => s != null).Count() != 1)
				throw new MicroPaymentException("Payment uncorrectly signed");
			return sig;
		}

		public Money GetFundAmount()
		{
			return Amount + Fees;
		}
	}


	public class MicroChannelState
	{
		public bool PayerState
		{
			get;
			set;
		}
		[JsonConverter(typeof(BitcoinSerializableJsonConverter))]
		public Transaction Refund
		{
			get;
			set;
		}
		[JsonConverter(typeof(BitcoinSerializableJsonConverter))]
		public Transaction Fund
		{
			get;
			set;
		}
		public int Sequence
		{
			get;
			set;
		}
		[JsonConverter(typeof(MoneyJsonConverter))]
		public Money Paid
		{
			get;
			set;
		}
		[JsonConverter(typeof(BitcoinSerializableJsonConverter))]
		public Transaction Payment
		{
			get;
			set;
		}

		public MicroChannelArguments Arguments
		{
			get;
			set;
		}

	
		public MicroChannelState Parse(string json)
		{
			JsonSerializerSettings settings = new JsonSerializerSettings();
			settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
			return JsonConvert.DeserializeObject<MicroChannelState>(json, settings);
		}
		public override string ToString()
		{
			JsonSerializerSettings settings = new JsonSerializerSettings();
			settings.Formatting = Formatting.Indented;
			settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
			return JsonConvert.SerializeObject(this, settings);
		}

		public MicroEndpoint ToEndpoint()
		{
			if(!PayerState)
				return new PayeeEndpoint(this);
			else
				return new PayerEndpoint(this);
		}
	}
}
