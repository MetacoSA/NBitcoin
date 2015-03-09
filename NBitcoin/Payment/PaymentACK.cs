#if !NOPROTOBUF
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Payment
{
	public class PaymentACK
	{
		public const int MaxLength = 60000;
		public static PaymentACK Load(byte[] data)
		{
			return Load(new MemoryStream(data));
		}

		public static PaymentACK Load(Stream source)
		{
			if(source.CanSeek && source.Length > MaxLength)
				throw new ArgumentException("PaymentACK messages larger than " + MaxLength + " bytes should be rejected", "source");
			var data = PaymentRequest.Serializer.Deserialize<Proto.PaymentACK>(source);
			return new PaymentACK(data);
		}
		public PaymentACK()
		{

		}
		public PaymentACK(PaymentMessage payment)
		{
			_Payment = payment;
		}
		internal PaymentACK(Proto.PaymentACK data)
		{
			_Payment = new PaymentMessage(data.payment);
			Memo = data.memoSpecified ? data.memo : null;
			OriginalData = data;
		}

		private readonly PaymentMessage _Payment = new PaymentMessage();
		public readonly static string MediaType = "application/bitcoin-paymentack";
		public PaymentMessage Payment
		{
			get
			{
				return _Payment;
			}
		}

		public string Memo
		{
			get;
			set;
		}

		internal Proto.PaymentACK OriginalData
		{
			get;
			set;
		}

		public byte[] ToBytes()
		{
			MemoryStream ms = new MemoryStream();
			WriteTo(ms);
			return ms.ToArray();
		}

		public void WriteTo(Stream output)
		{
			var data = OriginalData == null ? new Proto.PaymentACK() : (Proto.PaymentACK)PaymentRequest.Serializer.DeepClone(OriginalData);
			data.memo = Memo;
			data.payment = Payment.ToData();
			PaymentRequest.Serializer.Serialize(output, data);
		}
	}
	public class PaymentMessage
	{
		public const int MaxLength = 50000;
		public static PaymentMessage Load(byte[] data)
		{
			return Load(new MemoryStream(data));
		}

		public PaymentACK CreateACK(string memo = null)
		{
			return new PaymentACK(this)
			{
				Memo = memo
			};
		}

		public static PaymentMessage Load(Stream source)
		{
			if(source.CanSeek && source.Length > MaxLength)
				throw new ArgumentException("Payment messages larger than " + MaxLength + " bytes should be rejected by the merchant's server", "source");
			var data = PaymentRequest.Serializer.Deserialize<Proto.Payment>(source);
			return new PaymentMessage(data);
		}

		public string Memo
		{
			get;
			set;
		}
		public byte[] MerchantData
		{
			get;
			set;
		}

		private readonly List<PaymentOutput> _RefundTo = new List<PaymentOutput>();
		public List<PaymentOutput> RefundTo
		{
			get
			{
				return _RefundTo;
			}
		}

		private readonly List<Transaction> _Transactions = new List<Transaction>();
		public readonly static string MediaType = "application/bitcoin-payment";
		public PaymentMessage()
		{

		}
		internal PaymentMessage(Proto.Payment data)
		{
			Memo = data.memoSpecified ? data.memo : null;
			MerchantData = data.merchant_data;
			foreach(var tx in data.transactions)
			{
				Transactions.Add(new Transaction(tx));
			}
			foreach(var refund in data.refund_to)
			{
				RefundTo.Add(new PaymentOutput(refund));
			}
			OriginalData = data;
		}

		public PaymentMessage(PaymentRequest request)
		{
			this.MerchantData = request.Details.MerchantData;
		}
		public List<Transaction> Transactions
		{
			get
			{
				return _Transactions;
			}
		}

		public Uri ImplicitPaymentUrl
		{
			get;
			set;
		}

		internal Proto.Payment OriginalData
		{
			get;
			set;
		}

		public byte[] ToBytes()
		{
			MemoryStream ms = new MemoryStream();
			WriteTo(ms);
			return ms.ToArray();
		}

		public void WriteTo(Stream output)
		{
			PaymentRequest.Serializer.Serialize(output, this.ToData());
		}

		internal Proto.Payment ToData()
		{
			var data = OriginalData == null ? new Proto.Payment() : (Proto.Payment)PaymentRequest.Serializer.DeepClone(OriginalData);
			data.memo = Memo;
			data.merchant_data = MerchantData;

			foreach(var refund in RefundTo)
			{
				data.refund_to.Add(refund.ToData());
			}
			foreach(var transaction in Transactions)
			{
				data.transactions.Add(transaction.ToBytes());
			}

			return data;
		}

		/// <summary>
		/// Send the payment to given address
		/// </summary>
		/// <param name="paymentUrl">ImplicitPaymentUrl if null</param>
		/// <returns>The PaymentACK</returns>
		public PaymentACK SubmitPayment(Uri paymentUrl = null)
		{
			if(paymentUrl == null)
				paymentUrl = ImplicitPaymentUrl;
			if(paymentUrl == null)
				throw new ArgumentNullException("paymentUrl");
			try
			{
				return SubmitPaymentAsync(paymentUrl, null).Result;
			}
			catch(AggregateException ex)
			{
				throw ex.InnerException;
			}
		}

		public async Task<PaymentACK> SubmitPaymentAsync(Uri paymentUrl, HttpClient httpClient)
		{
			bool own = false;
			if(paymentUrl == null)
				paymentUrl = ImplicitPaymentUrl;
			if(paymentUrl == null)
				throw new ArgumentNullException("paymentUrl");
			if(httpClient == null)
			{
				httpClient = new HttpClient();
				own = true;
			}

			try
			{
				var request = new HttpRequestMessage(HttpMethod.Post, paymentUrl.OriginalString);
				request.Headers.Clear();
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(PaymentACK.MediaType));
				request.Content = new ByteArrayContent(this.ToBytes());
				request.Content.Headers.ContentType = new MediaTypeHeaderValue(PaymentMessage.MediaType);

				var result = await httpClient.SendAsync(request).ConfigureAwait(false);
				if(!result.IsSuccessStatusCode)
					throw new WebException(result.StatusCode + "(" + (int)result.StatusCode + ")");

				if(result.Content.Headers.ContentType == null || !result.Content.Headers.ContentType.MediaType.Equals(PaymentACK.MediaType, StringComparison.InvariantCultureIgnoreCase))
				{
					throw new WebException("Invalid contenttype received, expecting " + PaymentACK.MediaType + ", but got " + result.Content.Headers.ContentType);
				}
				var response = await result.Content.ReadAsStreamAsync().ConfigureAwait(false);
				return PaymentACK.Load(response);
			}
			finally
			{
				if(own)
					httpClient.Dispose();
			}
		}
	}
}
#endif